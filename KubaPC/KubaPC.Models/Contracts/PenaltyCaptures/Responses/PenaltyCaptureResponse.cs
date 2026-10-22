namespace KubaPC.Models.Contracts.PenaltyCaptures.Responses
{
    public class PenaltyCaptureResponse
    {
        public Guid Id { get; set; }
        public Guid? PaymentCertificateId { get; set; }
        public Guid RouteId { get; set; }
        public string RouteNumber { get; set; } = string.Empty;
        public Guid TripId { get; set; }
        public string TripNumber { get; set; } = string.Empty;
        public DateOnly TripDate { get; set; }
        public Guid PenaltyListItemId { get; set; }
        public string PenaltyCode { get; set; } = string.Empty;
        public string PenaltyDescription { get; set; } = string.Empty;
        public string PenaltyType { get; set; } = string.Empty;
        public decimal? RandAmount { get; set; }
        public Guid? VehicleId { get; set; }
        public string? VehicleNumber { get; set; }
        public string? TripDetail { get; set; }
        public string CapturedByRole { get; set; } = string.Empty;
        public string CapturedByName { get; set; } = string.Empty;
        public Guid CapturedById { get; set; }
        public string CaptureStatus { get; set; } = string.Empty;
        public string? StatusReason { get; set; }

        public decimal? EffectiveRandAmount { get; set; }

        public bool IsRepeatEscalationApplied { get; set; }
        public bool IsMine { get; set; }
        public string? StatusUpdatedByName { get; set; }
        public DateTime? StatusUpdatedAt { get; set; }
    }
}
