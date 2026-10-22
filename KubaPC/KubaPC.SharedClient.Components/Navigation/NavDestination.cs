namespace KubaPC.SharedClient.Components.Navigation
{

    public sealed record NavDestination(string Label, string Href, string Icon, bool Exact = false, string? Roles = null, KubaApp? App = null)
    {
        public bool IsVisibleTo(IEnumerable<string> userRoles, KubaApp app) =>
            (App is null || App == app)
            && (Roles is null || Roles.Split(',', StringSplitOptions.TrimEntries).Intersect(userRoles).Any());
    }
}
