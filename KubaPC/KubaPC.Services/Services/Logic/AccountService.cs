using KubaPC.Models.Contracts.Accounts.Requests;
using KubaPC.Models.Internal;
using KubaPC.Services.Data;
using KubaPC.Services.Security;
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
        private const string SUPER_ADMIN_ONLY = "Only a SuperAdmin can assign, change or manage a SuperAdmin account.";
        private const string OPERATOR_STAFF_ROLE = "Operator Staff";
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
            const string sql = @"SELECT id, email_address, full_name, password_hash, must_change_password, token_version
                                 FROM accounts
                                 WHERE lower(email_address) = lower(@EmailAddress)
                                 AND is_active";

            return await _dataAccess.QuerySingleOrDefaultAsync<AccountCredentialsModel>(sql, new { EmailAddress = emailAddress });
        }

        public async Task<SessionStateModel?> SelectSessionState(Guid accountId)
        {
            const string sql = @"SELECT a.is_active, a.token_version, a.must_change_password,
                                        COALESCE(array_agg(r.role_name) FILTER (WHERE r.role_name IS NOT NULL), '{}') AS role_names
                                 FROM accounts a
                                 LEFT JOIN account_roles ar ON ar.account_id = a.id AND ar.is_active
                                 LEFT JOIN roles r ON r.id = ar.role_id AND r.is_active
                                 WHERE a.id = @AccountId
                                 GROUP BY a.id, a.is_active, a.token_version, a.must_change_password";

            SessionStateRowModel? row = await _dataAccess.QuerySingleOrDefaultAsync<SessionStateRowModel>(sql, new { AccountId = accountId });

            return row is null ? null : new SessionStateModel
            {
                IsActive = row.IsActive,
                TokenVersion = row.TokenVersion,
                MustChangePassword = row.MustChangePassword,
                RoleNames = row.RoleNames.ToList()
            };
        }

        private sealed class SessionStateRowModel
        {
            public bool IsActive { get; set; }
            public int TokenVersion { get; set; }
            public bool MustChangePassword { get; set; }
            public string[] RoleNames { get; set; } = Array.Empty<string>();
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

        public async Task<CreateAccountResult> CreateAccount(CreateAccountRequest request, Caller caller)
        {
            if (!caller.IsSuperAdmin && request.RoleNames.Contains(SUPER_ADMIN_ROLE))
            {
                return CreateAccountResult.Forbidden(SUPER_ADMIN_ONLY);
            }

            Guid createdBy = caller.AccountId;
            AccountCredentialsModel? existingAccount = await SelectActiveByEmail(request.EmailAddress);

            if (existingAccount != null)
            {
                return new CreateAccountResult(null, $"An active account already exists for {request.EmailAddress}.");
            }

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
                                   token_version = token_version + 1,
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
        public async Task<PagedResponse<AccountListResponse>> SelectPagedAccountList(AccountListRequest request, Caller caller)
        {
            string sortColumn = SORTABLE_COLUMNS.TryGetValue(request.SortBy ?? string.Empty, out string? mappedColumn) ? mappedColumn : "full_name";
            string sortDirection = request.SortDescending ? "DESC" : "ASC";

            string sql = $@"SELECT a.id, a.email_address, a.full_name, a.is_active, a.last_login_at, a.must_change_password,
                                   (SELECT string_agg(r.role_name, ', ' ORDER BY r.role_name)
                                    FROM account_roles ar
                                    JOIN roles r ON r.id = ar.role_id AND r.is_active
                                    WHERE ar.account_id = a.id AND ar.is_active) AS role_names,
                                   (SELECT string_agg(o.operator_name, ', ' ORDER BY o.operator_name)
                                    FROM account_operators ao
                                    JOIN operators o ON o.id = ao.operator_id AND o.is_active
                                    WHERE ao.account_id = a.id AND ao.is_active) AS operator_names,
                                   count(*) OVER() AS total_count
                            FROM accounts a
                            WHERE (@Search IS NULL OR a.full_name ILIKE @SearchPattern OR a.email_address ILIKE @SearchPattern)
                              AND (@IsActive IS NULL OR a.is_active = @IsActive)
                              AND (@IsUnscoped OR (NOT EXISTS (SELECT 1 FROM account_roles sar
                                                                JOIN roles sr ON sr.id = sar.role_id
                                                                WHERE sar.account_id = a.id AND sr.role_name = @SuperAdminRole)
                                                   AND NOT EXISTS (SELECT 1 FROM account_operators held
                                                                   WHERE held.account_id = a.id AND held.is_active
                                                                     AND NOT EXISTS (SELECT 1 FROM account_operators mine
                                                                                     WHERE mine.account_id = @Actor AND mine.is_active
                                                                                       AND mine.operator_id = held.operator_id))
                                                   AND (a.created_by = @Actor
                                                        OR EXISTS (SELECT 1 FROM account_operators held
                                                                   WHERE held.account_id = a.id AND held.is_active))))
                            ORDER BY {sortColumn} {sortDirection}
                            LIMIT @PageSize OFFSET @Offset";

            DynamicParameters parameters = new();
            OperatorScope.AddParameters(parameters, caller);
            parameters.Add("SuperAdminRole", SUPER_ADMIN_ROLE, DbType.String);
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
                    RoleNames = row.RoleNames,
                    OperatorNames = row.OperatorNames
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

        public async Task<AccountOperationResult> UpdateAccount(UpdateAccountRequest request, Caller caller)
        {
            AccountOperationResult allowed = await CheckManage(caller, request.Id);

            if (!allowed.IsSuccess)
            {
                return allowed;
            }

            if (!caller.IsSuperAdmin && request.RoleNames.Contains(SUPER_ADMIN_ROLE))
            {
                return AccountOperationResult.Forbidden(SUPER_ADMIN_ONLY);
            }

            Guid modifiedBy = caller.AccountId;

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

            int updatedCount = await SaveAccountAndRoles(request, modifiedBy, areRolesChanging);

            return updatedCount == 0
                ? AccountOperationResult.Failure("No active account was found with that id.")
                : AccountOperationResult.Success();
        }

        private async Task<int> SaveAccountAndRoles(UpdateAccountRequest request, Guid modifiedBy, bool areRolesChanging)
        {
            const string sql = @"WITH target_roles AS (
                           SELECT id FROM roles WHERE role_name = ANY(@RoleNames) AND is_active
                         ),
                         updated_account AS (
                           UPDATE accounts
                           SET full_name = @FullName,
                               token_version = token_version + CASE WHEN @AreRolesChanging THEN 1 ELSE 0 END,
                               modified_at = now(), modified_by = @ModifiedBy
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
                AreRolesChanging = areRolesChanging,
                ModifiedBy = modifiedBy
            });

            return (int)updatedCount;
        }

        public async Task<List<Guid>> SelectOperatorIds(Guid accountId, Caller caller)
        {
            AccountOperationResult allowed = await CheckManage(caller, accountId);

            if (!allowed.IsSuccess)
            {
                return new List<Guid>();
            }

            return await SelectGrantedOperatorIds(accountId);
        }

        private async Task<List<Guid>> SelectGrantedOperatorIds(Guid accountId)
        {
            const string sql = @"SELECT ao.operator_id
                                 FROM account_operators ao
                                 JOIN operators o ON o.id = ao.operator_id AND o.is_active
                                 WHERE ao.account_id = @AccountId AND ao.is_active
                                 ORDER BY o.operator_name";

            return await _dataAccess.QueryAsync<Guid>(sql, new { AccountId = accountId });
        }

        public async Task<AccountOperationResult> UpdateOperators(UpdateAccountOperatorsRequest request, Caller caller)
        {
            if (!caller.IsSuperAdmin && request.AccountId == caller.AccountId)
            {
                return AccountOperationResult.Forbidden("You cannot change the operators on your own account.");
            }

            AccountOperationResult allowed = await CheckManage(caller, request.AccountId);

            if (!allowed.IsSuccess)
            {
                return allowed;
            }

            Guid modifiedBy = caller.AccountId;
            List<Guid> operatorIds = request.OperatorIds.Distinct().ToList();

            if (!caller.IsSuperAdmin)
            {
                List<Guid> heldByCaller = await SelectGrantedOperatorIds(caller.AccountId);

                if (operatorIds.Any(operatorId => !heldByCaller.Contains(operatorId)))
                {
                    return AccountOperationResult.Forbidden("You can only grant operators your own account holds.");
                }
            }

            const string knownSql = @"SELECT count(*) FROM operators WHERE id = ANY(@OperatorIds) AND is_active";
            long knownCount = await _dataAccess.QuerySingleOrDefaultAsync<long>(knownSql, new { OperatorIds = operatorIds });

            if (knownCount != operatorIds.Count)
            {
                return AccountOperationResult.Failure("One of the operators is unknown or no longer active.");
            }

            if (operatorIds.Count > 1)
            {
                List<string> roleNames = await SelectRoleNames(request.AccountId);

                if (roleNames.Contains(OPERATOR_STAFF_ROLE))
                {
                    return AccountOperationResult.Failure("Operator Staff belong to one operator only.");
                }
            }

            const string sql = @"WITH target_account AS (
                                   SELECT id FROM accounts WHERE id = @AccountId AND is_active
                                 ),
                                 removed AS (
                                   UPDATE account_operators
                                   SET is_active = FALSE, modified_at = now(), modified_by = @ModifiedBy
                                   WHERE account_id = @AccountId AND is_active AND operator_id <> ALL(@OperatorIds)
                                   RETURNING id
                                 ),
                                 restored AS (
                                   UPDATE account_operators
                                   SET is_active = TRUE, modified_at = now(), modified_by = @ModifiedBy
                                   WHERE account_id = @AccountId AND NOT is_active AND operator_id = ANY(@OperatorIds)
                                   RETURNING id
                                 ),
                                 added AS (
                                   INSERT INTO account_operators (account_id, operator_id, created_by)
                                   SELECT target_account.id, wanted.operator_id, @ModifiedBy
                                   FROM target_account
                                   CROSS JOIN unnest(@OperatorIds) AS wanted(operator_id)
                                   WHERE NOT EXISTS (SELECT 1 FROM account_operators
                                                     WHERE account_id = target_account.id AND operator_id = wanted.operator_id)
                                   RETURNING id
                                 )
                                 SELECT count(*) FROM target_account";

            long accountCount = await _dataAccess.QuerySingleOrDefaultAsync<long>(sql, new
            {
                request.AccountId,
                OperatorIds = operatorIds,
                ModifiedBy = modifiedBy
            });

            return accountCount == 0
                ? AccountOperationResult.Failure("No active account was found with that id.")
                : AccountOperationResult.Success();
        }

        public async Task<AccountOperationResult> DeactivateAccount(Guid accountId, Caller caller)
        {
            AccountOperationResult allowed = await CheckManage(caller, accountId);

            if (!allowed.IsSuccess)
            {
                return allowed;
            }

            Guid modifiedBy = caller.AccountId;

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
                           SET is_active = FALSE, token_version = token_version + 1, modified_at = now(), modified_by = @ModifiedBy
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

        public async Task<AccountOperationResult> ReactivateAccount(Guid accountId, Caller caller)
        {
            AccountOperationResult allowed = await CheckManage(caller, accountId);

            if (!allowed.IsSuccess)
            {
                return allowed;
            }

            Guid modifiedBy = caller.AccountId;
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

        public async Task<AccountOperationResult> UpdateOwnName(Guid accountId, string fullName)
        {
            const string sql = @"UPDATE accounts
                                 SET full_name = @FullName,
                                     modified_at = now(),
                                     modified_by = @AccountId
                                 WHERE id = @AccountId AND is_active";

            int updatedCount = await _dataAccess.ExecuteAsync(sql, new
            {
                AccountId = accountId,
                FullName = fullName.Trim()
            });

            return updatedCount == 0
                ? AccountOperationResult.Failure("Your account is no longer active.")
                : AccountOperationResult.Success();
        }

        public async Task<AccountOperationResult> ResetPassword(Guid accountId, string temporaryPassword, Caller caller)
        {
            AccountOperationResult allowed = await CheckManage(caller, accountId);

            if (!allowed.IsSuccess)
            {
                return allowed;
            }

            Guid modifiedBy = caller.AccountId;

            if (accountId == modifiedBy)
            {
                return AccountOperationResult.Failure("Use Change password for your own account.");
            }

            const string sql = @"UPDATE accounts
                         SET password_hash = @PasswordHash,
                             password_changed_at = now(),
                             must_change_password = TRUE,
                             token_version = token_version + 1,
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

        private async Task<AccountOperationResult> CheckManage(Caller caller, Guid targetAccountId)
        {
            if (caller.IsSuperAdmin)
            {
                return AccountOperationResult.Success();
            }

            if (await HasEverHeldSuperAdmin(targetAccountId))
            {
                return AccountOperationResult.Forbidden(SUPER_ADMIN_ONLY);
            }

            if (!await IsWithinCallerScope(caller.AccountId, targetAccountId))
            {
                return AccountOperationResult.Forbidden("That account holds an operator your account does not, or was not created by you.");
            }

            return AccountOperationResult.Success();
        }

        private async Task<bool> HasEverHeldSuperAdmin(Guid accountId)
        {
            const string sql = @"SELECT count(*)
                                 FROM account_roles ar
                                 JOIN roles r ON r.id = ar.role_id
                                 WHERE ar.account_id = @AccountId AND r.role_name = @RoleName";

            long count = await _dataAccess.QuerySingleOrDefaultAsync<long>(sql, new { AccountId = accountId, RoleName = SUPER_ADMIN_ROLE });

            return count > 0;
        }

        private async Task<bool> IsWithinCallerScope(Guid callerAccountId, Guid targetAccountId)
        {
            const string sql = @"SELECT NOT EXISTS (SELECT 1 FROM account_operators held
                                                    WHERE held.account_id = @TargetAccountId AND held.is_active
                                                      AND NOT EXISTS (SELECT 1 FROM account_operators mine
                                                                      WHERE mine.account_id = @CallerAccountId AND mine.is_active
                                                                        AND mine.operator_id = held.operator_id))
                                        AND (EXISTS (SELECT 1 FROM account_operators held
                                                     WHERE held.account_id = @TargetAccountId AND held.is_active)
                                             OR EXISTS (SELECT 1 FROM accounts a
                                                        WHERE a.id = @TargetAccountId AND a.created_by = @CallerAccountId))";

            return await _dataAccess.QuerySingleOrDefaultAsync<bool>(sql, new { CallerAccountId = callerAccountId, TargetAccountId = targetAccountId });
        }

    }
}
