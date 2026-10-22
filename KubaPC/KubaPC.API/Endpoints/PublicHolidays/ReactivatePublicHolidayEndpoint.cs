using FastEndpoints;
using KubaPC.Models.Contracts.PublicHolidays.Requests;
using KubaPC.Models.Internal;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.PublicHolidays
{
    public class ReactivatePublicHolidayEndpoint : Endpoint<ReactivatePublicHolidayRequest>
    {
        private readonly IPublicHolidayService publicHolidayService;

        public ReactivatePublicHolidayEndpoint(IPublicHolidayService publicHolidayService)
        {
            this.publicHolidayService = publicHolidayService;
        }

        public override void Configure()
        {
            Post("/public-holidays/reactivate");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("SuperAdmin");
        }

        public override async Task HandleAsync(ReactivatePublicHolidayRequest request, CancellationToken ct)
        {
            try
            {
                Guid actor = Guid.Parse(User.FindFirst("UserId")!.Value);

                OperationResult result = await publicHolidayService.Reactivate(request.Id, actor);

                if (!result.IsSuccess)
                {
                    AddError(result.Error!);
                    await Send.ErrorsAsync(409, ct);
                    return;
                }

                await Send.OkAsync(new { message = "Public holiday restored." }, cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Public holiday restore failed for {HolidayId}", request.Id);
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
