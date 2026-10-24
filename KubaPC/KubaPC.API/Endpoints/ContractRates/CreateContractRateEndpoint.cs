using FastEndpoints;
using KubaPC.API.Security;
using KubaPC.Models.Contracts.ContractRates.Requests;
using KubaPC.Models.Internal;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.ContractRates
{
    public class CreateContractRateEndpoint : Endpoint<CreateContractRateRequest, Guid>
    {
        private readonly IContractRateService contractRateService;

        public CreateContractRateEndpoint(IContractRateService contractRateService)
        {
            this.contractRateService = contractRateService;
        }

        public override void Configure()
        {
            Post("/contract-rates/create");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Admin");
        }

        public override async Task HandleAsync(CreateContractRateRequest request, CancellationToken ct)
        {
            try
            {
                Caller caller = CallerReader.Read(User);

                CreateResult result = await contractRateService.CreateRate(request, caller);

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
                Logger.LogError(exception, "Contract rate creation failed for {ContractId}", request.ContractId);
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
