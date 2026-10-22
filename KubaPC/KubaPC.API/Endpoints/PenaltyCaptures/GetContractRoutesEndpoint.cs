using FastEndpoints;
using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.PenaltyCaptures.Requests;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.PenaltyCaptures
{
    public class GetContractRoutesEndpoint : Endpoint<CaptureLookupRequest, List<DropDownResponse>>
    {
        private readonly IPenaltyCaptureService penaltyCaptureService;

        public GetContractRoutesEndpoint(IPenaltyCaptureService penaltyCaptureService)
        {
            this.penaltyCaptureService = penaltyCaptureService;
        }

        public override void Configure()
        {
            Get("/penalty-captures/routes");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
        }

        public override async Task HandleAsync(CaptureLookupRequest request, CancellationToken ct)
        {
            try
            {
                await Send.OkAsync(await penaltyCaptureService.SelectContractRoutes(request.ContractId), cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Get contract routes failed for {ContractId}", request.ContractId);
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
