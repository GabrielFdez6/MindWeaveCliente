using MindWeaveClient.AuthenticationService;
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
using System.Windows.Input;
using System.Windows.Navigation;

namespace MindWeaveClient.ViewModel.Authentication
{
    public class CreateAccountViewModel : BaseViewModel
    {
        private const int MAX_LENGTH_FIRST_NAME = 45;
        private const int MAX_LENGTH_LAST_NAME = 45;
        private const int MAX_LENGTH_USERNAME = 16;
        private const int MAX_LENGTH_EMAIL = 45;
        private const int MAX_LENGTH_PASSWORD = 128;
        private const int FEMALE = 1;
        private const int MALE = 2;
        private const int OTHER = 3;
        private const int PREFER_NOT_SAY = 4;
        private const string AUTH_ACCOUNT_NOT_VERIFIED = "AUTH_ACCOUNT_NOT_VERIFIED";


        private string firstName;
        private string lastName;
        private string username;
        private string email;
        private DateTime? birthDate = DateTime.Now;
        private string password;
        private bool isFemale;
        private bool isMale;
        private bool isOther;
        private bool isPreferNotToSay;

        private readonly IAuthenticationService authenticationService;
        private readonly IDialogService dialogService;
        private readonly CreateAccountValidator validator;
        private readonly INavigationService navigationService;
        private readonly IServiceExceptionHandler exceptionHandler;

        public string FirstName
        {
            get => firstName;
            set
            {
                string processedValue = clampString(value, MAX_LENGTH_FIRST_NAME);
                if (firstName != processedValue)
                {
                    firstName = processedValue;
                    OnPropertyChanged();

                    if (!string.IsNullOrEmpty(processedValue))
                    {
                        markAsTouched(nameof(FirstName));
                    }
                    validate(validator, this);
                    OnPropertyChanged(nameof(FirstNameError));
                }
            }
        }

        public string LastName
        {
            get => lastName;
            set
            {
                string processedValue = clampString(value, MAX_LENGTH_LAST_NAME);

                if (lastName != processedValue)
                {
                    lastName = processedValue;
                    OnPropertyChanged();

                    if (!string.IsNullOrEmpty(processedValue))
                    {
                        markAsTouched(nameof(LastName));
                    }
                    validate(validator, this);
                    OnPropertyChanged(nameof(LastNameError));
                }
            }
        }

        public string Username
        {
            get => username;
            set
            {
                string processedValue = clampString(value, MAX_LENGTH_USERNAME);

                if (username != processedValue)
                {
                    username = processedValue;
                    OnPropertyChanged();

                    if (!string.IsNullOrEmpty(processedValue))
                    {
                        markAsTouched(nameof(Username));
                    }
                    validate(validator, this);
                    OnPropertyChanged(nameof(UsernameError));
                }
            }
        }

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
                    validate(validator, this);
                    OnPropertyChanged(nameof(EmailError));
                }
            }
        }

        public DateTime? BirthDate
        {
            get => birthDate;
            set
            {
                birthDate = value;
                OnPropertyChanged();
                if (value.HasValue)
                {
                    markAsTouched(nameof(BirthDate));
                }
                validate(validator, this);
                OnPropertyChanged(nameof(BirthDateError));
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

        public bool IsFemale
        {
            get => isFemale;
            set
            {
                isFemale = value;
                OnPropertyChanged();
                markAsTouched(nameof(IsFemale));
                validate(validator, this);
                OnPropertyChanged(nameof(GenderError));
            }
        }

        public bool IsMale
        {
            get => isMale;
            set
            {
                isMale = value;
                OnPropertyChanged();
                markAsTouched(nameof(IsFemale));
                validate(validator, this);
                OnPropertyChanged(nameof(GenderError));
            }
        }

        public bool IsOther
        {
            get => isOther;
            set
            {
                isOther = value;
                OnPropertyChanged();
                markAsTouched(nameof(IsFemale));
                validate(validator, this);
                OnPropertyChanged(nameof(GenderError));
            }
        }

        public bool IsPreferNotToSay
        {
            get => isPreferNotToSay;
            set
            {
                isPreferNotToSay = value;
                OnPropertyChanged();
                markAsTouched(nameof(IsFemale));
                validate(validator, this);
                OnPropertyChanged(nameof(GenderError));
            }
        }

        public string FirstNameError
        {
            get
            {
                var errors = GetErrors(nameof(FirstName)) as List<string>;
                return errors?.FirstOrDefault();
            }
        }

        public string LastNameError
        {
            get
            {
                var errors = GetErrors(nameof(LastName)) as List<string>;
                return errors?.FirstOrDefault();
            }
        }

        public string UsernameError
        {
            get
            {
                var errors = GetErrors(nameof(Username)) as List<string>;
                return errors?.FirstOrDefault();
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

        public string BirthDateError
        {
            get
            {
                var errors = GetErrors(nameof(BirthDate)) as List<string>;
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

        public string GenderError
        {
            get
            {
                var errors = GetErrors(nameof(IsFemale)) as List<string>;
                return errors?.FirstOrDefault();
            }
        }

        public ICommand SignUpCommand { get; }
        public ICommand GoToLoginCommand { get; }

        public CreateAccountViewModel()
        {
            this.validator = new CreateAccountValidator();
            validate(validator, this);
        }

        public CreateAccountViewModel(
            IAuthenticationService authenticationService,
            IDialogService dialogService,
            CreateAccountValidator validator,
            INavigationService navigationService,
            IServiceExceptionHandler exceptionHandler)
        {
            this.authenticationService = authenticationService;
            this.dialogService = dialogService;
            this.validator = validator;
            this.navigationService = navigationService;
            this.exceptionHandler = exceptionHandler;

            SignUpCommand = new RelayCommand(async (param) => await executeSignUp(), (param) => canExecuteSignUp());
            GoToLoginCommand = new RelayCommand((param) => executeGoToLogin());

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

        private bool canExecuteSignUp()
        {
            return !IsBusy && !HasErrors;
        }

        private async Task executeSignUp()
        {
            markAllAsTouched();

            if (HasErrors)
            {
                return;
            }
            SetBusy(true);
            var userProfile = new UserProfileDto
            {
                FirstName = this.FirstName.Trim(),
                LastName = this.LastName.Trim(),
                Username = this.Username.Trim(),
                Email = this.Email.Trim(),
                DateOfBirth = BirthDate.Value,
                GenderId = getSelectedGenderId()
            };

            try
            {
                OperationResultDto result = await authenticationService.registerAsync(userProfile, this.Password);

                if (result.Success)
                {
                    SessionService.PendingVerificationEmail = this.Email;

                    await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        navigationService.navigateTo<VerificationPage>();
                    });
                }
                else
                {

                    if (result.MessageCode == AUTH_ACCOUNT_NOT_VERIFIED)
                    {
                        bool userWantsToVerify = dialogService.showWarning(
                            MessageCodeInterpreter.translate(AUTH_ACCOUNT_NOT_VERIFIED),
                            Lang.WarningTitle
                        );

                        if (userWantsToVerify)
                        {
                            var resendResult = await authenticationService.resendVerificationCodeAsync(this.Email);

                            if (resendResult.Success)
                            {
                                SessionService.PendingVerificationEmail = this.Email;

                                dialogService.showInfo(
                                    MessageCodeInterpreter.translate(resendResult.MessageCode),
                                    Lang.InfoMsgResendSuccessTitle
                                );

                                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                                {
                                    navigationService.navigateTo<VerificationPage>();
                                });
                            }
                            else
                            {
                                string resendError = MessageCodeInterpreter.translate(resendResult.MessageCode);
                                dialogService.showError(resendError, Lang.ErrorTitle);
                            }
                        }

                        return;
                    }

                    string localizedMessage = MessageCodeInterpreter.translate(result.MessageCode);
                    dialogService.showError(localizedMessage, Lang.ErrorTitle);
                }
            }
            catch (Exception ex)
            {
                exceptionHandler.handleException(ex, Lang.RegistrationOperation);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void executeGoToLogin()
        {
            navigationService.navigateTo<LoginPage>();
        }

        private int getSelectedGenderId()
        {
            if (IsFemale) return FEMALE;
            if (IsMale) return MALE;
            return IsOther ? OTHER : PREFER_NOT_SAY;
        }
    }
}