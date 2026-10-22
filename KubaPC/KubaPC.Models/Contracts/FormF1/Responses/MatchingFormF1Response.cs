namespace KubaPC.Models.Contracts.FormF1.Responses
{
    public class MatchingFormF1Response
    {
        public Guid? Id { get; set; }
        public string CapturedByRole { get; set; } = string.Empty;
        public string CapturedByName { get; set; } = string.Empty;
        public string RecordStatus { get; set; } = string.Empty;
        public string RecordType { get; set; } = string.Empty;
        public DateTime? CapturedAt { get; set; }
        public bool IsUndecided { get; set; }
    }
}
