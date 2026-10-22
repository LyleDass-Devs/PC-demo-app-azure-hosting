using FastEndpoints;
using KubaPC.Models.Contracts.PenaltyCaptures.Requests;
using KubaPC.Models.Internal;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.PenaltyCaptures
{
    public class ReviewPenaltyCaptureEndpoint : Endpoint<ReviewPenaltyCaptureRequest>
    {
        private readonly IPenaltyReviewService penaltyReviewService;

        public ReviewPenaltyCaptureEndpoint(IPenaltyReviewService penaltyReviewService)
        {
            this.penaltyReviewService = penaltyReviewService;
        }

        public override void Configure()
        {
            Post("/penalty-captures/review");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Authoriser");
        }

        public override async Task HandleAsync(ReviewPenaltyCaptureRequest request, CancellationToken ct)
        {
            try
            {
                Guid actor = Guid.Parse(User.FindFirst("UserId")!.Value);

                OperationResult result = await penaltyReviewService.ReviewCapture(request, actor);

                if (!result.IsSuccess)
                {
                    AddError(result.Error!);
                    await Send.ErrorsAsync(409, ct);
                    return;
                }

                await Send.OkAsync(new { message = "Penalty reviewed." }, cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Penalty review failed for {CaptureId}", request.Id);
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
