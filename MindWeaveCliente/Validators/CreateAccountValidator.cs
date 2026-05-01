using FluentValidation;
using MindWeaveClient.Properties.Langs;
using MindWeaveClient.ViewModel.Authentication;
using System;

namespace MindWeaveClient.Validators
{
    public class CreateAccountValidator : AbstractValidator<CreateAccountViewModel>
    {
        private const string CREDENTIAL_POLICY_REGEX = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!@#\\$%^&*(),.?\"":{}|<>]).{8,}$";
        private const string EMAIL_REGEX = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        private const string NAME_VALIDATOR_REGEX = @"^(?=.*\p{L})[\p{L}\p{M} '\-\.]+$";
        private const string USERNAME_VALIDATOR_REGEX = @"^[a-zA-Z0-9][a-zA-Z0-9._-]*$";

        private const int MAX_LENGTH_FIRST_NAME = 45;
        private const int MAX_LENGTH_LAST_NAME = 45;
        private const int MAX_LENGTH_USERNAME = 20;
        private const int MIN_LENGTH_USERNAME = 3;
        private const int MIN_LENGTH_NAME = 3;

        private const int MAX_LENGTH_EMAIL = 45;
        private const int MAX_LENGTH_PASSWORD = 128;
        private const int MIN_AGE_REQUIRED = 13;
        private const int MAX_AGE_ALLOWED = 100;

        public CreateAccountValidator()
        {
            RuleFor(vm => vm.FirstName)
                .NotEmpty().WithMessage(Lang.ValidationFirstNameRequired)
                .Length(MIN_LENGTH_NAME, MAX_LENGTH_FIRST_NAME).WithMessage(Lang.ValidationFirstNameLength)
                .Matches(NAME_VALIDATOR_REGEX).WithMessage(Lang.ValidationNameInvalidCharacters);

            RuleFor(vm => vm.LastName)
                .NotEmpty().WithMessage(Lang.ValidationLastNameRequired)
                .Length(MIN_LENGTH_NAME, MAX_LENGTH_LAST_NAME).WithMessage(Lang.ValidationLastNameLength)
                .Matches(NAME_VALIDATOR_REGEX).WithMessage(Lang.ValidationNameInvalidCharacters);

            RuleFor(vm => vm.Username)
                .NotEmpty().WithMessage(Lang.ValidationUsernameRequired)
                .Length(MIN_LENGTH_USERNAME, MAX_LENGTH_USERNAME).WithMessage(Lang.ValidationUsernameLength)
                .Matches(USERNAME_VALIDATOR_REGEX).WithMessage(Lang.ValidationUsernameInvalidFormat);

            RuleFor(vm => vm.Email)
                .NotEmpty().WithMessage(Lang.ValidationEmailRequired)
                .EmailAddress().WithMessage(Lang.ValidationEmailInvalid)
                .Matches(EMAIL_REGEX).WithMessage(Lang.ValidationEmailInvalid)
                .MaximumLength(MAX_LENGTH_EMAIL);

            RuleFor(vm => vm.BirthDate)
                .NotNull().WithMessage(Lang.ValidationBirthDateRequired)
                .Must(beValidAge).WithMessage(Lang.ValidationBirthDateInvalid);

            RuleFor(vm => vm.Password)
                .NotEmpty().WithMessage(Lang.ValidationPasswordRequired)
                .MaximumLength(MAX_LENGTH_PASSWORD)
                .Matches(CREDENTIAL_POLICY_REGEX).WithMessage(Lang.ValidationPasswordPolicy);

            RuleFor(vm => vm.IsFemale)
                .Must((vm, _) => vm.IsFemale || vm.IsMale || vm.IsOther || vm.IsPreferNotToSay)
                .WithMessage(Lang.ValidationGenderRequired)
                .When(vm => !vm.IsFemale && !vm.IsMale && !vm.IsOther && !vm.IsPreferNotToSay, ApplyConditionTo.CurrentValidator);
        }

        private static bool beValidAge(DateTime? birthDate)
        {
            if (!birthDate.HasValue) return false;
            var date = birthDate.Value.Date;
            var today = DateTime.Today;
            DateTime maxDateForMinAge = today.AddYears(-MIN_AGE_REQUIRED);
            DateTime minDateForMaxAge = today.AddYears(-MAX_AGE_ALLOWED);
            return date <= maxDateForMinAge && date >= minDateForMaxAge;
        }
    }
}