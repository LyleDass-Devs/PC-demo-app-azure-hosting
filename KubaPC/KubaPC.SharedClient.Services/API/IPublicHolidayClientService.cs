using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.PublicHolidays.Requests;
using KubaPC.Models.Contracts.PublicHolidays.Responses;
using KubaPC.SharedClient.Services.Models;

namespace KubaPC.SharedClient.Services.Api
{
    public interface IPublicHolidayClientService
    {
        Task<ApiResult<PagedResponse<PublicHolidayResponse>>> GetPaged(PublicHolidayListRequest request);
        Task<ApiResult<List<int>>> GetYears();
        Task<ApiResult<Guid>> Create(CreatePublicHolidayRequest request);
        Task<ApiResult> Update(UpdatePublicHolidayRequest request);
        Task<ApiResult> Deactivate(Guid holidayId);
        Task<ApiResult> Reactivate(Guid holidayId);
    }
}
