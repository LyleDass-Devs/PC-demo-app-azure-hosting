using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.Contracts.Requests;

namespace KubaPC.API.Validators.Contracts
{
    public class UpdateContractValidator : Validator<UpdateContractRequest>
    {
        public UpdateContractValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.ContractNumber).NotEmpty().WithMessage("Enter the contract number.").MaximumLength(50);
            RuleFor(x => x.ContractName).MaximumLength(200);
            RuleFor(x => x.BankName).MaximumLength(100);
            RuleFor(x => x.BankBranchName).MaximumLength(100);
            RuleFor(x => x.BankBranchCode).MaximumLength(20);
            RuleFor(x => x.BankAccountName).MaximumLength(100);
            RuleFor(x => x.BankAccountNumber).MaximumLength(30);
            RuleFor(x => x.BankAccountType).MaximumLength(30);
            RuleFor(x => x.CommencementDate).NotEmpty().WithMessage("Enter the commencement date.");
            RuleFor(x => x.ExpiryDate).GreaterThan(x => x.CommencementDate).When(x => x.ExpiryDate.HasValue)
                .WithMessage("The expiry date must be after the commencement date.");
            RuleFor(x => x.BankAccountNumber).Matches(@"^[0-9A-Za-z -]+$").When(x => !string.IsNullOrWhiteSpace(x.BankAccountNumber))
                .WithMessage("Enter the account number, or clear all bank details.");
            RuleFor(x => x.BankName).NotEmpty().When(HasAnyBankDetail)
                .WithMessage("Enter the bank name, or clear all bank details.");
            RuleFor(x => x.BankAccountName).NotEmpty().When(HasAnyBankDetail)
                .WithMessage("Enter the account name, or clear all bank details.");
            RuleFor(x => x.BankBranchCode).NotEmpty().When(HasAnyBankDetail)
                .WithMessage("Enter the branch code, or clear all bank details.");
        }

        private static bool HasAnyBankDetail(UpdateContractRequest x) =>
            !string.IsNullOrWhiteSpace(x.BankName) || !string.IsNullOrWhiteSpace(x.BankBranchName) || !string.IsNullOrWhiteSpace(x.BankBranchCode)
            || !string.IsNullOrWhiteSpace(x.BankAccountName) || !string.IsNullOrWhiteSpace(x.BankAccountNumber) || !string.IsNullOrWhiteSpace(x.BankAccountType);
    }
}
