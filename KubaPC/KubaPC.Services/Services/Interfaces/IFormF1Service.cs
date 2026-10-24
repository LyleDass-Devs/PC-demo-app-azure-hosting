using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.FormF1.Requests;
using KubaPC.Models.Contracts.FormF1.Responses;
using KubaPC.Models.Internal;

namespace KubaPC.Services.Services.Interfaces
{
    public interface IFormF1Service
    {
        Task<PagedResponse<FormF1RecordResponse>> SelectPaged(FormF1ListRequest request, Caller caller);
        Task<FormF1LookupsResponse> SelectLookups();
        Task<CreateResult> Create(CreateFormF1RecordRequest request, Caller caller, string capturedByRole);
        Task<OperationResult> Update(UpdateFormF1RecordRequest request, Caller caller);
        Task<OperationResult> Delete(DeleteFormF1RecordRequest request, Caller caller);
    }
}
