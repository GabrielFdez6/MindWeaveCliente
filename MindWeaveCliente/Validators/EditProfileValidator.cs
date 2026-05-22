using FluentValidation;
using MindWeaveClient.Properties.Langs;
using MindWeaveClient.ViewModel.Main;
using System;

namespace MindWeaveClient.Validators
{
    public class EditProfileValidator : AbstractValidator<EditProfileViewModel>
    {
        private const string PROFILE = "Profile";
        private const string PASSWORD = "Password";
        private const string CREDENTIAL_POLICY_REGEX = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!@#\\$%^&*(),.?\"":{}|<>]).{8,}$";
        private const string NAME_VALIDATOR_REGEX = @"^(?=.*\p{L})[\p{L}\p{M} '\-\.]+$";


        private const int MAX_NAME_LENGTH = 45;
        private const int MIN_NAME_LENGTH = 3;
        private const int MAX_PASSWORD_INPUT_LENGTH = 128;
        private const int MIN_AGE = -13;

        public EditProfileValidator()
        {
            RuleSet(PROFILE, () =>
            {
                RuleFor(x => x.FirstName)
                    .NotEmpty().WithMessage(Lang.ValidationFirstNameRequired)
                    .Length(MIN_NAME_LENGTH, MAX_NAME_LENGTH).WithMessage(Lang.ValidationFirstNameLength)
                    .Matches(NAME_VALIDATOR_REGEX).WithMessage(Lang.ValidationNameInvalidCharacters);

                RuleFor(x => x.LastName)
                    .NotEmpty().WithMessage(Lang.ValidationLastNameRequired)
                    .Length(MIN_NAME_LENGTH, MAX_NAME_LENGTH).WithMessage(Lang.ValidationLastNameLength)
                    .Matches(NAME_VALIDATOR_REGEX).WithMessage(Lang.ValidationNameInvalidCharacters);

                RuleFor(x => x.DateOfBirth)
                    .NotNull().WithMessage(Lang.ValidationBirthDateRequired)
                    .LessThan(DateTime.Now.AddYears(MIN_AGE)).WithMessage(Lang.ValidationBirthDateInvalid);
            });



            RuleSet(PASSWORD, () =>
            {
                RuleFor(x => x.CurrentPassword)
                    .NotEmpty().WithMessage(Lang.ValidationCurrentPasswordRequired)
                    .MaximumLength(MAX_PASSWORD_INPUT_LENGTH);

                RuleFor(x => x.NewPassword)
                    .NotEmpty().WithMessage(Lang.ValidationPasswordRequired)
                    .MaximumLength(MAX_PASSWORD_INPUT_LENGTH)
                    .Matches(CREDENTIAL_POLICY_REGEX).WithMessage(Lang.ValidationPasswordPolicy);

                RuleFor(x => x.ConfirmPassword)
                    .Equal(x => x.NewPassword).WithMessage(Lang.ValidationPasswordsDoNotMatch)
                    .MaximumLength(MAX_PASSWORD_INPUT_LENGTH);
            });


        }
    }
}