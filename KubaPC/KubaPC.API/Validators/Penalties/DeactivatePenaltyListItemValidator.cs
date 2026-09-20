using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.Penalties.Requests;

namespace KubaPC.API.Validators.Penalties
{
    public class DeactivatePenaltyListItemValidator : Validator<DeactivatePenaltyListItemRequest>
    {
        public DeactivatePenaltyListItemValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
