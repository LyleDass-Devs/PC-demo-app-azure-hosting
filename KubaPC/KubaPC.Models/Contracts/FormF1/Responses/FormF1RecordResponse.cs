namespace KubaPC.Models.Contracts.FormF1.Responses
{
    public class FormF1RecordResponse
    {
        public Guid Id { get; set; }
        public Guid? PaymentCertificateId { get; set; }
        public Guid RouteId { get; set; }
        public string RouteNumber { get; set; } = string.Empty;
        public Guid TripId { get; set; }
        public string TripNumber { get; set; } = string.Empty;
        public DateOnly TripDate { get; set; }
        public string RecordType { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public Guid? DnoReasonId { get; set; }
        public string? DnoReasonValue { get; set; }
        public string? DnoExplanation { get; set; }
        public string CapturedByRole { get; set; } = string.Empty;
        public string CapturedByName { get; set; } = string.Empty;
        public Guid CapturedById { get; set; }
        public string RecordStatus { get; set; } = string.Empty;
        public string? StatusReason { get; set; }
        public string? StatusUpdatedByName { get; set; }
        public DateTime? StatusUpdatedAt { get; set; }
        public bool IsMine { get; set; }
    }
}
