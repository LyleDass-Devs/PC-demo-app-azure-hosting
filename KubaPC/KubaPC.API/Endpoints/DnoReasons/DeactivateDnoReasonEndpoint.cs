using FastEndpoints;
using KubaPC.Models.Contracts.DnoReasons.Requests;
using KubaPC.Models.Internal;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.DnoReasons
{
    public class DeactivateDnoReasonEndpoint : Endpoint<DeactivateDnoReasonRequest>
    {
        private readonly IDnoReasonService dnoReasonService;

        public DeactivateDnoReasonEndpoint(IDnoReasonService dnoReasonService)
        {
            this.dnoReasonService = dnoReasonService;
        }

        public override void Configure()
        {
            Post("/dno-reasons/deactivate");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("SuperAdmin");
        }

        public override async Task HandleAsync(DeactivateDnoReasonRequest request, CancellationToken ct)
        {
            try
            {
                Guid actor = Guid.Parse(User.FindFirst("UserId")!.Value);

                OperationResult result = await dnoReasonService.Deactivate(request.Id, actor);

                if (!result.IsSuccess)
                {
                    AddError(result.Error!);
                    await Send.ErrorsAsync(409, ct);
                    return;
                }

                await Send.OkAsync(new { message = "Reason deactivated." }, cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "DNO reason deactivation failed for {ReasonId}", request.Id);
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
