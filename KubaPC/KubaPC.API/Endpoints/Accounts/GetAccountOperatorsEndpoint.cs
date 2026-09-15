using FastEndpoints;
using KubaPC.Models.Contracts.Accounts.Requests;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.Accounts
{
    public class GetAccountOperatorsEndpoint : Endpoint<GetAccountOperatorsRequest, List<Guid>>
    {
        private readonly IAccountService accountService;

        public GetAccountOperatorsEndpoint(IAccountService accountService)
        {
            this.accountService = accountService;
        }

        public override void Configure()
        {
            Get("/accounts/{id}/operators");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Admin");
        }

        public override async Task HandleAsync(GetAccountOperatorsRequest request, CancellationToken ct)
        {
            try
            {
                await Send.OkAsync(await accountService.SelectOperatorIds(request.Id), cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Get account operators failed for {AccountId}", request.Id);
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
