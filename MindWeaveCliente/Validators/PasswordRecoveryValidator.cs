using FluentValidation;
using MindWeaveClient.Properties.Langs;
using MindWeaveClient.ViewModel.Authentication;

namespace MindWeaveClient.Validators
{
    public class PasswordRecoveryValidator : AbstractValidator<PasswordRecoveryViewModel>
    {
        private const string STEP1 = "Step1";
        private const string STEP2 = "Step2";
        private const string STEP3 = "Step3";
        private const string CREDENTIAL_POLICY_REGEX = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!@#\\$%^&*(),.?\"":{}|<>]).{8,}$";
        private const string VERIFICATION_CODE_REGEX = "^[0-9]{6}$";
        private const string EMAIL_REGEX = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        private const int MAX_EMAIL_LENGTH = 45;
        private const int VERIFICATION_CODE_LENGTH = 6;

        private const int MAX_PASSWORD_INPUT_LENGTH = 128;

        public PasswordRecoveryValidator()
        {
            RuleSet(STEP1, () =>
            {
                RuleFor(vm => vm.Email)
                    .NotEmpty().WithMessage(Lang.ValidationEmailRequired)
                    .MaximumLength(MAX_EMAIL_LENGTH)
                    .Matches(EMAIL_REGEX)
                    .WithMessage(Lang.ValidationEmailInvalid)
                    .EmailAddress().WithMessage(Lang.ValidationEmailInvalid);
            });

            RuleSet(STEP2, () =>
            {
                RuleFor(vm => vm.VerificationCode)
                    .NotEmpty().WithMessage(Lang.ValidationVerificationCodeRequired)
                    .Length(VERIFICATION_CODE_LENGTH).WithMessage(Lang.ValidationVerificationCodeFormat)
                    .Matches(VERIFICATION_CODE_REGEX).WithMessage(Lang.ValidationVerificationCodeFormat);
            });

            RuleSet(STEP3, () =>
            {
                RuleFor(vm => vm.NewPassword)
                    .NotEmpty().WithMessage(Lang.ValidationPasswordNewRequired)
                    .MaximumLength(MAX_PASSWORD_INPUT_LENGTH)
                    .Matches(CREDENTIAL_POLICY_REGEX).WithMessage(Lang.ValidationPasswordPolicy);

                RuleFor(vm => vm.ConfirmPassword)
                    .NotEmpty().WithMessage(Lang.ValidationPasswordConfirmRequired)
                    .MaximumLength(MAX_PASSWORD_INPUT_LENGTH)
                    .Equal(vm => vm.NewPassword).WithMessage(Lang.ValidationPasswordsDoNotMatch);
            });
        }
    }
}