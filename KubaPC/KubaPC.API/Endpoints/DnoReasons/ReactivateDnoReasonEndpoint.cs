using FastEndpoints;
using KubaPC.Models.Contracts.DnoReasons.Requests;
using KubaPC.Models.Internal;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.DnoReasons
{
    public class ReactivateDnoReasonEndpoint : Endpoint<ReactivateDnoReasonRequest>
    {
        private readonly IDnoReasonService dnoReasonService;

        public ReactivateDnoReasonEndpoint(IDnoReasonService dnoReasonService)
        {
            this.dnoReasonService = dnoReasonService;
        }

        public override void Configure()
        {
            Post("/dno-reasons/reactivate");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("SuperAdmin");
        }

        public override async Task HandleAsync(ReactivateDnoReasonRequest request, CancellationToken ct)
        {
            try
            {
                Guid actor = Guid.Parse(User.FindFirst("UserId")!.Value);

                OperationResult result = await dnoReasonService.Reactivate(request.Id, actor);

                if (!result.IsSuccess)
                {
                    AddError(result.Error!);
                    await Send.ErrorsAsync(409, ct);
                    return;
                }

                await Send.OkAsync(new { message = "Reason reactivated." }, cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "DNO reason reactivation failed for {ReasonId}", request.Id);
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
