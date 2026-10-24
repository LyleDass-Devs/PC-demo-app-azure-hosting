namespace KubaPC.Models.Contracts.Accounts.Requests
{
    public class CreateAccountRequest
    {
        public string EmailAddress { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public List<string> RoleNames { get; set; } = new();
    }
}
