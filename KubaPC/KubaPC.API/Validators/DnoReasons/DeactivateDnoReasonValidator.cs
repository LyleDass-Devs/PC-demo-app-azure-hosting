using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.DnoReasons.Requests;

namespace KubaPC.API.Validators.DnoReasons
{
    public class DeactivateDnoReasonValidator : Validator<DeactivateDnoReasonRequest>
    {
        public DeactivateDnoReasonValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
