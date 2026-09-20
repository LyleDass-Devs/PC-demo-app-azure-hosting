using KubaPC.Models.Contracts.ReferenceUrls.Requests;
using KubaPC.Models.Contracts.ReferenceUrls.Responses;
using KubaPC.SharedClient.Services.Models;

namespace KubaPC.SharedClient.Services.Api
{
    public interface IReferenceUrlClientService
    {
        Task<ApiResult<List<ReferenceUrlResponse>>> GetAll();
        Task<ApiResult> Save(SaveReferenceUrlRequest request);
    }
}
