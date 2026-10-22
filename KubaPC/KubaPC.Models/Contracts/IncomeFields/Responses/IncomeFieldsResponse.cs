namespace KubaPC.Models.Contracts.IncomeFields.Responses
{
    public class IncomeFieldsResponse
    {
        public Guid Id { get; set; }
        public Guid ContractId { get; set; }
        public bool IncomePrivateHiresEnabled { get; set; }
        public bool IncomeDedicatedContractsEnabled { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }
}
