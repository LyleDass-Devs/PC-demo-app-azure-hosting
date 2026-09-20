using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.Escalations.Requests;

namespace KubaPC.API.Validators.Escalations
{
    public class GetEscalationParametersValidator : Validator<GetEscalationParametersRequest>
    {
        public GetEscalationParametersValidator()
        {
            RuleFor(x => x.ContractId).NotEmpty();
        }
    }
}
