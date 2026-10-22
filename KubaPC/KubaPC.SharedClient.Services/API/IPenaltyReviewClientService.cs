using KubaPC.Models.Contracts.PenaltyCaptures.Requests;
using KubaPC.Models.Contracts.PenaltyCaptures.Responses;
using KubaPC.SharedClient.Services.Models;

namespace KubaPC.SharedClient.Services.Api
{
    public interface IPenaltyReviewClientService
    {
        Task<ApiResult<MatchingCaptureResponse>> GetMatching(Guid captureId);
        Task<ApiResult> Review(ReviewPenaltyCaptureRequest request);
    }
}
