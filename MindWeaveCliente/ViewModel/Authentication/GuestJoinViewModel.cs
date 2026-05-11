using MindWeaveClient.MatchmakingService;
using MindWeaveClient.Properties.Langs;
using MindWeaveClient.Services;
using MindWeaveClient.Services.Abstractions;
using MindWeaveClient.Utilities.Abstractions;
using MindWeaveClient.Utilities.Implementations;
using MindWeaveClient.Validators;
using MindWeaveClient.View.Authentication;
using MindWeaveClient.View.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MindWeaveClient.ViewModel.Authentication
{
    public class GuestJoinViewModel : BaseViewModel
    {
        private const int MAX_LENGTH_LOBBY_CODE = 6;
        private const int MAX_LENGTH_EMAIL = 45;
        private const int MAX_LENGTH_USERNAME = 16;

        private string lobbyCode;
        private string guestEmail;
        private string desiredUsername;

        private readonly IMatchmakingService matchmakingService;
        private readonly IDialogService dialogService;
        private readonly GuestJoinValidator validator;
        private readonly INavigationService navigationService;
        private readonly IWindowNavigationService windowNavigationService;
        private readonly ICurrentLobbyService currentLobbyService;
        private readonly IServiceExceptionHandler exceptionHandler;

        public string LobbyCode
        {
            get => lobbyCode;
            set
            {
                string processedValue = clampString(value, MAX_LENGTH_LOBBY_CODE);

                if (lobbyCode != processedValue)
                {
                    lobbyCode = processedValue;
                    OnPropertyChanged();

                    if (!string.IsNullOrEmpty(processedValue))
                    {
                        markAsTouched(nameof(LobbyCode));
                    }
                    validate(validator, this);
                    OnPropertyChanged(nameof(LobbyCodeError));
                }
            }
        }

        public string GuestEmail
        {
            get => guestEmail;
            set
            {
                string processedValue = clampString(value, MAX_LENGTH_EMAIL);

                if (guestEmail != processedValue)
                {
                    guestEmail = processedValue;
                    OnPropertyChanged();

                    if (!string.IsNullOrEmpty(processedValue))
                    {
                        markAsTouched(nameof(GuestEmail));
                    }
                    validate(validator, this);
                    OnPropertyChanged(nameof(GuestEmailError));
                }
            }
        }

        public string DesiredUsername
        {
            get => desiredUsername;
            set
            {
                string processedValue = clampString(value, MAX_LENGTH_USERNAME);

                if (desiredUsername != processedValue)
                {
                    desiredUsername = processedValue;
                    OnPropertyChanged();

                    if (!string.IsNullOrEmpty(processedValue))
                    {
                        markAsTouched(nameof(DesiredUsername));
                    }

                    validate(validator, this);
                    OnPropertyChanged(nameof(DesiredUsernameError));
                }
            }
        }
        public string LobbyCodeError
        {
            get
            {
                var errors = GetErrors(nameof(LobbyCode)) as List<string>;
                return errors?.FirstOrDefault();
            }
        }

        public string GuestEmailError
        {
            get
            {
                var errors = GetErrors(nameof(GuestEmail)) as List<string>;
                return errors?.FirstOrDefault();
            }
        }

        public string DesiredUsernameError
        {
            get
            {
                var errors = GetErrors(nameof(DesiredUsername)) as List<string>;
                return errors?.FirstOrDefault();
            }
        }

        public ICommand JoinAsGuestCommand { get; }
        public ICommand GoBackCommand { get; }

        public GuestJoinViewModel()
        {
            this.validator = new GuestJoinValidator();
            validate(validator, this);
        }

        public GuestJoinViewModel(
            IMatchmakingService matchmakingService,
            IDialogService dialogService,
            GuestJoinValidator validator,
            INavigationService navigationService,
            IWindowNavigationService windowNavigationService,
            ICurrentLobbyService currentLobbyService,
            IServiceExceptionHandler exceptionHandler)
        {
            this.matchmakingService = matchmakingService;
            this.dialogService = dialogService;
            this.validator = validator;
            this.navigationService = navigationService;
            this.windowNavigationService = windowNavigationService;
            this.currentLobbyService = currentLobbyService;
            this.exceptionHandler = exceptionHandler;

            JoinAsGuestCommand = new RelayCommand(async param => await executeJoinAsGuestAsync(), param => canExecuteJoin());
            GoBackCommand = new RelayCommand(param => executeGoBack(), param => !IsBusy);

            validate(validator, this);
        }

        private static string clampString(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        private bool canExecuteJoin()
        {
            return !IsBusy && !HasErrors;
        }

        private void executeGoBack()
        {
            navigationService.goBack();
        }

        private async Task executeJoinAsGuestAsync()
        {
            markAllAsTouched();
            if (HasErrors) return;
            SetBusy(true);

            var joinRequest = new GuestJoinRequestDto
            {
                LobbyCode = this.LobbyCode.Trim(),
                GuestEmail = this.GuestEmail.Trim(),
                DesiredGuestUsername = this.DesiredUsername.Trim()
            };

            try
            {
                GuestJoinServiceResultDto serviceResult = await matchmakingService.joinLobbyAsGuestAsync(joinRequest);

                if (serviceResult.WcfResult.Success && serviceResult.WcfResult.InitialLobbyState != null)
                {
                    currentLobbyService.setInitialState(serviceResult.WcfResult.InitialLobbyState);
                    windowNavigationService.openWindow<GameWindow>();

                    var mainWindow = Application.Current.Windows.OfType<AuthenticationWindow>().FirstOrDefault();
                    if (mainWindow != null)
                    {
                        mainWindow.IsExitConfirmed = true;
                    }

                    windowNavigationService.closeWindow<AuthenticationWindow>();
                }
                else
                {
                    string localizedMessage = MessageCodeInterpreter.translate(serviceResult.WcfResult.MessageCode);

                    dialogService.showError(localizedMessage, Lang.ErrorTitle);
                    if (!serviceResult.DidMatchmakingConnect)
                    {
                        matchmakingService.disconnect();
                    }
                }
            }
            catch (Exception ex)
            {
                exceptionHandler.handleException(ex, Lang.GuestJoinOperation);
                matchmakingService.disconnect();
            }
            finally
            {
                SetBusy(false);
            }
        }
    }
}