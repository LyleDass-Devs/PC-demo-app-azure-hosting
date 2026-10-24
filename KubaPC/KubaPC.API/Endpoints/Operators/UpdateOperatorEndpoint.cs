using FastEndpoints;
using KubaPC.API.Security;
using KubaPC.Models.Contracts.Operators.Requests;
using KubaPC.Models.Internal;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.Operators
{
    public class UpdateOperatorEndpoint : Endpoint<UpdateOperatorRequest>
    {
        private readonly IOperatorService operatorService;

        public UpdateOperatorEndpoint(IOperatorService operatorService)
        {
            this.operatorService = operatorService;
        }

        public override void Configure()
        {
            Post("/operators/update");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Admin");
        }

        public override async Task HandleAsync(UpdateOperatorRequest request, CancellationToken ct)
        {
            try
            {
                Caller caller = CallerReader.Read(User);

                OperationResult result = await operatorService.UpdateOperator(request, caller);

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

                await Send.OkAsync(new { message = "Operator updated." }, cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Operator update failed for {OperatorId}", request.Id);
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
