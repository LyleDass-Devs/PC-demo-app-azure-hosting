using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.PenaltyCaptures.Requests;
using KubaPC.Models.Contracts.PenaltyCaptures.Responses;
using KubaPC.Models.Internal;

namespace KubaPC.Services.Services.Interfaces
{
    public interface IPenaltyCaptureService
    {
        Task<PagedResponse<PenaltyCaptureResponse>> SelectPagedCaptures(PenaltyCaptureListRequest request, Guid actor);
        Task<CreateResult> CreateCapture(CreatePenaltyCaptureRequest request, Guid actor, string capturedByRole);
        Task<OperationResult> UpdateCapture(UpdatePenaltyCaptureRequest request, Guid actor);
        Task<OperationResult> DeleteCapture(DeletePenaltyCaptureRequest request, Guid actor);
        Task<List<DropDownResponse>> SelectContractRoutes(Guid contractId);
        Task<List<TripOptionResponse>> SelectRouteTrips(TripLookupRequest request);
        Task<List<DropDownResponse>> SelectContractVehicles(Guid contractId);
        Task<List<DropDownResponse>> SelectContractPenaltyItems(Guid contractId, string? penaltyType);
    }
}
