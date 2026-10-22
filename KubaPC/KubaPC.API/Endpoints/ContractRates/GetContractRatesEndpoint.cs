using FastEndpoints;
using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.ContractRates.Requests;
using KubaPC.Models.Contracts.ContractRates.Responses;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.ContractRates
{
    public class GetContractRatesEndpoint : Endpoint<ContractRateListRequest, PagedResponse<ContractRateResponse>>
    {
        private readonly IContractRateService contractRateService;

        public GetContractRatesEndpoint(IContractRateService contractRateService)
        {
            this.contractRateService = contractRateService;
        }

        public override void Configure()
        {
            Get("/contract-rates");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Admin");
        }

        public override async Task HandleAsync(ContractRateListRequest request, CancellationToken ct)
        {
            try
            {
                await Send.OkAsync(await contractRateService.SelectPagedRates(request), cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Get contract rates failed");
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
