using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.Contracts.Requests;

namespace KubaPC.API.Validators.Contracts
{
    public class ReactivateContractValidator : Validator<ReactivateContractRequest>
    {
        public ReactivateContractValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
