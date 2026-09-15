namespace KubaPC.Models.Contracts.Penalties.Responses
{
    public class PenaltyTypeResponse
    {
        public string Value { get; set; } = string.Empty;
        public string EscalationCountingScope { get; set; } = string.Empty;
    }

    public class PenaltyLookupsResponse
    {
        public List<PenaltyTypeResponse> PenaltyTypes { get; set; } = new();
        public List<string> RevenueTypes { get; set; } = new();
    }
}
