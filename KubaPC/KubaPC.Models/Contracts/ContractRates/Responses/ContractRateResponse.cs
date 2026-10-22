namespace KubaPC.Models.Contracts.ContractRates.Responses
{
    public class ContractRateResponse
    {
        public Guid Id { get; set; }
        public Guid ContractId { get; set; }
        public string ContractNumber { get; set; } = string.Empty;
        public Guid OperatorId { get; set; }
        public string OperatorName { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public decimal OperatedRatePerKm { get; set; }
        public decimal StandingRatePerKm { get; set; }
        public DateOnly EffectiveFromDate { get; set; }
        public DateOnly? EffectiveToDate { get; set; }
        public bool IsCurrent { get; set; }
        public bool IsActive { get; set; }
    }
}
