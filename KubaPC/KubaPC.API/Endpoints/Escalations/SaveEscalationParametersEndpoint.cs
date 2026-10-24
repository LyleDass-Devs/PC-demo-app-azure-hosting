using FastEndpoints;
using KubaPC.API.Security;
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
                Caller caller = CallerReader.Read(User);

                OperationResult result = await escalationParameterService.Save(request, caller);

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
