using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.Penalties.Requests;

namespace KubaPC.API.Validators.Penalties
{
    public class ReactivatePenaltyListItemValidator : Validator<ReactivatePenaltyListItemRequest>
    {
        public ReactivatePenaltyListItemValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
