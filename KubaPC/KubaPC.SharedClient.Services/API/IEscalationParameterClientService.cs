using KubaPC.Models.Contracts.Escalations.Requests;
using KubaPC.Models.Contracts.Escalations.Responses;
using KubaPC.SharedClient.Services.Models;

namespace KubaPC.SharedClient.Services.Api
{
    public interface IEscalationParameterClientService
    {
        Task<ApiResult<EscalationParametersResponse>> Get(Guid contractId);
        Task<ApiResult> Save(SaveEscalationParametersRequest request);
    }
}
