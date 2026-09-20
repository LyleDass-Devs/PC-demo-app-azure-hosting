namespace KubaPC.Models.Contracts.DnoReasons.Requests
{
    public class UpdateDnoReasonRequest
    {
        public Guid Id { get; set; }
        public string ReasonValue { get; set; } = string.Empty;
    }
}
