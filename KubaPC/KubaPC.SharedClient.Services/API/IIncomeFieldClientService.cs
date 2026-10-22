using KubaPC.Models.Contracts.IncomeFields.Requests;
using KubaPC.Models.Contracts.IncomeFields.Responses;
using KubaPC.SharedClient.Services.Models;

namespace KubaPC.SharedClient.Services.Api
{
    public interface IIncomeFieldClientService
    {
        Task<ApiResult<IncomeFieldsResponse>> Get(Guid contractId);
        Task<ApiResult> Save(SaveIncomeFieldsRequest request);
    }
}
