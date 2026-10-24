using FastEndpoints;
using KubaPC.API.Security;
using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.FormF1.Requests;
using KubaPC.Models.Contracts.FormF1.Responses;
using KubaPC.Models.Internal;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.FormF1
{
    public class GetFormF1ListEndpoint : Endpoint<FormF1ListRequest, PagedResponse<FormF1RecordResponse>>
    {
        private readonly IFormF1Service formF1Service;

        public GetFormF1ListEndpoint(IFormF1Service formF1Service)
        {
            this.formF1Service = formF1Service;
        }

        public override void Configure()
        {
            Get("/form-f1");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Reader");
        }

        public override async Task HandleAsync(FormF1ListRequest request, CancellationToken ct)
        {
            try
            {
                Caller caller = CallerReader.Read(User);

                await Send.OkAsync(await formF1Service.SelectPaged(request, caller), cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Get Form F1 records failed");
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
