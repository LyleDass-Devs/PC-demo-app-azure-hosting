using KubaPC.Models.Contracts.Escalations.Requests;
using KubaPC.Models.Contracts.Escalations.Responses;
using KubaPC.SharedClient.Services.Http;
using KubaPC.SharedClient.Services.Models;

namespace KubaPC.SharedClient.Services.Api
{
    public class EscalationParameterClientService : IEscalationParameterClientService
    {
        private readonly IHttpService httpService;

        public EscalationParameterClientService(IHttpService httpService)
        {
            this.httpService = httpService;
        }

        public async Task<ApiResult<EscalationParametersResponse>> Get(Guid contractId)
        {
            return await httpService.Get<EscalationParametersResponse>($"/escalation-parameters?contractId={contractId}");
        }

        public async Task<ApiResult> Save(SaveEscalationParametersRequest request)
        {
            return await httpService.Post("/escalation-parameters/save", request);
        }
    }
}
