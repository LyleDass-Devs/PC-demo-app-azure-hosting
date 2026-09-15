using KubaPC.Models.Contracts.Accounts.Requests;
using KubaPC.Models.Internal;
using KubaPC.Services.Data;
using KubaPC.Services.Helpers;
using System.Collections.Generic;
using KubaPC.Services.Services.Interfaces;
using System;
using System.Text;
using System.Data;
using Dapper;
using KubaPC.Models.Contracts.Accounts.Responses;
using KubaPC.Models.Contracts.Common.Responses;

namespace KubaPC.Services.Services.Logic
{
    public class AccountService : BaseService, IAccountService
    {
        private const string SUPER_ADMIN_ROLE = "SuperAdmin";
        private static readonly Dictionary<string, string> SORTABLE_COLUMNS = new(StringComparer.OrdinalIgnoreCase)
        {
            ["fullName"] = "full_name",
            ["emailAddress"] = "email_address",
            ["lastLoginAt"] = "last_login_at",
            ["createdAt"] = "created_at"
        };
        private readonly IPasswordHelper passwordHelper;
        public AccountService(INpgsqlDataAccess dataAccess, IPasswordHelper passwordHelper) : base(dataAccess)
        {
            this.passwordHelper = passwordHelper;
        }

        public async Task<AccountCredentialsModel?> SelectActiveByEmail(string emailAddress)
        {
            const string sql = @"SELECT id, email_address, full_name, password_hash, must_change_password
                                 FROM accounts
                                 WHERE lower(email_address) = lower(@EmailAddress)
                                 AND is_active";


            return await _dataAccess.QuerySingleOrDefaultAsync<AccountCredentialsModel>(sql, new { EmailAddress = emailAddress });
        }

        public async Task<List<string>> SelectRoleNames(Guid accountId)
        {
            const string sql = @"SELECT r.role_name
                                 FROM account_roles ar
                                 JOIN roles r ON r.id = ar.role_id AND r.is_active
                                 WHERE ar.account_id = @AccountId AND ar.is_active";

            return await _dataAccess.QueryAsync<string>(sql, new { AccountId = accountId });
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

            long knownRoleCount = await _dataAccess.QuerySingleOrDefaultAsync<long>(roleCountSql, new { request.RoleNames });

            if (knownRoleCount != request.RoleNames.Distinct().Count())
            {
                return new CreateAccountResult(null, "One or more role names do not exist");
            }

            const string sql = @"WITH new_account AS (
                       INSERT INTO accounts (email_address, full_name, password_hash,  password_changed_at, must_change_password, created_by)
                       VALUES (@EmailAddress, @FullName, @PasswordHash, now(), TRUE, @CreatedBy)
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

        public async Task<bool> ChangePassword(Guid accountId, string oldPassword, string newPassword)
        {
            const string sql = @"SELECT password_hash
                                 FROM accounts
                                 WHERE id = @AccountId AND is_active";

            string? currentHash = await _dataAccess.QuerySingleOrDefaultAsync<string>(sql, new { AccountId = accountId });

            if (currentHash is null || !passwordHelper.Verify(oldPassword, currentHash))
            {
                return false;
            }

            const string updateSql = @"UPDATE accounts
                               SET password_hash = @PasswordHash,
                                   password_changed_at = now(),
                                   must_change_password = FALSE,
                                   modified_at = now(),
                                   modified_by = @AccountId
                               WHERE id = @AccountId AND is_active";

            await _dataAccess.ExecuteAsync(updateSql, new
            {
                AccountId = accountId,
                PasswordHash = passwordHelper.Hash(newPassword)
            });

            return true;

        }
        public async Task<PagedResponse<AccountListResponse>> SelectPagedAccountList(AccountListRequest request)
        {
            string sortColumn = SORTABLE_COLUMNS.TryGetValue(request.SortBy ?? string.Empty, out string? mappedColumn) ? mappedColumn : "full_name";
            string sortDirection = request.SortDescending ? "DESC" : "ASC";

            string sql = $@"SELECT a.id, a.email_address, a.full_name, a.is_active, a.last_login_at, a.must_change_password,
                                   (SELECT string_agg(r.role_name, ', ' ORDER BY r.role_name)
                                    FROM account_roles ar
                                    JOIN roles r ON r.id = ar.role_id AND r.is_active
                                    WHERE ar.account_id = a.id AND ar.is_active) AS role_names,
                                   count(*) OVER() AS total_count
                            FROM accounts a
                            WHERE (@Search IS NULL OR a.full_name ILIKE @SearchPattern OR a.email_address ILIKE @SearchPattern)
                              AND (@IsActive IS NULL OR a.is_active = @IsActive)
                            ORDER BY {sortColumn} {sortDirection}
                            LIMIT @PageSize OFFSET @Offset";

            DynamicParameters parameters = new();
            parameters.Add("Search", request.Search, DbType.String);
            parameters.Add("SearchPattern", $"%{request.Search}%", DbType.String);
            parameters.Add("IsActive", request.IsActive, DbType.Boolean);
            parameters.Add("PageSize", request.PageSize, DbType.Int32);
            parameters.Add("Offset", (request.Page - 1) * request.PageSize, DbType.Int32);

            List<AccountListRowModel> rows = await _dataAccess.QueryAsync<AccountListRowModel>(sql, parameters);

            long itemCount = rows.Count > 0 ? rows[0].TotalCount : 0;

            return new PagedResponse<AccountListResponse>
            {
                Page = request.Page,
                PageSize = request.PageSize,
                ItemCount = itemCount,
                PageCount = (int)Math.Ceiling(itemCount / (double)request.PageSize),
                Data = rows.Select(row => new AccountListResponse
                {
                    Id = row.Id,
                    EmailAddress = row.EmailAddress,
                    FullName = row.FullName,
                    IsActive = row.IsActive,
                    LastLoginAt = row.LastLoginAt,
                    MustChangePassword = row.MustChangePassword,
                    RoleNames = row.RoleNames
                }).ToList()
            };
        }

        private async Task<bool> AreAllRoleNamesKnown(List<string> roleNames)
        {
            const string sql = @"SELECT count(*)
                         FROM roles
                         WHERE role_name = ANY(@RoleNames) AND is_active";

            long knownRoleCount = await _dataAccess.QuerySingleOrDefaultAsync<long>(sql, new { RoleNames = roleNames });

            return knownRoleCount == roleNames.Distinct().Count();
        }

        private async Task<bool> IsLastSuperAdmin(Guid accountId)
        {
            const string sql = @"SELECT count(*)
                         FROM account_roles ar
                         JOIN roles r ON r.id = ar.role_id AND r.is_active
                         JOIN accounts a ON a.id = ar.account_id AND a.is_active
                         WHERE r.role_name = @RoleName
                           AND ar.is_active
                           AND ar.account_id <> @AccountId";

            long otherSuperAdminCount = await _dataAccess.QuerySingleOrDefaultAsync<long>(sql, new
            {
                RoleName = SUPER_ADMIN_ROLE,
                AccountId = accountId
            });

            return otherSuperAdminCount == 0;
        }

        public async Task<AccountOperationResult> UpdateAccount(UpdateAccountRequest request, Guid modifiedBy)
        {

            if (!await AreAllRoleNamesKnown(request.RoleNames))
            {
                return AccountOperationResult.Failure("One or more role names do not exist.");
            }

            List<string> currentRoleNames = await SelectRoleNames(request.Id);
            bool areRolesChanging = !currentRoleNames.OrderBy(name => name).SequenceEqual(request.RoleNames.Distinct().OrderBy(name => name));


            if (areRolesChanging && request.Id == modifiedBy)
            {
                return AccountOperationResult.Failure("You cannot change the roles on your own account.");
            }

            bool isLosingSuperAdmin = currentRoleNames.Contains(SUPER_ADMIN_ROLE)
                             && !request.RoleNames.Contains(SUPER_ADMIN_ROLE);

            if (isLosingSuperAdmin && await IsLastSuperAdmin(request.Id))
            {
                return AccountOperationResult.Failure("This is the last SuperAdmin account; its SuperAdmin role cannot be removed.");
            }

            int updatedCount = await SaveAccountAndRoles(request, modifiedBy);

            return updatedCount == 0
                ? AccountOperationResult.Failure("No active account was found with that id.")
                : AccountOperationResult.Success();
        }

        private async Task<int> SaveAccountAndRoles(UpdateAccountRequest request, Guid modifiedBy)
        {
            const string sql = @"WITH target_roles AS (
                           SELECT id FROM roles WHERE role_name = ANY(@RoleNames) AND is_active
                         ),
                         updated_account AS (
                           UPDATE accounts
                           SET full_name = @FullName, modified_at = now(), modified_by = @ModifiedBy
                           WHERE id = @AccountId AND is_active
                           RETURNING id
                         ),
                         removed_roles AS (
                           UPDATE account_roles
                           SET is_active = FALSE, modified_at = now(), modified_by = @ModifiedBy
                           WHERE account_id = @AccountId
                             AND is_active
                             AND role_id NOT IN (SELECT id FROM target_roles)
                           RETURNING id
                         ),
                         restored_roles AS (
                           UPDATE account_roles
                           SET is_active = TRUE, modified_at = now(), modified_by = @ModifiedBy
                           WHERE account_id = @AccountId
                             AND NOT is_active
                             AND role_id IN (SELECT id FROM target_roles)
                           RETURNING id
                         ),
                         added_roles AS (
                           INSERT INTO account_roles (account_id, role_id, created_by)
                           SELECT updated_account.id, target_roles.id, @ModifiedBy
                           FROM updated_account
                           CROSS JOIN target_roles
                           WHERE NOT EXISTS (SELECT 1 FROM account_roles
                                             WHERE account_id = updated_account.id
                                               AND role_id = target_roles.id)
                           RETURNING id
                         )
                         SELECT count(*) FROM updated_account";

            long updatedCount = await _dataAccess.QuerySingleOrDefaultAsync<long>(sql, new
            {
                AccountId = request.Id,
                request.FullName,
                RoleNames = request.RoleNames.Distinct().ToList(),
                ModifiedBy = modifiedBy
            });

            return (int)updatedCount;
        }

        public async Task<AccountOperationResult> DeactivateAccount(Guid accountId, Guid modifiedBy)
        {
            if (accountId == modifiedBy)
            {
                return AccountOperationResult.Failure("You cannot deactivate your own account.");
            }

            List<string> roleNames = await SelectRoleNames(accountId);

            if (roleNames.Contains(SUPER_ADMIN_ROLE) && await IsLastSuperAdmin(accountId))
            {
                return AccountOperationResult.Failure("This is the last SuperAdmin account; it cannot be deactivated.");
            }

            const string sql = @"WITH deactivated_roles AS (
                           UPDATE account_roles
                           SET is_active = FALSE, modified_at = now(), modified_by = @ModifiedBy
                           WHERE account_id = @AccountId AND is_active
                           RETURNING id
                         ),
                         deactivated_account AS (
                           UPDATE accounts
                           SET is_active = FALSE, modified_at = now(), modified_by = @ModifiedBy
                           WHERE id = @AccountId AND is_active
                           RETURNING id
                         )
                         SELECT count(*) FROM deactivated_account";

            long deactivatedCount = await _dataAccess.QuerySingleOrDefaultAsync<long>(sql, new
            {
                AccountId = accountId,
                ModifiedBy = modifiedBy
            });

            return deactivatedCount == 0
                ? AccountOperationResult.Failure("No active account was found with that id.")
                : AccountOperationResult.Success();
        }

        /// <summary>
        /// Brings a deactivated account back, together with the roles it lost at deactivation.
        /// Deactivation stamps the account and its roles with the same modified_at, so that
        /// timestamp is what identifies the roles to restore. The password is left as it was;
        /// an administrator resets it separately if the person no longer knows it.
        /// </summary>
        public async Task<AccountOperationResult> ReactivateAccount(Guid accountId, Guid modifiedBy)
        {
            const string conflictSql = @"SELECT count(*)
                                         FROM accounts other
                                         JOIN accounts target ON target.id = @AccountId
                                         WHERE other.is_active
                                           AND other.id <> target.id
                                           AND lower(other.email_address) = lower(target.email_address)";

            long conflictCount = await _dataAccess.QuerySingleOrDefaultAsync<long>(conflictSql, new { AccountId = accountId });

            if (conflictCount > 0)
            {
                return AccountOperationResult.Failure("An active account already exists with that email address.");
            }

            const string sql = @"WITH target AS (
                                   SELECT id, modified_at AS deactivated_at
                                   FROM accounts
                                   WHERE id = @AccountId AND NOT is_active
                                 ),
                                 restored_roles AS (
                                   UPDATE account_roles ar
                                   SET is_active = TRUE, modified_at = now(), modified_by = @ModifiedBy
                                   FROM target
                                   WHERE ar.account_id = target.id
                                     AND NOT ar.is_active
                                     AND ar.modified_at = target.deactivated_at
                                   RETURNING ar.id
                                 ),
                                 reactivated_account AS (
                                   UPDATE accounts a
                                   SET is_active = TRUE, modified_at = now(), modified_by = @ModifiedBy
                                   FROM target
                                   WHERE a.id = target.id
                                   RETURNING a.id
                                 )
                                 SELECT count(*) FROM reactivated_account";

            long reactivatedCount = await _dataAccess.QuerySingleOrDefaultAsync<long>(sql, new
            {
                AccountId = accountId,
                ModifiedBy = modifiedBy
            });

            return reactivatedCount == 0
                ? AccountOperationResult.Failure("No deactivated account was found with that id.")
                : AccountOperationResult.Success();
        }

        public async Task<AccountOperationResult> ResetPassword(Guid accountId, string temporaryPassword, Guid modifiedBy)
        {

            if (accountId == modifiedBy)
            {
                return AccountOperationResult.Failure("Use Change password for your own account.");
            }

            const string sql = @"UPDATE accounts
                         SET password_hash = @PasswordHash,
                             password_changed_at = now(),
                             must_change_password = TRUE,
                             modified_at = now(),
                             modified_by = @ModifiedBy
                         WHERE id = @AccountId AND is_active";

            int updatedCount = await _dataAccess.ExecuteAsync(sql, new
            {
                AccountId = accountId,
                ModifiedBy = modifiedBy,
                PasswordHash = passwordHelper.Hash(temporaryPassword)
            });

            return updatedCount == 0 ? AccountOperationResult.Failure("No active account was found with that id.") : AccountOperationResult.Success();
        }

    }
}
