using FastEndpoints;
using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.Operators.Requests;
using KubaPC.Models.Contracts.Operators.Responses;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.Operators
{
    public class GetOperatorListEndpoint : Endpoint<OperatorListRequest, PagedResponse<OperatorListResponse>>
    {
        private readonly IOperatorService operatorService;

        public GetOperatorListEndpoint(IOperatorService operatorService)
        {
            this.operatorService = operatorService;
        }

        public override void Configure()
        {
            Get("/operators");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Admin");
        }

        public override async Task HandleAsync(OperatorListRequest request, CancellationToken ct)
        {
            try
            {
                PagedResponse<OperatorListResponse> response = await operatorService.SelectPagedOperatorList(request);
                await Send.OkAsync(response, cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Get operator list failed");
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
