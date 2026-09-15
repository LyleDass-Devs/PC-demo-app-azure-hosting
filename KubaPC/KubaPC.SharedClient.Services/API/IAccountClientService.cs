using KubaPC.Models.Contracts.Accounts.Requests;
using KubaPC.Models.Contracts.Accounts.Responses;
using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.SharedClient.Services.Models;

namespace KubaPC.SharedClient.Services.Api
{
    public interface IAccountClientService
    {
        Task<ApiResult<CurrentUserResponse>> GetCurrent();
        Task<ApiResult<PagedResponse<AccountListResponse>>> GetPaged(AccountListRequest request);
        Task<ApiResult<Guid>> Create(CreateAccountRequest request);
        Task<ApiResult> Update(UpdateAccountRequest request);
        Task<ApiResult> Deactivate(Guid accountId);
        Task<ApiResult> Reactivate(Guid accountId);
        Task<ApiResult> UpdateOwn(UpdateOwnAccountRequest request);
        Task<ApiResult> ChangePassword(ChangePasswordRequest request);
        Task<ApiResult> ResetPassword(ResetPasswordRequest request);
    }
}