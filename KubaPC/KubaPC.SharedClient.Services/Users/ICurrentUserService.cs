using KubaPC.Models.Contracts.Accounts.Responses;

namespace KubaPC.SharedClient.Services.Users
{

    public interface ICurrentUserService
    {

        CurrentUserResponse? Current { get; }
        Task<CurrentUserResponse?> Get();
        Task<CurrentUserResponse?> Refresh();
        void Clear();

        event Action? Changed;
    }
}
