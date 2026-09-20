using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.DnoReasons.Requests;

namespace KubaPC.API.Validators.DnoReasons
{
    public class ReactivateDnoReasonValidator : Validator<ReactivateDnoReasonRequest>
    {
        public ReactivateDnoReasonValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
        }
    }
}
