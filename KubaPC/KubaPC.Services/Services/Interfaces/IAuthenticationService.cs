using System;
using System.Collections.Generic;
using System.Text;
using KubaPC.Models.Contracts.Authentication.Requests;
using KubaPC.Models.Contracts.Authentication.Responses;

namespace KubaPC.Services.Services.Interfaces
{
    public interface IAuthenticationService
    {
         Task<AuthenticationResponse?> Authenticate(AuthenticationRequest request);
    }
}
