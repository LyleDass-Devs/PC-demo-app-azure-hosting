using FastEndpoints;
using KubaPC.Models.Contracts.Accounts.Requests;
using KubaPC.Models.Internal;
using KubaPC.Services.Services.Interfaces;
using KubaPC.Services.Services.Logic;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Net;

namespace KubaPC.API.Endpoints.Accounts
{
    public class CreateAccountEndpoint : Endpoint<CreateAccountRequest, Guid>
    {
        private readonly IAccountService accountService;

        public CreateAccountEndpoint(IAccountService accountService)
        {
            this.accountService = accountService;
        }

        public override void Configure()
        {
            Post("/accounts/create");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Admin");
        }

        public override async Task HandleAsync(CreateAccountRequest request, CancellationToken ct)
        {
            try
            {
                string? createdByClaim = User.FindFirst("UserId")?.Value;

                if (!Guid.TryParse(createdByClaim, out Guid createdBy))
                {
                    await Send.UnauthorizedAsync(cancellation: ct);
                    return;
                }

                CreateAccountResult result = await accountService.CreateAccount(request, createdBy);

                if (!result.IsSuccess)
                {
                    AddError(result.Error ?? "Account could not be created.");
                    await Send.ErrorsAsync((int)HttpStatusCode.Conflict, cancellation: ct);
                    return;
                }

                await Send.ResponseAsync(result.Id!.Value, (int)HttpStatusCode.Created, cancellation: ct);

            }
            catch (Exception e)
            {
                Logger.LogError(e, "Create account failed for {EmailAddress}", request.EmailAddress);
                await Send.ErrorsAsync((int)HttpStatusCode.InternalServerError, cancellation: ct);
            }
        }
    }
}
