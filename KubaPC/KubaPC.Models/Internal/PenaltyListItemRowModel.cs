namespace KubaPC.Models.Internal
{
    public class PenaltyListItemRowModel
    {
        public Guid Id { get; set; }
        public Guid ContractId { get; set; }
        public string PenaltyCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PenaltyType { get; set; } = string.Empty;
        public string? RevenueType { get; set; }
        public long? RandAmountMinor { get; set; }
        public string? PenaltyFormula { get; set; }
        public bool RepeatPenaltyEscalationEnabled { get; set; }
        public bool IsActive { get; set; }
        public int CaptureCount { get; set; }
        public long TotalCount { get; set; }
    }
}
