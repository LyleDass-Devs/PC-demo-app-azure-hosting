using FastEndpoints;
using KubaPC.Models.Contracts.Accounts.Requests;
using KubaPC.Models.Contracts.Accounts.Responses;
using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.Accounts
{
    public class GetAccountListEndpoint : Endpoint<AccountListRequest, PagedResponse<AccountListResponse>>
    {
        private readonly IAccountService accountService;
        public GetAccountListEndpoint(IAccountService accountService)
        {
            this.accountService = accountService;
        }

        public override void Configure()
        {
            Get("/accounts");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Admin");
        }

        public override async Task HandleAsync(AccountListRequest request, CancellationToken ct)
        {
            try
            {
                PagedResponse<AccountListResponse> response = await accountService.SelectPagedAccountList(request);
                await Send.OkAsync(response, cancellation: ct);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Get Account list failed");
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
