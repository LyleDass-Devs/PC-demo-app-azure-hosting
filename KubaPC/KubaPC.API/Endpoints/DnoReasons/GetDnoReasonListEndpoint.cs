using FastEndpoints;
using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.DnoReasons.Requests;
using KubaPC.Models.Contracts.DnoReasons.Responses;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.DnoReasons
{
    public class GetDnoReasonListEndpoint : Endpoint<DnoReasonListRequest, PagedResponse<DnoReasonResponse>>
    {
        private readonly IDnoReasonService dnoReasonService;

        public GetDnoReasonListEndpoint(IDnoReasonService dnoReasonService)
        {
            this.dnoReasonService = dnoReasonService;
        }

        public override void Configure()
        {
            Get("/dno-reasons");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Admin");
        }

        public override async Task HandleAsync(DnoReasonListRequest request, CancellationToken ct)
        {
            try
            {
                await Send.OkAsync(await dnoReasonService.SelectPaged(request), cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Get DNO reason list failed");
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
