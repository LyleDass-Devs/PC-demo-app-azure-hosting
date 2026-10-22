using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.IncomeFields.Requests;

namespace KubaPC.API.Validators.IncomeFields
{
    public class SaveIncomeFieldsValidator : Validator<SaveIncomeFieldsRequest>
    {
        public SaveIncomeFieldsValidator()
        {
            RuleFor(x => x.ContractId).NotEmpty().WithMessage("Choose a contract.");
        }
    }
}
