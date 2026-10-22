using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.PublicHolidays.Requests;
using KubaPC.Models.Contracts.PublicHolidays.Responses;
using KubaPC.Models.Internal;

namespace KubaPC.Services.Services.Interfaces
{
    public interface IPublicHolidayService
    {
        Task<PagedResponse<PublicHolidayResponse>> SelectPaged(PublicHolidayListRequest request);
        Task<List<int>> SelectYears();
        Task<CreateResult> Create(CreatePublicHolidayRequest request, Guid actor);
        Task<OperationResult> Update(UpdatePublicHolidayRequest request, Guid actor);
        Task<OperationResult> Deactivate(Guid holidayId, Guid actor);
        Task<OperationResult> Reactivate(Guid holidayId, Guid actor);
    }
}
