using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.FormF1.Requests;

namespace KubaPC.API.Validators.FormF1
{
    public class ReviewFormF1RecordValidator : Validator<ReviewFormF1RecordRequest>
    {
        public ReviewFormF1RecordValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RecordStatus).NotEmpty().WithMessage("Choose a decision.").MaximumLength(50);
            RuleFor(x => x.StatusReason).MaximumLength(500);
            RuleFor(x => x.StatusReason)
                .NotEmpty()
                .When(x => x.RecordStatus is "Rejected" or "Under Dispute")
                .WithMessage("Give a reason for this decision.");
        }
    }
}
