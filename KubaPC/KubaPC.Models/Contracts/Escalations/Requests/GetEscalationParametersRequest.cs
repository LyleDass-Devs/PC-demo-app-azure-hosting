using System;
using System.Collections.Generic;
using System.Text;

namespace KubaPC.Models.Contracts.Escalations.Requests
{
    public class GetEscalationParametersRequest
    {
        public Guid ContractId { get; set; }
    }
}
