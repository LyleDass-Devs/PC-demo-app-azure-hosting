namespace KubaPC.Models.Contracts.Contracts.Requests
{
    /// <summary>Corrects a contract. The operator it belongs to never changes.</summary>
    public class UpdateContractRequest
    {
        public Guid Id { get; set; }
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
