using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.FormF1.Requests;

namespace KubaPC.API.Validators.FormF1
{
    public class DeleteFormF1RecordValidator : Validator<DeleteFormF1RecordRequest>
    {
        public DeleteFormF1RecordValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.Reason).NotEmpty().WithMessage("Give a reason for removing the record.").MaximumLength(500);
        }
    }
}
