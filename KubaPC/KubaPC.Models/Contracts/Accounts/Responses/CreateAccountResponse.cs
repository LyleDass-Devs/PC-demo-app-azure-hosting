namespace KubaPC.Models.Contracts.Accounts.Responses
{
    public class CreateAccountResponse
    {
        public Guid Id { get; set; }
        public string TemporaryPassword { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}
