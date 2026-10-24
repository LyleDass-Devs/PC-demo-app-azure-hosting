using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.ContractRates.Requests;
using KubaPC.Models.Contracts.ContractRates.Responses;
using KubaPC.Models.Internal;

namespace KubaPC.Services.Services.Interfaces
{
    public interface IContractRateService
    {
        Task<PagedResponse<ContractRateResponse>> SelectPagedRates(ContractRateListRequest request, Caller caller);
        Task<CreateResult> CreateRate(CreateContractRateRequest request, Caller caller);
        Task<OperationResult> UpdateRate(UpdateContractRateRequest request, Caller caller);
        Task<OperationResult> DeactivateRate(Guid rateId, Caller caller);
        Task<List<string>> SelectVehicleTypes();
    }
}
