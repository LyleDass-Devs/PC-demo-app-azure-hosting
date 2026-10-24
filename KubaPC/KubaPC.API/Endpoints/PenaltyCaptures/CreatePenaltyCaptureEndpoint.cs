using FastEndpoints;
using KubaPC.API.Security;
using KubaPC.Models.Contracts.PenaltyCaptures.Requests;
using KubaPC.Models.Internal;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.PenaltyCaptures
{
    public class CreatePenaltyCaptureEndpoint : Endpoint<CreatePenaltyCaptureRequest, Guid>
    {
        private const string MONITOR_ROLE = "Monitor";
        private const string OPERATOR_STAFF_ROLE = "Operator Staff";

        private readonly IPenaltyCaptureService penaltyCaptureService;

        public CreatePenaltyCaptureEndpoint(IPenaltyCaptureService penaltyCaptureService)
        {
            this.penaltyCaptureService = penaltyCaptureService;
        }

        public override void Configure()
        {
            Post("/penalty-captures/create");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Capture");
        }

        public override async Task HandleAsync(CreatePenaltyCaptureRequest request, CancellationToken ct)
        {
            try
            {
                Caller caller = CallerReader.Read(User);
                string capturedByRole = User.IsInRole(MONITOR_ROLE) ? MONITOR_ROLE : OPERATOR_STAFF_ROLE;

                CreateResult result = await penaltyCaptureService.CreateCapture(request, caller, capturedByRole);

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
                Logger.LogError(exception, "Penalty capture failed for trip {TripId}", request.TripId);
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
