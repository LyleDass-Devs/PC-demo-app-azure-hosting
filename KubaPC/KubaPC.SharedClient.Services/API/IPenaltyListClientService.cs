using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.Penalties.Requests;
using KubaPC.Models.Contracts.Penalties.Responses;
using KubaPC.SharedClient.Services.Models;

namespace KubaPC.SharedClient.Services.Api
{
    public interface IPenaltyListClientService
    {
        Task<ApiResult<PagedResponse<PenaltyListItemResponse>>> GetPaged(PenaltyListItemListRequest request);
        Task<ApiResult<PenaltyLookupsResponse>> GetLookups();
        Task<ApiResult<Guid>> Create(CreatePenaltyListItemRequest request);
        Task<ApiResult> Update(UpdatePenaltyListItemRequest request);
        Task<ApiResult> Deactivate(Guid itemId);
        Task<ApiResult> Reactivate(Guid itemId);
    }
}
