using KubaPC.Models.Contracts.Accounts.Requests;
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

    }
}
