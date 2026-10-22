using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.IncomeFields.Requests;

namespace KubaPC.API.Validators.IncomeFields
{
    public class GetIncomeFieldsValidator : Validator<GetIncomeFieldsRequest>
    {
        public GetIncomeFieldsValidator()
        {
            RuleFor(x => x.ContractId).NotEmpty();
        }
    }
}
