namespace KubaPC.Models.Contracts.PenaltyCaptures.Responses
{
    public class TripOptionResponse
    {
        public Guid Id { get; set; }
        public string TripNumber { get; set; } = string.Empty;
        public DateOnly ScheduledDate { get; set; }
        public TimeOnly? ScheduledDeparture { get; set; }
        public string RouteNumber { get; set; } = string.Empty;
        public string Direction { get; set; } = string.Empty;
    }
}
