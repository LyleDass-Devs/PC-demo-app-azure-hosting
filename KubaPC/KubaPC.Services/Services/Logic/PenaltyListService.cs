using Dapper;
using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.Penalties.Requests;
using KubaPC.Models.Contracts.Penalties.Responses;
using KubaPC.Models.Internal;
using KubaPC.Services.Data;
using KubaPC.Services.Security;
using KubaPC.Services.Services.Interfaces;
using System.Data;

namespace KubaPC.Services.Services.Logic
{
    public class PenaltyListService : BaseService, IPenaltyListService
    {
        private static readonly Dictionary<string, string> SORTABLE_COLUMNS = new(StringComparer.OrdinalIgnoreCase)
        {
            ["penaltyCode"] = "p.penalty_code",
            ["description"] = "p.description",
            ["penaltyType"] = "p.penalty_type",
            ["randAmount"] = "p.rand_amount_minor",
            ["contractNumber"] = "c.contract_number",
            ["operatorName"] = "o.operator_name"
        };

        private const string ELEVATE = "BEGIN; SET LOCAL ROLE kubapc_administrator; ";
        private const string COMMIT = "; COMMIT;";

        private readonly IOperatorAccessService access;

        public PenaltyListService(INpgsqlDataAccess dataAccess, IOperatorAccessService access) : base(dataAccess)
        {
            this.access = access;
        }

        public async Task<PagedResponse<PenaltyListItemResponse>> SelectPagedItems(PenaltyListItemListRequest request, Caller caller)
        {
            string sortColumn = SORTABLE_COLUMNS.TryGetValue(request.SortBy ?? string.Empty, out string? mappedColumn) ? mappedColumn : "p.penalty_code";
            string sortDirection = request.SortDescending ? "DESC" : "ASC";

            string sql = $@"SELECT p.id, p.contract_id, c.contract_number, c.operator_id, o.operator_name, p.penalty_code, p.description, p.penalty_type, p.revenue_type,
                                   p.rand_amount_minor, p.penalty_formula, p.repeat_penalty_escalation_enabled, p.is_active,
                                   (SELECT count(*) FROM penalty_captures pc WHERE pc.penalty_list_item_id = p.id) AS capture_count,
                                   count(*) OVER() AS total_count
                            FROM penalty_list_items p
                            JOIN contracts c ON c.id = p.contract_id
                            JOIN operators o ON o.id = c.operator_id
                            WHERE (@ContractId IS NULL OR p.contract_id = @ContractId)
                              AND (@OperatorId IS NULL OR c.operator_id = @OperatorId)
                              AND (@Search IS NULL OR p.penalty_code ILIKE @SearchPattern ESCAPE '\' OR p.description ILIKE @SearchPattern ESCAPE '\')
                              AND (@IsActive IS NULL OR p.is_active = @IsActive)
                              {OperatorScope.ForOperatorColumn("c.operator_id")}
                            ORDER BY {sortColumn} {sortDirection}
                            LIMIT @PageSize OFFSET @Offset";

            DynamicParameters parameters = new();
            OperatorScope.AddParameters(parameters, caller);
            parameters.Add("ContractId", request.ContractId, DbType.Guid);
            parameters.Add("OperatorId", request.OperatorId, DbType.Guid);
            parameters.Add("Search", request.Search, DbType.String);
            parameters.Add("SearchPattern", SearchPattern.Build(request.Search), DbType.String);
            parameters.Add("IsActive", request.IsActive, DbType.Boolean);
            parameters.Add("PageSize", request.PageSize, DbType.Int32);
            parameters.Add("Offset", (request.Page - 1) * request.PageSize, DbType.Int32);

            List<PenaltyListItemRowModel> rows = await _dataAccess.QueryAsync<PenaltyListItemRowModel>(sql, parameters);

            long itemCount = rows.Count > 0 ? rows[0].TotalCount : 0;

            return new PagedResponse<PenaltyListItemResponse>
            {
                Page = request.Page,
                PageSize = request.PageSize,
                ItemCount = itemCount,
                PageCount = (int)Math.Ceiling(itemCount / (double)request.PageSize),
                Data = rows.Select(row => new PenaltyListItemResponse
                {
                    Id = row.Id,
                    ContractId = row.ContractId,
                    ContractNumber = row.ContractNumber,
                    OperatorId = row.OperatorId,
                    OperatorName = row.OperatorName,
                    PenaltyCode = row.PenaltyCode,
                    Description = row.Description,
                    PenaltyType = row.PenaltyType,
                    RevenueType = row.RevenueType,
                    RandAmount = row.RandAmountMinor is null ? null : Money.ToRands(row.RandAmountMinor.Value),
                    PenaltyFormula = row.PenaltyFormula,
                    RepeatPenaltyEscalationEnabled = row.RepeatPenaltyEscalationEnabled,
                    IsActive = row.IsActive,
                    CaptureCount = row.CaptureCount
                }).ToList()
            };
        }

        public async Task<PenaltyLookupsResponse> SelectLookups()
        {
            const string typesSql = @"SELECT penalty_type_value AS value, escalation_counting_scope
                                      FROM penalty_types ORDER BY display_order";
            const string revenueSql = @"SELECT revenue_type_value FROM revenue_types ORDER BY display_order";

            return new PenaltyLookupsResponse
            {
                PenaltyTypes = await _dataAccess.QueryAsync<PenaltyTypeResponse>(typesSql, new { }),
                RevenueTypes = await _dataAccess.QueryAsync<string>(revenueSql, new { })
            };
        }

        public async Task<CreateResult> CreateItem(CreatePenaltyListItemRequest request, Caller caller)
        {
            OperationResult allowed = await access.CheckContract(caller, request.ContractId);

            if (!allowed.IsSuccess)
            {
                return CreateResult.From(allowed);
            }

            Guid createdBy = caller.AccountId;
            const string contractSql = @"SELECT count(*) FROM contracts WHERE id = @ContractId AND is_active";
            long contractCount = await _dataAccess.QuerySingleOrDefaultAsync<long>(contractSql, new { request.ContractId });

            if (contractCount == 0)
            {
                return CreateResult.Failure("The contract is not active, so no penalty can be added to it.");
            }

            string code = request.PenaltyCode.Trim();

            if (await IsCodeTaken(request.ContractId, code, null))
            {
                return CreateResult.Failure($"An active penalty already uses the code {code} on this contract.");
            }

            const string sql = ELEVATE + @"INSERT INTO penalty_list_items (contract_id, penalty_code, description, penalty_type, revenue_type,
                                                                 rand_amount_minor, penalty_formula, repeat_penalty_escalation_enabled, created_by)
                                 VALUES (@ContractId, @PenaltyCode, @Description, @PenaltyType, @RevenueType,
                                         @RandAmountMinor, @PenaltyFormula, @RepeatPenaltyEscalationEnabled, @CreatedBy)
                                 RETURNING id" + COMMIT;

            Guid id = await _dataAccess.QuerySingleOrDefaultAsync<Guid>(sql, new
            {
                request.ContractId,
                PenaltyCode = code,
                Description = request.Description.Trim(),
                request.PenaltyType,
                RevenueType = Clean(request.RevenueType),
                RandAmountMinor = request.RandAmount is null ? (long?)null : Money.ToMinor(request.RandAmount.Value),
                PenaltyFormula = Clean(request.PenaltyFormula),
                request.RepeatPenaltyEscalationEnabled,
                CreatedBy = createdBy
            });

            return CreateResult.Success(id);
        }

        public async Task<OperationResult> UpdateItem(UpdatePenaltyListItemRequest request, Caller caller)
        {
            OperationResult allowed = await access.CheckPenaltyListItem(caller, request.Id);

            if (!allowed.IsSuccess)
            {
                return allowed;
            }

            Guid modifiedBy = caller.AccountId;
            const string contractSql = @"SELECT contract_id FROM penalty_list_items WHERE id = @Id AND is_active";
            Guid? contractId = await _dataAccess.QuerySingleOrDefaultAsync<Guid?>(contractSql, new { request.Id });

            if (contractId is null)
            {
                return OperationResult.Failure("No active penalty was found with that id.");
            }

            string code = request.PenaltyCode.Trim();

            if (await IsCodeTaken(contractId.Value, code, request.Id))
            {
                return OperationResult.Failure($"An active penalty already uses the code {code} on this contract.");
            }

            const string sql = ELEVATE + @"UPDATE penalty_list_items
                                 SET penalty_code = @PenaltyCode,
                                     description = @Description,
                                     penalty_type = @PenaltyType,
                                     revenue_type = @RevenueType,
                                     rand_amount_minor = @RandAmountMinor,
                                     penalty_formula = @PenaltyFormula,
                                     repeat_penalty_escalation_enabled = @RepeatPenaltyEscalationEnabled,
                                     modified_at = now(),
                                     modified_by = @ModifiedBy
                                 WHERE id = @Id AND is_active" + COMMIT;

            await _dataAccess.ExecuteAsync(sql, new
            {
                request.Id,
                PenaltyCode = code,
                Description = request.Description.Trim(),
                request.PenaltyType,
                RevenueType = Clean(request.RevenueType),
                RandAmountMinor = request.RandAmount is null ? (long?)null : Money.ToMinor(request.RandAmount.Value),
                PenaltyFormula = Clean(request.PenaltyFormula),
                request.RepeatPenaltyEscalationEnabled,
                ModifiedBy = modifiedBy
            });

            return OperationResult.Success();
        }

        public async Task<OperationResult> DeactivateItem(Guid itemId, Caller caller)
        {
            OperationResult allowed = await access.CheckPenaltyListItem(caller, itemId);

            if (!allowed.IsSuccess)
            {
                return allowed;
            }

            Guid modifiedBy = caller.AccountId;
            const string sql = ELEVATE + @"UPDATE penalty_list_items
                                 SET is_active = FALSE, modified_at = now(), modified_by = @ModifiedBy
                                 WHERE id = @ItemId AND is_active" + COMMIT;

            int updatedCount = await _dataAccess.ExecuteAsync(sql, new { ItemId = itemId, ModifiedBy = modifiedBy });

            return updatedCount == 0
                ? OperationResult.Failure("No active penalty was found with that id.")
                : OperationResult.Success();
        }

        public async Task<OperationResult> ReactivateItem(Guid itemId, Caller caller)
        {
            OperationResult allowed = await access.CheckPenaltyListItem(caller, itemId);

            if (!allowed.IsSuccess)
            {
                return allowed;
            }

            Guid modifiedBy = caller.AccountId;
            const string itemSql = @"SELECT p.contract_id, p.penalty_code
                                     FROM penalty_list_items p
                                     JOIN contracts c ON c.id = p.contract_id AND c.is_active
                                     WHERE p.id = @ItemId AND NOT p.is_active";
            PenaltyListItemRowModel? item = await _dataAccess.QuerySingleOrDefaultAsync<PenaltyListItemRowModel>(itemSql, new { ItemId = itemId });

            if (item is null)
            {
                return OperationResult.Failure("No deactivated penalty was found with that id on an active contract.");
            }

            if (await IsCodeTaken(item.ContractId, item.PenaltyCode, itemId))
            {
                return OperationResult.Failure($"An active penalty already uses the code {item.PenaltyCode} on this contract.");
            }

            const string sql = ELEVATE + @"UPDATE penalty_list_items
                                 SET is_active = TRUE, modified_at = now(), modified_by = @ModifiedBy
                                 WHERE id = @ItemId AND NOT is_active" + COMMIT;

            await _dataAccess.ExecuteAsync(sql, new { ItemId = itemId, ModifiedBy = modifiedBy });

            return OperationResult.Success();
        }

        private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private async Task<bool> IsCodeTaken(Guid contractId, string code, Guid? exceptItemId)
        {
            const string sql = @"SELECT count(*)
                                 FROM penalty_list_items
                                 WHERE contract_id = @ContractId
                                   AND lower(penalty_code) = lower(@Code)
                                   AND is_active
                                   AND (@ExceptId IS NULL OR id <> @ExceptId)";

            DynamicParameters parameters = new();
            parameters.Add("ContractId", contractId, DbType.Guid);
            parameters.Add("Code", code, DbType.String);
            parameters.Add("ExceptId", exceptItemId, DbType.Guid);

            long count = await _dataAccess.QuerySingleOrDefaultAsync<long>(sql, parameters);
            return count > 0;
        }
    }
}
