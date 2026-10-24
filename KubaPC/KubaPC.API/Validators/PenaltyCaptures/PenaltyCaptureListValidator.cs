using FluentValidation;
using KubaPC.API.Validators.Common;
using KubaPC.Models.Contracts.PenaltyCaptures.Requests;

namespace KubaPC.API.Validators.PenaltyCaptures
{
    public class PenaltyCaptureListValidator : PagedListValidator<PenaltyCaptureListRequest>
    {
        public PenaltyCaptureListValidator()
        {
            RuleFor(x => x.CapturedByRole).MaximumLength(50);
            RuleFor(x => x.CaptureStatus).MaximumLength(50);
        }
    }
}
