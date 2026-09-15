using FastEndpoints;
using KubaPC.Models.Contracts.Accounts.Requests;
using KubaPC.Models.Internal;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.Accounts
{
    public class ResetPasswordEndpoint : Endpoint<ResetPasswordRequest>
    {
        private readonly IAccountService accountService;

        public ResetPasswordEndpoint(IAccountService accountService)
        {
            this.accountService = accountService;
        }

        public override void Configure()
        {
            Post("/accounts/reset-password");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Admin");
        }

        public override async Task HandleAsync(ResetPasswordRequest request, CancellationToken ct)
        {
            try
            {
                Guid modifiedBy = Guid.Parse(User.FindFirst("UserId")!.Value);

                AccountOperationResult result = await accountService.ResetPassword(request.Id, request.TemporaryPassword, modifiedBy);

                if (!result.IsSuccess)
                {
                    AddError(result.Error!);
                    await Send.ErrorsAsync(409, ct);
                    return;
                }

                await Send.OkAsync(new { message = "Password reset. The person must choose a new one at next sign-in." }, cancellation: ct);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Password reset failed for {AccountId}", request.Id);
                await Send.ErrorsAsync(500, ct);
            }           
        }
    }
}