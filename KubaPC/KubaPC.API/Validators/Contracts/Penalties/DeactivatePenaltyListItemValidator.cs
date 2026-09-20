using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.Contracts.Penalties.Requests;

namespace KubaPC.API.Validators.Contracts.Penalties
{
    public class DeactivatePenaltyListItemValidator : Validator<DeactivatePenaltyListItemRequest>
    {
        public DeactivatePenaltyListItemValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
