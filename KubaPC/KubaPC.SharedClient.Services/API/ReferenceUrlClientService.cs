using KubaPC.Models.Contracts.ReferenceUrls.Requests;
using KubaPC.Models.Contracts.ReferenceUrls.Responses;
using KubaPC.SharedClient.Services.Http;
using KubaPC.SharedClient.Services.Models;

namespace KubaPC.SharedClient.Services.Api
{
    public class ReferenceUrlClientService : IReferenceUrlClientService
    {
        private readonly IHttpService httpService;

        public ReferenceUrlClientService(IHttpService httpService)
        {
            this.httpService = httpService;
        }

        public async Task<ApiResult<List<ReferenceUrlResponse>>> GetAll()
        {
            return await httpService.Get<List<ReferenceUrlResponse>>("/reference-urls");
        }

        public async Task<ApiResult> Save(SaveReferenceUrlRequest request)
        {
            return await httpService.Post("/reference-urls/save", request);
        }
    }
}
