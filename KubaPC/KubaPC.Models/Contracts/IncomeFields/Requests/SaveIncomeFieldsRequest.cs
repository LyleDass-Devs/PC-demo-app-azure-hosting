namespace KubaPC.Models.Contracts.IncomeFields.Requests
{
    public class SaveIncomeFieldsRequest
    {
        public Guid ContractId { get; set; }
        public bool IncomePrivateHiresEnabled { get; set; }
        public bool IncomeDedicatedContractsEnabled { get; set; }
    }
}
