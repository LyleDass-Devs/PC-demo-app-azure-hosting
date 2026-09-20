using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.Escalations.Requests;

namespace KubaPC.API.Validators.Escalations
{
    public class SaveEscalationParametersValidator : Validator<SaveEscalationParametersRequest>
    {
        private const int EARLIEST_BASE_YEAR = 1990;
        public SaveEscalationParametersValidator()
        {
            RuleFor(x => x.ContractId).NotEmpty().WithMessage("Choose a contract.");
            RuleFor(x => x.LabourWeight).InclusiveBetween(0, 1).WithMessage("A weight is between 0 and 1.");
            RuleFor(x => x.FuelWeight).InclusiveBetween(0, 1).WithMessage("A weight is between 0 and 1.");
            RuleFor(x => x.CpiWeight).InclusiveBetween(0, 1).WithMessage("A weight is between 0 and 1.");
            RuleFor(x => x.LabourWeight + x.FuelWeight + x.CpiWeight)
                .Equal(1.00m).WithMessage("The three weights must add up to exactly 1.00.")
                .WithName("cpiWeight");
            RuleFor(x => x.BaseMonth).InclusiveBetween(1, 12).WithMessage("Choose a month.");
            RuleFor(x => x.BaseYear).InclusiveBetween(EARLIEST_BASE_YEAR, DateTime.UtcNow.Year).WithMessage("Enter a base year.");
            RuleFor(x => x.LabourBaseValue).GreaterThan(0).WithMessage("Enter the labour base value.");
            RuleFor(x => x.FuelBaseValue).GreaterThan(0).WithMessage("Enter the fuel base value.");
            RuleFor(x => x.CpiBaseValue).GreaterThan(0).WithMessage("Enter the CPI base value.");
        }
    }
    
}
