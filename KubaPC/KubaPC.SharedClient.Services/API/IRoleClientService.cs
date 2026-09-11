using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.SharedClient.Services.Models;

namespace KubaPC.SharedClient.Services.Api
{
    public interface IRoleClientService
    {
        Task<ApiResult<List<DropDownResponse>>> GetAll();
    }
}