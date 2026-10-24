using FastEndpoints;
using KubaPC.API.Security;
using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.Contracts.Requests;
using KubaPC.Models.Contracts.Contracts.Responses;
using KubaPC.Models.Internal;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.Contracts
{
    public class GetContractListEndpoint : Endpoint<ContractListRequest, PagedResponse<ContractListResponse>>
    {
        private readonly IContractService contractService;

        public GetContractListEndpoint(IContractService contractService)
        {
            this.contractService = contractService;
        }

        public override void Configure()
        {
            Get("/contracts");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Admin");
        }

        public override async Task HandleAsync(ContractListRequest request, CancellationToken ct)
        {
            try
            {
                PagedResponse<ContractListResponse> response = await contractService.SelectPagedContractList(request, CallerReader.Read(User));
                await Send.OkAsync(response, cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Get contract list failed");
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
