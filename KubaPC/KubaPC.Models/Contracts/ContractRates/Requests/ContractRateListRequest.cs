using KubaPC.Models.Contracts.Common.Requests;

namespace KubaPC.Models.Contracts.ContractRates.Requests
{
    public class ContractRateListRequest : PagedListRequest
    {
        public Guid? ContractId { get; set; }
        public Guid? OperatorId { get; set; }
        public string? VehicleType { get; set; }
    }
}
