using FastEndpoints;
using KubaPC.Models.Contracts.Contracts.Requests;
using KubaPC.Models.Internal;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.Contracts
{
    public class ReactivateContractEndpoint : Endpoint<ReactivateContractRequest>
    {
        private readonly IContractService contractService;

        public ReactivateContractEndpoint(IContractService contractService)
        {
            this.contractService = contractService;
        }

        public override void Configure()
        {
            Post("/contracts/reactivate");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Admin");
        }

        public override async Task HandleAsync(ReactivateContractRequest request, CancellationToken ct)
        {
            try
            {
                Guid actor = Guid.Parse(User.FindFirst("UserId")!.Value);

                OperationResult result = await contractService.ReactivateContract(request.Id, actor);

                if (!result.IsSuccess)
                {
                    AddError(result.Error!);
                    await Send.ErrorsAsync(409, ct);
                    return;
                }

                await Send.OkAsync(new { message = "Contract reactivated." }, cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Contract reactivation failed for {ContractId}", request.Id);
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
