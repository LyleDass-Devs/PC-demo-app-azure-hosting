using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.Accounts.Requests;

namespace KubaPC.API.Validators.Accounts
{
    public class GetAccountOperatorsValidator : Validator<GetAccountOperatorsRequest>
    {
        public GetAccountOperatorsValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
