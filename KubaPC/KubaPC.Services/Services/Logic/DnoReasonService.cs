using Dapper;
using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.DnoReasons.Requests;
using KubaPC.Models.Contracts.DnoReasons.Responses;
using KubaPC.Models.Internal;
using KubaPC.Services.Data;
using KubaPC.Services.Services.Interfaces;
using System.Data;

namespace KubaPC.Services.Services.Logic
{
    public class DnoReasonService : BaseService, IDnoReasonService
    {
        private const string ELEVATE = "BEGIN; SET LOCAL ROLE kubapc_administrator; ";
        private const string COMMIT = "; COMMIT;";

        private static readonly Dictionary<string, string> SORTABLE_COLUMNS = new(StringComparer.OrdinalIgnoreCase)
        {
            ["reasonValue"] = "r.reason_value",
            ["createdAt"] = "r.created_at"
        };

        public DnoReasonService(INpgsqlDataAccess dataAccess) : base(dataAccess)
        {
        }

        public async Task<PagedResponse<DnoReasonResponse>> SelectPaged(DnoReasonListRequest request)
        {
            string sortColumn = SORTABLE_COLUMNS.TryGetValue(request.SortBy ?? string.Empty, out string? mappedColumn) ? mappedColumn : "r.reason_value";
            string sortDirection = request.SortDescending ? "DESC" : "ASC";

            string sql = $@"SELECT r.id, r.reason_value, r.is_active,
                                   (SELECT count(*) FROM form_f1_records f WHERE f.dno_reason_id = r.id) AS use_count,
                                   count(*) OVER() AS total_count
                            FROM dno_reasons r
                            WHERE (@Search IS NULL OR r.reason_value ILIKE @SearchPattern)
                              AND (@IsActive IS NULL OR r.is_active = @IsActive)
                            ORDER BY {sortColumn} {sortDirection}
                            LIMIT @PageSize OFFSET @Offset";

            DynamicParameters parameters = new();
            parameters.Add("Search", request.Search, DbType.String);
            parameters.Add("SearchPattern", $"%{request.Search}%", DbType.String);
            parameters.Add("IsActive", request.IsActive, DbType.Boolean);
            parameters.Add("PageSize", request.PageSize, DbType.Int32);
            parameters.Add("Offset", (request.Page - 1) * request.PageSize, DbType.Int32);

            List<DnoReasonRowModel> rows = await _dataAccess.QueryAsync<DnoReasonRowModel>(sql, parameters);

            long itemCount = rows.Count > 0 ? rows[0].TotalCount : 0;

            return new PagedResponse<DnoReasonResponse>
            {
                Page = request.Page,
                PageSize = request.PageSize,
                ItemCount = itemCount,
                PageCount = (int)Math.Ceiling(itemCount / (double)request.PageSize),
                Data = rows.Select(row => new DnoReasonResponse
                {
                    Id = row.Id,
                    ReasonValue = row.ReasonValue,
                    IsActive = row.IsActive,
                    UseCount = row.UseCount
                }).ToList()
            };
        }

        public async Task<List<DropDownResponse>> SelectDropDown()
        {
            const string sql = @"SELECT id, reason_value AS value FROM dno_reasons WHERE is_active ORDER BY reason_value";
            return await _dataAccess.QueryAsync<DropDownResponse>(sql, new { });
        }

        public async Task<CreateResult> Create(CreateDnoReasonRequest request, Guid createdBy)
        {
            string value = request.ReasonValue.Trim();

            if (await IsTaken(value, null))
            {
                return CreateResult.Failure($"An active reason already reads {value}.");
            }

            const string sql = ELEVATE + @"INSERT INTO dno_reasons (reason_value, created_by) VALUES (@ReasonValue, @CreatedBy) RETURNING id" + COMMIT;

            Guid id = await _dataAccess.QuerySingleOrDefaultAsync<Guid>(sql, new { ReasonValue = value, CreatedBy = createdBy });

            return CreateResult.Success(id);
        }

        public async Task<OperationResult> Update(UpdateDnoReasonRequest request, Guid modifiedBy)
        {
            string value = request.ReasonValue.Trim();

            if (await IsTaken(value, request.Id))
            {
                return OperationResult.Failure($"An active reason already reads {value}.");
            }

            const string sql = ELEVATE + @"UPDATE dno_reasons
                                           SET reason_value = @ReasonValue, modified_at = now(), modified_by = @ModifiedBy
                                           WHERE id = @Id AND is_active" + COMMIT;

            int updatedCount = await _dataAccess.ExecuteAsync(sql, new { request.Id, ReasonValue = value, ModifiedBy = modifiedBy });

            return updatedCount == 0
                ? OperationResult.Failure("No active reason was found with that id.")
                : OperationResult.Success();
        }

        public async Task<OperationResult> Deactivate(Guid reasonId, Guid modifiedBy)
        {
            const string sql = ELEVATE + @"UPDATE dno_reasons
                                           SET is_active = FALSE, modified_at = now(), modified_by = @ModifiedBy
                                           WHERE id = @ReasonId AND is_active" + COMMIT;

            int updatedCount = await _dataAccess.ExecuteAsync(sql, new { ReasonId = reasonId, ModifiedBy = modifiedBy });

            return updatedCount == 0
                ? OperationResult.Failure("No active reason was found with that id.")
                : OperationResult.Success();
        }

        public async Task<OperationResult> Reactivate(Guid reasonId, Guid modifiedBy)
        {
            const string valueSql = @"SELECT reason_value FROM dno_reasons WHERE id = @ReasonId AND NOT is_active";
            string? value = await _dataAccess.QuerySingleOrDefaultAsync<string>(valueSql, new { ReasonId = reasonId });

            if (value is null)
            {
                return OperationResult.Failure("No deactivated reason was found with that id.");
            }

            if (await IsTaken(value, reasonId))
            {
                return OperationResult.Failure($"An active reason already reads {value}.");
            }

            const string sql = ELEVATE + @"UPDATE dno_reasons
                                           SET is_active = TRUE, modified_at = now(), modified_by = @ModifiedBy
                                           WHERE id = @ReasonId AND NOT is_active" + COMMIT;

            await _dataAccess.ExecuteAsync(sql, new { ReasonId = reasonId, ModifiedBy = modifiedBy });

            return OperationResult.Success();
        }

        private async Task<bool> IsTaken(string value, Guid? exceptId)
        {
            const string sql = @"SELECT count(*) FROM dno_reasons
                                 WHERE lower(reason_value) = lower(@Value) AND is_active
                                   AND (@ExceptId IS NULL OR id <> @ExceptId)";

            DynamicParameters parameters = new();
            parameters.Add("Value", value, DbType.String);
            parameters.Add("ExceptId", exceptId, DbType.Guid);

            long count = await _dataAccess.QuerySingleOrDefaultAsync<long>(sql, parameters);
            return count > 0;
        }
    }
}
