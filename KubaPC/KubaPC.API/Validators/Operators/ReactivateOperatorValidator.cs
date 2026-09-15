using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.Operators.Requests;

namespace KubaPC.API.Validators.Operators
{
    public class ReactivateOperatorValidator : Validator<ReactivateOperatorRequest>
    {
        public ReactivateOperatorValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
