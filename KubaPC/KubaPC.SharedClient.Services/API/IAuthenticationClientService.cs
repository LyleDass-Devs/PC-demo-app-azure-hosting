using KubaPC.Models.Contracts.Authentication.Requests;
using KubaPC.Models.Contracts.Authentication.Responses;
using KubaPC.SharedClient.Services.Models;

namespace KubaPC.SharedClient.Services.Api
{
    public interface IAuthenticationClientService
    {
        Task<ApiResult<AuthenticationResponse>> Login(AuthenticationRequest request);
        Task SignOut();
    }
}