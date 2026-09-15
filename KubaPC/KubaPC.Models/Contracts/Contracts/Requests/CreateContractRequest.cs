namespace KubaPC.Models.Contracts.Contracts.Requests
{
    /// <summary>
    /// A new contract for one operator. The contract number is the public reference that appears
    /// on every certificate. Bank details are optional but, when given, must be complete.
    /// </summary>
    public class CreateContractRequest
    {
        public Guid OperatorId { get; set; }
        public string ContractNumber { get; set; } = string.Empty;
        public string? ContractName { get; set; }
        public DateOnly CommencementDate { get; set; }
        public DateOnly? ExpiryDate { get; set; }
        public string? BankName { get; set; }
        public string? BankBranchName { get; set; }
        public string? BankBranchCode { get; set; }
        public string? BankAccountName { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? BankAccountType { get; set; }
    }
}
