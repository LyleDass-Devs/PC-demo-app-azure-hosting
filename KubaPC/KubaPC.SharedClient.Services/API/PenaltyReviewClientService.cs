using KubaPC.Models.Contracts.PenaltyCaptures.Requests;
using KubaPC.Models.Contracts.PenaltyCaptures.Responses;
using KubaPC.SharedClient.Services.Http;
using KubaPC.SharedClient.Services.Models;

namespace KubaPC.SharedClient.Services.Api
{
    public class PenaltyReviewClientService : IPenaltyReviewClientService
    {
        private readonly IHttpService httpService;

        public PenaltyReviewClientService(IHttpService httpService)
        {
            this.httpService = httpService;
        }

        public async Task<ApiResult<MatchingCaptureResponse>> GetMatching(Guid captureId)
        {
            return await httpService.Get<MatchingCaptureResponse>($"/penalty-captures/matching?captureId={captureId}");
        }

        public async Task<ApiResult> Review(ReviewPenaltyCaptureRequest request)
        {
            return await httpService.Post("/penalty-captures/review", request);
        }
    }
}
