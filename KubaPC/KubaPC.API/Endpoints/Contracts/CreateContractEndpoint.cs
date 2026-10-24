using FastEndpoints;
using KubaPC.API.Security;
using KubaPC.Models.Contracts.Contracts.Requests;
using KubaPC.Models.Internal;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.Contracts
{
    public class CreateContractEndpoint : Endpoint<CreateContractRequest, Guid>
    {
        private readonly IContractService contractService;

        public CreateContractEndpoint(IContractService contractService)
        {
            this.contractService = contractService;
        }

        public override void Configure()
        {
            Post("/contracts/create");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Admin");
        }

        public override async Task HandleAsync(CreateContractRequest request, CancellationToken ct)
        {
            try
            {
                Caller caller = CallerReader.Read(User);

                CreateResult result = await contractService.CreateContract(request, caller);

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

                await Send.ResponseAsync(result.Id!.Value, 201, cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Contract creation failed for {ContractNumber}", request.ContractNumber);
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
