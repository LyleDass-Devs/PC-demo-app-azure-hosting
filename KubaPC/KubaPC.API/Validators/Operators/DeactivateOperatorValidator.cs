using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.Operators.Requests;

namespace KubaPC.API.Validators.Operators
{
    public class DeactivateOperatorValidator : Validator<DeactivateOperatorRequest>
    {
        public DeactivateOperatorValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
