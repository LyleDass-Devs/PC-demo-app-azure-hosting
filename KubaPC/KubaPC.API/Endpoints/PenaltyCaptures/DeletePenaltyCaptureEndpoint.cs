using FastEndpoints;
using KubaPC.API.Security;
using KubaPC.Models.Contracts.PenaltyCaptures.Requests;
using KubaPC.Models.Internal;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.PenaltyCaptures
{
    public class DeletePenaltyCaptureEndpoint : Endpoint<DeletePenaltyCaptureRequest>
    {
        private readonly IPenaltyCaptureService penaltyCaptureService;

        public DeletePenaltyCaptureEndpoint(IPenaltyCaptureService penaltyCaptureService)
        {
            this.penaltyCaptureService = penaltyCaptureService;
        }

        public override void Configure()
        {
            Post("/penalty-captures/delete");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Capture");
        }

        public override async Task HandleAsync(DeletePenaltyCaptureRequest request, CancellationToken ct)
        {
            try
            {
                Caller caller = CallerReader.Read(User);

                OperationResult result = await penaltyCaptureService.DeleteCapture(request, caller);

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

                await Send.OkAsync(new { message = "Penalty capture removed." }, cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Penalty capture removal failed for {CaptureId}", request.Id);
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
