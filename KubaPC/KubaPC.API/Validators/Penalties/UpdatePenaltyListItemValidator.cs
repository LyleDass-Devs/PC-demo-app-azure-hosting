using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.Penalties.Requests;

namespace KubaPC.API.Validators.Penalties
{
    public class UpdatePenaltyListItemValidator : Validator<UpdatePenaltyListItemRequest>
    {
        public UpdatePenaltyListItemValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.PenaltyCode).NotEmpty().WithMessage("Enter the penalty code.").MaximumLength(50);
            RuleFor(x => x.Description).NotEmpty().WithMessage("Enter a description.").MaximumLength(500);
            RuleFor(x => x.PenaltyType).NotEmpty().WithMessage("Choose a penalty type.");
            RuleFor(x => x.RandAmount).GreaterThan(0).When(x => x.RandAmount.HasValue).WithMessage("The amount must be more than zero.");
            RuleFor(x => x.PenaltyFormula).MaximumLength(500);
            RuleFor(x => x)
                .Must(x => x.RandAmount.HasValue || !string.IsNullOrWhiteSpace(x.PenaltyFormula))
                .WithMessage("Give a fixed amount or a formula, or both.")
                .WithName("randAmount");
        }
    }
}
