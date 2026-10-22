using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.FormF1.Requests;

namespace KubaPC.API.Validators.FormF1
{
    public class UpdateFormF1RecordValidator : Validator<UpdateFormF1RecordRequest>
    {
        public UpdateFormF1RecordValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.RouteId).NotEmpty().WithMessage("Choose a route.");
            RuleFor(x => x.TripId).NotEmpty().WithMessage("Choose a trip.");
            RuleFor(x => x.RecordType).NotEmpty().WithMessage("Choose what happened.");
            RuleFor(x => x.Reason).NotEmpty().WithMessage("Describe what happened.").MaximumLength(500);
            RuleFor(x => x.DnoExplanation).MaximumLength(500);
            RuleFor(x => x.ChangeReason).NotEmpty().WithMessage("Give a reason for the change.").MaximumLength(500);
        }
    }
}
