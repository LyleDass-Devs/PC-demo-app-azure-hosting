using Dapper;
using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.Operators.Requests;
using KubaPC.Models.Contracts.Operators.Responses;
using KubaPC.Models.Internal;
using KubaPC.Services.Data;
using KubaPC.Services.Security;
using KubaPC.Services.Services.Interfaces;
using System.Data;

namespace KubaPC.Services.Services.Logic
{
    public class OperatorService : BaseService, IOperatorService
    {
        private static readonly Dictionary<string, string> SORTABLE_COLUMNS = new(StringComparer.OrdinalIgnoreCase)
        {
            ["operatorName"] = "o.operator_name",
            ["idSociete"] = "o.id_societe",
            ["createdAt"] = "o.created_at"
        };

        private readonly IOperatorAccessService access;

        public OperatorService(INpgsqlDataAccess dataAccess, IOperatorAccessService access) : base(dataAccess)
        {
            this.access = access;
        }

        public async Task<PagedResponse<OperatorListResponse>> SelectPagedOperatorList(OperatorListRequest request, Caller caller)
        {
            string sortColumn = SORTABLE_COLUMNS.TryGetValue(request.SortBy ?? string.Empty, out string? mappedColumn) ? mappedColumn : "o.operator_name";
            string sortDirection = request.SortDescending ? "DESC" : "ASC";

            string sql = $@"SELECT o.id, o.id_societe, o.operator_name, o.is_active, o.created_at,
                                   (SELECT count(*) FROM vehicles v WHERE v.operator_id = o.id AND v.is_active) AS vehicle_count,
                                   (SELECT count(*) FROM contracts c WHERE c.operator_id = o.id AND c.is_active) AS contract_count,
                                   count(*) OVER() AS total_count
                            FROM operators o
                            WHERE (@Search IS NULL OR o.operator_name ILIKE @SearchPattern OR o.id_societe ILIKE @SearchPattern)
                              AND (@IsActive IS NULL OR o.is_active = @IsActive)
                              {OperatorScope.ForOperatorColumn("o.id")}
                            ORDER BY {sortColumn} {sortDirection}
                            LIMIT @PageSize OFFSET @Offset";

            DynamicParameters parameters = new();
            OperatorScope.AddParameters(parameters, caller);
            parameters.Add("Search", request.Search, DbType.String);
            parameters.Add("SearchPattern", $"%{request.Search}%", DbType.String);
            parameters.Add("IsActive", request.IsActive, DbType.Boolean);
            parameters.Add("PageSize", request.PageSize, DbType.Int32);
            parameters.Add("Offset", (request.Page - 1) * request.PageSize, DbType.Int32);

            List<OperatorListRowModel> rows = await _dataAccess.QueryAsync<OperatorListRowModel>(sql, parameters);

            long itemCount = rows.Count > 0 ? rows[0].TotalCount : 0;

            return new PagedResponse<OperatorListResponse>
            {
                Page = request.Page,
                PageSize = request.PageSize,
                ItemCount = itemCount,
                PageCount = (int)Math.Ceiling(itemCount / (double)request.PageSize),
                Data = rows.Select(row => new OperatorListResponse
                {
                    Id = row.Id,
                    IdSociete = row.IdSociete,
                    OperatorName = row.OperatorName,
                    IsActive = row.IsActive,
                    VehicleCount = row.VehicleCount,
                    ContractCount = row.ContractCount,
                    CreatedAt = row.CreatedAt
                }).ToList()
            };
        }

        public async Task<List<DropDownResponse>> SelectDropDown(Caller caller)
        {
            string sql = $@"SELECT o.id, o.operator_name AS value
                            FROM operators o
                            WHERE o.is_active
                            {OperatorScope.ForOperatorColumn("o.id")}
                            ORDER BY o.operator_name";

            DynamicParameters parameters = new();
            OperatorScope.AddParameters(parameters, caller);

            return await _dataAccess.QueryAsync<DropDownResponse>(sql, parameters);
        }

        public async Task<OperatorListResponse?> SelectOperator(Guid operatorId, Caller caller)
        {
            OperationResult allowed = await access.CheckOperator(caller, operatorId);

            if (!allowed.IsSuccess)
            {
                return null;
            }

            const string sql = @"SELECT o.id, o.id_societe, o.operator_name, o.is_active, o.created_at,
                                        (SELECT count(*) FROM vehicles v WHERE v.operator_id = o.id AND v.is_active) AS vehicle_count,
                                        (SELECT count(*) FROM contracts c WHERE c.operator_id = o.id AND c.is_active) AS contract_count,
                                        0::bigint AS total_count
                                 FROM operators o
                                 WHERE o.id = @OperatorId";

            OperatorListRowModel? row = await _dataAccess.QuerySingleOrDefaultAsync<OperatorListRowModel>(sql, new { OperatorId = operatorId });

            return row is null ? null : new OperatorListResponse
            {
                Id = row.Id,
                IdSociete = row.IdSociete,
                OperatorName = row.OperatorName,
                IsActive = row.IsActive,
                VehicleCount = row.VehicleCount,
                ContractCount = row.ContractCount,
                CreatedAt = row.CreatedAt
            };
        }

        public async Task<CreateResult> CreateOperator(CreateOperatorRequest request, Guid createdBy)
        {
            string idSociete = request.IdSociete.Trim();

            if (await IsIdSocieteTaken(idSociete, null))
            {
                return CreateResult.Failure($"An active operator already uses the company code {idSociete}.");
            }

            const string sql = @"INSERT INTO operators (id_societe, operator_name, created_by)
                                 VALUES (@IdSociete, @OperatorName, @CreatedBy)
                                 RETURNING id";

            Guid id = await _dataAccess.QuerySingleOrDefaultAsync<Guid>(sql, new
            {
                IdSociete = idSociete,
                OperatorName = request.OperatorName.Trim(),
                CreatedBy = createdBy
            });

            return CreateResult.Success(id);
        }

        public async Task<OperationResult> UpdateOperator(UpdateOperatorRequest request, Caller caller)
        {
            OperationResult allowed = await access.CheckOperator(caller, request.Id);

            if (!allowed.IsSuccess)
            {
                return allowed;
            }

            Guid modifiedBy = caller.AccountId;
            string idSociete = request.IdSociete.Trim();

            if (await IsIdSocieteTaken(idSociete, request.Id))
            {
                return OperationResult.Failure($"An active operator already uses the company code {idSociete}.");
            }

            const string sql = @"UPDATE operators
                                 SET id_societe = @IdSociete,
                                     operator_name = @OperatorName,
                                     modified_at = now(),
                                     modified_by = @ModifiedBy
                                 WHERE id = @Id AND is_active";

            int updatedCount = await _dataAccess.ExecuteAsync(sql, new
            {
                request.Id,
                IdSociete = idSociete,
                OperatorName = request.OperatorName.Trim(),
                ModifiedBy = modifiedBy
            });

            return updatedCount == 0
                ? OperationResult.Failure("No active operator was found with that id.")
                : OperationResult.Success();
        }

        public async Task<OperationResult> DeactivateOperator(Guid operatorId, Caller caller)
        {
            OperationResult allowed = await access.CheckOperator(caller, operatorId);

            if (!allowed.IsSuccess)
            {
                return allowed;
            }

            Guid modifiedBy = caller.AccountId;
            const string sql = @"UPDATE operators
                                 SET is_active = FALSE, modified_at = now(), modified_by = @ModifiedBy
                                 WHERE id = @OperatorId AND is_active";

            int updatedCount = await _dataAccess.ExecuteAsync(sql, new { OperatorId = operatorId, ModifiedBy = modifiedBy });

            return updatedCount == 0
                ? OperationResult.Failure("No active operator was found with that id.")
                : OperationResult.Success();
        }

        public async Task<OperationResult> ReactivateOperator(Guid operatorId, Caller caller)
        {
            OperationResult allowed = await access.CheckOperator(caller, operatorId);

            if (!allowed.IsSuccess)
            {
                return allowed;
            }

            Guid modifiedBy = caller.AccountId;
            const string codeSql = @"SELECT id_societe FROM operators WHERE id = @OperatorId AND NOT is_active";
            string? idSociete = await _dataAccess.QuerySingleOrDefaultAsync<string>(codeSql, new { OperatorId = operatorId });

            if (idSociete is null)
            {
                return OperationResult.Failure("No deactivated operator was found with that id.");
            }

            if (await IsIdSocieteTaken(idSociete, operatorId))
            {
                return OperationResult.Failure($"An active operator already uses the company code {idSociete}.");
            }

            const string sql = @"UPDATE operators
                                 SET is_active = TRUE, modified_at = now(), modified_by = @ModifiedBy
                                 WHERE id = @OperatorId AND NOT is_active";

            await _dataAccess.ExecuteAsync(sql, new { OperatorId = operatorId, ModifiedBy = modifiedBy });

            return OperationResult.Success();
        }

        private async Task<bool> IsIdSocieteTaken(string idSociete, Guid? exceptOperatorId)
        {
            const string sql = @"SELECT count(*)
                                 FROM operators
                                 WHERE lower(id_societe) = lower(@IdSociete)
                                   AND is_active
                                   AND (@ExceptId IS NULL OR id <> @ExceptId)";

            DynamicParameters parameters = new();
            parameters.Add("IdSociete", idSociete, DbType.String);
            parameters.Add("ExceptId", exceptOperatorId, DbType.Guid);

            long count = await _dataAccess.QuerySingleOrDefaultAsync<long>(sql, parameters);
            return count > 0;
        }
    }
}
