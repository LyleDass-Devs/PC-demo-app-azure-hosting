using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.Contracts.Penalties.Requests;
using KubaPC.Models.Contracts.Contracts.Penalties.Responses;
using KubaPC.SharedClient.Services.Http;
using KubaPC.SharedClient.Services.Models;

namespace KubaPC.SharedClient.Services.Api
{
    public class PenaltyListClientService : IPenaltyListClientService
    {
        private readonly IHttpService httpService;

        public PenaltyListClientService(IHttpService httpService)
        {
            this.httpService = httpService;
        }

        public async Task<ApiResult<PagedResponse<PenaltyListItemResponse>>> GetPaged(PenaltyListItemListRequest request)
        {
            return await httpService.Get<PagedResponse<PenaltyListItemResponse>>($"/penalty-list?{PagedQuery.From(request)}{(request.ContractId is null ? "" : $"&contractId={request.ContractId}")}{(request.OperatorId is null ? "" : $"&operatorId={request.OperatorId}")}");
        }

        public async Task<ApiResult<PenaltyLookupsResponse>> GetLookups()
        {
            return await httpService.Get<PenaltyLookupsResponse>("/penalty-list/lookups");
        }

        public async Task<ApiResult<Guid>> Create(CreatePenaltyListItemRequest request)
        {
            return await httpService.Post<Guid>("/penalty-list/create", request);
        }

        public async Task<ApiResult> Update(UpdatePenaltyListItemRequest request)
        {
            return await httpService.Post("/penalty-list/update", request);
        }

        public async Task<ApiResult> Deactivate(Guid itemId)
        {
            return await httpService.Post("/penalty-list/deactivate", new DeactivatePenaltyListItemRequest { Id = itemId });
        }

        public async Task<ApiResult> Reactivate(Guid itemId)
        {
            return await httpService.Post("/penalty-list/reactivate", new ReactivatePenaltyListItemRequest { Id = itemId });
        }
    }
}
