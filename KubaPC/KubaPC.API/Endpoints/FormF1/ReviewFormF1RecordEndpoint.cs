using FastEndpoints;
using KubaPC.API.Security;
using KubaPC.Models.Contracts.FormF1.Requests;
using KubaPC.Models.Internal;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.FormF1
{
    public class ReviewFormF1RecordEndpoint : Endpoint<ReviewFormF1RecordRequest>
    {
        private readonly IFormF1ReviewService formF1ReviewService;

        public ReviewFormF1RecordEndpoint(IFormF1ReviewService formF1ReviewService)
        {
            this.formF1ReviewService = formF1ReviewService;
        }

        public override void Configure()
        {
            Post("/form-f1/review");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Authoriser");
        }

        public override async Task HandleAsync(ReviewFormF1RecordRequest request, CancellationToken ct)
        {
            try
            {
                Caller caller = CallerReader.Read(User);

                OperationResult result = await formF1ReviewService.ReviewRecord(request, caller);

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

                await Send.OkAsync(new { message = "Form F1 record reviewed." }, cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Form F1 review failed for {RecordId}", request.Id);
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
