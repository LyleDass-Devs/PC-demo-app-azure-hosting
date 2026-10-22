namespace KubaPC.Models.Contracts.PenaltyCaptures.Responses
{
    public class MatchingCaptureResponse
    {
        public Guid? Id { get; set; }
        public string CapturedByRole { get; set; } = string.Empty;
        public string CapturedByName { get; set; } = string.Empty;
        public string CaptureStatus { get; set; } = string.Empty;
        public string PenaltyCode { get; set; } = string.Empty;
        public DateTime? CapturedAt { get; set; }
        public bool IsUndecided { get; set; }
    }
}
