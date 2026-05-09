using MindWeaveClient.Properties.Langs;
using MindWeaveClient.Services;
using MindWeaveClient.Services.Abstractions;
using MindWeaveClient.Utilities.Abstractions;
using MindWeaveClient.Utilities.Implementations;
using MindWeaveClient.Validators;
using MindWeaveClient.View.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace MindWeaveClient.ViewModel.Authentication
{
    public class PasswordRecoveryViewModel : BaseViewModel
    {
        private const int MAX_LENGTH_EMAIL = 45;
        private const int MAX_LENGTH_CODE = 6;
        private const int MAX_LENGTH_PASSWORD = 128;
        private const int RESEND_COOLDOWN_SECONDS = 60;
        private const string CODE_INVALID_OR_EXPIRED = "AUTH_CODE_INVALID_OR_EXPIRED";
        private const string STEP1 = "Step1";
        private const string STEP2 = "Step2";
        private const string STEP3 = "Step3";
        private const string AUTH_ACCOUNT_NOT_VERIFIED = "AUTH_ACCOUNT_NOT_VERIFIED";
        private const string ACCOUNT_NOT_VERIFIED = "ACCOUNT_NOT_VERIFIED";

        private readonly IAuthenticationService authenticationService;
        private readonly IDialogService dialogService;
        private readonly PasswordRecoveryValidator validator;
        private readonly INavigationService navigationService;
        private readonly IServiceExceptionHandler exceptionHandler;

        private bool isStep1Visible = true;
        private bool isStep2Visible;
        private bool isStep3Visible;
        private bool canResendCode = true;
        private int remainingSeconds = 0;
        private DispatcherTimer resendTimer;

        public bool IsStep1Visible
        {
            get => isStep1Visible;
            set
            {
                isStep1Visible = value;
                OnPropertyChanged();
            }
        }

        public bool IsStep2Visible
        {
            get => isStep2Visible;
            set
            {
                isStep2Visible = value;
                OnPropertyChanged();
            }
        }

        public bool IsStep3Visible
        {
            get => isStep3Visible;
            set
            {
                isStep3Visible = value;
                OnPropertyChanged();
            }
        }

        public bool CanResendCode
        {
            get => canResendCode;
            set
            {
                if (canResendCode != value)
                {
                    canResendCode = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ResendTimerText => $"⏱ {remainingSeconds}s";

        public bool IsTimerVisible => !CanResendCode;

        private string email;
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
                        markAsTouched(nameof(Email));
                    }
                    validateCurrentStep();
                    OnPropertyChanged(nameof(EmailError));
                }
            }
        }

        private string verificationCode;
        public string VerificationCode
        {
            get => verificationCode;
            set
            {
                string processedValue = clampString(value, MAX_LENGTH_CODE);

                if (verificationCode != processedValue)
                {
                    verificationCode = processedValue;
                    OnPropertyChanged();

                    if (!string.IsNullOrEmpty(processedValue))
                    {
                        markAsTouched(nameof(VerificationCode));
                    }
                    validateCurrentStep();
                    OnPropertyChanged(nameof(VerificationCodeError));
                }
            }
        }

        private string newPassword;
        public string NewPassword
        {
            get => newPassword;
            set
            {
                string processedValue = clampString(value, MAX_LENGTH_PASSWORD);

                if (newPassword != processedValue)
                {
                    newPassword = processedValue;
                    OnPropertyChanged();

                    if (!string.IsNullOrEmpty(processedValue))
                    {
                        markAsTouched(nameof(NewPassword));
                    }

                    validateCurrentStep();
                    OnPropertyChanged(nameof(NewPasswordError));
                }
            }
        }

        private string confirmPassword;
        public string ConfirmPassword
        {
            get => confirmPassword;
            set
            {
                string processedValue = clampString(value, MAX_LENGTH_PASSWORD);

                if (confirmPassword != processedValue)
                {
                    confirmPassword = processedValue;
                    OnPropertyChanged();

                    if (!string.IsNullOrEmpty(processedValue))
                    {
                        markAsTouched(nameof(ConfirmPassword));
                    }
                    validateCurrentStep();
                    OnPropertyChanged(nameof(ConfirmPasswordError));
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

        public string VerificationCodeError
        {
            get
            {
                var errors = GetErrors(nameof(VerificationCode)) as List<string>;
                return errors?.FirstOrDefault();
            }
        }

        public string NewPasswordError
        {
            get
            {
                var errors = GetErrors(nameof(NewPassword)) as List<string>;
                return errors?.FirstOrDefault();
            }
        }

        public string ConfirmPasswordError
        {
            get
            {
                var errors = GetErrors(nameof(ConfirmPassword)) as List<string>;
                return errors?.FirstOrDefault();
            }
        }

        public ICommand SendCodeCommand { get; }
        public ICommand VerifyCodeCommand { get; }
        public ICommand ResendCodeCommand { get; }
        public ICommand SavePasswordCommand { get; }
        public ICommand GoBackCommand { get; }

        public PasswordRecoveryViewModel()
        {
            this.validator = new PasswordRecoveryValidator();
            validateCurrentStep();
        }

        public PasswordRecoveryViewModel(
            IAuthenticationService authenticationService,
            IDialogService dialogService,
            PasswordRecoveryValidator validator,
            INavigationService navigationService,
            IServiceExceptionHandler exceptionHandler)
        {
            this.authenticationService = authenticationService;
            this.dialogService = dialogService;
            this.validator = validator;
            this.navigationService = navigationService;
            this.exceptionHandler = exceptionHandler;

            SendCodeCommand = new RelayCommand(async param => await executeSendCodeAsync(), param => !HasErrors && !IsBusy);
            VerifyCodeCommand = new RelayCommand(async param => await executeVerifyCodeAsync(), param => !HasErrors && !IsBusy);
            ResendCodeCommand = new RelayCommand(async param => await executeSendCodeAsync(true), param => CanResendCode && !IsBusy);
            SavePasswordCommand = new RelayCommand(async param => await executeSavePasswordAsync(), param => !HasErrors && !IsBusy);
            GoBackCommand = new RelayCommand(param => executeGoBack());

            initializeTimer();
            validateCurrentStep();
        }

        private void initializeTimer()
        {
            resendTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            resendTimer.Tick += onTimerTick;
        }

        private void onTimerTick(object sender, EventArgs e)
        {
            remainingSeconds--;
            OnPropertyChanged(nameof(ResendTimerText));

            if (remainingSeconds <= 0)
            {
                stopResendTimer();
            }
        }

        private void startResendTimer()
        {
            CanResendCode = false;
            remainingSeconds = RESEND_COOLDOWN_SECONDS;
            OnPropertyChanged(nameof(ResendTimerText));
            OnPropertyChanged(nameof(IsTimerVisible));
            resendTimer.Start();
        }

        private void stopResendTimer()
        {
            resendTimer?.Stop();
            CanResendCode = true;
            remainingSeconds = 0;
            OnPropertyChanged(nameof(IsTimerVisible));
        }

        private static string clampString(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        private async Task executeSendCodeAsync(bool isResend = false)
        {
            if (!isResend)
            {
                markAsTouched(nameof(Email));
                if (HasErrors) return;
            }

            SetBusy(true);
            try
            {
                var result = await authenticationService.sendPasswordRecoveryCodeAsync(Email);

                if (result.Success)
                {
                    dialogService.showInfo(Lang.ValidationPasswordRecoveryCodeSent, Lang.InfoMsgResendSuccessTitle);

                    if (!isResend)
                    {
                        IsStep1Visible = false;
                        IsStep2Visible = true;
                        IsStep3Visible = false;
                        VerificationCode = string.Empty;

                        clearTouchedState();
                        validateCurrentStep();
                    }

                    startResendTimer();
                    return;
                }

                bool isUnverified =
                    result.MessageCode == AUTH_ACCOUNT_NOT_VERIFIED ||
                    result.MessageCode == ACCOUNT_NOT_VERIFIED;

                if (isUnverified)
                {
                    bool userWantsToVerify = dialogService.showWarning(
                        MessageCodeInterpreter.translate(AUTH_ACCOUNT_NOT_VERIFIED),
                        Lang.WarningTitle
                    );

                    if (!userWantsToVerify)
                        return; 

                    var resendResult = await authenticationService.resendVerificationCodeAsync(Email);

                    if (resendResult.Success)
                    {
                        SessionService.PendingVerificationEmail = Email;

                        dialogService.showInfo(
                            MessageCodeInterpreter.translate(resendResult.MessageCode),
                            Lang.InfoMsgResendSuccessTitle
                        );

                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            navigationService.navigateTo<VerificationPage>();
                        });

                        return;
                    }

                    dialogService.showError(
                        MessageCodeInterpreter.translate(resendResult.MessageCode),
                        Lang.ErrorTitle
                    );

                    return;
                }

                string errorMsg = MessageCodeInterpreter.translate(result.MessageCode);
                dialogService.showError(errorMsg, Lang.ErrorTitle);
            }
            catch (Exception ex)
            {
                exceptionHandler.handleException(ex, Lang.PasswordRecoveryOperation);
            }
            finally
            {
                SetBusy(false);
            }
        }



        private Task executeVerifyCodeAsync()
        {
            markAsTouched(nameof(VerificationCode));
            if (HasErrors) return Task.CompletedTask;

            SetBusy(true);

            IsStep1Visible = false;
            IsStep2Visible = false;
            IsStep3Visible = true;
            NewPassword = "";
            ConfirmPassword = "";

            clearTouchedState();
            validateCurrentStep();

            stopResendTimer();

            SetBusy(false);
            return Task.CompletedTask;
        }

        private async Task executeSavePasswordAsync()
        {
            markAsTouched(nameof(NewPassword));
            markAsTouched(nameof(ConfirmPassword));

            if (HasErrors) return;

            SetBusy(true);
            try
            {
                var result = await authenticationService.resetPasswordWithCodeAsync(Email, VerificationCode, NewPassword);

                if (result.Success)
                {
                    dialogService.showInfo(Lang.InfoMsgPasswordResetSuccessBody, Lang.InfoMsgPasswordResetSuccessTitle);
                    stopResendTimer();
                    navigationService.navigateTo<LoginPage>();
                }
                else
                {
                    string errorMsg = MessageCodeInterpreter.translate(result.MessageCode);
                    dialogService.showError(errorMsg, Lang.ErrorTitle);

                    if (result.MessageCode == CODE_INVALID_OR_EXPIRED)
                    {
                        IsStep1Visible = false;
                        IsStep2Visible = true;
                        IsStep3Visible = false;
                        VerificationCode = string.Empty;

                        clearTouchedState();
                        validateCurrentStep();
                    }
                }
            }
            catch (Exception ex)
            {
                exceptionHandler.handleException(ex, Lang.PasswordRecoveryOperation);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void executeGoBack()
        {
            if (IsStep3Visible)
            {
                IsStep1Visible = false;
                IsStep2Visible = true;
                IsStep3Visible = false;

                clearTouchedState();
                validateCurrentStep();
            }
            else if (IsStep2Visible)
            {
                IsStep1Visible = true;
                IsStep2Visible = false;
                IsStep3Visible = false;

                stopResendTimer();

                clearTouchedState();
                validateCurrentStep();
            }
            else
            {
                stopResendTimer();
                navigationService.goBack();
            }
        }

        private void validateCurrentStep()
        {
            if (IsStep1Visible)
            {
                validate(validator, this, STEP1);
            }
            else if (IsStep2Visible)
            {
                validate(validator, this, STEP2);
            }
            else if (IsStep3Visible)
            {
                validate(validator, this, STEP3);
            }
        }
    }
}