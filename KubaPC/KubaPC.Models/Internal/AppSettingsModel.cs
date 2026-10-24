namespace KubaPC.Models.Internal
{
    public class AppSettingsModel
    {
        public string Secret { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int TokenExpireHours { get; set; }
        public int LoginLockoutAttempts { get; set; } = 10;
        public int LoginLockoutMinutes { get; set; } = 15;
        public int TemporaryPasswordHours { get; set; } = 72;
    }
}
