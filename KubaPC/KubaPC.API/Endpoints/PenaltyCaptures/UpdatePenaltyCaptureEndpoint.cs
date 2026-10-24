using FastEndpoints;
using KubaPC.API.Security;
using KubaPC.Models.Contracts.PenaltyCaptures.Requests;
using KubaPC.Models.Internal;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.PenaltyCaptures
{
    public class UpdatePenaltyCaptureEndpoint : Endpoint<UpdatePenaltyCaptureRequest>
    {
        private readonly IPenaltyCaptureService penaltyCaptureService;

        public UpdatePenaltyCaptureEndpoint(IPenaltyCaptureService penaltyCaptureService)
        {
            this.penaltyCaptureService = penaltyCaptureService;
        }

        public override void Configure()
        {
            Post("/penalty-captures/update");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Capture");
        }

        public override async Task HandleAsync(UpdatePenaltyCaptureRequest request, CancellationToken ct)
        {
            try
            {
                Caller caller = CallerReader.Read(User);

                OperationResult result = await penaltyCaptureService.UpdateCapture(request, caller);

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

                await Send.OkAsync(new { message = "Penalty capture updated." }, cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Penalty capture update failed for {CaptureId}", request.Id);
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
