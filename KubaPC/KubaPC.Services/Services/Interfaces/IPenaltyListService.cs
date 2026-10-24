using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.Penalties.Requests;
using KubaPC.Models.Contracts.Penalties.Responses;
using KubaPC.Models.Internal;

namespace KubaPC.Services.Services.Interfaces
{
    public interface IPenaltyListService
    {
        Task<PagedResponse<PenaltyListItemResponse>> SelectPagedItems(PenaltyListItemListRequest request, Caller caller);
        Task<PenaltyLookupsResponse> SelectLookups();
        Task<CreateResult> CreateItem(CreatePenaltyListItemRequest request, Caller caller);
        Task<OperationResult> UpdateItem(UpdatePenaltyListItemRequest request, Caller caller);
        Task<OperationResult> DeactivateItem(Guid itemId, Caller caller);
        Task<OperationResult> ReactivateItem(Guid itemId, Caller caller);
    }
}
