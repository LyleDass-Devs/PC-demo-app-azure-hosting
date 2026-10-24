using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.Accounts.Requests;

namespace KubaPC.API.Validators.Accounts
{
    public class CreateAccountValidator :Validator<CreateAccountRequest>
    {
        private const int MINIMUM_PASSWORD_LENGTH = 8;
        private const int MAXIMUM_PASSWORD_LENGTH = 72;
        public CreateAccountValidator()
        {
            RuleFor(x => x.EmailAddress).NotEmpty().MaximumLength(320);
            RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Password).NotEmpty().MinimumLength(MINIMUM_PASSWORD_LENGTH).WithMessage($"Password must be at least {MINIMUM_PASSWORD_LENGTH} characters.")
                .MaximumLength(MAXIMUM_PASSWORD_LENGTH)
                .Matches("[A-Za-z]").WithMessage("The password must contain at least one letter.")
                .Matches("[0-9]").WithMessage("The password must contain at least one digit.");
            RuleFor(x => x.RoleNames).NotEmpty();
            RuleForEach(x => x.RoleNames).NotEmpty().MaximumLength(50);
        }
    }
}
