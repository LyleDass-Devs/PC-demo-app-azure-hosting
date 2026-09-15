using KubaPC.SharedClient.Components.Icons;
using KubaPC.SharedClient.Services;

namespace KubaPC.SharedClient.Components.Navigation
{
    /// <summary>One task the home board can launch. Roles null means everyone; App null means both clients have the screen.</summary>
    public sealed record HomeTask(string Title, string Description, string Href, string Icon, string EdgeColor, string? Roles = null, KubaApp? App = null)
    {
        public bool IsVisibleTo(IEnumerable<string> userRoles, KubaApp app) =>
            (App is null || App == app)
            && (Roles is null || Roles.Split(',', StringSplitOptions.TrimEntries).Intersect(userRoles).Any());
    }

    /// <summary>
    /// Every task the app offers, in display order. Tasks whose module has not landed yet
    /// point at the coming-soon page, so every tile is a real destination.
    /// Vehicles and Shifts get their role filters when the Maintenance and Driver roles exist.
    /// </summary>
    public static class HomeTasks
    {
        private const string WORKFLOW_ROLES = "Operator Staff,Monitor,Authoriser,DOT,Admin,SuperAdmin";
        private const string CAPTURE_ROLES = "Operator Staff,Monitor,Authoriser,Admin,SuperAdmin";
        private const string ADMIN_ROLES = "SuperAdmin,Admin";

        public static readonly IReadOnlyList<HomeTask> All = new[]
        {
            new HomeTask("Payment certificates", "Create, submit and approve the month's certificate.", ClientRoutes.ComingSoon("certificates"), KubaIcons.Regular.Receipt, "var(--kuba-indigo)", WORKFLOW_ROLES),
            new HomeTask("Penalties", "Capture and review penalties against a certificate.", ClientRoutes.ComingSoon("penalties"), KubaIcons.Regular.WarningCircle, "var(--warn)", CAPTURE_ROLES),
            new HomeTask("Forms", "Forms A, B&H, C, D, E, F1 and G for the selected certificate.", ClientRoutes.ComingSoon("forms"), KubaIcons.Regular.ClipboardText, "var(--info)", WORKFLOW_ROLES),
            new HomeTask("Vehicles", "Record a breakdown or damage with photos.", ClientRoutes.ComingSoon("vehicles"), KubaIcons.Regular.Bus, "var(--ok)", App: KubaApp.Operator),
            new HomeTask("Shifts", "See today's duties and report a trip that did not run.", ClientRoutes.ComingSoon("shifts"), KubaIcons.Regular.Clock, "var(--info)", App: KubaApp.Operator),
            new HomeTask("Catalogue and rates", "Penalty list items, contract rates, escalation parameters.", ClientRoutes.CATALOGUE, KubaIcons.Regular.Gear, "var(--kuba-indigo)", ADMIN_ROLES, KubaApp.Admin),
            new HomeTask("Operators", "Bus companies, their company codes and contracts.", ClientRoutes.OPERATORS, KubaIcons.Regular.Bus, "var(--kuba-indigo)", ADMIN_ROLES, KubaApp.Admin),
            new HomeTask("Contracts", "Every contract across operators, with dates and bank details.", ClientRoutes.CONTRACTS, KubaIcons.Regular.ClipboardText, "var(--info)", ADMIN_ROLES, KubaApp.Admin),
            new HomeTask("Accounts", "Create sign-ins, set roles, deactivate leavers.", ClientRoutes.ACCOUNTS, KubaIcons.Regular.Users, "var(--kuba-indigo)", ADMIN_ROLES, KubaApp.Admin),
            new HomeTask("My account", "Your roles, your password, sign out.", ClientRoutes.ME, KubaIcons.Regular.User, "var(--ok)")
        };
    }
}
