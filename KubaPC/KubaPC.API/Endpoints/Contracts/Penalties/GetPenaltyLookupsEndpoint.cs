using FastEndpoints;
using KubaPC.Models.Contracts.Contracts.Penalties.Responses;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.Contracts.Penalties
{
    public class GetPenaltyLookupsEndpoint : EndpointWithoutRequest<PenaltyLookupsResponse>
    {
        private readonly IPenaltyListService penaltyListService;

        public GetPenaltyLookupsEndpoint(IPenaltyListService penaltyListService)
        {
            this.penaltyListService = penaltyListService;
        }

        public override void Configure()
        {
            Get("/penalty-list/lookups");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
        }

        public override async Task HandleAsync(CancellationToken ct)
        {
            try
            {
                await Send.OkAsync(await penaltyListService.SelectLookups(), cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Get penalty lookups failed");
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
