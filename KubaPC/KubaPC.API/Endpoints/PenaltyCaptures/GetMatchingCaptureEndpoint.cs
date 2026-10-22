using FastEndpoints;
using KubaPC.Models.Contracts.PenaltyCaptures.Requests;
using KubaPC.Models.Contracts.PenaltyCaptures.Responses;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.PenaltyCaptures
{
    public class GetMatchingCaptureEndpoint : Endpoint<MatchingCaptureLookupRequest, MatchingCaptureResponse>
    {
        private readonly IPenaltyReviewService penaltyReviewService;

        public GetMatchingCaptureEndpoint(IPenaltyReviewService penaltyReviewService)
        {
            this.penaltyReviewService = penaltyReviewService;
        }

        public override void Configure()
        {
            Get("/penalty-captures/matching");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Authoriser");
        }

        public override async Task HandleAsync(MatchingCaptureLookupRequest request, CancellationToken ct)
        {
            try
            {
                await Send.OkAsync(await penaltyReviewService.SelectMatchingUndecided(request.CaptureId), cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Matching capture lookup failed for {CaptureId}", request.CaptureId);
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
