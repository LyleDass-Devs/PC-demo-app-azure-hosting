using FastEndpoints;
using KubaPC.Models.Contracts.ReferenceUrls.Requests;
using KubaPC.Models.Internal;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.ReferenceUrls
{
    public class SaveReferenceUrlEndpoint : Endpoint<SaveReferenceUrlRequest>
    {
        private readonly IReferenceUrlService referenceUrlService;

        public SaveReferenceUrlEndpoint(IReferenceUrlService referenceUrlService)
        {
            this.referenceUrlService = referenceUrlService;
        }

        public override void Configure()
        {
            Post("/reference-urls/save");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("SuperAdmin");
        }

        public override async Task HandleAsync(SaveReferenceUrlRequest request, CancellationToken ct)
        {
            try
            {
                Guid actor = Guid.Parse(User.FindFirst("UserId")!.Value);

                OperationResult result = await referenceUrlService.Save(request, actor);

                if (!result.IsSuccess)
                {
                    AddError(result.Error!);
                    await Send.ErrorsAsync(409, ct);
                    return;
                }

                await Send.OkAsync(new { message = "Reference link saved." }, cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Save reference URL failed for {UrlKey}", request.UrlKey);
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
