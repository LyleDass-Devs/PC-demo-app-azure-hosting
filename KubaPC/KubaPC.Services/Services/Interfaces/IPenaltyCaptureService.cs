using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.PenaltyCaptures.Requests;
using KubaPC.Models.Contracts.PenaltyCaptures.Responses;
using KubaPC.Models.Internal;

namespace KubaPC.Services.Services.Interfaces
{
    public interface IPenaltyCaptureService
    {
        Task<PagedResponse<PenaltyCaptureResponse>> SelectPagedCaptures(PenaltyCaptureListRequest request, Caller caller);
        Task<CreateResult> CreateCapture(CreatePenaltyCaptureRequest request, Caller caller, string capturedByRole);
        Task<OperationResult> UpdateCapture(UpdatePenaltyCaptureRequest request, Caller caller);
        Task<OperationResult> DeleteCapture(DeletePenaltyCaptureRequest request, Caller caller);
        Task<LookupResult<List<DropDownResponse>>> SelectContractRoutes(Guid contractId, Caller caller);
        Task<LookupResult<List<TripOptionResponse>>> SelectRouteTrips(TripLookupRequest request, Caller caller);
        Task<LookupResult<List<DropDownResponse>>> SelectContractVehicles(Guid contractId, Caller caller);
        Task<LookupResult<List<DropDownResponse>>> SelectContractPenaltyItems(Guid contractId, string? penaltyType, Caller caller);
    }
}
