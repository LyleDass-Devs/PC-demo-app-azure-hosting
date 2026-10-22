namespace KubaPC.Models.Contracts.PublicHolidays.Requests
{
    public class CreatePublicHolidayRequest
    {
        public DateOnly HolidayDate { get; set; }
        public string HolidayName { get; set; } = string.Empty;
    }
}
