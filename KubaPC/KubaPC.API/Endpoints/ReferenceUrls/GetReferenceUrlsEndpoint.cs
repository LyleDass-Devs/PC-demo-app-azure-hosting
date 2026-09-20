using FastEndpoints;
using KubaPC.Models.Contracts.ReferenceUrls.Responses;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.ReferenceUrls
{
    public class GetReferenceUrlsEndpoint : EndpointWithoutRequest<List<ReferenceUrlResponse>>
    {
        private readonly IReferenceUrlService referenceUrlService;

        public GetReferenceUrlsEndpoint(IReferenceUrlService referenceUrlService)
        {
            this.referenceUrlService = referenceUrlService;
        }

        public override void Configure()
        {
            Get("/reference-urls");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
        }

        public override async Task HandleAsync(CancellationToken ct)
        {
            try
            {
                await Send.OkAsync(await referenceUrlService.SelectAll(), cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Get reference URLs failed");
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
