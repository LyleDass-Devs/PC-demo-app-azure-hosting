using FastEndpoints;
using KubaPC.API.Security;
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
                Caller caller = CallerReader.Read(User);

                AccountOperationResult result = await accountService.ResetPassword(request.Id, request.TemporaryPassword, caller);

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