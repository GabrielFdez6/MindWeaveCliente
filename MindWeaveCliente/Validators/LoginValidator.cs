using FluentValidation;
using MindWeaveClient.Properties.Langs;
using MindWeaveClient.ViewModel.Authentication;

namespace MindWeaveClient.Validators
{
    public class LoginValidator : AbstractValidator<LoginViewModel>
    {
        private const int MAX_EMAIL_LENGTH = 45;
        private const int MAX_PASSWORD_LENGTH = 128;

        public LoginValidator()
        {
            RuleFor(vm => vm.Email)
                .NotEmpty().WithMessage(Lang.ValidationEmailRequired)
                .EmailAddress().WithMessage(Lang.ValidationEmailInvalid)
                .MaximumLength(MAX_EMAIL_LENGTH);

            RuleFor(vm => vm.Password)
                .NotEmpty().WithMessage(Lang.ValidationPasswordRequired)
                .MaximumLength(MAX_PASSWORD_LENGTH);
        }
    }
}