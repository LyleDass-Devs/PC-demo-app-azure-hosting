using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.Contracts.Requests;

namespace KubaPC.API.Validators.Contracts
{
    public class GetContractValidator : Validator<GetContractRequest>
    {
        public GetContractValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
