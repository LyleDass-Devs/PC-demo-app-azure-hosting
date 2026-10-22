using FastEndpoints;
using KubaPC.Models.Contracts.FormF1.Requests;
using KubaPC.Models.Contracts.FormF1.Responses;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.FormF1
{
    public class GetMatchingFormF1Endpoint : Endpoint<MatchingFormF1LookupRequest, MatchingFormF1Response>
    {
        private readonly IFormF1ReviewService formF1ReviewService;

        public GetMatchingFormF1Endpoint(IFormF1ReviewService formF1ReviewService)
        {
            this.formF1ReviewService = formF1ReviewService;
        }

        public override void Configure()
        {
            Get("/form-f1/matching");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Authoriser");
        }

        public override async Task HandleAsync(MatchingFormF1LookupRequest request, CancellationToken ct)
        {
            try
            {
                await Send.OkAsync(await formF1ReviewService.SelectMatchingUndecided(request.RecordId), cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Matching Form F1 lookup failed for {RecordId}", request.RecordId);
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
