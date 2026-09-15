using FastEndpoints;
using KubaPC.Models.Contracts.Accounts.Requests;
using KubaPC.Models.Internal;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.Accounts
{
    public class UpdateOwnAccountEndpoint : Endpoint<UpdateOwnAccountRequest>
    {
        private readonly IAccountService accountService;

        public UpdateOwnAccountEndpoint(IAccountService accountService)
        {
            this.accountService = accountService;
        }

        public override void Configure()
        {
            Put("/accounts/me");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
        }

        public override async Task HandleAsync(UpdateOwnAccountRequest request, CancellationToken ct)
        {
            try
            {
                Guid accountId = Guid.Parse(User.FindFirst("UserId")!.Value);

                AccountOperationResult result = await accountService.UpdateOwnName(accountId, request.FullName);

                if (!result.IsSuccess)
                {
                    AddError(result.Error!);
                    await Send.ErrorsAsync(409, ct);
                    return;
                }

                await Send.OkAsync(new { message = "Name updated." }, cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Own account update failed for {AccountId}", User.FindFirst("UserId")?.Value);
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
