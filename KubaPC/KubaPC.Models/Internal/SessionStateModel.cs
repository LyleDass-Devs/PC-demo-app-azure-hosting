namespace KubaPC.Models.Internal
{
    public class SessionStateModel
    {
        public bool IsActive { get; set; }
        public int TokenVersion { get; set; }
        public bool MustChangePassword { get; set; }
        public List<string> RoleNames { get; set; } = new();
    }
}
