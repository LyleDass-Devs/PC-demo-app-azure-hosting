using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.Operators.Requests;
using KubaPC.Models.Contracts.Operators.Responses;
using KubaPC.SharedClient.Services.Models;

namespace KubaPC.SharedClient.Services.Api
{
    public interface IOperatorClientService
    {
        Task<ApiResult<PagedResponse<OperatorListResponse>>> GetPaged(OperatorListRequest request);
        Task<ApiResult<OperatorListResponse>> Get(Guid operatorId);
        Task<ApiResult<List<DropDownResponse>>> GetDropDown();
        Task<ApiResult<Guid>> Create(CreateOperatorRequest request);
        Task<ApiResult> Update(UpdateOperatorRequest request);
        Task<ApiResult> Deactivate(Guid operatorId);
        Task<ApiResult> Reactivate(Guid operatorId);
    }
}
