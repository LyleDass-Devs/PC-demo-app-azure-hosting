using KubaPC.Models.Contracts.PenaltyCaptures.Requests;
using KubaPC.Models.Contracts.PenaltyCaptures.Responses;
using KubaPC.Models.Internal;

namespace KubaPC.Services.Services.Interfaces
{
    public interface IPenaltyReviewService
    {
        Task<MatchingCaptureResponse> SelectMatchingUndecided(Guid captureId);
        Task<OperationResult> ReviewCapture(ReviewPenaltyCaptureRequest request, Guid actor);
    }
}
