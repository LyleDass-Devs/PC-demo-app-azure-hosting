using FastEndpoints;
using KubaPC.Models.Contracts.IncomeFields.Requests;
using KubaPC.Models.Contracts.IncomeFields.Responses;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.IncomeFields
{
    public class GetIncomeFieldsEndpoint : Endpoint<GetIncomeFieldsRequest, IncomeFieldsResponse>
    {
        private readonly IIncomeFieldService incomeFieldService;

        public GetIncomeFieldsEndpoint(IIncomeFieldService incomeFieldService)
        {
            this.incomeFieldService = incomeFieldService;
        }

        public override void Configure()
        {
            Get("/income-fields");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Admin");
        }

        public override async Task HandleAsync(GetIncomeFieldsRequest request, CancellationToken ct)
        {
            try
            {
                IncomeFieldsResponse? response = await incomeFieldService.SelectByContract(request.ContractId);
                if (response is null)
                {
                    await Send.NoContentAsync(ct);
                    return;
                }

                await Send.OkAsync(response, cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Get income fields failed for {ContractId}", request.ContractId);
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
