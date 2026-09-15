using KubaPC.Models.Contracts.Accounts.Responses;

namespace KubaPC.SharedClient.Services.Users
{
    /// <summary>The signed-in account as the server reports it, fetched once per session.</summary>
    public interface ICurrentUserService
    {
        /// <summary>The cached account without fetching; null before the first Get or after Clear.</summary>
        CurrentUserResponse? Current { get; }
        Task<CurrentUserResponse?> Get();
        Task<CurrentUserResponse?> Refresh();
        void Clear();
        /// <summary>Raised after a refresh or clear, so shell parts showing the name can re-render.</summary>
        event Action? Changed;
    }
}
