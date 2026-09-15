using FastEndpoints;
using KubaPC.Models.Contracts.Accounts.Requests;
using KubaPC.Models.Internal;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.Accounts
{
    public class UpdateAccountOperatorsEndpoint : Endpoint<UpdateAccountOperatorsRequest>
    {
        private readonly IAccountService accountService;

        public UpdateAccountOperatorsEndpoint(IAccountService accountService)
        {
            this.accountService = accountService;
        }

        public override void Configure()
        {
            Post("/accounts/operators/update");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Admin");
        }

        public override async Task HandleAsync(UpdateAccountOperatorsRequest request, CancellationToken ct)
        {
            try
            {
                Guid modifiedBy = Guid.Parse(User.FindFirst("UserId")!.Value);

                AccountOperationResult result = await accountService.UpdateOperators(request, modifiedBy);

                if (!result.IsSuccess)
                {
                    AddError(result.Error!);
                    await Send.ErrorsAsync(409, ct);
                    return;
                }

                await Send.OkAsync(new { message = "Operators updated." }, cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Account operators update failed for {AccountId}", request.AccountId);
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
