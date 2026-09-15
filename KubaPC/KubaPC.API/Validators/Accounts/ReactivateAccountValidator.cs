using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.Accounts.Requests;

namespace KubaPC.API.Validators.Accounts
{
    public class ReactivateAccountValidator : Validator<ReactivateAccountRequest>
    {
        public ReactivateAccountValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
