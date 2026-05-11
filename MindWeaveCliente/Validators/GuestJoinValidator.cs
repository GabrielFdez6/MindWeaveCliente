using FluentValidation;
using MindWeaveClient.Properties.Langs;
using MindWeaveClient.ViewModel.Authentication;

namespace MindWeaveClient.Validators
{
    public class GuestJoinValidator : AbstractValidator<GuestJoinViewModel>
    {
        private const string LOBBY_CODE_REGEX = "^[a-zA-Z0-9]{6}$";
        private const string USERNAME_VALIDATOR_REGEX = @"^[a-zA-Z0-9][a-zA-Z0-9._-]*$";
        private const string EMAIL_REGEX = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";

        private const int LOBBY_CODE_LENGTH = 6;
        private const int MAX_EMAIL_LENGTH = 45;
        private const int MAX_USERNAME_LENGTH = 20;
        private const int MIN_USERNAME_LENGTH = 3;

        public GuestJoinValidator()
        {
            RuleFor(vm => vm.LobbyCode)
                .NotEmpty().WithMessage(Lang.ValidationLobbyCodeRequired)
                .Length(LOBBY_CODE_LENGTH).WithMessage(Lang.ValidationLobbyCodeFormat)
                .Matches(LOBBY_CODE_REGEX).WithMessage(Lang.ValidationLobbyCodeFormat);

            RuleFor(vm => vm.GuestEmail)
                .NotEmpty().WithMessage(Lang.ValidationEmailRequired)
                .EmailAddress().WithMessage(Lang.ValidationEmailInvalid)
                .Matches(EMAIL_REGEX).WithMessage(Lang.ValidationEmailInvalid)
                .MaximumLength(MAX_EMAIL_LENGTH);

            RuleFor(vm => vm.DesiredUsername)
                .NotEmpty().WithMessage(Lang.ValidationUsernameRequired)
                .Length(MIN_USERNAME_LENGTH, MAX_USERNAME_LENGTH).WithMessage(Lang.ValidationUsernameLength)
                .Matches(USERNAME_VALIDATOR_REGEX).WithMessage(Lang.ValidationUsernameInvalidFormat);
        }
    }
}