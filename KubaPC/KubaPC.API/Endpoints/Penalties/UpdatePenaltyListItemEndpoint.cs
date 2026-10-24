using FastEndpoints;
using KubaPC.API.Security;
using KubaPC.Models.Contracts.Penalties.Requests;
using KubaPC.Models.Internal;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.Penalties
{
    public class UpdatePenaltyListItemEndpoint : Endpoint<UpdatePenaltyListItemRequest>
    {
        private readonly IPenaltyListService penaltyListService;

        public UpdatePenaltyListItemEndpoint(IPenaltyListService penaltyListService)
        {
            this.penaltyListService = penaltyListService;
        }

        public override void Configure()
        {
            Post("/penalty-list/update");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Admin");
        }

        public override async Task HandleAsync(UpdatePenaltyListItemRequest request, CancellationToken ct)
        {
            try
            {
                Caller caller = CallerReader.Read(User);

                OperationResult result = await penaltyListService.UpdateItem(request, caller);

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

                await Send.OkAsync(new { message = "Penalty updated." }, cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Penalty update failed for {ItemId}", request.Id);
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
