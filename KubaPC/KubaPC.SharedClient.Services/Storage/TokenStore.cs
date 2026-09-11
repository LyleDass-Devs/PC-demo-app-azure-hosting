using Blazored.LocalStorage;
using System;
using System.Collections.Generic;
using System.Text;

namespace KubaPC.SharedClient.Services.Storage
{
    public class TokenStore : ITokenStore
    {
        private const string TOKEN_KEY = "kubapc.token";

        private readonly ILocalStorageService localStorageService;

        public TokenStore(ILocalStorageService localStorageService)
        {
            this.localStorageService = localStorageService;
        }

        public async Task<string?> GetToken()
        {
            try
            {
                return await localStorageService.GetItemAsStringAsync(TOKEN_KEY);
            }
            catch (Exception)
            {

                return null;
            }
        }

        public async Task SetToken(string token)
        {
            await localStorageService.SetItemAsStringAsync(TOKEN_KEY, token);
        }

        public async Task ClearToken()
        {
            await localStorageService.RemoveItemAsync(TOKEN_KEY);
        }
    }

}
