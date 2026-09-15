using FastEndpoints;
using KubaPC.Models.Contracts.Operators.Requests;
using KubaPC.Models.Contracts.Operators.Responses;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.Operators
{
    public class GetOperatorEndpoint : Endpoint<GetOperatorRequest, OperatorListResponse>
    {
        private readonly IOperatorService operatorService;

        public GetOperatorEndpoint(IOperatorService operatorService)
        {
            this.operatorService = operatorService;
        }

        public override void Configure()
        {
            Get("/operators/{id}");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Admin");
        }

        public override async Task HandleAsync(GetOperatorRequest request, CancellationToken ct)
        {
            try
            {
                OperatorListResponse? response = await operatorService.SelectOperator(request.Id);

                if (response is null)
                {
                    await Send.NotFoundAsync(ct);
                    return;
                }

                await Send.OkAsync(response, cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Get operator failed for {OperatorId}", request.Id);
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
