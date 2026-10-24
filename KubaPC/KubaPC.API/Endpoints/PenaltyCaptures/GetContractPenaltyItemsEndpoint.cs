using FastEndpoints;
using KubaPC.API.Security;
using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.PenaltyCaptures.Requests;
using KubaPC.Models.Internal;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.PenaltyCaptures
{
    public class GetContractPenaltyItemsEndpoint : Endpoint<CaptureLookupRequest, List<DropDownResponse>>
    {
        private readonly IPenaltyCaptureService penaltyCaptureService;

        public GetContractPenaltyItemsEndpoint(IPenaltyCaptureService penaltyCaptureService)
        {
            this.penaltyCaptureService = penaltyCaptureService;
        }

        public override void Configure()
        {
            Get("/penalty-captures/penalty-items");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Reader");
        }

        public override async Task HandleAsync(CaptureLookupRequest request, CancellationToken ct)
        {
            try
            {
                LookupResult<List<DropDownResponse>> result = await penaltyCaptureService.SelectContractPenaltyItems(request.ContractId, request.PenaltyType, CallerReader.Read(User));

                if (result.IsForbidden)
                {
                    await Send.ForbiddenAsync(ct);
                    return;
                }

                await Send.OkAsync(result.Data!, cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Get contract penalty items failed for {ContractId}", request.ContractId);
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
