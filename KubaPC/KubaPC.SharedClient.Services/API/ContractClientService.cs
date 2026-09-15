using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.Contracts.Requests;
using KubaPC.Models.Contracts.Contracts.Responses;
using KubaPC.SharedClient.Services.Http;
using KubaPC.SharedClient.Services.Models;

namespace KubaPC.SharedClient.Services.Api
{
    public class ContractClientService : IContractClientService
    {
        private readonly IHttpService httpService;

        public ContractClientService(IHttpService httpService)
        {
            this.httpService = httpService;
        }

        public async Task<ApiResult<PagedResponse<ContractListResponse>>> GetPaged(ContractListRequest request)
        {
            return await httpService.Get<PagedResponse<ContractListResponse>>($"/contracts?operatorId={request.OperatorId}&{PagedQuery.From(request)}");
        }

        public async Task<ApiResult<Guid>> Create(CreateContractRequest request)
        {
            return await httpService.Post<Guid>("/contracts/create", request);
        }

        public async Task<ApiResult> Update(UpdateContractRequest request)
        {
            return await httpService.Post("/contracts/update", request);
        }

        public async Task<ApiResult> Deactivate(Guid contractId)
        {
            return await httpService.Post("/contracts/deactivate", new DeactivateContractRequest { Id = contractId });
        }

        public async Task<ApiResult> Reactivate(Guid contractId)
        {
            return await httpService.Post("/contracts/reactivate", new ReactivateContractRequest { Id = contractId });
        }
    }
}
