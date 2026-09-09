using FastEndpoints;
using KubaPC.Models.Contracts.Accounts.Requests;
using KubaPC.Models.Internal;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.Accounts
{
    public class UpdateAccountEndpoint : Endpoint<UpdateAccountRequest>
    {
        private readonly IAccountService accountService;

        public UpdateAccountEndpoint(IAccountService accountService)
        {
            this.accountService = accountService;
        }

        public override void Configure()
        {
            Put("/accounts/update");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Admin");
        }

        public override async Task HandleAsync(UpdateAccountRequest request, CancellationToken ct)
        {
            try
            {
                Guid modifiedBy = Guid.Parse(User.FindFirst("UserId")!.Value);

                AccountOperationResult result = await accountService.UpdateAccount(request, modifiedBy);

                if (!result.IsSuccess)
                {
                    AddError(result.Error!);
                    await Send.ErrorsAsync(409, ct);
                    return;
                }

                await Send.OkAsync(new { message = "Account updated." }, cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Account update failed for {AccountId}", request.Id);
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}