using FastEndpoints;
using KubaPC.Models.Contracts.DnoReasons.Requests;
using KubaPC.Models.Internal;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.DnoReasons
{
    public class UpdateDnoReasonEndpoint : Endpoint<UpdateDnoReasonRequest>
    {
        private readonly IDnoReasonService dnoReasonService;

        public UpdateDnoReasonEndpoint(IDnoReasonService dnoReasonService)
        {
            this.dnoReasonService = dnoReasonService;
        }

        public override void Configure()
        {
            Post("/dno-reasons/update");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("SuperAdmin");
        }

        public override async Task HandleAsync(UpdateDnoReasonRequest request, CancellationToken ct)
        {
            try
            {
                Guid actor = Guid.Parse(User.FindFirst("UserId")!.Value);

                OperationResult result = await dnoReasonService.Update(request, actor);

                if (!result.IsSuccess)
                {
                    AddError(result.Error!);
                    await Send.ErrorsAsync(409, ct);
                    return;
                }

                await Send.OkAsync(new { message = "Reason updated." }, cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "DNO reason update failed for {ReasonId}", request.Id);
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
