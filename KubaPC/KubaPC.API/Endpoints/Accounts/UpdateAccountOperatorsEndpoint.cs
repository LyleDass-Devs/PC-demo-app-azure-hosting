using FastEndpoints;
using KubaPC.API.Security;
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
                Caller caller = CallerReader.Read(User);

                AccountOperationResult result = await accountService.UpdateOperators(request, caller);

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
