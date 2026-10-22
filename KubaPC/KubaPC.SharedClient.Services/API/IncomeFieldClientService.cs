using KubaPC.Models.Contracts.IncomeFields.Requests;
using KubaPC.Models.Contracts.IncomeFields.Responses;
using KubaPC.SharedClient.Services.Http;
using KubaPC.SharedClient.Services.Models;

namespace KubaPC.SharedClient.Services.Api
{
    public class IncomeFieldClientService : IIncomeFieldClientService
    {
        private readonly IHttpService httpService;

        public IncomeFieldClientService(IHttpService httpService)
        {
            this.httpService = httpService;
        }

        public async Task<ApiResult<IncomeFieldsResponse>> Get(Guid contractId)
        {
            return await httpService.Get<IncomeFieldsResponse>($"/income-fields?contractId={contractId}");
        }

        public async Task<ApiResult> Save(SaveIncomeFieldsRequest request)
        {
            return await httpService.Post("/income-fields/save", request);
        }
    }
}
