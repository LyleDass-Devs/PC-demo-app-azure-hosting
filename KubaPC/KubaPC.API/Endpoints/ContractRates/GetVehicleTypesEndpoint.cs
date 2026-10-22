using FastEndpoints;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.ContractRates
{
    public class GetVehicleTypesEndpoint : EndpointWithoutRequest<List<string>>
    {
        private readonly IContractRateService contractRateService;

        public GetVehicleTypesEndpoint(IContractRateService contractRateService)
        {
            this.contractRateService = contractRateService;
        }

        public override void Configure()
        {
            Get("/contract-rates/vehicle-types");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Admin");
        }

        public override async Task HandleAsync(CancellationToken ct)
        {
            try
            {
                await Send.OkAsync(await contractRateService.SelectVehicleTypes(), cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Get vehicle types failed");
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
