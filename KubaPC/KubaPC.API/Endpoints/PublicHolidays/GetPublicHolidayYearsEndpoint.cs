using FastEndpoints;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.PublicHolidays
{
    public class GetPublicHolidayYearsEndpoint : EndpointWithoutRequest<List<int>>
    {
        private readonly IPublicHolidayService publicHolidayService;

        public GetPublicHolidayYearsEndpoint(IPublicHolidayService publicHolidayService)
        {
            this.publicHolidayService = publicHolidayService;
        }

        public override void Configure()
        {
            Get("/public-holidays/years");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Admin");
        }

        public override async Task HandleAsync(CancellationToken ct)
        {
            try
            {
                await Send.OkAsync(await publicHolidayService.SelectYears(), cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Get public holiday years failed");
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
