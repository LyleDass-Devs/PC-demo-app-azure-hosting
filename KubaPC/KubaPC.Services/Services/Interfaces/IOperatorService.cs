using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.Operators.Requests;
using KubaPC.Models.Contracts.Operators.Responses;
using KubaPC.Models.Internal;

namespace KubaPC.Services.Services.Interfaces
{
    public interface IOperatorService
    {
        Task<PagedResponse<OperatorListResponse>> SelectPagedOperatorList(OperatorListRequest request);
        Task<OperatorListResponse?> SelectOperator(Guid operatorId);
        Task<List<DropDownResponse>> SelectDropDown();
        Task<CreateResult> CreateOperator(CreateOperatorRequest request, Guid createdBy);
        Task<OperationResult> UpdateOperator(UpdateOperatorRequest request, Guid modifiedBy);
        Task<OperationResult> DeactivateOperator(Guid operatorId, Guid modifiedBy);
        Task<OperationResult> ReactivateOperator(Guid operatorId, Guid modifiedBy);
    }
}
