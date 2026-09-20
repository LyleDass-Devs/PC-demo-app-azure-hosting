using FastEndpoints;
using KubaPC.Models.Contracts.Escalations.Requests;
using KubaPC.Models.Contracts.Escalations.Responses;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.Escalations
{
    public class GetEscalationParametersEndpoint : Endpoint<GetEscalationParametersRequest, EscalationParametersResponse>
    {
        private readonly IEscalationParameterService escalationParameterService;

        public GetEscalationParametersEndpoint(IEscalationParameterService escalationParameterService)
        {
            this.escalationParameterService = escalationParameterService;
        }

        public override void Configure()
        {
            Get("/escalation-parameters");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Admin");
        }

        public override async Task HandleAsync(GetEscalationParametersRequest request, CancellationToken ct)
        {
            try
            {
                EscalationParametersResponse? response = await escalationParameterService.SelectByContract(request.ContractId);
                if (response is null)
                {
                    await Send.NoContentAsync(ct);
                    return;
                }

                await Send.OkAsync(response, cancellation: ct);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Get escalation parameters failed for {ContractId}", request.ContractId);
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
