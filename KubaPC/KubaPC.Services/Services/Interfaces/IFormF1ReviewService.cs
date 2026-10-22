using KubaPC.Models.Contracts.FormF1.Requests;
using KubaPC.Models.Contracts.FormF1.Responses;
using KubaPC.Models.Internal;

namespace KubaPC.Services.Services.Interfaces
{
    public interface IFormF1ReviewService
    {
        Task<MatchingFormF1Response> SelectMatchingUndecided(Guid recordId);
        Task<OperationResult> ReviewRecord(ReviewFormF1RecordRequest request, Guid actor);
    }
}
