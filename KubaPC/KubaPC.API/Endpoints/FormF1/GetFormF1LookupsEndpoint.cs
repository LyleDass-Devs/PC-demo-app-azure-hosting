using FastEndpoints;
using KubaPC.Models.Contracts.FormF1.Responses;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.FormF1
{
    public class GetFormF1LookupsEndpoint : EndpointWithoutRequest<FormF1LookupsResponse>
    {
        private readonly IFormF1Service formF1Service;

        public GetFormF1LookupsEndpoint(IFormF1Service formF1Service)
        {
            this.formF1Service = formF1Service;
        }

        public override void Configure()
        {
            Get("/form-f1/lookups");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
        }

        public override async Task HandleAsync(CancellationToken ct)
        {
            try
            {
                await Send.OkAsync(await formF1Service.SelectLookups(), cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Get Form F1 lookups failed");
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
