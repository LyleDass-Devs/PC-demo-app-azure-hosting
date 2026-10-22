using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.FormF1.Requests;
using KubaPC.Models.Contracts.FormF1.Responses;
using KubaPC.SharedClient.Services.Models;

namespace KubaPC.SharedClient.Services.Api
{
    public interface IFormF1ClientService
    {
        Task<ApiResult<PagedResponse<FormF1RecordResponse>>> GetPaged(FormF1ListRequest request);
        Task<ApiResult<FormF1LookupsResponse>> GetLookups();
        Task<ApiResult<Guid>> Create(CreateFormF1RecordRequest request);
        Task<ApiResult> Update(UpdateFormF1RecordRequest request);
        Task<ApiResult> Delete(DeleteFormF1RecordRequest request);
        Task<ApiResult> Review(ReviewFormF1RecordRequest request);
        Task<ApiResult<MatchingFormF1Response>> GetMatching(Guid recordId);
    }
}
