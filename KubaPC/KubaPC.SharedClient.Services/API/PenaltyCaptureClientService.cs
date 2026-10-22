using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.PenaltyCaptures.Requests;
using KubaPC.Models.Contracts.PenaltyCaptures.Responses;
using KubaPC.SharedClient.Services.Http;
using KubaPC.SharedClient.Services.Models;

namespace KubaPC.SharedClient.Services.Api
{
    public class PenaltyCaptureClientService : IPenaltyCaptureClientService
    {
        private readonly IHttpService httpService;

        public PenaltyCaptureClientService(IHttpService httpService)
        {
            this.httpService = httpService;
        }

        public async Task<ApiResult<PagedResponse<PenaltyCaptureResponse>>> GetPaged(PenaltyCaptureListRequest request)
        {
            string certificateFilter = request.PaymentCertificateId is null ? string.Empty : $"&paymentCertificateId={request.PaymentCertificateId}";
            string tripFilter = request.TripId is null ? string.Empty : $"&tripId={request.TripId}";
            string statusFilter = string.IsNullOrWhiteSpace(request.CaptureStatus) ? string.Empty : $"&captureStatus={Uri.EscapeDataString(request.CaptureStatus)}";
            string contractFilter = request.ContractId is null ? string.Empty : $"&contractId={request.ContractId}";
            string roleFilter = string.IsNullOrWhiteSpace(request.CapturedByRole) ? string.Empty : $"&capturedByRole={Uri.EscapeDataString(request.CapturedByRole)}";

            return await httpService.Get<PagedResponse<PenaltyCaptureResponse>>($"/penalty-captures?{PagedQuery.From(request)}{certificateFilter}{tripFilter}{statusFilter}{contractFilter}{roleFilter}");
        }

        public async Task<ApiResult<Guid>> Create(CreatePenaltyCaptureRequest request)
        {
            return await httpService.Post<Guid>("/penalty-captures/create", request);
        }

        public async Task<ApiResult> Update(UpdatePenaltyCaptureRequest request)
        {
            return await httpService.Post("/penalty-captures/update", request);
        }

        public async Task<ApiResult> Delete(DeletePenaltyCaptureRequest request)
        {
            return await httpService.Post("/penalty-captures/delete", request);
        }

        public async Task<ApiResult<List<DropDownResponse>>> GetRoutes(Guid contractId)
        {
            return await httpService.Get<List<DropDownResponse>>($"/penalty-captures/routes?contractId={contractId}");
        }

        public async Task<ApiResult<List<TripOptionResponse>>> GetTrips(Guid routeId, Guid? paymentCertificateId)
        {
            string certificateFilter = paymentCertificateId is null ? string.Empty : $"&paymentCertificateId={paymentCertificateId}";

            return await httpService.Get<List<TripOptionResponse>>($"/penalty-captures/trips?routeId={routeId}{certificateFilter}");
        }

        public async Task<ApiResult<List<DropDownResponse>>> GetVehicles(Guid contractId)
        {
            return await httpService.Get<List<DropDownResponse>>($"/penalty-captures/vehicles?contractId={contractId}");
        }

        public async Task<ApiResult<List<DropDownResponse>>> GetPenaltyItems(Guid contractId, string? penaltyType)
        {
            string typeFilter = string.IsNullOrWhiteSpace(penaltyType) ? string.Empty : $"&penaltyType={Uri.EscapeDataString(penaltyType)}";

            return await httpService.Get<List<DropDownResponse>>($"/penalty-captures/penalty-items?contractId={contractId}{typeFilter}");
        }
    }
}
