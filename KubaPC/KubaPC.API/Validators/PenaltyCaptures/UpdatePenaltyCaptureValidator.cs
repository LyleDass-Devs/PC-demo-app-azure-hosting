using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.PenaltyCaptures.Requests;

namespace KubaPC.API.Validators.PenaltyCaptures
{
    public class UpdatePenaltyCaptureValidator : Validator<UpdatePenaltyCaptureRequest>
    {
        public UpdatePenaltyCaptureValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RouteId).NotEmpty().WithMessage("Choose a route.");
            RuleFor(x => x.TripId).NotEmpty().WithMessage("Choose a trip.");
            RuleFor(x => x.PenaltyListItemId).NotEmpty().WithMessage("Choose a penalty.");
            RuleFor(x => x.TripDetail).MaximumLength(500);
            RuleFor(x => x.Reason).NotEmpty().WithMessage("Give a reason for the change.").MaximumLength(500);
        }
    }
}
