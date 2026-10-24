using FastEndpoints;
using KubaPC.API.Security;
using KubaPC.Models.Contracts.Penalties.Requests;
using KubaPC.Models.Internal;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.Penalties
{
    public class CreatePenaltyListItemEndpoint : Endpoint<CreatePenaltyListItemRequest, Guid>
    {
        private readonly IPenaltyListService penaltyListService;

        public CreatePenaltyListItemEndpoint(IPenaltyListService penaltyListService)
        {
            this.penaltyListService = penaltyListService;
        }

        public override void Configure()
        {
            Post("/penalty-list/create");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Admin");
        }

        public override async Task HandleAsync(CreatePenaltyListItemRequest request, CancellationToken ct)
        {
            try
            {
                Caller caller = CallerReader.Read(User);

                CreateResult result = await penaltyListService.CreateItem(request, caller);

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

                await Send.ResponseAsync(result.Id!.Value, 201, cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Penalty creation failed for {PenaltyCode}", request.PenaltyCode);
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
