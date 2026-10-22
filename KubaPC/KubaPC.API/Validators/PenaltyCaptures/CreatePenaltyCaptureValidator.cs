using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.PenaltyCaptures.Requests;

namespace KubaPC.API.Validators.PenaltyCaptures
{
    public class CreatePenaltyCaptureValidator : Validator<CreatePenaltyCaptureRequest>
    {
        public CreatePenaltyCaptureValidator()
        {
            RuleFor(x => x.RouteId).NotEmpty().WithMessage("Choose a route.");
            RuleFor(x => x.TripId).NotEmpty().WithMessage("Choose a trip.");
            RuleFor(x => x.PenaltyListItemId).NotEmpty().WithMessage("Choose a penalty.");
            RuleFor(x => x.TripDetail).MaximumLength(500);
        }
    }
}
