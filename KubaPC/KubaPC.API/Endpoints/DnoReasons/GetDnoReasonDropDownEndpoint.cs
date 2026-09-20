using FastEndpoints;
using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.DnoReasons
{
    public class GetDnoReasonDropDownEndpoint : EndpointWithoutRequest<List<DropDownResponse>>
    {
        private readonly IDnoReasonService dnoReasonService;

        public GetDnoReasonDropDownEndpoint(IDnoReasonService dnoReasonService)
        {
            this.dnoReasonService = dnoReasonService;
        }

        public override void Configure()
        {
            Get("/dno-reasons/dropdown");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
        }

        public override async Task HandleAsync(CancellationToken ct)
        {
            try
            {
                await Send.OkAsync(await dnoReasonService.SelectDropDown(), cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Get DNO reason dropdown failed");
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
