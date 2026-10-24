using FastEndpoints;
using KubaPC.API.Security;
using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.Contracts.Requests;
using KubaPC.Models.Internal;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.Contracts
{
    public class GetContractDropDownEndpoint : Endpoint<ContractDropDownRequest, List<DropDownResponse>>
    {
        private readonly IContractService contractService;

        public GetContractDropDownEndpoint(IContractService contractService)
        {
            this.contractService = contractService;
        }

        public override void Configure()
        {
            Get("/contracts/dropdown");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Reader");
        }

        public override async Task HandleAsync(ContractDropDownRequest request, CancellationToken ct)
        {
            try
            {
                await Send.OkAsync(await contractService.SelectDropDown(request.OperatorId, CallerReader.Read(User)), cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Get contract dropdown failed");
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
