using MindWeaveClient.Properties.Langs;
using MindWeaveClient.Services;
using MindWeaveClient.Services.Abstractions;
using MindWeaveClient.Utilities.Abstractions;
using MindWeaveClient.Utilities.Implementations;
using MindWeaveClient.Validators;
using MindWeaveClient.View.Authentication;
using MindWeaveClient.View.Main;
using MindWeaveCliente.Services.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Navigation;

namespace MindWeaveClient.ViewModel.Authentication
{
    public class LoginViewModel : BaseViewModel
    {
        private const int MAX_LENGTH_EMAIL = 45;
        private const int MAX_LENGTH_PASSWORD = 128;
        private const string AUTH_ACCOUNT_NOT_VERIFIED = "AUTH_ACCOUNT_NOT_VERIFIED";

        private string email;
        private string password;
        private bool showUnverifiedControls;

        private readonly IAuthenticationService authenticationService;
        private readonly IDialogService dialogService;
        private readonly LoginValidator validator;
        private readonly INavigationService navigationService;
        private readonly IWindowNavigationService windowNavigationService;
        private readonly IServiceExceptionHandler exceptionHandler;
        public string Email
        {
            get => email;
            set
            {
                string processedValue = clampString(value, MAX_LENGTH_EMAIL);

                if (email != processedValue)
                {
                    email = processedValue;
                    OnPropertyChanged();

                    if (!string.IsNullOrEmpty(processedValue))
                    {
                        markAsTouched(nameof(email));
                    }
                    validate(validator, this);
                    OnPropertyChanged(nameof(EmailError));
                }
            }
        }

        public string Password
        {
            get => password;
            set
            {
                string processedValue = clampString(value, MAX_LENGTH_PASSWORD);

                if (password != processedValue)
                {
                    password = processedValue;
                    OnPropertyChanged();

                    if (!string.IsNullOrEmpty(processedValue))
                    {
                        markAsTouched(nameof(Password));
                    }

                    validate(validator, this);
                    OnPropertyChanged(nameof(PasswordError));
                }
            }
        }

        public string EmailError
        {
            get
            {
                var errors = GetErrors(nameof(Email)) as List<string>;
                return errors?.FirstOrDefault();
            }
        }

        public string PasswordError
        {
            get
            {
                var errors = GetErrors(nameof(Password)) as List<string>;
                return errors?.FirstOrDefault();
            }
        }

        public bool ShowUnverifiedControls
        {
            get => showUnverifiedControls;
            set
            {
                showUnverifiedControls = value;
                OnPropertyChanged();
            }
        }

        public ICommand LoginCommand { get; }
        public ICommand SignUpCommand { get; }
        public ICommand ForgotPasswordCommand { get; }
        public ICommand GuestLoginCommand { get; }
        public ICommand ResendVerificationCommand { get; }
        public ICommand ExitCommand { get; }

        public LoginViewModel(
            IAuthenticationService authenticationService,
            IDialogService dialogService,
            LoginValidator validator,
            INavigationService navigationService,
            IWindowNavigationService windowNavigationService,
            IServiceExceptionHandler exceptionHandler)
        {
            this.authenticationService = authenticationService;
            this.dialogService = dialogService;
            this.validator = validator;
            this.navigationService = navigationService;
            this.windowNavigationService = windowNavigationService;
            this.exceptionHandler = exceptionHandler;

            LoginCommand = new RelayCommand(async (param) => await executeLoginAsync(), (param) => canExecuteLogin());
            SignUpCommand = new RelayCommand((param) => executeGoToSignUp());
            ForgotPasswordCommand = new RelayCommand((param) => executeGoToForgotPassword());
            GuestLoginCommand = new RelayCommand((param) => executeGoToGuestJoin());
            ResendVerificationCommand = new RelayCommand(async (param) => await executeResendVerificationAsync());
            ExitCommand = new RelayCommand(executeExit);

            validate(validator, this);
            this.exceptionHandler = exceptionHandler;
        }

        private static string clampString(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        private bool canExecuteLogin()
        {
            return !IsBusy && !HasErrors;
        }

        private async Task executeLoginAsync()
        {
            markAllAsTouched();

            if (HasErrors) { return; }

            SetBusy(true);
            try
            {
                var serviceResult = await authenticationService.loginAsync(Email, Password);

                if (serviceResult.WcfLoginResult.OperationResult.Success)
                {
                    handleSuccessfulLogin(serviceResult);
                }
                else
                {
                    await handleFailedLogin(serviceResult);
                }
            }
            catch (Exception ex)
            {
                exceptionHandler.handleException(ex, Lang.LoginOperation);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void handleSuccessfulLogin(LoginServiceResultDto result)
        {
            if (!result.IsSocialServiceConnected)
            {
                dialogService.showWarning(Lang.WarningMsgSocialConnectFailed, Lang.WarningTitle);
            }

            if (!result.IsMatchmakingServiceConnected)
            {
                dialogService.showWarning(Lang.WarningMsgMatchmakingConnectFailed, Lang.WarningTitle);
            }

            windowNavigationService.openWindow<MainWindow>();
            windowNavigationService.closeWindow<AuthenticationWindow>();
        }

        private async Task handleFailedLogin(LoginServiceResultDto result)
        {
            if (result.WcfLoginResult.ResultCode == AUTH_ACCOUNT_NOT_VERIFIED)
            {
                bool userWantsToVerify = dialogService.showWarning(
            MessageCodeInterpreter.translate(AUTH_ACCOUNT_NOT_VERIFIED),
            Lang.WarningTitle);

                if (userWantsToVerify)
                {

                    var resendResult = await authenticationService.resendVerificationCodeAsync(Email);

                    if (resendResult.Success)
                    {
                        SessionService.PendingVerificationEmail = this.Email;

                        dialogService.showInfo(MessageCodeInterpreter.translate(resendResult.MessageCode), Lang.InfoMsgResendSuccessTitle);

                        navigationService.navigateTo<VerificationPage>();
                    }
                    else
                    {

                        string localizedMessage = MessageCodeInterpreter.translate(resendResult.MessageCode);
                        dialogService.showError(localizedMessage, Lang.ErrorTitle);
                    }
                }
            }
            else
            {
                string localizedMessage = MessageCodeInterpreter.translate(
                    result.WcfLoginResult.OperationResult.MessageCode
                );

                dialogService.showError(localizedMessage, Lang.ErrorTitle);
            }
        }

        private async Task executeResendVerificationAsync()
        {
            SetBusy(true);
            try
            {
                var result = await authenticationService.resendVerificationCodeAsync(Email);

                if (result.Success)
                {
                    string successMessage = MessageCodeInterpreter.translate(result.MessageCode);
                    dialogService.showInfo(successMessage, Lang.InfoMsgResendSuccessTitle);
                    SessionService.PendingVerificationEmail = this.Email;
                    navigationService.navigateTo<VerificationPage>();
                }
                else
                {
                    string localizedMessage = MessageCodeInterpreter.translate(result.MessageCode);
                    dialogService.showError(localizedMessage, Lang.ErrorTitle);
                }
            }
            catch (Exception ex)
            {
                exceptionHandler.handleException(ex, Lang.ResendCodeOperation);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void executeGoToSignUp()
        {
            navigationService.navigateTo<CreateAccountPage>();
        }

        private void executeGoToForgotPassword()
        {
            navigationService.navigateTo<PasswordRecoveryPage>();
        }

        private void executeGoToGuestJoin()
        {
            navigationService.navigateTo<GuestJoinPage>();
        }

        private static void executeExit(object parameter)
        {
            System.Windows.Application.Current.Shutdown();
        }


    }
}