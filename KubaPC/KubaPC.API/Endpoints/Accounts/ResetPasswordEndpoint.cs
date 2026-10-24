using FastEndpoints;
using KubaPC.API.Security;
using KubaPC.Models.Contracts.Accounts.Requests;
using KubaPC.Models.Contracts.Accounts.Responses;
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
            Options(x => x.RequireRateLimiting(RateLimitPolicies.SENSITIVE));
        }

        public override async Task HandleAsync(ResetPasswordRequest request, CancellationToken ct)
        {
            try
            {
                Caller caller = CallerReader.Read(User);

                ResetPasswordResult result = await accountService.ResetPassword(request.Id, caller);

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

                await Send.OkAsync(new ResetPasswordResponse
                {
                    TemporaryPassword = result.TemporaryPassword!,
                    ExpiresAt = result.ExpiresAt!.Value
                }, cancellation: ct);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Password reset failed for {AccountId}", request.Id);
                await Send.ErrorsAsync(500, ct);
            }           
        }
    }
}