using FluentValidation;
using KubaPC.API.Validators.Common;
using KubaPC.Models.Contracts.FormF1.Requests;

namespace KubaPC.API.Validators.FormF1
{
    public class FormF1ListValidator : PagedListValidator<FormF1ListRequest>
    {
        public FormF1ListValidator()
        {
            RuleFor(x => x.RecordType).MaximumLength(50);
            RuleFor(x => x.RecordStatus).MaximumLength(50);
            RuleFor(x => x.CapturedByRole).MaximumLength(50);
        }
    }
}
