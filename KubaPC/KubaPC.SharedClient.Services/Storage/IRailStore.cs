namespace KubaPC.SharedClient.Services.Storage
{

    public interface IRailStore
    {
        Task<bool> GetCollapsed();
        Task SetCollapsed(bool collapsed);
    }
}
