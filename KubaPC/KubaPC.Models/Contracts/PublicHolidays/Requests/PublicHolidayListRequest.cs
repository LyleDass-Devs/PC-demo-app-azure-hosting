using KubaPC.Models.Contracts.Common.Requests;

namespace KubaPC.Models.Contracts.PublicHolidays.Requests
{
    public class PublicHolidayListRequest : PagedListRequest
    {
        public int? Year { get; set; }
    }
}
