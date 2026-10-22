namespace KubaPC.Models.Contracts.PublicHolidays.Requests
{
    public class UpdatePublicHolidayRequest
    {
        public Guid Id { get; set; }
        public DateOnly HolidayDate { get; set; }
        public string HolidayName { get; set; } = string.Empty;
    }
}
