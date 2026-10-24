namespace KubaPC.Models.Internal
{
    public class AccountCredentialsModel
    {
        public Guid Id { get; set; }
        public string EmailAddress { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? PasswordHash { get; set; } = string.Empty;
        public bool MustChangePassword { get; set; }
        public int TokenVersion { get; set; }
        public int FailedLoginCount { get; set; }
        public DateTime? LockedUntil { get; set; }
        public DateTime? TemporaryPasswordExpiresAt { get; set; }
    }
}
