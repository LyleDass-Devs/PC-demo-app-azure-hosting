using FastEndpoints;
using KubaPC.Models.Contracts.PublicHolidays.Requests;
using KubaPC.Models.Internal;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.PublicHolidays
{
    public class UpdatePublicHolidayEndpoint : Endpoint<UpdatePublicHolidayRequest>
    {
        private readonly IPublicHolidayService publicHolidayService;

        public UpdatePublicHolidayEndpoint(IPublicHolidayService publicHolidayService)
        {
            this.publicHolidayService = publicHolidayService;
        }

        public override void Configure()
        {
            Post("/public-holidays/update");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("SuperAdmin");
        }

        public override async Task HandleAsync(UpdatePublicHolidayRequest request, CancellationToken ct)
        {
            try
            {
                Guid actor = Guid.Parse(User.FindFirst("UserId")!.Value);

                OperationResult result = await publicHolidayService.Update(request, actor);

                if (!result.IsSuccess)
                {
                    AddError(result.Error!);
                    await Send.ErrorsAsync(409, ct);
                    return;
                }

                await Send.OkAsync(new { message = "Public holiday updated." }, cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Public holiday update failed for {HolidayId}", request.Id);
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
