using FastEndpoints;
using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.Penalties.Requests;
using KubaPC.Models.Contracts.Penalties.Responses;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.Penalties
{
    public class GetPenaltyListEndpoint : Endpoint<PenaltyListItemListRequest, PagedResponse<PenaltyListItemResponse>>
    {
        private readonly IPenaltyListService penaltyListService;

        public GetPenaltyListEndpoint(IPenaltyListService penaltyListService)
        {
            this.penaltyListService = penaltyListService;
        }

        public override void Configure()
        {
            Get("/penalty-list");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Admin");
        }

        public override async Task HandleAsync(PenaltyListItemListRequest request, CancellationToken ct)
        {
            try
            {
                await Send.OkAsync(await penaltyListService.SelectPagedItems(request), cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Get penalty list failed for contract {ContractId}", request.ContractId);
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
