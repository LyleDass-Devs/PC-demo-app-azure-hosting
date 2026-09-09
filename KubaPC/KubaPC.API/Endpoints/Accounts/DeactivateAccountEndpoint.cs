using FastEndpoints;
using KubaPC.Models.Contracts.Accounts.Requests;
using KubaPC.Models.Internal;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.Accounts
{

    public class DeactivateAccountEndpoint : Endpoint<DeactivateAccountRequest>
    {
        private readonly IAccountService accountService;

        public DeactivateAccountEndpoint(IAccountService accountService)
        {
            this.accountService = accountService;
        }

        public override void Configure()
        {
            Post("/accounts/deactivate");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Admin");
        }

        public override async Task HandleAsync(DeactivateAccountRequest request, CancellationToken ct)
        {
            try
            {
                Guid modifiedBy = Guid.Parse(User.FindFirst("UserId")!.Value);

                AccountOperationResult result = await accountService.DeactivateAccount(request.Id, modifiedBy);

                if (!result.IsSuccess)
                {
                    AddError(result.Error!);
                    await Send.ErrorsAsync(409, ct);
                    return;
                }

                await Send.OkAsync(new { message = "Account deactivated." }, cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Account deactivation failed for {AccountId}", request.Id);
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}