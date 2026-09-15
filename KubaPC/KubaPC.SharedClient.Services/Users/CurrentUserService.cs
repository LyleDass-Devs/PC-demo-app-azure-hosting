using KubaPC.Models.Contracts.Accounts.Responses;
using KubaPC.SharedClient.Services.Api;
using KubaPC.SharedClient.Services.Models;

namespace KubaPC.SharedClient.Services.Users
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IAccountClientService accountClientService;
        private CurrentUserResponse? cached;

        public event Action? Changed;

        public CurrentUserResponse? Current => cached;

        public CurrentUserService(IAccountClientService accountClientService)
        {
            this.accountClientService = accountClientService;
        }

        public async Task<CurrentUserResponse?> Get()
        {
            return cached ?? await Refresh();
        }

        public async Task<CurrentUserResponse?> Refresh()
        {
            ApiResult<CurrentUserResponse> result = await accountClientService.GetCurrent();
            cached = result.IsSuccess ? result.Data : null;
            Changed?.Invoke();
            return cached;
        }

        public void Clear()
        {
            cached = null;
            Changed?.Invoke();
        }
    }
}
