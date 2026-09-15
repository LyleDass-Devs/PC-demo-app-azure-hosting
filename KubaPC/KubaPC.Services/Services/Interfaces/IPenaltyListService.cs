using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.Penalties.Requests;
using KubaPC.Models.Contracts.Penalties.Responses;
using KubaPC.Models.Internal;

namespace KubaPC.Services.Services.Interfaces
{
    public interface IPenaltyListService
    {
        Task<PagedResponse<PenaltyListItemResponse>> SelectPagedItems(PenaltyListItemListRequest request);
        Task<PenaltyLookupsResponse> SelectLookups();
        Task<CreateResult> CreateItem(CreatePenaltyListItemRequest request, Guid createdBy);
        Task<OperationResult> UpdateItem(UpdatePenaltyListItemRequest request, Guid modifiedBy);
        Task<OperationResult> DeactivateItem(Guid itemId, Guid modifiedBy);
        Task<OperationResult> ReactivateItem(Guid itemId, Guid modifiedBy);
    }
}
