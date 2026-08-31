using KubaPC.Models.Contracts.Accounts.Requests;
using KubaPC.Models.Internal;
using KubaPC.Services.Data;
using KubaPC.Services.Helpers;
using KubaPC.Services.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace KubaPC.Services.Services.Logic
{
    public class AccountService : BaseService, IAccountService
    {
        private readonly IPasswordHelper passwordHelper;
        public AccountService(INpgsqlDataAccess dataAccess, IPasswordHelper passwordHelper) : base(dataAccess)
        {
              this.passwordHelper = passwordHelper;  
        }

        public async Task<AccountCredentialsModel?> SelectActiveByEmail(string emailAddress)
        {
            const string sql = @"SELECT id, email_address, full_name, password_hash
                                 FROM accounts
                                 WHERE lower(email_address) = lower(@EmailAddress)
                                 AND is_active";


            return await _dataAccess.QuerySingleOrDefaultAsync<AccountCredentialsModel>(sql, new { EmailAddress = emailAddress});
        }

        public async Task<List<string>> SelectRoleNames(Guid accountId)
        {
            const string sql = @"SELECT r.role_name
                                 FROM account_roles ar
                                 JOIN roles r ON r.id = ar.role_id AND r.is_active
                                 WHERE ar.account_id = @AccountId AND ar.is_active";

            return await _dataAccess.QueryAsync<string>(sql, new {AccountId = accountId });
        }

        public async Task<int> UpdateLastLogin(Guid accountId)
        {

            const string sql = @"UPDATE accounts
                                 SET last_login_at = now()
                                 WHERE id = @AccountId";

            return await _dataAccess.ExecuteAsync(sql, new { AccountId = accountId });
        }

        public async Task<CreateAccountResult> CreateAccount(CreateAccountRequest request, Guid createdBy)
        {
            //check acc exist
            AccountCredentialsModel? existingAccount = await SelectActiveByEmail(request.EmailAddress);

            if (existingAccount != null)
            {
                return new CreateAccountResult(null, $"An active account already exists for {request.EmailAddress}.");
            }

            //check role exist and is valid

            const string roleCountSql = @"SELECT count(*)
                                          FROM roles
                                          WHERE role_name = ANY(@RoleNames) AND is_active";

            long knownRoleCount = await _dataAccess.QuerySingleOrDefaultAsync<long>(roleCountSql, new {request.RoleNames});

            if (knownRoleCount != request.RoleNames.Distinct().Count()) 
            {
                return new CreateAccountResult(null, "One or more role names do not exist");
            }

            const string sql = @"WITH new_account AS (
                       INSERT INTO accounts (email_address, full_name, password_hash, password_changed_at, created_by)
                       VALUES (@EmailAddress, @FullName, @PasswordHash, now(), @CreatedBy)
                       RETURNING id
                     ),
                     new_roles AS (
                       INSERT INTO account_roles (account_id, role_id, created_by)
                       SELECT new_account.id, r.id, @CreatedBy
                       FROM new_account
                       JOIN roles r ON r.role_name = ANY(@RoleNames) AND r.is_active)
                     SELECT id FROM new_account";


            Guid id = await _dataAccess.QuerySingleOrDefaultAsync<Guid>(sql, new
            {
                request.EmailAddress,
                request.FullName,
                PasswordHash = passwordHelper.Hash(request.Password),
                request.RoleNames,
                CreatedBy = createdBy
            });

            return new CreateAccountResult(id, null);
        }
    }
}
