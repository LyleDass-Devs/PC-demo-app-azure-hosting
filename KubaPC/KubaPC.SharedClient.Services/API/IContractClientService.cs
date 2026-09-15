using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.Contracts.Requests;
using KubaPC.Models.Contracts.Contracts.Responses;
using KubaPC.SharedClient.Services.Models;

namespace KubaPC.SharedClient.Services.Api
{
    public interface IContractClientService
    {
        Task<ApiResult<PagedResponse<ContractListResponse>>> GetPaged(ContractListRequest request);
        Task<ApiResult<ContractListResponse>> Get(Guid contractId);
        Task<ApiResult<Guid>> Create(CreateContractRequest request);
        Task<ApiResult> Update(UpdateContractRequest request);
        Task<ApiResult> Deactivate(Guid contractId);
        Task<ApiResult> Reactivate(Guid contractId);
    }
}
