using KubaPC.Models.Contracts.Escalations.Requests;
using KubaPC.Models.Contracts.Escalations.Responses;
using KubaPC.Models.Internal;
using System;
using System.Collections.Generic;
using System.Text;

namespace KubaPC.Services.Services.Interfaces
{
    public interface IEscalationParameterService 
    {
        Task<EscalationParametersResponse?> SelectByContract(Guid contractId, Caller caller);
        Task<OperationResult> Save(SaveEscalationParametersRequest request, Caller caller);
    }
}
