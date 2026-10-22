using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.PenaltyCaptures.Requests;
using KubaPC.Models.Contracts.PenaltyCaptures.Responses;
using KubaPC.SharedClient.Services.Models;

namespace KubaPC.SharedClient.Services.Api
{
    public interface IPenaltyCaptureClientService
    {
        Task<ApiResult<PagedResponse<PenaltyCaptureResponse>>> GetPaged(PenaltyCaptureListRequest request);
        Task<ApiResult<Guid>> Create(CreatePenaltyCaptureRequest request);
        Task<ApiResult> Update(UpdatePenaltyCaptureRequest request);
        Task<ApiResult> Delete(DeletePenaltyCaptureRequest request);
        Task<ApiResult<List<DropDownResponse>>> GetRoutes(Guid contractId);
        Task<ApiResult<List<TripOptionResponse>>> GetTrips(Guid routeId, Guid? paymentCertificateId);
        Task<ApiResult<List<DropDownResponse>>> GetVehicles(Guid contractId);
        Task<ApiResult<List<DropDownResponse>>> GetPenaltyItems(Guid contractId, string? penaltyType);
    }
}
