using FastEndpoints;
using KubaPC.Models.Contracts.Accounts.Responses;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;

namespace KubaPC.API.Endpoints.Accounts
{
    public class GetCurrentUserEndpoint : EndpointWithoutRequest<CurrentUserResponse>
    {
        public override void Configure()
        {
            Get("/accounts/current");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
        }

        public override async Task HandleAsync(CancellationToken ct)
        {
            CurrentUserResponse response = new()
            {
                Id = Guid.TryParse(User.FindFirst("UserId")?.Value, out Guid id) ? id : Guid.Empty,
                FullName = User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty,
                EmailAddress = User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty,
                Roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList()
            };

            await Send.OkAsync(response, cancellation: ct);
        }
    }
}
