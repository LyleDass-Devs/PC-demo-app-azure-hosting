using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.Contracts.Requests;
using KubaPC.Models.Contracts.Contracts.Responses;
using KubaPC.Models.Internal;

namespace KubaPC.Services.Services.Interfaces
{
    public interface IContractService
    {
        Task<PagedResponse<ContractListResponse>> SelectPagedContractList(ContractListRequest request);
        Task<ContractListResponse?> SelectContract(Guid contractId);
        Task<CreateResult> CreateContract(CreateContractRequest request, Guid createdBy);
        Task<OperationResult> UpdateContract(UpdateContractRequest request, Guid modifiedBy);
        Task<OperationResult> DeactivateContract(Guid contractId, Guid modifiedBy);
        Task<OperationResult> ReactivateContract(Guid contractId, Guid modifiedBy);
    }
}
