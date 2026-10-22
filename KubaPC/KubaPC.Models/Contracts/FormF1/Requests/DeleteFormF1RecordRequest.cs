namespace KubaPC.Models.Contracts.FormF1.Requests
{
    public class DeleteFormF1RecordRequest
    {
        public Guid Id { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
