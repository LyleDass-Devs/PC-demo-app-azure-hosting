namespace KubaPC.Models.Contracts.DnoReasons.Responses
{
    public class DnoReasonResponse
    {
        public Guid Id { get; set; }
        public string ReasonValue { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int UseCount { get; set; }
    }
}
