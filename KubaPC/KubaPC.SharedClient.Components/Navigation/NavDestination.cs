namespace KubaPC.SharedClient.Components.Navigation
{
    /// <summary>
    /// One place the navigation can take the user. Exact means the route must match completely (used for "/").
    /// Roles, when set, is the comma-separated list of roles that see this destination; null means everyone.
    /// App, when set, is the only client that has the screen; null means both apps.
    /// </summary>
    public sealed record NavDestination(string Label, string Href, string Icon, bool Exact = false, string? Roles = null, KubaApp? App = null)
    {
        public bool IsVisibleTo(IEnumerable<string> userRoles, KubaApp app) =>
            (App is null || App == app)
            && (Roles is null || Roles.Split(',', StringSplitOptions.TrimEntries).Intersect(userRoles).Any());
    }
}
