using FastEndpoints;
using KubaPC.Models.Contracts.FormF1.Requests;
using KubaPC.Models.Internal;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.FormF1
{
    public class UpdateFormF1RecordEndpoint : Endpoint<UpdateFormF1RecordRequest>
    {
        private readonly IFormF1Service formF1Service;

        public UpdateFormF1RecordEndpoint(IFormF1Service formF1Service)
        {
            this.formF1Service = formF1Service;
        }

        public override void Configure()
        {
            Post("/form-f1/update");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Capture");
        }

        public override async Task HandleAsync(UpdateFormF1RecordRequest request, CancellationToken ct)
        {
            try
            {
                Guid actor = Guid.Parse(User.FindFirst("UserId")!.Value);

                OperationResult result = await formF1Service.Update(request, actor);

                if (!result.IsSuccess)
                {
                    AddError(result.Error!);
                    await Send.ErrorsAsync(409, ct);
                    return;
                }

                await Send.OkAsync(new { message = "Form F1 record updated." }, cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Form F1 update failed for {RecordId}", request.Id);
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
