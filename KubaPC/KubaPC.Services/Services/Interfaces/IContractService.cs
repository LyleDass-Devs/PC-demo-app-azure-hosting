using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.Contracts.Requests;
using KubaPC.Models.Contracts.Contracts.Responses;
using KubaPC.Models.Internal;

namespace KubaPC.Services.Services.Interfaces
{
    public interface IContractService
    {
        Task<PagedResponse<ContractListResponse>> SelectPagedContractList(ContractListRequest request, Caller caller);
        Task<ContractListResponse?> SelectContract(Guid contractId, Caller caller);
        Task<List<DropDownResponse>> SelectDropDown(Guid? operatorId, Caller caller);
        Task<CreateResult> CreateContract(CreateContractRequest request, Caller caller);
        Task<OperationResult> UpdateContract(UpdateContractRequest request, Caller caller);
        Task<OperationResult> DeactivateContract(Guid contractId, Caller caller);
        Task<OperationResult> ReactivateContract(Guid contractId, Caller caller);
    }
}
