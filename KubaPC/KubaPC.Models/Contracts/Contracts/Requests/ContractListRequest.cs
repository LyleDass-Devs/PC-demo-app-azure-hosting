using KubaPC.Models.Contracts.Common.Requests;

namespace KubaPC.Models.Contracts.Contracts.Requests
{
    /// <summary>Contracts are always listed for one operator.</summary>
    public class ContractListRequest : PagedListRequest
    {
        public Guid OperatorId { get; set; }
    }
}
