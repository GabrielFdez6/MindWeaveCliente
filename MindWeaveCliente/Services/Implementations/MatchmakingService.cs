using MindWeaveClient.MatchmakingService;
using MindWeaveClient.Properties.Langs;
using MindWeaveClient.Services.Abstractions;
using MindWeaveClient.Services.Callbacks;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace MindWeaveClient.Services.Implementations
{
    public class MatchmakingService : IMatchmakingService
    {
        private const int JOIN_LOBBY_TIMEOUT_MS = 15000;

        private MatchmakingManagerClient proxy;
        private readonly IMatchmakingManagerCallback callbackHandler;

        private TaskCompletionSource<JoinLobbyResultDto> pendingJoinRequest;
        private readonly object joinLock = new object();

        public event Action<LobbyStateDto> OnLobbyStateUpdated;
        public event Action<MatchFoundDto> OnMatchFound;
        public event Action<string> OnLobbyCreationFailed;
        public event Action<string> OnKicked;
        public event Action<string> OnLobbyActionFailed;
        public event Action<PuzzleDefinitionDto, int> OnGameStarted;
        public event Action<string> OnLobbyDestroyed;

        public MatchmakingService(IMatchmakingManagerCallback callbackHandler)
        {
            this.callbackHandler = callbackHandler;
        }

        public async Task<LobbyCreationResultDto> createLobbyAsync(string hostUsername, LobbySettingsDto settings)
        {
            ensureClientIsCreated();
            return await executeServiceCallAsync(async () => await proxy.createLobbyAsync(hostUsername, settings));
        }

        public async Task<GuestJoinServiceResultDto> joinLobbyAsGuestAsync(GuestJoinRequestDto request)
        {
            ensureClientIsCreated();
            GuestJoinResultDto wcfResult = await executeServiceCallAsync(
                async () => await proxy.joinLobbyAsGuestAsync(request));

            if (wcfResult.Success && wcfResult.InitialLobbyState != null)
            {
                SessionService.setSession(wcfResult.PlayerId, wcfResult.AssignedGuestUsername, null, true);
            }
            return new GuestJoinServiceResultDto(wcfResult);
        }

        public async Task<JoinLobbyResultDto> joinLobbyWithConfirmationAsync(string username, string lobbyCode)
        {
            ensureClientIsCreated();

            TaskCompletionSource<JoinLobbyResultDto> tcs;

            lock (joinLock)
            {
                pendingJoinRequest?.TrySetCanceled();
                tcs = new TaskCompletionSource<JoinLobbyResultDto>();
                pendingJoinRequest = tcs;
            }

            using (var cts = new CancellationTokenSource(JOIN_LOBBY_TIMEOUT_MS))
            {
                cts.Token.Register(() =>
                {
                    lock (joinLock)
                    {
                        if (pendingJoinRequest == tcs)
                        {
                            tcs.TrySetResult(new JoinLobbyResultDto
                            {
                                Success = false,
                                MessageCode = "ERROR_TIMEOUT"
                            });
                            pendingJoinRequest = null;
                        }
                    }
                });

                try
                {
                    await executeOneWayCallAsync(() => proxy.joinLobby(username, lobbyCode));
                    return await tcs.Task;
                }
                catch (Exception ex)
                {
                    lock (joinLock)
                    {
                        if (pendingJoinRequest == tcs)
                        {
                            pendingJoinRequest = null;
                        }
                    }

                    return new JoinLobbyResultDto
                    {
                        Success = false,
                        MessageCode = mapExceptionToMessageCode(ex)
                    };
                }
            }
        }

        public async Task leaveLobbyAsync(string username, string lobbyId)
        {
            await executeOneWayCallAsync(() => proxy.leaveLobby(username, lobbyId));
            disconnect();
        }

        public async Task startGameAsync(string hostUsername, string lobbyId)
        {
            await executeOneWayCallAsync(() => proxy.startGame(hostUsername, lobbyId));
        }

        public async Task kickPlayerAsync(string hostUsername, string playerToKick, string lobbyId)
        {
            await executeOneWayCallAsync(() => proxy.kickPlayer(hostUsername, playerToKick, lobbyId));
        }

        public async Task inviteToLobbyAsync(string inviter, string invited, string lobbyId)
        {
            await executeOneWayCallAsync(() => proxy.inviteToLobby(inviter, invited, lobbyId));
        }

        public async Task inviteGuestByEmailAsync(GuestInvitationDto invitationData)
        {
            await executeOneWayCallAsync(() => proxy.inviteGuestByEmail(invitationData));
        }

        public async Task requestPieceMoveAsync(string lobbyCode, PieceMovementDto movement)
        {
            ensureClientIsCreated();
            await executeTaskCallAsync(async () => await proxy.requestPieceMoveAsync(lobbyCode, movement.PieceId, movement.X, movement.Y));
        }

        public async Task requestPieceDragAsync(string lobbyCode, int pieceId)
        {
            ensureClientIsCreated();
            await executeTaskCallAsync(async () => await proxy.requestPieceDragAsync(lobbyCode, pieceId));
        }

        public async Task requestPieceDropAsync(string lobbyCode, PieceMovementDto movement)
        {
            ensureClientIsCreated();
            await executeTaskCallAsync(async () => await proxy.requestPieceDropAsync(lobbyCode, movement.PieceId, movement.X, movement.Y));
        }

        public async Task leaveGameAsync(string username, string lobbyCode)
        {
            ensureClientIsCreated();
            await executeTaskCallAsync(async () => await proxy.leaveGameAsync(username, lobbyCode));
        }

        public void disconnect(bool forceAbort = false)
        {
            lock (joinLock)
            {
                pendingJoinRequest?.TrySetCanceled();
                pendingJoinRequest = null;
            }

            unsubscribeFromCallbackEvents();

            if (proxy == null) return;

            try
            {
                if (forceAbort)
                {
                    if (proxy.State != CommunicationState.Closed)
                    {
                        abortProxySafe();
                    }
                }
                else
                {
                    if (proxy.State == CommunicationState.Opened)
                    {
                        proxy.Close();
                    }
                    else
                    {
                        abortProxySafe();
                    }
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
            finally
            {
                proxy = null;
            }
        }

        private void ensureClientIsCreated()
        {
            if (proxy != null && proxy.State != CommunicationState.Closed)
            {
                return;
            }
            if (proxy != null)
            {
                abortProxySafe();
            }

            var instanceContext = new InstanceContext(callbackHandler);
            proxy = new MatchmakingManagerClient(instanceContext);

            subscribeToCallbackEvents();
        }

        private void subscribeToCallbackEvents()
        {
            if (!(callbackHandler is MatchmakingCallbackHandler handler))
            {
                return;
            }

            handler.OnLobbyStateUpdatedEvent += handleLobbyStateUpdated;
            handler.OnMatchFoundEvent += handleMatchFound;
            handler.OnLobbyCreationFailedEvent += handleLobbyCreationFailed;
            handler.OnLobbyActionFailedEvent += handleLobbyActionFailed;
            handler.OnKickedEvent += handleKicked;
            handler.OnLobbyDestroyedEvent += handleLobbyDestroyedCallback;
            handler.OnGameStartedNavigation += handleGameStartedCallback;
        }

        private void unsubscribeFromCallbackEvents()
        {
            if (callbackHandler is MatchmakingCallbackHandler handler)
            {
                handler.OnLobbyStateUpdatedEvent -= handleLobbyStateUpdated;
                handler.OnMatchFoundEvent -= handleMatchFound;
                handler.OnLobbyCreationFailedEvent -= handleLobbyCreationFailed;
                handler.OnKickedEvent -= handleKicked;
                handler.OnGameStartedNavigation -= handleGameStartedCallback;
                handler.OnLobbyDestroyedEvent -= handleLobbyDestroyedCallback;
                handler.OnLobbyActionFailedEvent -= handleLobbyActionFailed;
            }
        }

        private async Task<T> executeServiceCallAsync<T>(Func<Task<T>> call)
        {
            validateProxyState();

            try
            {
                return await call.Invoke();
            }
            catch (EndpointNotFoundException)
            {
                abortProxySafe();
                proxy = null;
                throw;
            }
            catch (CommunicationObjectFaultedException)
            {
                abortProxySafe();
                proxy = null;
                throw;
            }
            catch (CommunicationException)
            {
                abortProxySafe();
                proxy = null;
                throw;
            }
            catch (TimeoutException)
            {
                abortProxySafe();
                proxy = null;
                throw;
            }
            catch (SocketException)
            {
                abortProxySafe();
                proxy = null;
                throw;
            }
        }

        private async Task executeOneWayCallAsync(Action call)
        {
            validateProxyState();

            try
            {
                await Task.Run(call);
            }
            catch (EndpointNotFoundException)
            {
                abortProxySafe();
                proxy = null;
                throw;
            }
            catch (CommunicationObjectFaultedException)
            {
                abortProxySafe();
                proxy = null;
                throw;
            }
            catch (CommunicationException)
            {
                abortProxySafe();
                proxy = null;
                throw;
            }
            catch (TimeoutException)
            {
                abortProxySafe();
                proxy = null;
                throw;
            }
            catch (SocketException)
            {
                abortProxySafe();
                proxy = null;
                throw;
            }
        }

        private async Task executeTaskCallAsync(Func<Task> call)
        {
            validateProxyState();

            try
            {
                await call.Invoke();
            }
            catch (EndpointNotFoundException)
            {
                abortProxySafe();
                proxy = null;
                throw;
            }
            catch (CommunicationObjectFaultedException)
            {
                abortProxySafe();
                proxy = null;
                throw;
            }
            catch (CommunicationException)
            {
                abortProxySafe();
                proxy = null;
                throw;
            }
            catch (TimeoutException)
            {
                abortProxySafe();
                proxy = null;
                throw;
            }
            catch (SocketException)
            {
                abortProxySafe();
                proxy = null;
                throw;
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
                 * Ignore: The goal is to try to release the resource.
                 * If the proxy is already in a faulted or disconnected state,
                 * Abort() might throw an exception that adds no value
                 * and could interrupt the application's shutdown flow.
               */
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
                throw new CommunicationObjectFaultedException(Lang.ErrorMsgServerOffline);
            }

            if (proxy.State == CommunicationState.Closed)
            {
                proxy = null;
                throw new CommunicationObjectFaultedException(Lang.ErrorMsgServerOffline);
            }
        }

        private static string mapExceptionToMessageCode(Exception ex)
        {
            switch (ex)
            {
                case EndpointNotFoundException _:
                    return "ERROR_SERVER_UNAVAILABLE";
                case CommunicationObjectFaultedException _:
                    return "ERROR_CONNECTION_LOST";
                case CommunicationException _:
                    return "ERROR_COMMUNICATION";
                case TimeoutException _:
                    return "ERROR_TIMEOUT";
                case SocketException _:
                    return "ERROR_NETWORK";
                default:
                    return "ERROR_UNKNOWN";
            }
        }

        private void handleLobbyStateUpdated(LobbyStateDto state)
        {
            lock (joinLock)
            {
                if (pendingJoinRequest != null)
                {
                    pendingJoinRequest.TrySetResult(new JoinLobbyResultDto
                    {
                        Success = true,
                        InitialLobbyState = state
                    });
                    pendingJoinRequest = null;
                }
            }

            OnLobbyStateUpdated?.Invoke(state);
        }

        private void handleMatchFound(string lobbyCode, List<string> players, LobbySettingsDto settings, string puzzleImagePath)
        {
            var matchData = new MatchFoundDto
            {
                LobbyCode = lobbyCode,
                Players = players,
                Settings = settings,
                PuzzleImagePath = puzzleImagePath
            };

            OnMatchFound?.Invoke(matchData);
        }

        private void handleLobbyCreationFailed(string reason)
        {
            lock (joinLock)
            {
                if (pendingJoinRequest != null)
                {
                    pendingJoinRequest.TrySetResult(new JoinLobbyResultDto
                    {
                        Success = false,
                        MessageCode = reason
                    });
                    pendingJoinRequest = null;
                    return;
                }
            }

            OnLobbyCreationFailed?.Invoke(reason);
        }

        private void handleKicked(string reason)
        {
            OnKicked?.Invoke(reason);
        }

        private void handleGameStartedCallback(PuzzleDefinitionDto puzzleDefinition, int matchDurationSeconds)
        {
            OnGameStarted?.Invoke(puzzleDefinition, matchDurationSeconds);
        }

        private void handleLobbyActionFailed(string message)
        {
            OnLobbyActionFailed?.Invoke(message);
        }

        private void handleLobbyDestroyedCallback(string reason)
        {
            OnLobbyDestroyed?.Invoke(reason);
        }
    }
}