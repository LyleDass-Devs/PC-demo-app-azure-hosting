using KubaPC.Models.Contracts.Common.Requests;

namespace KubaPC.Models.Contracts.Contracts.Requests
{
    public class ContractListRequest : PagedListRequest
    {
        public Guid? OperatorId { get; set; }
    }
}
