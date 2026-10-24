using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.Accounts.Requests;

namespace KubaPC.API.Validators.Accounts
{
    public class UpdateAccountOperatorsValidator : Validator<UpdateAccountOperatorsRequest>
    {
        private const int MAXIMUM_OPERATORS = 50;

        public UpdateAccountOperatorsValidator()
        {
            RuleFor(x => x.AccountId).NotEmpty();
            RuleFor(x => x.OperatorIds).NotNull().Must(ids => ids.Count <= MAXIMUM_OPERATORS).WithMessage("Too many operators.");
        }
    }
}
