using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.Contracts.Requests;

namespace KubaPC.API.Validators.Contracts
{
    public class CreateContractValidator : Validator<CreateContractRequest>
    {
        public CreateContractValidator()
        {
            RuleFor(x => x.OperatorId).NotEmpty().WithMessage("Choose an operator.");
            RuleFor(x => x.ContractNumber).NotEmpty().WithMessage("Enter the contract number.").MaximumLength(50);
            RuleFor(x => x.ContractName).MaximumLength(200);
            RuleFor(x => x.CommencementDate).NotEmpty().WithMessage("Enter the commencement date.");
            RuleFor(x => x.ExpiryDate).GreaterThan(x => x.CommencementDate).When(x => x.ExpiryDate.HasValue)
                .WithMessage("The expiry date must be after the commencement date.");
            RuleFor(x => x.BankAccountNumber).NotEmpty().When(HasAnyBankDetail)
                .WithMessage("Enter the account number, or clear all bank details.");
            RuleFor(x => x.BankName).NotEmpty().When(HasAnyBankDetail)
                .WithMessage("Enter the bank name, or clear all bank details.");
            RuleFor(x => x.BankAccountName).NotEmpty().When(HasAnyBankDetail)
                .WithMessage("Enter the account name, or clear all bank details.");
            RuleFor(x => x.BankBranchCode).NotEmpty().When(HasAnyBankDetail)
                .WithMessage("Enter the branch code, or clear all bank details.");
        }

        // Bank details are optional as a block: none at all, or the four that Form A prints.
        private static bool HasAnyBankDetail(CreateContractRequest x) =>
            !string.IsNullOrWhiteSpace(x.BankName) || !string.IsNullOrWhiteSpace(x.BankBranchName) || !string.IsNullOrWhiteSpace(x.BankBranchCode)
            || !string.IsNullOrWhiteSpace(x.BankAccountName) || !string.IsNullOrWhiteSpace(x.BankAccountNumber) || !string.IsNullOrWhiteSpace(x.BankAccountType);
    }
}
