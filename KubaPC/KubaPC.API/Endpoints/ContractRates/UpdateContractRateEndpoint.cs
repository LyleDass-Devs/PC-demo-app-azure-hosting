using FastEndpoints;
using KubaPC.Models.Contracts.ContractRates.Requests;
using KubaPC.Models.Internal;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.ContractRates
{
    public class UpdateContractRateEndpoint : Endpoint<UpdateContractRateRequest>
    {
        private readonly IContractRateService contractRateService;

        public UpdateContractRateEndpoint(IContractRateService contractRateService)
        {
            this.contractRateService = contractRateService;
        }

        public override void Configure()
        {
            Post("/contract-rates/update");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Admin");
        }

        public override async Task HandleAsync(UpdateContractRateRequest request, CancellationToken ct)
        {
            try
            {
                Guid actor = Guid.Parse(User.FindFirst("UserId")!.Value);

                OperationResult result = await contractRateService.UpdateRate(request, actor);

                if (!result.IsSuccess)
                {
                    AddError(result.Error!);
                    await Send.ErrorsAsync(409, ct);
                    return;
                }

                await Send.OkAsync(new { message = "Contract rate updated." }, cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Contract rate update failed for {RateId}", request.Id);
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
