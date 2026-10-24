using FastEndpoints;
using KubaPC.API.Security;
using KubaPC.Models.Contracts.Accounts.Requests;
using KubaPC.Models.Contracts.Accounts.Responses;
using KubaPC.Models.Internal;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.Accounts
{
    public class ReactivateAccountEndpoint : Endpoint<ReactivateAccountRequest>
    {
        private readonly IAccountService accountService;

        public ReactivateAccountEndpoint(IAccountService accountService)
        {
            this.accountService = accountService;
        }

        public override void Configure()
        {
            Post("/accounts/reactivate");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Admin");
        }

        public override async Task HandleAsync(ReactivateAccountRequest request, CancellationToken ct)
        {
            try
            {
                Caller caller = CallerReader.Read(User);

                ResetPasswordResult result = await accountService.ReactivateAccount(request.Id, caller);

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
            catch (Exception exception)
            {
                Logger.LogError(exception, "Account reactivation failed for {AccountId}", request.Id);
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
