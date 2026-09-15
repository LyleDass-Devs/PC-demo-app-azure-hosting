using Dapper;
using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.Contracts.Requests;
using KubaPC.Models.Contracts.Contracts.Responses;
using KubaPC.Models.Internal;
using KubaPC.Services.Data;
using KubaPC.Services.Services.Interfaces;
using System.Data;

namespace KubaPC.Services.Services.Logic
{
    public class ContractService : BaseService, IContractService
    {
        private static readonly Dictionary<string, string> SORTABLE_COLUMNS = new(StringComparer.OrdinalIgnoreCase)
        {
            ["contractNumber"] = "c.contract_number",
            ["contractName"] = "c.contract_name",
            ["commencementDate"] = "c.commencement_date",
            ["expiryDate"] = "c.expiry_date",
            ["operatorName"] = "o.operator_name"
        };

        public ContractService(INpgsqlDataAccess dataAccess) : base(dataAccess)
        {
        }

        public async Task<PagedResponse<ContractListResponse>> SelectPagedContractList(ContractListRequest request)
        {
            string sortColumn = SORTABLE_COLUMNS.TryGetValue(request.SortBy ?? string.Empty, out string? mappedColumn) ? mappedColumn : "c.commencement_date";
            string sortDirection = request.SortDescending ? "DESC" : "ASC";

            string sql = $@"SELECT c.id, c.operator_id, o.operator_name, c.contract_number, c.contract_name, c.commencement_date, c.expiry_date, c.is_active,
                                   c.bank_name, c.bank_branch_name, c.bank_branch_code, c.bank_account_name, c.bank_account_number, c.bank_account_type,
                                   (SELECT count(*) FROM contract_rates r WHERE r.contract_id = c.id AND r.is_active) AS rate_count,
                                   (SELECT count(*) FROM payment_certificates p WHERE p.contract_id = c.id) AS certificate_count,
                                   count(*) OVER() AS total_count
                            FROM contracts c
                            JOIN operators o ON o.id = c.operator_id
                            WHERE (@OperatorId IS NULL OR c.operator_id = @OperatorId)
                              AND (@Search IS NULL OR c.contract_number ILIKE @SearchPattern OR c.contract_name ILIKE @SearchPattern)
                              AND (@IsActive IS NULL OR c.is_active = @IsActive)
                            ORDER BY {sortColumn} {sortDirection}
                            LIMIT @PageSize OFFSET @Offset";

            DynamicParameters parameters = new();
            parameters.Add("OperatorId", request.OperatorId, DbType.Guid);
            parameters.Add("Search", request.Search, DbType.String);
            parameters.Add("SearchPattern", $"%{request.Search}%", DbType.String);
            parameters.Add("IsActive", request.IsActive, DbType.Boolean);
            parameters.Add("PageSize", request.PageSize, DbType.Int32);
            parameters.Add("Offset", (request.Page - 1) * request.PageSize, DbType.Int32);

            List<ContractListRowModel> rows = await _dataAccess.QueryAsync<ContractListRowModel>(sql, parameters);

            long itemCount = rows.Count > 0 ? rows[0].TotalCount : 0;

            return new PagedResponse<ContractListResponse>
            {
                Page = request.Page,
                PageSize = request.PageSize,
                ItemCount = itemCount,
                PageCount = (int)Math.Ceiling(itemCount / (double)request.PageSize),
                Data = rows.Select(row => new ContractListResponse
                {
                    Id = row.Id,
                    OperatorId = row.OperatorId,
                    OperatorName = row.OperatorName,
                    ContractNumber = row.ContractNumber,
                    ContractName = row.ContractName,
                    CommencementDate = row.CommencementDate,
                    ExpiryDate = row.ExpiryDate,
                    IsActive = row.IsActive,
                    RateCount = row.RateCount,
                    CertificateCount = row.CertificateCount,
                    BankName = row.BankName,
                    BankBranchName = row.BankBranchName,
                    BankBranchCode = row.BankBranchCode,
                    BankAccountName = row.BankAccountName,
                    BankAccountNumber = row.BankAccountNumber,
                    BankAccountType = row.BankAccountType
                }).ToList()
            };
        }

        public async Task<ContractListResponse?> SelectContract(Guid contractId)
        {
            const string sql = @"SELECT c.id, c.operator_id, o.operator_name, c.contract_number, c.contract_name, c.commencement_date, c.expiry_date, c.is_active,
                                        c.bank_name, c.bank_branch_name, c.bank_branch_code, c.bank_account_name, c.bank_account_number, c.bank_account_type,
                                        0 AS rate_count, 0 AS certificate_count, 0::bigint AS total_count
                                 FROM contracts c
                                 JOIN operators o ON o.id = c.operator_id
                                 WHERE c.id = @ContractId";

            ContractListRowModel? row = await _dataAccess.QuerySingleOrDefaultAsync<ContractListRowModel>(sql, new { ContractId = contractId });

            return row is null ? null : new ContractListResponse
            {
                Id = row.Id,
                OperatorId = row.OperatorId,
                OperatorName = row.OperatorName,
                ContractNumber = row.ContractNumber,
                ContractName = row.ContractName,
                CommencementDate = row.CommencementDate,
                ExpiryDate = row.ExpiryDate,
                IsActive = row.IsActive,
                BankName = row.BankName,
                BankBranchName = row.BankBranchName,
                BankBranchCode = row.BankBranchCode,
                BankAccountName = row.BankAccountName,
                BankAccountNumber = row.BankAccountNumber,
                BankAccountType = row.BankAccountType
            };
        }

        public async Task<CreateResult> CreateContract(CreateContractRequest request, Guid createdBy)
        {
            const string operatorSql = @"SELECT count(*) FROM operators WHERE id = @OperatorId AND is_active";
            long operatorCount = await _dataAccess.QuerySingleOrDefaultAsync<long>(operatorSql, new { request.OperatorId });

            if (operatorCount == 0)
            {
                return CreateResult.Failure("The operator is not active, so no contract can be added to it.");
            }

            string contractNumber = request.ContractNumber.Trim();

            if (await IsContractNumberTaken(contractNumber, null))
            {
                return CreateResult.Failure($"An active contract already uses the number {contractNumber}.");
            }

            const string sql = @"INSERT INTO contracts (operator_id, contract_number, contract_name, commencement_date, expiry_date,
                                                        bank_name, bank_branch_name, bank_branch_code, bank_account_name, bank_account_number, bank_account_type,
                                                        created_by)
                                 VALUES (@OperatorId, @ContractNumber, @ContractName, @CommencementDate, @ExpiryDate,
                                         @BankName, @BankBranchName, @BankBranchCode, @BankAccountName, @BankAccountNumber, @BankAccountType,
                                         @CreatedBy)
                                 RETURNING id";

            Guid id = await _dataAccess.QuerySingleOrDefaultAsync<Guid>(sql, new
            {
                request.OperatorId,
                ContractNumber = contractNumber,
                ContractName = Clean(request.ContractName),
                request.CommencementDate,
                request.ExpiryDate,
                BankName = Clean(request.BankName),
                BankBranchName = Clean(request.BankBranchName),
                BankBranchCode = Clean(request.BankBranchCode),
                BankAccountName = Clean(request.BankAccountName),
                BankAccountNumber = Clean(request.BankAccountNumber),
                BankAccountType = Clean(request.BankAccountType),
                CreatedBy = createdBy
            });

            return CreateResult.Success(id);
        }

        public async Task<OperationResult> UpdateContract(UpdateContractRequest request, Guid modifiedBy)
        {
            string contractNumber = request.ContractNumber.Trim();

            if (await IsContractNumberTaken(contractNumber, request.Id))
            {
                return OperationResult.Failure($"An active contract already uses the number {contractNumber}.");
            }

            const string sql = @"UPDATE contracts
                                 SET contract_number = @ContractNumber,
                                     contract_name = @ContractName,
                                     commencement_date = @CommencementDate,
                                     expiry_date = @ExpiryDate,
                                     bank_name = @BankName,
                                     bank_branch_name = @BankBranchName,
                                     bank_branch_code = @BankBranchCode,
                                     bank_account_name = @BankAccountName,
                                     bank_account_number = @BankAccountNumber,
                                     bank_account_type = @BankAccountType,
                                     modified_at = now(),
                                     modified_by = @ModifiedBy
                                 WHERE id = @Id AND is_active";

            int updatedCount = await _dataAccess.ExecuteAsync(sql, new
            {
                request.Id,
                ContractNumber = contractNumber,
                ContractName = Clean(request.ContractName),
                request.CommencementDate,
                request.ExpiryDate,
                BankName = Clean(request.BankName),
                BankBranchName = Clean(request.BankBranchName),
                BankBranchCode = Clean(request.BankBranchCode),
                BankAccountName = Clean(request.BankAccountName),
                BankAccountNumber = Clean(request.BankAccountNumber),
                BankAccountType = Clean(request.BankAccountType),
                ModifiedBy = modifiedBy
            });

            return updatedCount == 0
                ? OperationResult.Failure("No active contract was found with that id.")
                : OperationResult.Success();
        }

        public async Task<OperationResult> DeactivateContract(Guid contractId, Guid modifiedBy)
        {
            const string sql = @"UPDATE contracts
                                 SET is_active = FALSE, modified_at = now(), modified_by = @ModifiedBy
                                 WHERE id = @ContractId AND is_active";

            int updatedCount = await _dataAccess.ExecuteAsync(sql, new { ContractId = contractId, ModifiedBy = modifiedBy });

            return updatedCount == 0
                ? OperationResult.Failure("No active contract was found with that id.")
                : OperationResult.Success();
        }

        public async Task<OperationResult> ReactivateContract(Guid contractId, Guid modifiedBy)
        {
            const string numberSql = @"SELECT c.contract_number
                                       FROM contracts c
                                       JOIN operators o ON o.id = c.operator_id AND o.is_active
                                       WHERE c.id = @ContractId AND NOT c.is_active";
            string? contractNumber = await _dataAccess.QuerySingleOrDefaultAsync<string>(numberSql, new { ContractId = contractId });

            if (contractNumber is null)
            {
                return OperationResult.Failure("No deactivated contract was found with that id on an active operator.");
            }

            if (await IsContractNumberTaken(contractNumber, contractId))
            {
                return OperationResult.Failure($"An active contract already uses the number {contractNumber}.");
            }

            const string sql = @"UPDATE contracts
                                 SET is_active = TRUE, modified_at = now(), modified_by = @ModifiedBy
                                 WHERE id = @ContractId AND NOT is_active";

            await _dataAccess.ExecuteAsync(sql, new { ContractId = contractId, ModifiedBy = modifiedBy });

            return OperationResult.Success();
        }

        // Empty strings from a form become NULL so optional columns stay honest.
        private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private async Task<bool> IsContractNumberTaken(string contractNumber, Guid? exceptContractId)
        {
            const string sql = @"SELECT count(*)
                                 FROM contracts
                                 WHERE lower(contract_number) = lower(@ContractNumber)
                                   AND is_active
                                   AND (@ExceptId IS NULL OR id <> @ExceptId)";

            DynamicParameters parameters = new();
            parameters.Add("ContractNumber", contractNumber, DbType.String);
            parameters.Add("ExceptId", exceptContractId, DbType.Guid);

            long count = await _dataAccess.QuerySingleOrDefaultAsync<long>(sql, parameters);
            return count > 0;
        }
    }
}
