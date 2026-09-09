using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.Accounts.Requests;

namespace KubaPC.API.Validators.Accounts
{
    public class DeactivateAccountValidator : Validator<DeactivateAccountRequest>
    {
        public DeactivateAccountValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}