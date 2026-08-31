using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.Accounts.Requests;

namespace KubaPC.API.Validators.Accounts
{
    public class CreateAccountValidator :Validator<CreateAccountRequest>
    {
        private const int MINIMUM_PASSWORD_LENGTH = 8;
        public CreateAccountValidator()
        {
            RuleFor(x => x.EmailAddress).NotEmpty().MaximumLength(320);
            RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.Password).NotEmpty().MinimumLength(MINIMUM_PASSWORD_LENGTH).WithMessage($"Password must be at least {MINIMUM_PASSWORD_LENGTH} characters.");
            RuleFor(x => x.RoleNames).NotEmpty();
        }
    }
}
