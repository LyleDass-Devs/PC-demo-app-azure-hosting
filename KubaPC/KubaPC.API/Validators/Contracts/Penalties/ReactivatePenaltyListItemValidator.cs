using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.Contracts.Penalties.Requests;

namespace KubaPC.API.Validators.Contracts.Penalties
{
    public class ReactivatePenaltyListItemValidator : Validator<ReactivatePenaltyListItemRequest>
    {
        public ReactivatePenaltyListItemValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
