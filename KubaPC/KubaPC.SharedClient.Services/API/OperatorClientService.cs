using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.Operators.Requests;
using KubaPC.Models.Contracts.Operators.Responses;
using KubaPC.SharedClient.Services.Http;
using KubaPC.SharedClient.Services.Models;

namespace KubaPC.SharedClient.Services.Api
{
    public class OperatorClientService : IOperatorClientService
    {
        private readonly IHttpService httpService;

        public OperatorClientService(IHttpService httpService)
        {
            this.httpService = httpService;
        }

        public async Task<ApiResult<PagedResponse<OperatorListResponse>>> GetPaged(OperatorListRequest request)
        {
            return await httpService.Get<PagedResponse<OperatorListResponse>>($"/operators?{PagedQuery.From(request)}");
        }

        public async Task<ApiResult<OperatorListResponse>> Get(Guid operatorId)
        {
            return await httpService.Get<OperatorListResponse>($"/operators/{operatorId}");
        }

        public async Task<ApiResult<List<DropDownResponse>>> GetDropDown()
        {
            return await httpService.Get<List<DropDownResponse>>("/operators/dropdown");
        }

        public async Task<ApiResult<Guid>> Create(CreateOperatorRequest request)
        {
            return await httpService.Post<Guid>("/operators/create", request);
        }

        public async Task<ApiResult> Update(UpdateOperatorRequest request)
        {
            return await httpService.Post("/operators/update", request);
        }

        public async Task<ApiResult> Deactivate(Guid operatorId)
        {
            return await httpService.Post("/operators/deactivate", new DeactivateOperatorRequest { Id = operatorId });
        }

        public async Task<ApiResult> Reactivate(Guid operatorId)
        {
            return await httpService.Post("/operators/reactivate", new ReactivateOperatorRequest { Id = operatorId });
        }
    }
}
