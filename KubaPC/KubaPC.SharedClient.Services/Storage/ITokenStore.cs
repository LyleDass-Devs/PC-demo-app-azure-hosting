using System;
using System.Collections.Generic;
using System.Text;

namespace KubaPC.SharedClient.Services.Storage
{
    public interface ITokenStore
    {
        Task<string?> GetToken();
        Task SetToken(string token);
        Task ClearToken();
    }
}
