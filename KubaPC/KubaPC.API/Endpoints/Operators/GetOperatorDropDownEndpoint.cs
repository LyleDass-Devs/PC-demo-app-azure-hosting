using FastEndpoints;
using KubaPC.API.Security;
using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Internal;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.Operators
{
    public class GetOperatorDropDownEndpoint : EndpointWithoutRequest<List<DropDownResponse>>
    {
        private readonly IOperatorService operatorService;

        public GetOperatorDropDownEndpoint(IOperatorService operatorService)
        {
            this.operatorService = operatorService;
        }

        public override void Configure()
        {
            Get("/operators/dropdown");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Admin");
        }

        public override async Task HandleAsync(CancellationToken ct)
        {
            try
            {
                await Send.OkAsync(await operatorService.SelectDropDown(CallerReader.Read(User)), cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Get operator dropdown failed");
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
