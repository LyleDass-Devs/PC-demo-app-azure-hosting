using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.Operators.Requests;

namespace KubaPC.API.Validators.Operators
{
    public class CreateOperatorValidator : Validator<CreateOperatorRequest>
    {
        public CreateOperatorValidator()
        {
            RuleFor(x => x.IdSociete).NotEmpty().WithMessage("Enter the company code.").MaximumLength(50);
            RuleFor(x => x.OperatorName).NotEmpty().WithMessage("Enter the operator name.").MaximumLength(200);
        }
    }
}
