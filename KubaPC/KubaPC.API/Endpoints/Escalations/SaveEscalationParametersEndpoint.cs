using FastEndpoints;
using KubaPC.Models.Contracts.Escalations.Requests;
using KubaPC.Models.Internal;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.Escalations
{
    public class SaveEscalationParametersEndpoint : Endpoint<SaveEscalationParametersRequest>
    {
        private readonly IEscalationParameterService escalationParameterService;

        public SaveEscalationParametersEndpoint(IEscalationParameterService escalationParameterService)
        {
            this.escalationParameterService = escalationParameterService;
        }

        public override void Configure()
        {
            Post("/escalation-parameters/save");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Admin");
        }

        public override async Task HandleAsync(SaveEscalationParametersRequest request, CancellationToken ct)
        {
            try
            {
                Guid actor = Guid.Parse(User.FindFirst("UserId")!.Value);

                OperationResult result = await escalationParameterService.Save(request, actor);

                if (!result.IsSuccess)
                {
                    AddError(result.Error!);
                    await Send.ErrorsAsync(409, ct);
                    return;
                }

                await Send.OkAsync(new { message = "Escalation parameters saved." }, cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Save escalation parameters failed for {ContractId}", request.ContractId);
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
