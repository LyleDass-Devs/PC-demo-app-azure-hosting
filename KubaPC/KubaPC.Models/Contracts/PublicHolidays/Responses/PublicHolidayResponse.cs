namespace KubaPC.Models.Contracts.PublicHolidays.Responses
{
    public class PublicHolidayResponse
    {
        public Guid Id { get; set; }
        public DateOnly HolidayDate { get; set; }
        public string HolidayName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
