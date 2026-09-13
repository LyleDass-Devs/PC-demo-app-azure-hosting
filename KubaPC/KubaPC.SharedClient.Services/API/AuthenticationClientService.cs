using KubaPC.Models.Contracts.Authentication.Requests;
using KubaPC.Models.Contracts.Authentication.Responses;
using KubaPC.SharedClient.Services.Authentication;
using KubaPC.SharedClient.Services.Http;
using KubaPC.SharedClient.Services.Models;
using KubaPC.SharedClient.Services.Users;
using Microsoft.AspNetCore.Components.Authorization;

namespace KubaPC.SharedClient.Services.Api
{
    public class AuthenticationClientService : IAuthenticationClientService
    {
        private readonly IHttpService httpService;
        private readonly AuthenticationStateProvider authenticationStateProvider;
        private readonly ICurrentUserService currentUserService;

        public AuthenticationClientService(IHttpService httpService, AuthenticationStateProvider authenticationStateProvider, ICurrentUserService currentUserService)
        {
            this.httpService = httpService;
            this.authenticationStateProvider = authenticationStateProvider;
            this.currentUserService = currentUserService;
        }

        public async Task<ApiResult<AuthenticationResponse>> Login(AuthenticationRequest request)
        {
            currentUserService.Clear();

            ApiResult<AuthenticationResponse> result = await httpService.Post<AuthenticationResponse>("/authentication/authenticate", request);

            if (result.IsSuccess && result.Data is not null)
            {
                await Provider.MarkUserAsAuthenticated(result.Data.Token);
            }

            return result;
        }

        public async Task SignOut()
        {
            currentUserService.Clear();
            await Provider.MarkUserAsSignedOut();
        }

   
        private KubaPcAuthenticationStateProvider Provider => (KubaPcAuthenticationStateProvider)authenticationStateProvider;
    }
}