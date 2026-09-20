using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.DnoReasons.Requests;
using KubaPC.Models.Contracts.DnoReasons.Responses;
using KubaPC.SharedClient.Services.Http;
using KubaPC.SharedClient.Services.Models;

namespace KubaPC.SharedClient.Services.Api
{
    public class DnoReasonClientService : IDnoReasonClientService
    {
        private readonly IHttpService httpService;

        public DnoReasonClientService(IHttpService httpService)
        {
            this.httpService = httpService;
        }

        public async Task<ApiResult<PagedResponse<DnoReasonResponse>>> GetPaged(DnoReasonListRequest request)
        {
            return await httpService.Get<PagedResponse<DnoReasonResponse>>($"/dno-reasons?{PagedQuery.From(request)}");
        }

        public async Task<ApiResult<List<DropDownResponse>>> GetDropDown()
        {
            return await httpService.Get<List<DropDownResponse>>("/dno-reasons/dropdown");
        }

        public async Task<ApiResult<Guid>> Create(CreateDnoReasonRequest request)
        {
            return await httpService.Post<Guid>("/dno-reasons/create", request);
        }

        public async Task<ApiResult> Update(UpdateDnoReasonRequest request)
        {
            return await httpService.Post("/dno-reasons/update", request);
        }

        public async Task<ApiResult> Deactivate(Guid reasonId)
        {
            return await httpService.Post("/dno-reasons/deactivate", new DeactivateDnoReasonRequest { Id = reasonId });
        }

        public async Task<ApiResult> Reactivate(Guid reasonId)
        {
            return await httpService.Post("/dno-reasons/reactivate", new ReactivateDnoReasonRequest { Id = reasonId });
        }
    }
}
