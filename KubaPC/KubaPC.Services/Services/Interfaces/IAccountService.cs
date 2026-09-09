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
        Task<int> UpdateLastLogin(Guid accountId);
        Task<CreateAccountResult> CreateAccount(CreateAccountRequest request, Guid createdBy);
        Task<bool> ChangePassword(Guid accountId, string oldPassword, string newPassword);
        Task<PagedResponse<AccountListResponse>> SelectPagedAccountList(AccountListRequest request);

        Task<AccountOperationResult> UpdateAccount(UpdateAccountRequest request, Guid modifiedBy);
        Task<AccountOperationResult> DeactivateAccount(Guid accountId, Guid modifiedBy);

    }
}
