namespace KubaPC.Models.Contracts.PenaltyCaptures.Requests
{
    public class ReviewPenaltyCaptureRequest
    {
        public Guid Id { get; set; }
        public string CaptureStatus { get; set; } = string.Empty;
        public string? StatusReason { get; set; }
        public bool CascadeToMatching { get; set; }
    }
}
