namespace KubaPC.Models.Contracts.ContractRates.Requests
{
    public class CreateContractRateRequest
    {
        public Guid ContractId { get; set; }
        public string VehicleType { get; set; } = string.Empty;
        public decimal OperatedRatePerKm { get; set; }
        public decimal StandingRatePerKm { get; set; }
        public DateOnly EffectiveFromDate { get; set; }
    }
}
