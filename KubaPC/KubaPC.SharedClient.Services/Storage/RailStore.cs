using Blazored.LocalStorage;

namespace KubaPC.SharedClient.Services.Storage
{
    public class RailStore : IRailStore
    {
        private const string COLLAPSED_KEY = "kubapc.rail.collapsed";

        private readonly ILocalStorageService localStorageService;

        public RailStore(ILocalStorageService localStorageService)
        {
            this.localStorageService = localStorageService;
        }

        public async Task<bool> GetCollapsed()
        {
            // Storage can be refused (private mode); an unreadable preference is simply the default.
            try
            {
                return await localStorageService.GetItemAsync<bool?>(COLLAPSED_KEY) ?? false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task SetCollapsed(bool collapsed)
        {
            await localStorageService.SetItemAsync(COLLAPSED_KEY, collapsed);
        }
    }
}
