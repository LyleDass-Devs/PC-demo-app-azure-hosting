namespace KubaPC.Models.Contracts.FormF1.Requests
{
    public class UpdateFormF1RecordRequest
    {
        public Guid Id { get; set; }
        public Guid RouteId { get; set; }
        public Guid TripId { get; set; }
        public string RecordType { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public Guid? DnoReasonId { get; set; }
        public string? DnoExplanation { get; set; }
        public string ChangeReason { get; set; } = string.Empty;
    }
}
