namespace KubaPC.Models.Contracts.Accounts.Requests
{
    /// <summary>A signed-in person changes their own display name.</summary>
    public class UpdateOwnAccountRequest
    {
        public string FullName { get; set; } = string.Empty;
    }
}
