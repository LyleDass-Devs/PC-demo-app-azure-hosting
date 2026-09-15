using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.Contracts.Requests;

namespace KubaPC.API.Validators.Contracts
{
    public class DeactivateContractValidator : Validator<DeactivateContractRequest>
    {
        public DeactivateContractValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
