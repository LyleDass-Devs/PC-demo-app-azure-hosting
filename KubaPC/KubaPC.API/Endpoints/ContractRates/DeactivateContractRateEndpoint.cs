using FastEndpoints;
using KubaPC.API.Security;
using KubaPC.Models.Contracts.ContractRates.Requests;
using KubaPC.Models.Internal;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.ContractRates
{
    public class DeactivateContractRateEndpoint : Endpoint<DeactivateContractRateRequest>
    {
        private readonly IContractRateService contractRateService;

        public DeactivateContractRateEndpoint(IContractRateService contractRateService)
        {
            this.contractRateService = contractRateService;
        }

        public override void Configure()
        {
            Post("/contract-rates/deactivate");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Admin");
        }

        public override async Task HandleAsync(DeactivateContractRateRequest request, CancellationToken ct)
        {
            try
            {
                Caller caller = CallerReader.Read(User);

                OperationResult result = await contractRateService.DeactivateRate(request.Id, caller);

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

                await Send.OkAsync(new { message = "Contract rate removed." }, cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Contract rate removal failed for {RateId}", request.Id);
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
