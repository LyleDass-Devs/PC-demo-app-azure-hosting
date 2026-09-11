using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.SharedClient.Services.Http;
using KubaPC.SharedClient.Services.Models;

namespace KubaPC.SharedClient.Services.Api
{
    public class RoleClientService : IRoleClientService
    {
        private readonly IHttpService httpService;

        public RoleClientService(IHttpService httpService)
        {
            this.httpService = httpService;
        }

        public async Task<ApiResult<List<DropDownResponse>>> GetAll()
        {
            return await httpService.Get<List<DropDownResponse>>("/roles");
        }
    }
}