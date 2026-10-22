namespace KubaPC.Models.Contracts.PenaltyCaptures.Requests
{
    public class UpdatePenaltyCaptureRequest
    {
        public Guid Id { get; set; }
        public Guid RouteId { get; set; }
        public Guid TripId { get; set; }
        public Guid PenaltyListItemId { get; set; }
        public Guid? VehicleId { get; set; }
        public string? TripDetail { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
