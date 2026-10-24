using FastEndpoints;
using KubaPC.API.Security;
using KubaPC.Models.Contracts.Contracts.Requests;
using KubaPC.Models.Contracts.Contracts.Responses;
using KubaPC.Models.Internal;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.Contracts
{
    public class GetContractEndpoint : Endpoint<GetContractRequest, ContractListResponse>
    {
        private readonly IContractService contractService;

        public GetContractEndpoint(IContractService contractService)
        {
            this.contractService = contractService;
        }

        public override void Configure()
        {
            Get("/contracts/{id}");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Admin");
        }

        public override async Task HandleAsync(GetContractRequest request, CancellationToken ct)
        {
            try
            {
                ContractListResponse? response = await contractService.SelectContract(request.Id, CallerReader.Read(User));

                if (response is null)
                {
                    await Send.NotFoundAsync(ct);
                    return;
                }

                await Send.OkAsync(response, cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Get contract failed for {ContractId}", request.Id);
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
