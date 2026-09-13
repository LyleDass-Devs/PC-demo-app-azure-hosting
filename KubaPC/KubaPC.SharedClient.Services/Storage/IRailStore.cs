namespace KubaPC.SharedClient.Services.Storage
{
    /// <summary>Remembers whether the user collapsed the navigation rail.</summary>
    public interface IRailStore
    {
        Task<bool> GetCollapsed();
        Task SetCollapsed(bool collapsed);
    }
}
