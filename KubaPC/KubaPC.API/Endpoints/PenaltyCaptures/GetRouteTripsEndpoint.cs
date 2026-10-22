using FastEndpoints;
using KubaPC.Models.Contracts.PenaltyCaptures.Requests;
using KubaPC.Models.Contracts.PenaltyCaptures.Responses;
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
        }

        public override async Task HandleAsync(TripLookupRequest request, CancellationToken ct)
        {
            try
            {
                await Send.OkAsync(await penaltyCaptureService.SelectRouteTrips(request), cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Get route trips failed for {RouteId}", request.RouteId);
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
