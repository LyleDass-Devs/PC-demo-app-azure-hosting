using KubaPC.Models.Contracts.IncomeFields.Requests;
using KubaPC.Models.Contracts.IncomeFields.Responses;
using KubaPC.Models.Internal;

namespace KubaPC.Services.Services.Interfaces
{
    public interface IIncomeFieldService
    {
        Task<IncomeFieldsResponse?> SelectByContract(Guid contractId);
        Task<OperationResult> Save(SaveIncomeFieldsRequest request, Guid actor);
    }
}
