using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.FormF1.Requests;

namespace KubaPC.API.Validators.FormF1
{
    public class CreateFormF1RecordValidator : Validator<CreateFormF1RecordRequest>
    {
        public CreateFormF1RecordValidator()
        {
            RuleFor(x => x.RouteId).NotEmpty().WithMessage("Choose a route.");
            RuleFor(x => x.TripId).NotEmpty().WithMessage("Choose a trip.");
            RuleFor(x => x.RecordType).NotEmpty().WithMessage("Choose what happened.").MaximumLength(50);
            RuleFor(x => x.Reason).NotEmpty().WithMessage("Describe what happened.").MaximumLength(500);
            RuleFor(x => x.DnoExplanation).MaximumLength(500);
        }
    }
}
