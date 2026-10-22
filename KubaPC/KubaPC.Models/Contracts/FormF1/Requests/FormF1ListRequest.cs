using KubaPC.Models.Contracts.Common.Requests;

namespace KubaPC.Models.Contracts.FormF1.Requests
{
    public class FormF1ListRequest : PagedListRequest
    {
        public Guid? PaymentCertificateId { get; set; }
        public Guid? TripId { get; set; }
        public Guid? ContractId { get; set; }
        public string? RecordType { get; set; }
        public string? RecordStatus { get; set; }
        public string? CapturedByRole { get; set; }
    }
}
