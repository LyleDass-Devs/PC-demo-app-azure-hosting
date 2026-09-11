using KubaPC.Models.Contracts.Accounts.Requests;
using KubaPC.Models.Contracts.Accounts.Responses;
using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.SharedClient.Services.Http;
using KubaPC.SharedClient.Services.Models;

namespace KubaPC.SharedClient.Services.Api
{
    public class AccountClientService : IAccountClientService
    {
        private readonly IHttpService httpService;

        public AccountClientService(IHttpService httpService)
        {
            this.httpService = httpService;
        }

        public async Task<ApiResult<CurrentUserResponse>> GetCurrent()
        {
            return await httpService.Get<CurrentUserResponse>("/accounts/current");
        }

       
        public async Task<ApiResult<PagedResponse<AccountListResponse>>> GetPaged(AccountListRequest request)
        {
            List<string> parameters = new()
            {
                $"page={request.Page}",
                $"pageSize={request.PageSize}",
                $"sortDescending={request.SortDescending}"
            };

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                parameters.Add($"search={Uri.EscapeDataString(request.Search)}");
            }

            if (request.IsActive.HasValue)
            {
                parameters.Add($"isActive={request.IsActive.Value}");
            }

            if (!string.IsNullOrWhiteSpace(request.SortBy))
            {
                parameters.Add($"sortBy={Uri.EscapeDataString(request.SortBy)}");
            }

            return await httpService.Get<PagedResponse<AccountListResponse>>($"/accounts?{string.Join("&", parameters)}");
        }

        public async Task<ApiResult<Guid>> Create(CreateAccountRequest request)
        {
            return await httpService.Post<Guid>("/accounts/create", request);
        }

        public async Task<ApiResult> Update(UpdateAccountRequest request)
        {
            return await httpService.Put("/accounts/update", request);
        }

        public async Task<ApiResult> Deactivate(Guid accountId)
        {
            return await httpService.Post("/accounts/deactivate", new DeactivateAccountRequest { Id = accountId });
        }

        public async Task<ApiResult> ChangePassword(ChangePasswordRequest request)
        {
            return await httpService.Post("/accounts/change-password", request);
        }
    }
}