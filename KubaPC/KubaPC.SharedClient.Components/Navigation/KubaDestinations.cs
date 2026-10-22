using KubaPC.SharedClient.Components.Icons;
using KubaPC.SharedClient.Services;

namespace KubaPC.SharedClient.Components.Navigation
{

    public static class KubaDestinations
    {
        public static readonly IReadOnlyList<NavDestination> All = new[]
        {
            new NavDestination("Home", ClientRoutes.HOME, KubaIcons.Regular.House, Exact: true),
            new NavDestination("Operators", ClientRoutes.OPERATORS, KubaIcons.Regular.Bus, Roles: "SuperAdmin,Admin", App: KubaApp.Admin),
            new NavDestination("Contracts", ClientRoutes.CONTRACTS, KubaIcons.Regular.ClipboardText, Roles: "SuperAdmin,Admin", App: KubaApp.Admin),
            new NavDestination("Catalogue", ClientRoutes.CATALOGUE, KubaIcons.Regular.Gear, Roles: "SuperAdmin,Admin", App: KubaApp.Admin),
            new NavDestination("Accounts", ClientRoutes.ACCOUNTS, KubaIcons.Regular.Users, Roles: "SuperAdmin,Admin", App: KubaApp.Admin),
            new NavDestination("Me", ClientRoutes.ME, KubaIcons.Regular.User)
        };
    }
}
