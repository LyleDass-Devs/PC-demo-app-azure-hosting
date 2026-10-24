using FastEndpoints;
using KubaPC.API.Security;
using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.PenaltyCaptures.Requests;
using KubaPC.Models.Contracts.PenaltyCaptures.Responses;
using KubaPC.Models.Internal;
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
            Policies("Reader");
        }

        public override async Task HandleAsync(PenaltyCaptureListRequest request, CancellationToken ct)
        {
            try
            {
                Caller caller = CallerReader.Read(User);

                await Send.OkAsync(await penaltyCaptureService.SelectPagedCaptures(request, caller), cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Get penalty captures failed");
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
