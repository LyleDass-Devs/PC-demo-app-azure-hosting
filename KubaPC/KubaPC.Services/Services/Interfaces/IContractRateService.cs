using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.ContractRates.Requests;
using KubaPC.Models.Contracts.ContractRates.Responses;
using KubaPC.Models.Internal;

namespace KubaPC.Services.Services.Interfaces
{
    public interface IContractRateService
    {
        Task<PagedResponse<ContractRateResponse>> SelectPagedRates(ContractRateListRequest request);
        Task<CreateResult> CreateRate(CreateContractRateRequest request, Guid createdBy);
        Task<OperationResult> UpdateRate(UpdateContractRateRequest request, Guid modifiedBy);
        Task<OperationResult> DeactivateRate(Guid rateId, Guid modifiedBy);
        Task<List<string>> SelectVehicleTypes();
    }
}
