using FastEndpoints;
using KubaPC.Models.Contracts.FormF1.Requests;
using KubaPC.Models.Internal;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.FormF1
{
    public class CreateFormF1RecordEndpoint : Endpoint<CreateFormF1RecordRequest, Guid>
    {
        private const string MONITOR_ROLE = "Monitor";
        private const string OPERATOR_STAFF_ROLE = "Operator Staff";

        private readonly IFormF1Service formF1Service;

        public CreateFormF1RecordEndpoint(IFormF1Service formF1Service)
        {
            this.formF1Service = formF1Service;
        }

        public override void Configure()
        {
            Post("/form-f1/create");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Capture");
        }

        public override async Task HandleAsync(CreateFormF1RecordRequest request, CancellationToken ct)
        {
            try
            {
                Guid actor = Guid.Parse(User.FindFirst("UserId")!.Value);
                string capturedByRole = User.IsInRole(MONITOR_ROLE) ? MONITOR_ROLE : OPERATOR_STAFF_ROLE;

                CreateResult result = await formF1Service.Create(request, actor, capturedByRole);

                if (!result.IsSuccess)
                {
                    AddError(result.Error!);
                    await Send.ErrorsAsync(409, ct);
                    return;
                }

                await Send.ResponseAsync(result.Id!.Value, 201, cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Form F1 capture failed for trip {TripId}", request.TripId);
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
