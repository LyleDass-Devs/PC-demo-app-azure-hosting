using KubaPC.Models.Contracts.ReferenceUrls.Requests;
using KubaPC.Models.Contracts.ReferenceUrls.Responses;
using KubaPC.Models.Internal;

namespace KubaPC.Services.Services.Interfaces
{
    public interface IReferenceUrlService
    {
        Task<List<ReferenceUrlResponse>> SelectAll();
        Task<OperationResult> Save(SaveReferenceUrlRequest request, Guid actor);
    }
}
