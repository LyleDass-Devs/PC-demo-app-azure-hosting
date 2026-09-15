using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.Accounts.Requests;

namespace KubaPC.API.Validators.Accounts
{
    public class UpdateOwnAccountValidator : Validator<UpdateOwnAccountRequest>
    {
        public UpdateOwnAccountValidator()
        {
            RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        }
    }
}
