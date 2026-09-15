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
            return await httpService.Get<PagedResponse<AccountListResponse>>($"/accounts?{PagedQuery.From(request)}");
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

        public async Task<ApiResult> Reactivate(Guid accountId)
        {
            return await httpService.Post("/accounts/reactivate", new ReactivateAccountRequest { Id = accountId });
        }

        public async Task<ApiResult<List<Guid>>> GetOperatorIds(Guid accountId)
        {
            return await httpService.Get<List<Guid>>($"/accounts/{accountId}/operators");
        }

        public async Task<ApiResult> UpdateOperators(UpdateAccountOperatorsRequest request)
        {
            return await httpService.Post("/accounts/operators/update", request);
        }

        public async Task<ApiResult> UpdateOwn(UpdateOwnAccountRequest request)
        {
            return await httpService.Put("/accounts/me", request);
        }

        public async Task<ApiResult> ChangePassword(ChangePasswordRequest request)
        {
            return await httpService.Post("/accounts/change-password", request);
        }

        public async Task<ApiResult> ResetPassword(ResetPasswordRequest request)
        {
            return await httpService.Post("/accounts/reset-password", request);
        }
    }
}