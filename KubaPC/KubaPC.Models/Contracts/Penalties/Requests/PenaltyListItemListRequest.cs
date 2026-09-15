using KubaPC.Models.Contracts.Common.Requests;

namespace KubaPC.Models.Contracts.Penalties.Requests
{
    public class PenaltyListItemListRequest : PagedListRequest
    {
        public Guid? ContractId { get; set; }
        public Guid? OperatorId { get; set; }
    }
}
