using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.Accounts.Requests;

namespace KubaPC.API.Validators.Accounts
{
    public class UpdateAccountValidator : Validator<UpdateAccountRequest>
    {
        public UpdateAccountValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.RoleNames).NotEmpty().WithMessage("An account must keep at least one role.");
            RuleForEach(x => x.RoleNames).NotEmpty();
        }

    }
}
