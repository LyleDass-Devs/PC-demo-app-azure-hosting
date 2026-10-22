namespace KubaPC.Models.Contracts.PenaltyCaptures.Requests
{
    public class CaptureLookupRequest
    {
        public Guid ContractId { get; set; }
        public string? PenaltyType { get; set; }
    }
}
