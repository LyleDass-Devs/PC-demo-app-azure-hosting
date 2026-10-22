using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.PenaltyCaptures.Requests;

namespace KubaPC.API.Validators.PenaltyCaptures
{
    public class DeletePenaltyCaptureValidator : Validator<DeletePenaltyCaptureRequest>
    {
        public DeletePenaltyCaptureValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Reason).NotEmpty().WithMessage("Give a reason for removing the capture.").MaximumLength(500);
        }
    }
}
