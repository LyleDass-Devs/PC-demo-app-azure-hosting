using KubaPC.Models.Contracts.Authentication.Requests;
using KubaPC.Models.Contracts.Authentication.Responses;
using KubaPC.SharedClient.Services.Authentication;
using KubaPC.SharedClient.Services.Http;
using KubaPC.SharedClient.Services.Models;
using Microsoft.AspNetCore.Components.Authorization;

namespace KubaPC.SharedClient.Services.Api
{
    public class AuthenticationClientService : IAuthenticationClientService
    {
        private readonly IHttpService httpService;
        private readonly AuthenticationStateProvider authenticationStateProvider;

        public AuthenticationClientService(IHttpService httpService, AuthenticationStateProvider authenticationStateProvider)
        {
            this.httpService = httpService;
            this.authenticationStateProvider = authenticationStateProvider;
        }

        public async Task<ApiResult<AuthenticationResponse>> Login(AuthenticationRequest request)
        {
            ApiResult<AuthenticationResponse> result = await httpService.Post<AuthenticationResponse>("/authentication/authenticate", request);

            if (result.IsSuccess && result.Data is not null)
            {
                await Provider.MarkUserAsAuthenticated(result.Data.Token);
            }

            return result;
        }

        public async Task SignOut()
        {
            await Provider.MarkUserAsSignedOut();
        }

        // Registered against the framework's base type, so the concrete type is
        // recovered here rather than registering the same instance twice.
        private KubaPcAuthenticationStateProvider Provider =>
            (KubaPcAuthenticationStateProvider)authenticationStateProvider;
    }
}