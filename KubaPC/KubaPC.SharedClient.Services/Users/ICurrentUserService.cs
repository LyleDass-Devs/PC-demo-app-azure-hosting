using KubaPC.Models.Contracts.Accounts.Responses;

namespace KubaPC.SharedClient.Services.Users
{
    /// <summary>The signed-in account as the server reports it, fetched once per session.</summary>
    public interface ICurrentUserService
    {
        Task<CurrentUserResponse?> Get();
        Task<CurrentUserResponse?> Refresh();
        void Clear();
    }
}
