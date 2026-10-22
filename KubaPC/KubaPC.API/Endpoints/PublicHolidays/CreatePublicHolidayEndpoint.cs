using FastEndpoints;
using KubaPC.Models.Contracts.PublicHolidays.Requests;
using KubaPC.Models.Internal;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.PublicHolidays
{
    public class CreatePublicHolidayEndpoint : Endpoint<CreatePublicHolidayRequest, Guid>
    {
        private readonly IPublicHolidayService publicHolidayService;

        public CreatePublicHolidayEndpoint(IPublicHolidayService publicHolidayService)
        {
            this.publicHolidayService = publicHolidayService;
        }

        public override void Configure()
        {
            Post("/public-holidays/create");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("SuperAdmin");
        }

        public override async Task HandleAsync(CreatePublicHolidayRequest request, CancellationToken ct)
        {
            try
            {
                Guid actor = Guid.Parse(User.FindFirst("UserId")!.Value);

                CreateResult result = await publicHolidayService.Create(request, actor);

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
                Logger.LogError(exception, "Public holiday creation failed for {HolidayDate}", request.HolidayDate);
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
