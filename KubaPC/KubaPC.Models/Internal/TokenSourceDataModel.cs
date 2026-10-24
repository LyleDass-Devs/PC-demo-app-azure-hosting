namespace KubaPC.Models.Internal
{
    public class TokenSourceDataModel
    {
        public LoggedInUserModel User { get; set; } = new();
        public List<string> UserRoles { get; set; } = new();
        public bool MustChangePassword { get; set; }
        public int TokenVersion { get; set; }
    }
}
