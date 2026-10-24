using KubaPC.Models.Contracts.PenaltyCaptures.Requests;
using KubaPC.Models.Contracts.PenaltyCaptures.Responses;
using KubaPC.Models.Internal;

namespace KubaPC.Services.Services.Interfaces
{
    public interface IPenaltyReviewService
    {
        Task<LookupResult<MatchingCaptureResponse>> SelectMatchingUndecided(Guid captureId, Caller caller);
        Task<OperationResult> ReviewCapture(ReviewPenaltyCaptureRequest request, Caller caller);
    }
}
