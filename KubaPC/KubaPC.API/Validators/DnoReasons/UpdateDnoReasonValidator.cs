using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.DnoReasons.Requests;

namespace KubaPC.API.Validators.DnoReasons
{
    public class UpdateDnoReasonValidator : Validator<UpdateDnoReasonRequest>
    {
        public UpdateDnoReasonValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.ReasonValue).NotEmpty().WithMessage("Enter the reason.").MaximumLength(100);
        }
    }
}
