using FastEndpoints;
using KubaPC.API.Security;
using KubaPC.Models.Contracts.Operators.Requests;
using KubaPC.Models.Internal;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.Operators
{
    public class DeactivateOperatorEndpoint : Endpoint<DeactivateOperatorRequest>
    {
        private readonly IOperatorService operatorService;

        public DeactivateOperatorEndpoint(IOperatorService operatorService)
        {
            this.operatorService = operatorService;
        }

        public override void Configure()
        {
            Post("/operators/deactivate");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Admin");
        }

        public override async Task HandleAsync(DeactivateOperatorRequest request, CancellationToken ct)
        {
            try
            {
                Caller caller = CallerReader.Read(User);

                OperationResult result = await operatorService.DeactivateOperator(request.Id, caller);

                if (!result.IsSuccess)
                {
                if (result.IsForbidden)
                {
                    await Send.ForbiddenAsync(ct);
                    return;
                }

                    AddError(result.Error!);
                    await Send.ErrorsAsync(409, ct);
                    return;
                }

                await Send.OkAsync(new { message = "Operator deactivated." }, cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Operator deactivation failed for {OperatorId}", request.Id);
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
