namespace KubaPC.Models.Contracts.Accounts.Requests
{
    public class UpdateAccountOperatorsRequest
    {
        public Guid AccountId { get; set; }
        public List<Guid> OperatorIds { get; set; } = new();
    }
}
