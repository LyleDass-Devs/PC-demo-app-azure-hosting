namespace KubaPC.Models.Contracts.FormF1.Requests
{
    public class ReviewFormF1RecordRequest
    {
        public Guid Id { get; set; }
        public string RecordStatus { get; set; } = string.Empty;
        public string? StatusReason { get; set; }
        public bool CascadeToMatching { get; set; }
    }
}
