using FastEndpoints;
using KubaPC.Models.Contracts.Accounts.Responses;
using KubaPC.Models.Internal;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;

namespace KubaPC.API.Endpoints.Accounts
{
    public class GetCurrentUserEndpoint : EndpointWithoutRequest<CurrentUserResponse>
    {
        private readonly IAccountService accountService;

        public GetCurrentUserEndpoint(IAccountService accountService)
        {
            this.accountService = accountService;
        }

        public override void Configure()
        {
            Get("/accounts/current");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
        }

        public override async Task HandleAsync(CancellationToken ct)
        {
            string emailAddress = User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;

            // The token carries the name as it was at sign-in; the account row has the current one.
            AccountCredentialsModel? account = await accountService.SelectActiveByEmail(emailAddress);

            CurrentUserResponse response = new()
            {
                Id = Guid.TryParse(User.FindFirst("UserId")?.Value, out Guid id) ? id : Guid.Empty,
                FullName = account?.FullName ?? User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty,
                EmailAddress = User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty,
                MustChangePassword = User.HasClaim("PasswordChangeRequired", "true"),
                Roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList()
            };

            await Send.OkAsync(response, cancellation: ct);
        }
    }
}
