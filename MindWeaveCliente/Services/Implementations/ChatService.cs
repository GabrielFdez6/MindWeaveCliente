using MindWeaveClient.ChatManagerService;
using MindWeaveClient.Properties.Langs;
using MindWeaveClient.Services.Abstractions;
using MindWeaveClient.Services.Callbacks;
using System;
using System.Net.Sockets;
using System.ServiceModel;
using System.Threading.Tasks;

namespace MindWeaveClient.Services.Implementations
{
    public class ChatService : IChatService
    {
        private ChatManagerClient proxy;
        private ChatCallbackHandler callbackHandler;

        private string connectedUsername;
        private string connectedLobbyId;

        public event Action<ChatMessageDto> OnMessageReceived;
        public event Action<string> OnSystemMessageReceived;

        public bool isConnected()
        {
            return proxy != null && proxy.State == CommunicationState.Opened;
        }

        public async Task connectAsync(string username, string lobbyId)
        {
            if (isConnected())
            {
                if (connectedUsername == username && connectedLobbyId == lobbyId)
                {
                    return;
                }

                await disconnectAsync();
            }

            if (proxy != null)
            {
                abortProxySafe();
            }

            try
            {
                callbackHandler = new ChatCallbackHandler();
                var instanceContext = new InstanceContext(callbackHandler);
                proxy = new ChatManagerClient(instanceContext);

                callbackHandler.OnMessageReceivedEvent += handleMessageReceived;
                callbackHandler.OnSystemMessageReceivedEvent += handleSystemMessageCallback;

                await Task.Run(() => proxy.joinLobbyChat(username, lobbyId));

                connectedUsername = username;
                connectedLobbyId = lobbyId;
            }
            catch (EndpointNotFoundException)
            {
                cleanupOnError();
                throw;
            }
            catch (CommunicationObjectFaultedException)
            {
                cleanupOnError();
                throw;
            }
            catch (CommunicationException)
            {
                cleanupOnError();
                throw;
            }
            catch (TimeoutException)
            {
                cleanupOnError();
                throw;
            }
            catch (SocketException)
            {
                cleanupOnError();
                throw;
            }
        }

        public async Task disconnectAsync(bool forceAbort = false)
        {
            if (forceAbort)
            {
                await disconnectAsync(string.Empty, string.Empty, true);
            }
            else if (!string.IsNullOrEmpty(connectedUsername) && !string.IsNullOrEmpty(connectedLobbyId))
            {
                await disconnectAsync(connectedUsername, connectedLobbyId);
            }
        }

        public async Task disconnectAsync(string username, string lobbyId, bool forceAbort = false)
        {
            try
            {
                if (forceAbort)
                {
                    abortProxySafe();
                }
                else
                {
                    if (!isConnected())
                    {
                        return;
                    }

                    if (username != connectedUsername || lobbyId != connectedLobbyId)
                    {
                        return;
                    }

                    await Task.Run(() => proxy.leaveLobbyChat(username, lobbyId));
                    closeProxySafe();
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
            catch (Exception)
            {
                abortProxySafe();
            }
            finally
            {
                cleanup();
            }
        }

        public async Task sendLobbyMessageAsync(string username, string lobbyId, string message)
        {
            validateProxyState();

            try
            {
                await Task.Run(() => proxy.sendLobbyMessage(username, lobbyId, message));
            }
            catch (EndpointNotFoundException)
            {
                cleanupOnError();
                throw;
            }
            catch (CommunicationObjectFaultedException)
            {
                cleanupOnError();
                throw;
            }
            catch (CommunicationException)
            {
                cleanupOnError();
                throw;
            }
            catch (TimeoutException)
            {
                cleanupOnError();
                throw;
            }
            catch (SocketException)
            {
                cleanupOnError();
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
                cleanup();
                throw new CommunicationObjectFaultedException(Lang.ErrorMsgServerOffline);
            }

            if (proxy.State == CommunicationState.Closed || proxy.State == CommunicationState.Closing)
            {
                cleanup();
                throw new CommunicationObjectFaultedException(Lang.ErrorMsgServerOffline);
            }
        }

        private void handleMessageReceived(ChatMessageDto message)
        {
            OnMessageReceived?.Invoke(message);
        }

        private void handleSystemMessageCallback(string message)
        {
            OnSystemMessageReceived?.Invoke(message);
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
                 *Ignore: The goal is to try to release the resource.
                 * If the proxy is already in a faulted or disconnected state,
                 * Abort() might throw an exception that adds no value
                 * and could interrupt the application's shutdown flow.
                 */
            }
        }

        private void cleanup()
        {
            if (callbackHandler != null)
            {
                callbackHandler.OnMessageReceivedEvent -= handleMessageReceived;
                callbackHandler.OnSystemMessageReceivedEvent -= handleSystemMessageCallback;
            }

            proxy = null;
            callbackHandler = null;
            connectedUsername = null;
            connectedLobbyId = null;
        }

        private void cleanupOnError()
        {
            abortProxySafe();
            cleanup();
        }
    }
}