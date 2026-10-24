using KubaPC.Models.Contracts.Accounts.Requests;
using KubaPC.Models.Contracts.Accounts.Responses;
using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Internal;
using System;
using System.Collections.Generic;
using System.Text;

namespace KubaPC.Services.Services.Interfaces
{
    public interface IAccountService
    {
        Task<AccountCredentialsModel?> SelectActiveByEmail(string emailAddress);
        Task<List<string>> SelectRoleNames(Guid accountId);
        Task<SessionStateModel?> SelectSessionState(Guid accountId);
        Task<int> UpdateLastLogin(Guid accountId);
        Task<CreateAccountResult> CreateAccount(CreateAccountRequest request, Caller caller);
        Task<bool> ChangePassword(Guid accountId, string oldPassword, string newPassword);
        Task<PagedResponse<AccountListResponse>> SelectPagedAccountList(AccountListRequest request, Caller caller);

        Task<AccountOperationResult> UpdateAccount(UpdateAccountRequest request, Caller caller);
        Task<AccountOperationResult> DeactivateAccount(Guid accountId, Caller caller);
        Task<List<Guid>> SelectOperatorIds(Guid accountId, Caller caller);
        Task<AccountOperationResult> UpdateOperators(UpdateAccountOperatorsRequest request, Caller caller);
        Task<AccountOperationResult> ReactivateAccount(Guid accountId, Caller caller);
        Task<AccountOperationResult> UpdateOwnName(Guid accountId, string fullName);
        Task<AccountOperationResult> ResetPassword(Guid accountId, string temporaryPassword, Caller caller);

    }
}
