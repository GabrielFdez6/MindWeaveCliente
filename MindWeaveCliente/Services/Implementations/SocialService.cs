using MindWeaveClient.Properties.Langs;
using MindWeaveClient.Services.Abstractions;
using MindWeaveClient.Services.Callbacks;
using MindWeaveClient.SocialManagerService;
using System;
using System.Net.Sockets;
using System.ServiceModel;
using System.Threading.Tasks;

namespace MindWeaveClient.Services.Implementations
{
    public class SocialService : ISocialService
    {
        private SocialManagerClient proxy;
        private readonly SocialCallbackHandler callbackHandler;
        private string currentUsername;
        private bool isDisposed;
        private readonly object lockObject = new object();

        public event Action<string, bool> FriendStatusChanged;
        public event Action<string> FriendRequestReceived;
        public event Action<string, bool> FriendResponseReceived;
        public event Action<string, string> LobbyInviteReceived;

        public SocialService(ISocialManagerCallback callbackHandler)
        {
            this.callbackHandler = callbackHandler as SocialCallbackHandler;

            if (this.callbackHandler == null)
            {
                return;
            }

            this.callbackHandler.FriendStatusChanged += onFriendStatusChanged;
            this.callbackHandler.FriendRequestReceived += onFriendRequestReceived;
            this.callbackHandler.FriendResponseReceived += onFriendResponseReceived;
            this.callbackHandler.LobbyInviteReceived += onLobbyInviteReceived;
        }

        public async Task connectAsync(string username)
        {
            lock (lockObject)
            {
                if (proxy != null && proxy.State == CommunicationState.Opened &&
                    currentUsername == username)
                {
                    return;
                }

                if (proxy != null)
                {
                    cleanupProxy();
                }
            }

            try
            {
                var instanceContext = new InstanceContext(callbackHandler);
                proxy = new SocialManagerClient(instanceContext);

                await proxy.connectAsync(username);
                currentUsername = username;
            }
            catch (EndpointNotFoundException)
            {
                cleanupProxy();
                throw;
            }
            catch (CommunicationObjectFaultedException)
            {
                cleanupProxy();
                throw;
            }
            catch (CommunicationException)
            {
                cleanupProxy();
                throw;
            }
            catch (TimeoutException)
            {
                cleanupProxy();
                throw;
            }
            catch (SocketException)
            {
                cleanupProxy();
                throw;
            }
        }

        public async Task disconnectAsync(string username, bool forceAbort = false)
        {
            try
            {
                if (forceAbort)
                {
                    abortProxySafe();
                }
                else
                {
                    if (proxy != null && proxy.State == CommunicationState.Opened)
                    {
                        await proxy.disconnectAsync(username);
                        closeProxySafe();
                    }
                    else
                    {
                        abortProxySafe();
                    }
                }
            }
            catch (EndpointNotFoundException)
            {
                abortProxySafe();
            }
            catch (CommunicationException)
            {
                abortProxySafe();
            }
            catch (TimeoutException)
            {
                abortProxySafe();
            }
            catch (SocketException)
            {
                abortProxySafe();
            }
            finally
            {
                cleanupProxy();
            }
        }

        public async Task<FriendDto[]> getFriendsListAsync(string username)
        {
            return await executeServiceCallAsync(async () => await proxy.getFriendsListAsync(username));
        }

        public async Task<FriendRequestInfoDto[]> getFriendRequestsAsync(string username)
        {
            return await executeServiceCallAsync(async () => await proxy.getFriendRequestsAsync(username));
        }

        public async Task<PlayerSearchResultDto[]> searchPlayersAsync(string username, string query)
        {
            return await executeServiceCallAsync(async () => await proxy.searchPlayersAsync(username, query));
        }

        public async Task<OperationResultDto> sendFriendRequestAsync(string username, string targetUsername)
        {
            return await executeServiceCallAsync(async () => await proxy.sendFriendRequestAsync(username, targetUsername));
        }

        public async Task<OperationResultDto> respondToFriendRequestAsync(string username, string requesterUsername, bool accept)
        {
            return await executeServiceCallAsync(async () => await proxy.respondToFriendRequestAsync(username, requesterUsername, accept));
        }

        public async Task<OperationResultDto> removeFriendAsync(string username, string friendUsername)
        {
            return await executeServiceCallAsync(async () => await proxy.removeFriendAsync(username, friendUsername));
        }

        private void onFriendStatusChanged(string friendUsername, bool isOnline)
        {
            FriendStatusChanged?.Invoke(friendUsername, isOnline);
        }

        private void onFriendRequestReceived(string fromUsername)
        {
            FriendRequestReceived?.Invoke(fromUsername);
        }

        private void onFriendResponseReceived(string fromUsername, bool accepted)
        {
            FriendResponseReceived?.Invoke(fromUsername, accepted);
        }

        private void onLobbyInviteReceived(string fromUsername, string lobbyCode)
        {
            LobbyInviteReceived?.Invoke(fromUsername, lobbyCode);
        }

        private async Task<T> executeServiceCallAsync<T>(Func<Task<T>> call)
        {
            validateProxyState();

            try
            {
                return await call();
            }
            catch (EndpointNotFoundException)
            {
                cleanupProxy();
                throw;
            }
            catch (CommunicationObjectFaultedException)
            {
                cleanupProxy();
                throw;
            }
            catch (CommunicationException)
            {
                cleanupProxy();
                throw;
            }
            catch (TimeoutException)
            {
                cleanupProxy();
                throw;
            }
            catch (SocketException)
            {
                cleanupProxy();
                throw;
            }
        }

        private void validateProxyState()
        {
            if (proxy == null)
            {
                throw new CommunicationObjectFaultedException(Lang.ErrorMsgServerOffline);
            }

            if (proxy.State == CommunicationState.Faulted)
            {
                abortProxySafe();
                proxy = null;
                currentUsername = null;
                throw new CommunicationObjectFaultedException(Lang.ErrorMsgServerOffline);
            }

            if (proxy.State == CommunicationState.Closed || proxy.State == CommunicationState.Closing)
            {
                proxy = null;
                currentUsername = null;
                throw new CommunicationObjectFaultedException(Lang.ErrorMsgServerOffline);
            }
        }

        private void closeProxySafe()
        {
            try
            {
                if (proxy != null && proxy.State == CommunicationState.Opened)
                {
                    proxy.Close();
                }
            }
            catch (CommunicationException)
            {
                abortProxySafe();
            }
            catch (TimeoutException)
            {
                abortProxySafe();
            }
        }

        private void abortProxySafe()
        {
            try
            {
                proxy?.Abort();
            }
            catch
            {
                /*
                 * Ignore: This is a fail-safe cleanup operation.
                 * We catch the generic Exception to suppress any error (like CommunicationException,
                 * ObjectDisposedException, or others) that might occur if the connection
                 * is already broken or the proxy is invalid.
                 * The goal is to ensure the application flow is never interrupted by a cleanup failure.
                 */
            }
        }

        private void cleanupProxy()
        {
            if (proxy != null)
            {
                try
                {
                    if (proxy.State != CommunicationState.Closed &&
                        proxy.State != CommunicationState.Closing)
                    {
                        proxy.Abort();
                    }
                }
                catch
                {
                    /*
                     * Ignore: This is a fail-safe cleanup operation.
                     * We catch the generic Exception to suppress any error (like CommunicationException,
                     * ObjectDisposedException, or others) that might occur if the connection
                     * is already broken or the proxy is invalid.
                     * The goal is to ensure the application flow is never interrupted by a cleanup failure.
                     */
                }
            }

            proxy = null;
            currentUsername = null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed) return;

            if (disposing)
            {
                if (callbackHandler != null)
                {
                    callbackHandler.FriendStatusChanged -= onFriendStatusChanged;
                    callbackHandler.FriendRequestReceived -= onFriendRequestReceived;
                    callbackHandler.FriendResponseReceived -= onFriendResponseReceived;
                    callbackHandler.LobbyInviteReceived -= onLobbyInviteReceived;
                }

                cleanupProxy();
            }

            isDisposed = true;
        }
    }
}