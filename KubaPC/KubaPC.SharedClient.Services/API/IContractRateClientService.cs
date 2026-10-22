using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.ContractRates.Requests;
using KubaPC.Models.Contracts.ContractRates.Responses;
using KubaPC.SharedClient.Services.Models;

namespace KubaPC.SharedClient.Services.Api
{
    public interface IContractRateClientService
    {
        Task<ApiResult<PagedResponse<ContractRateResponse>>> GetPaged(ContractRateListRequest request);
        Task<ApiResult<Guid>> Create(CreateContractRateRequest request);
        Task<ApiResult> Update(UpdateContractRateRequest request);
        Task<ApiResult> Deactivate(Guid rateId);
        Task<ApiResult<List<string>>> GetVehicleTypes();
    }
}
