using Blazored.SessionStorage;

namespace KubaPC.SharedClient.Services.Storage
{
    public class TokenStore : ITokenStore
    {
        private const string TOKEN_KEY = "kubapc.token";

        private readonly ISessionStorageService sessionStorageService;

        public TokenStore(ISessionStorageService sessionStorageService)
        {
            this.sessionStorageService = sessionStorageService;
        }

        public async Task<string?> GetToken()
        {
            try
            {
                return await sessionStorageService.GetItemAsStringAsync(TOKEN_KEY);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task SetToken(string token)
        {
            await sessionStorageService.SetItemAsStringAsync(TOKEN_KEY, token);
        }

        public async Task ClearToken()
        {
            await sessionStorageService.RemoveItemAsync(TOKEN_KEY);
        }
    }
}
