using FastEndpoints;
using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.PenaltyCaptures.Requests;
using KubaPC.Models.Contracts.PenaltyCaptures.Responses;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.PenaltyCaptures
{
    public class GetPenaltyCaptureListEndpoint : Endpoint<PenaltyCaptureListRequest, PagedResponse<PenaltyCaptureResponse>>
    {
        private readonly IPenaltyCaptureService penaltyCaptureService;

        public GetPenaltyCaptureListEndpoint(IPenaltyCaptureService penaltyCaptureService)
        {
            this.penaltyCaptureService = penaltyCaptureService;
        }

        public override void Configure()
        {
            Get("/penalty-captures");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
        }

        public override async Task HandleAsync(PenaltyCaptureListRequest request, CancellationToken ct)
        {
            try
            {
                Guid actor = Guid.Parse(User.FindFirst("UserId")!.Value);

                await Send.OkAsync(await penaltyCaptureService.SelectPagedCaptures(request, actor), cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Get penalty captures failed");
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
