namespace KubaPC.Models.Contracts.Accounts.Responses
{
    public class ResetPasswordResponse
    {
        public string TemporaryPassword { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}
