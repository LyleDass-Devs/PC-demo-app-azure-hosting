namespace KubaPC.Models.Internal
{
    public sealed class Caller
    {
        public const string SUPER_ADMIN_ROLE = "SuperAdmin";
        public const string ADMIN_ROLE = "Admin";

        private readonly HashSet<string> roles;

        public Caller(Guid accountId, IEnumerable<string> roles)
        {
            AccountId = accountId;
            this.roles = new HashSet<string>(roles, StringComparer.Ordinal);
        }

        public Guid AccountId { get; }

        public IReadOnlyCollection<string> Roles => roles;

        public bool IsSuperAdmin => roles.Contains(SUPER_ADMIN_ROLE);

        public bool IsAdmin => roles.Contains(ADMIN_ROLE);

        public bool IsUnscoped => IsSuperAdmin;

        public bool HasRole(string role) => roles.Contains(role);
    }
}
