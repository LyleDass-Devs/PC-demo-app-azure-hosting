using FastEndpoints;
using KubaPC.Models.Contracts.Operators.Requests;
using KubaPC.Models.Internal;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.Operators
{
    public class CreateOperatorEndpoint : Endpoint<CreateOperatorRequest, Guid>
    {
        private readonly IOperatorService operatorService;

        public CreateOperatorEndpoint(IOperatorService operatorService)
        {
            this.operatorService = operatorService;
        }

        public override void Configure()
        {
            Post("/operators/create");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("SuperAdmin");
        }

        public override async Task HandleAsync(CreateOperatorRequest request, CancellationToken ct)
        {
            try
            {
                Guid actor = Guid.Parse(User.FindFirst("UserId")!.Value);

                CreateResult result = await operatorService.CreateOperator(request, actor);

                if (!result.IsSuccess)
                {
                    AddError(result.Error!);
                    await Send.ErrorsAsync(409, ct);
                    return;
                }

                await Send.ResponseAsync(result.Id!.Value, 201, cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Operator creation failed for {IdSociete}", request.IdSociete);
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
