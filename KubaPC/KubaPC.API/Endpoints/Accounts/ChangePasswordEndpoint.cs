using FastEndpoints;
using KubaPC.Models.Contracts.Accounts.Requests;
using KubaPC.Services.Services.Interfaces;
using KubaPC.Services.Services.Logic;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.Accounts
{
    public class ChangePasswordEndpoint : Endpoint<ChangePasswordRequest>
    {
        private readonly IAccountService accountService;

        public ChangePasswordEndpoint(IAccountService accountService)
        {
            this.accountService = accountService;
        }

        public override void Configure()
        {
            Post("/accounts/change-password");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
        }

        public override async Task HandleAsync(ChangePasswordRequest request, CancellationToken ct)
        {
            try
            {
                Guid accountId = Guid.Parse(User.FindFirst("UserId")!.Value);
                bool isChanged = await accountService.ChangePassword(accountId, request.OldPassword, request.NewPassword);

                if (!isChanged)
                {
                    AddError("The current password is incorrect.");
                    await Send.ErrorsAsync(400, ct);
                    return;
                }

                await Send.OkAsync(new { message = "Password changed. Please log in again." }, cancellation: ct);
            }
            catch (Exception e)
            {

                Logger.LogError(e, "Password change failed");
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
