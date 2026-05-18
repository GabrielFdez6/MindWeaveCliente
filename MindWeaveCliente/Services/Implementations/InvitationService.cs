using MindWeaveClient.Properties.Langs;
using MindWeaveClient.Services.Abstractions;
using MindWeaveClient.Utilities.Abstractions;
using MindWeaveClient.Utilities.Implementations;
using MindWeaveClient.View.Game;
using System;
using System.Linq;
using System.Net.Sockets;
using System.ServiceModel;
using System.Threading.Tasks;
using System.Windows;

namespace MindWeaveClient.Services.Implementations
{
    public class InvitationService : IInvitationService
    {
        private readonly IDialogService dialogService;
        private readonly IMatchmakingService matchmakingService;
        private readonly IWindowNavigationService windowNavigationService;
        private readonly ICurrentLobbyService currentLobbyService;
        private readonly ISocialService socialService;
        private readonly IServiceExceptionHandler exceptionHandler;

        private bool isProcessingInvite;

        public InvitationService(
            IDialogService dialogService,
            IMatchmakingService matchmakingService,
            IWindowNavigationService windowNavigationService,
            ICurrentLobbyService currentLobbyService,
            ISocialService socialService,
            IServiceExceptionHandler exceptionHandler)
        {
            this.dialogService = dialogService;
            this.matchmakingService = matchmakingService;
            this.windowNavigationService = windowNavigationService;
            this.currentLobbyService = currentLobbyService;
            this.socialService = socialService;
            this.exceptionHandler = exceptionHandler;
        }

        public void subscribeToGlobalInvites()
        {
            socialService.LobbyInviteReceived += onLobbyInviteReceived;
        }

        public void unsubscribeFromGlobalInvites()
        {
            socialService.LobbyInviteReceived -= onLobbyInviteReceived;
        }

        private async void onLobbyInviteReceived(string fromUsername, string lobbyId)
        {
            if (isProcessingInvite)
            {
                return;
            }

            if (fromUsername.Equals(SessionService.Username, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var gameWindow = Application.Current.Windows.OfType<GameWindow>().FirstOrDefault();
            if (gameWindow != null)
            {
                return;
            }

            isProcessingInvite = true;

            try
            {
                string message = string.Format(
                    Lang.InviteReceivedBody,
                    fromUsername,
                    lobbyId);

                bool result = dialogService.showConfirmation(message, Lang.InviteReceivedTitle);

                if (result)
                {
                    await joinLobbyFromInvite(lobbyId);
                }
            }
            finally
            {
                isProcessingInvite = false;
            }
        }

        private async Task joinLobbyFromInvite(string lobbyId)
        {
            try
            {
                if (string.IsNullOrEmpty(SessionService.Username))
                {
                    dialogService.showError(Lang.ErrorTitle, Lang.ErrorSessionExpired);
                    return;
                }

                var joinResult = await matchmakingService.joinLobbyWithConfirmationAsync(SessionService.Username, lobbyId);

                if (joinResult.Success)
                {
                    currentLobbyService.setInitialState(joinResult.InitialLobbyState);

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        windowNavigationService.openWindow<GameWindow>();
                        windowNavigationService.closeWindow<View.Main.MainWindow>();
                    });
                }
                else
                {
                    string errorMsg = MessageCodeInterpreter.translate(joinResult.MessageCode, joinResult.MessageCode); 
                    dialogService.showError(Lang.ErrorTitle, errorMsg);
                }
            }
            catch (EndpointNotFoundException ex)
            {
                exceptionHandler.handleExceptionAsync(ex, Lang.JoinLobbyOperation);
                disconnectMatchmakingSafe();
            }
            catch (CommunicationObjectFaultedException ex)
            {
                exceptionHandler.handleExceptionAsync(ex, Lang.JoinLobbyOperation);
                disconnectMatchmakingSafe();
            }
            catch (CommunicationException ex)
            {
                exceptionHandler.handleExceptionAsync(ex, Lang.JoinLobbyOperation);
                disconnectMatchmakingSafe();
            }
            catch (TimeoutException ex)
            {
                exceptionHandler.handleExceptionAsync(ex, Lang.JoinLobbyOperation);
                disconnectMatchmakingSafe();
            }
            catch (SocketException ex)
            {
                exceptionHandler.handleExceptionAsync(ex, Lang.JoinLobbyOperation);
                disconnectMatchmakingSafe();
            }

        }

        private void disconnectMatchmakingSafe()
        {
            try
            {
                matchmakingService.disconnect();
            }
            catch (Exception)
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

    }
}