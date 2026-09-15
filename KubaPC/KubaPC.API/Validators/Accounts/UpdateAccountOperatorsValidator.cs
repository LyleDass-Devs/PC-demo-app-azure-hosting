using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.Accounts.Requests;

namespace KubaPC.API.Validators.Accounts
{
    public class UpdateAccountOperatorsValidator : Validator<UpdateAccountOperatorsRequest>
    {
        public UpdateAccountOperatorsValidator()
        {
            RuleFor(x => x.AccountId).NotEmpty();
            RuleFor(x => x.OperatorIds).NotNull();
        }
    }
}
