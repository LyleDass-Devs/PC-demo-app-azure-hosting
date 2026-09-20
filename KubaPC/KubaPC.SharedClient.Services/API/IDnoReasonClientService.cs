using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.DnoReasons.Requests;
using KubaPC.Models.Contracts.DnoReasons.Responses;
using KubaPC.SharedClient.Services.Models;

namespace KubaPC.SharedClient.Services.Api
{
    public interface IDnoReasonClientService
    {
        Task<ApiResult<PagedResponse<DnoReasonResponse>>> GetPaged(DnoReasonListRequest request);
        Task<ApiResult<List<DropDownResponse>>> GetDropDown();
        Task<ApiResult<Guid>> Create(CreateDnoReasonRequest request);
        Task<ApiResult> Update(UpdateDnoReasonRequest request);
        Task<ApiResult> Deactivate(Guid reasonId);
        Task<ApiResult> Reactivate(Guid reasonId);
    }
}
