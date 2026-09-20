using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.DnoReasons.Requests;
using KubaPC.Models.Contracts.DnoReasons.Responses;
using KubaPC.Models.Internal;

namespace KubaPC.Services.Services.Interfaces
{
    public interface IDnoReasonService
    {
        Task<PagedResponse<DnoReasonResponse>> SelectPaged(DnoReasonListRequest request);
        Task<List<DropDownResponse>> SelectDropDown();
        Task<CreateResult> Create(CreateDnoReasonRequest request, Guid createdBy);
        Task<OperationResult> Update(UpdateDnoReasonRequest request, Guid modifiedBy);
        Task<OperationResult> Deactivate(Guid reasonId, Guid modifiedBy);
        Task<OperationResult> Reactivate(Guid reasonId, Guid modifiedBy);
    }
}
