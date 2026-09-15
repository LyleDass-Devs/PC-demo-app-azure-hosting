namespace KubaPC.Models.Contracts.Operators.Responses
{
    /// <summary>One operator as shown in the list. IdSociete is the company code the SAEIV feed uses.</summary>
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
