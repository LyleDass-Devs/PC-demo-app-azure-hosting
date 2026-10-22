using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.ContractRates.Requests;
using KubaPC.Models.Contracts.ContractRates.Responses;
using KubaPC.SharedClient.Services.Http;
using KubaPC.SharedClient.Services.Models;

namespace KubaPC.SharedClient.Services.Api
{
    public class ContractRateClientService : IContractRateClientService
    {
        private readonly IHttpService httpService;

        public ContractRateClientService(IHttpService httpService)
        {
            this.httpService = httpService;
        }

        public async Task<ApiResult<PagedResponse<ContractRateResponse>>> GetPaged(ContractRateListRequest request)
        {
            string contractFilter = request.ContractId is null ? string.Empty : $"&contractId={request.ContractId}";
            string operatorFilter = request.OperatorId is null ? string.Empty : $"&operatorId={request.OperatorId}";

            return await httpService.Get<PagedResponse<ContractRateResponse>>($"/contract-rates?{PagedQuery.From(request)}{contractFilter}{operatorFilter}");
        }

        public async Task<ApiResult<Guid>> Create(CreateContractRateRequest request)
        {
            return await httpService.Post<Guid>("/contract-rates/create", request);
        }

        public async Task<ApiResult> Update(UpdateContractRateRequest request)
        {
            return await httpService.Post("/contract-rates/update", request);
        }

        public async Task<ApiResult> Deactivate(Guid rateId)
        {
            return await httpService.Post("/contract-rates/deactivate", new DeactivateContractRateRequest { Id = rateId });
        }

        public async Task<ApiResult<List<string>>> GetVehicleTypes()
        {
            return await httpService.Get<List<string>>("/contract-rates/vehicle-types");
        }
    }
}
