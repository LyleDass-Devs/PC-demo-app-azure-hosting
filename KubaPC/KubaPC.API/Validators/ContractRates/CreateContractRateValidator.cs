using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.ContractRates.Requests;

namespace KubaPC.API.Validators.ContractRates
{
    public class CreateContractRateValidator : Validator<CreateContractRateRequest>
    {
        private const decimal HIGHEST_SENSIBLE_RATE = 10000m;

        public CreateContractRateValidator()
        {
            RuleFor(x => x.ContractId).NotEmpty().WithMessage("Choose a contract.");
            RuleFor(x => x.VehicleType).NotEmpty().WithMessage("Choose a vehicle type.").MaximumLength(50);
            RuleFor(x => x.OperatedRatePerKm).GreaterThan(0).WithMessage("Enter the operated rate.").LessThanOrEqualTo(HIGHEST_SENSIBLE_RATE);
            RuleFor(x => x.StandingRatePerKm).GreaterThan(0).WithMessage("Enter the standing rate.").LessThanOrEqualTo(HIGHEST_SENSIBLE_RATE);
            RuleFor(x => x.EffectiveFromDate).NotEqual(default(DateOnly)).WithMessage("Choose the date this rate starts.");
        }
    }
}
