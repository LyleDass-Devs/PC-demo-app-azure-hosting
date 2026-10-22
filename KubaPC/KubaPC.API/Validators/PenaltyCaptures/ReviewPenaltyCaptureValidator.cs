using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.PenaltyCaptures.Requests;

namespace KubaPC.API.Validators.PenaltyCaptures
{
    public class ReviewPenaltyCaptureValidator : Validator<ReviewPenaltyCaptureRequest>
    {
        public ReviewPenaltyCaptureValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.CaptureStatus).NotEmpty().WithMessage("Choose a decision.");
            RuleFor(x => x.StatusReason).MaximumLength(500);
            RuleFor(x => x.StatusReason)
                .NotEmpty()
                .When(x => x.CaptureStatus is "Rejected" or "Under Dispute")
                .WithMessage("Give a reason for this decision.");
        }
    }
}
