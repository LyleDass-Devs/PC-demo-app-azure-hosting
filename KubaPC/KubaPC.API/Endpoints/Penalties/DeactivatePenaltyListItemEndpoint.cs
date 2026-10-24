using FastEndpoints;
using KubaPC.API.Security;
using KubaPC.Models.Contracts.Penalties.Requests;
using KubaPC.Models.Internal;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.Penalties
{
    public class DeactivatePenaltyListItemEndpoint : Endpoint<DeactivatePenaltyListItemRequest>
    {
        private readonly IPenaltyListService penaltyListService;

        public DeactivatePenaltyListItemEndpoint(IPenaltyListService penaltyListService)
        {
            this.penaltyListService = penaltyListService;
        }

        public override void Configure()
        {
            Post("/penalty-list/deactivate");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Admin");
        }

        public override async Task HandleAsync(DeactivatePenaltyListItemRequest request, CancellationToken ct)
        {
            try
            {
                Caller caller = CallerReader.Read(User);

                OperationResult result = await penaltyListService.DeactivateItem(request.Id, caller);

                if (!result.IsSuccess)
                {
                if (result.IsForbidden)
                {
                    await Send.ForbiddenAsync(ct);
                    return;
                }

                    AddError(result.Error!);
                    await Send.ErrorsAsync(409, ct);
                    return;
                }

                await Send.OkAsync(new { message = "Penalty deactivated." }, cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Penalty deactivation failed for {ItemId}", request.Id);
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
