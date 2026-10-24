using FastEndpoints;
using KubaPC.API.Security;
using KubaPC.Models.Contracts.Contracts.Requests;
using KubaPC.Models.Internal;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.Contracts
{
    public class UpdateContractEndpoint : Endpoint<UpdateContractRequest>
    {
        private readonly IContractService contractService;

        public UpdateContractEndpoint(IContractService contractService)
        {
            this.contractService = contractService;
        }

        public override void Configure()
        {
            Post("/contracts/update");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Admin");
        }

        public override async Task HandleAsync(UpdateContractRequest request, CancellationToken ct)
        {
            try
            {
                Caller caller = CallerReader.Read(User);

                OperationResult result = await contractService.UpdateContract(request, caller);

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

                await Send.OkAsync(new { message = "Contract updated." }, cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Contract update failed for {ContractId}", request.Id);
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
