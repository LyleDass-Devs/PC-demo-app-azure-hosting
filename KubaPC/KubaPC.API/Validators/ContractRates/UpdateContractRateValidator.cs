using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.ContractRates.Requests;

namespace KubaPC.API.Validators.ContractRates
{
    public class UpdateContractRateValidator : Validator<UpdateContractRateRequest>
    {
        private const decimal HIGHEST_SENSIBLE_RATE = 10000m;

        public UpdateContractRateValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.OperatedRatePerKm).GreaterThan(0).WithMessage("Enter the operated rate.").LessThanOrEqualTo(HIGHEST_SENSIBLE_RATE);
            RuleFor(x => x.StandingRatePerKm).GreaterThan(0).WithMessage("Enter the standing rate.").LessThanOrEqualTo(HIGHEST_SENSIBLE_RATE);
            RuleFor(x => x.EffectiveFromDate).NotEqual(default(DateOnly)).WithMessage("Choose the date this rate starts.");
        }
    }
}
