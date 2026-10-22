namespace KubaPC.Models.Contracts.PenaltyCaptures.Requests
{
    public class TripLookupRequest
    {
        public Guid RouteId { get; set; }
        public Guid? PaymentCertificateId { get; set; }
        public DateOnly? TripDate { get; set; }
    }
}
