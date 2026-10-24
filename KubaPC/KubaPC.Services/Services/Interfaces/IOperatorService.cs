using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.Operators.Requests;
using KubaPC.Models.Contracts.Operators.Responses;
using KubaPC.Models.Internal;

namespace KubaPC.Services.Services.Interfaces
{
    public interface IOperatorService
    {
        Task<PagedResponse<OperatorListResponse>> SelectPagedOperatorList(OperatorListRequest request, Caller caller);
        Task<OperatorListResponse?> SelectOperator(Guid operatorId, Caller caller);
        Task<List<DropDownResponse>> SelectDropDown(Caller caller);
        Task<CreateResult> CreateOperator(CreateOperatorRequest request, Guid createdBy);
        Task<OperationResult> UpdateOperator(UpdateOperatorRequest request, Caller caller);
        Task<OperationResult> DeactivateOperator(Guid operatorId, Caller caller);
        Task<OperationResult> ReactivateOperator(Guid operatorId, Caller caller);
    }
}
