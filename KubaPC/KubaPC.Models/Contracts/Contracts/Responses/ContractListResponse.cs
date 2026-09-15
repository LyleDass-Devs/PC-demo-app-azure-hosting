namespace KubaPC.Models.Contracts.Contracts.Responses
{
    public class ContractListResponse
    {
        public Guid Id { get; set; }
        public Guid OperatorId { get; set; }
        public string OperatorName { get; set; } = string.Empty;
        public string ContractNumber { get; set; } = string.Empty;
        public string? ContractName { get; set; }
        public DateOnly CommencementDate { get; set; }
        public DateOnly? ExpiryDate { get; set; }
        public bool IsActive { get; set; }
        public int RateCount { get; set; }
        public int CertificateCount { get; set; }
        public string? BankName { get; set; }
        public string? BankBranchName { get; set; }
        public string? BankBranchCode { get; set; }
        public string? BankAccountName { get; set; }
        public string? BankAccountNumber { get; set; }
        public string? BankAccountType { get; set; }
        public bool HasBankDetails => !string.IsNullOrWhiteSpace(BankAccountNumber);
    }
}
