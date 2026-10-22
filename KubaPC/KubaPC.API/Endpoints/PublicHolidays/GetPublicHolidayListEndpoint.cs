using FastEndpoints;
using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.PublicHolidays.Requests;
using KubaPC.Models.Contracts.PublicHolidays.Responses;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.PublicHolidays
{
    public class GetPublicHolidayListEndpoint : Endpoint<PublicHolidayListRequest, PagedResponse<PublicHolidayResponse>>
    {
        private readonly IPublicHolidayService publicHolidayService;

        public GetPublicHolidayListEndpoint(IPublicHolidayService publicHolidayService)
        {
            this.publicHolidayService = publicHolidayService;
        }

        public override void Configure()
        {
            Get("/public-holidays");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Admin");
        }

        public override async Task HandleAsync(PublicHolidayListRequest request, CancellationToken ct)
        {
            try
            {
                await Send.OkAsync(await publicHolidayService.SelectPaged(request), cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Get public holidays failed");
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
