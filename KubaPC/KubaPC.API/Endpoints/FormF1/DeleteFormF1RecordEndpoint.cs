using FastEndpoints;
using KubaPC.API.Security;
using KubaPC.Models.Contracts.FormF1.Requests;
using KubaPC.Models.Internal;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.FormF1
{
    public class DeleteFormF1RecordEndpoint : Endpoint<DeleteFormF1RecordRequest>
    {
        private readonly IFormF1Service formF1Service;

        public DeleteFormF1RecordEndpoint(IFormF1Service formF1Service)
        {
            this.formF1Service = formF1Service;
        }

        public override void Configure()
        {
            Post("/form-f1/delete");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Capture");
        }

        public override async Task HandleAsync(DeleteFormF1RecordRequest request, CancellationToken ct)
        {
            try
            {
                Caller caller = CallerReader.Read(User);

                OperationResult result = await formF1Service.Delete(request, caller);

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

                await Send.OkAsync(new { message = "Form F1 record removed." }, cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Form F1 removal failed for {RecordId}", request.Id);
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
