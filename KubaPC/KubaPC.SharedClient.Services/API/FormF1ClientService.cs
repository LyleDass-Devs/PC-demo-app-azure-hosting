using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.FormF1.Requests;
using KubaPC.Models.Contracts.FormF1.Responses;
using KubaPC.SharedClient.Services.Http;
using KubaPC.SharedClient.Services.Models;

namespace KubaPC.SharedClient.Services.Api
{
    public class FormF1ClientService : IFormF1ClientService
    {
        private readonly IHttpService httpService;

        public FormF1ClientService(IHttpService httpService)
        {
            this.httpService = httpService;
        }

        public async Task<ApiResult<PagedResponse<FormF1RecordResponse>>> GetPaged(FormF1ListRequest request)
        {
            string certificateFilter = request.PaymentCertificateId is null ? string.Empty : $"&paymentCertificateId={request.PaymentCertificateId}";
            string tripFilter = request.TripId is null ? string.Empty : $"&tripId={request.TripId}";
            string contractFilter = request.ContractId is null ? string.Empty : $"&contractId={request.ContractId}";
            string typeFilter = string.IsNullOrWhiteSpace(request.RecordType) ? string.Empty : $"&recordType={Uri.EscapeDataString(request.RecordType)}";
            string statusFilter = string.IsNullOrWhiteSpace(request.RecordStatus) ? string.Empty : $"&recordStatus={Uri.EscapeDataString(request.RecordStatus)}";
            string roleFilter = string.IsNullOrWhiteSpace(request.CapturedByRole) ? string.Empty : $"&capturedByRole={Uri.EscapeDataString(request.CapturedByRole)}";

            return await httpService.Get<PagedResponse<FormF1RecordResponse>>($"/form-f1?{PagedQuery.From(request)}{certificateFilter}{tripFilter}{contractFilter}{typeFilter}{statusFilter}{roleFilter}");
        }

        public async Task<ApiResult<FormF1LookupsResponse>> GetLookups()
        {
            return await httpService.Get<FormF1LookupsResponse>("/form-f1/lookups");
        }

        public async Task<ApiResult<Guid>> Create(CreateFormF1RecordRequest request)
        {
            return await httpService.Post<Guid>("/form-f1/create", request);
        }

        public async Task<ApiResult> Update(UpdateFormF1RecordRequest request)
        {
            return await httpService.Post("/form-f1/update", request);
        }

        public async Task<ApiResult> Delete(DeleteFormF1RecordRequest request)
        {
            return await httpService.Post("/form-f1/delete", request);
        }

        public async Task<ApiResult> Review(ReviewFormF1RecordRequest request)
        {
            return await httpService.Post("/form-f1/review", request);
        }

        public async Task<ApiResult<MatchingFormF1Response>> GetMatching(Guid recordId)
        {
            return await httpService.Get<MatchingFormF1Response>($"/form-f1/matching?recordId={recordId}");
        }
    }
}
