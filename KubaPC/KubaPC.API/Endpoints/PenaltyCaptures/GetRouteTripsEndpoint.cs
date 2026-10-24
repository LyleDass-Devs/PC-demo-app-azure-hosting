using FastEndpoints;
using KubaPC.API.Security;
using KubaPC.Models.Contracts.PenaltyCaptures.Requests;
using KubaPC.Models.Contracts.PenaltyCaptures.Responses;
using KubaPC.Models.Internal;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.PenaltyCaptures
{
    public class GetRouteTripsEndpoint : Endpoint<TripLookupRequest, List<TripOptionResponse>>
    {
        private readonly IPenaltyCaptureService penaltyCaptureService;

        public GetRouteTripsEndpoint(IPenaltyCaptureService penaltyCaptureService)
        {
            this.penaltyCaptureService = penaltyCaptureService;
        }

        public override void Configure()
        {
            Get("/penalty-captures/trips");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Reader");
        }

        public override async Task HandleAsync(TripLookupRequest request, CancellationToken ct)
        {
            try
            {
                LookupResult<List<TripOptionResponse>> result = await penaltyCaptureService.SelectRouteTrips(request, CallerReader.Read(User));

                if (result.IsForbidden)
                {
                    await Send.ForbiddenAsync(ct);
                    return;
                }

                await Send.OkAsync(result.Data!, cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Get route trips failed for {RouteId}", request.RouteId);
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
