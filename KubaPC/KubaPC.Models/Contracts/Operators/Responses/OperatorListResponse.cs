namespace KubaPC.Models.Contracts.Operators.Responses
{
    public class OperatorListResponse
    {
        public Guid Id { get; set; }
        public string IdSociete { get; set; } = string.Empty;
        public string OperatorName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int VehicleCount { get; set; }
        public int ContractCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
