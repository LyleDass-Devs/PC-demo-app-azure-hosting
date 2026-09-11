using KubaPC.SharedClient.Services.Storage;
using Microsoft.AspNetCore.Components.Authorization;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace KubaPC.SharedClient.Services.Authentication
{
    public class KubaPcAuthenticationStateProvider : AuthenticationStateProvider
    {
        private const string AUTHENTICATION_TYPE = "KubaPCJwt";

        private readonly ITokenStore tokenStore;

        public KubaPcAuthenticationStateProvider(ITokenStore tokenStore)
        {
            this.tokenStore = tokenStore;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            string? token = await tokenStore.GetToken();

            if (string.IsNullOrWhiteSpace(token) || JwtClaimsReader.IsExpired(token))
            {
                if (!string.IsNullOrWhiteSpace(token))
                {
                    await tokenStore.ClearToken();
                }

                return CreateAnonymousState();
            }

            ClaimsIdentity identity = new(JwtClaimsReader.ReadClaims(token), AUTHENTICATION_TYPE);

            return new AuthenticationState(new ClaimsPrincipal(identity));
        }

        /// Call after a successful sign-in so the interface updates immediately.
        public async Task MarkUserAsAuthenticated(string token)
        {
            await tokenStore.SetToken(token);
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        public async Task MarkUserAsSignedOut()
        {
            await tokenStore.ClearToken();
            NotifyAuthenticationStateChanged(Task.FromResult(CreateAnonymousState()));
        }

        private static AuthenticationState CreateAnonymousState()
        {
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }
}
