namespace KubaPC.Models.Contracts.Accounts.Requests
{
    /// <summary>An administrator brings a deactivated account back.</summary>
    public class ReactivateAccountRequest
    {
        public Guid Id { get; set; }
    }
}
