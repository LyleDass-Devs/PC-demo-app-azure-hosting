namespace KubaPC.Models.Contracts.PenaltyCaptures.Requests
{
    public class DeletePenaltyCaptureRequest
    {
        public Guid Id { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
