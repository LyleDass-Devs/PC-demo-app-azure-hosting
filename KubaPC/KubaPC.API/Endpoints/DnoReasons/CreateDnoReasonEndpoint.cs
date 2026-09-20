using FastEndpoints;
using KubaPC.Models.Contracts.DnoReasons.Requests;
using KubaPC.Models.Internal;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.DnoReasons
{
    public class CreateDnoReasonEndpoint : Endpoint<CreateDnoReasonRequest, Guid>
    {
        private readonly IDnoReasonService dnoReasonService;

        public CreateDnoReasonEndpoint(IDnoReasonService dnoReasonService)
        {
            this.dnoReasonService = dnoReasonService;
        }

        public override void Configure()
        {
            Post("/dno-reasons/create");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("SuperAdmin");
        }

        public override async Task HandleAsync(CreateDnoReasonRequest request, CancellationToken ct)
        {
            try
            {
                Guid actor = Guid.Parse(User.FindFirst("UserId")!.Value);

                CreateResult result = await dnoReasonService.Create(request, actor);

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
                Logger.LogError(exception, "DNO reason creation failed for {ReasonValue}", request.ReasonValue);
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
