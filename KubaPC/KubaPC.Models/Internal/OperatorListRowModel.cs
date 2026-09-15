namespace KubaPC.Models.Internal
{
    /// <summary>Raw row from the operator list query, including the window total.</summary>
    public class OperatorListRowModel
    {
        public Guid Id { get; set; }
        public string IdSociete { get; set; } = string.Empty;
        public string OperatorName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int VehicleCount { get; set; }
        public int ContractCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public long TotalCount { get; set; }
    }
}
