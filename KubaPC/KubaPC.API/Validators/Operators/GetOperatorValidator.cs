using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.Operators.Requests;

namespace KubaPC.API.Validators.Operators
{
    public class GetOperatorValidator : Validator<GetOperatorRequest>
    {
        public GetOperatorValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
