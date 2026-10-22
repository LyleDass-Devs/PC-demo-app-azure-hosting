namespace KubaPC.Models.Contracts.FormF1.Requests
{
    public class CreateFormF1RecordRequest
    {
        public Guid? PaymentCertificateId { get; set; }
        public Guid RouteId { get; set; }
        public Guid TripId { get; set; }
        public string RecordType { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public Guid? DnoReasonId { get; set; }
        public string? DnoExplanation { get; set; }
    }
}
