using KubaPC.Models.Contracts.Common.Requests;

namespace KubaPC.Models.Contracts.PenaltyCaptures.Requests
{
    public class PenaltyCaptureListRequest : PagedListRequest
    {
        public Guid? PaymentCertificateId { get; set; }
        public Guid? TripId { get; set; }
        public Guid? ContractId { get; set; }
        public string? CapturedByRole { get; set; }
        public string? CaptureStatus { get; set; }
    }
}
