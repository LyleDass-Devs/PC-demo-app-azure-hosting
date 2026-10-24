using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.Accounts.Requests;

namespace KubaPC.API.Validators.Accounts
{
    public class CreateAccountValidator : Validator<CreateAccountRequest>
    {
        private const int MAXIMUM_ROLES = 20;

        public CreateAccountValidator()
        {
            RuleFor(x => x.EmailAddress).NotEmpty().EmailAddress().MaximumLength(320);
            RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.RoleNames).NotEmpty().Must(roles => roles.Count <= MAXIMUM_ROLES).WithMessage("Too many roles.");
            RuleForEach(x => x.RoleNames).NotEmpty().MaximumLength(50);
        }
    }
}
