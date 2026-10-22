using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.PublicHolidays.Requests;
using KubaPC.Models.Contracts.PublicHolidays.Responses;
using KubaPC.SharedClient.Services.Http;
using KubaPC.SharedClient.Services.Models;

namespace KubaPC.SharedClient.Services.Api
{
    public class PublicHolidayClientService : IPublicHolidayClientService
    {
        private readonly IHttpService httpService;

        public PublicHolidayClientService(IHttpService httpService)
        {
            this.httpService = httpService;
        }

        public async Task<ApiResult<PagedResponse<PublicHolidayResponse>>> GetPaged(PublicHolidayListRequest request)
        {
            string yearFilter = request.Year is null ? string.Empty : $"&year={request.Year}";

            return await httpService.Get<PagedResponse<PublicHolidayResponse>>($"/public-holidays?{PagedQuery.From(request)}{yearFilter}");
        }

        public async Task<ApiResult<List<int>>> GetYears()
        {
            return await httpService.Get<List<int>>("/public-holidays/years");
        }

        public async Task<ApiResult<Guid>> Create(CreatePublicHolidayRequest request)
        {
            return await httpService.Post<Guid>("/public-holidays/create", request);
        }

        public async Task<ApiResult> Update(UpdatePublicHolidayRequest request)
        {
            return await httpService.Post("/public-holidays/update", request);
        }

        public async Task<ApiResult> Deactivate(Guid holidayId)
        {
            return await httpService.Post("/public-holidays/deactivate", new DeactivatePublicHolidayRequest { Id = holidayId });
        }

        public async Task<ApiResult> Reactivate(Guid holidayId)
        {
            return await httpService.Post("/public-holidays/reactivate", new ReactivatePublicHolidayRequest { Id = holidayId });
        }
    }
}
