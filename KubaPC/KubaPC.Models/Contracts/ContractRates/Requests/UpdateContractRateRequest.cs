namespace KubaPC.Models.Contracts.ContractRates.Requests
{
    public class UpdateContractRateRequest
    {
        public Guid Id { get; set; }
        public decimal OperatedRatePerKm { get; set; }
        public decimal StandingRatePerKm { get; set; }
        public DateOnly EffectiveFromDate { get; set; }
    }
}
