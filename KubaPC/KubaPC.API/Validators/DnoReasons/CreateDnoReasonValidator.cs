using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.DnoReasons.Requests;

namespace KubaPC.API.Validators.DnoReasons
{
    public class CreateDnoReasonValidator : Validator<CreateDnoReasonRequest>
    {
        public CreateDnoReasonValidator()
        {
            RuleFor(x => x.ReasonValue).NotEmpty().WithMessage("Enter the reason.").MaximumLength(100);
        }
    }
}
