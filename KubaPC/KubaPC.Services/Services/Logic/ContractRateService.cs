using Dapper;
using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.ContractRates.Requests;
using KubaPC.Models.Contracts.ContractRates.Responses;
using KubaPC.Models.Internal;
using KubaPC.Services.Data;
using KubaPC.Services.Security;
using KubaPC.Services.Services.Interfaces;
using System.Data;

namespace KubaPC.Services.Services.Logic
{
    public class ContractRateService : BaseService, IContractRateService
    {
        private static readonly Dictionary<string, string> SORTABLE_COLUMNS = new(StringComparer.OrdinalIgnoreCase)
        {
            ["contractNumber"] = "c.contract_number",
            ["operatorName"] = "o.operator_name",
            ["vehicleType"] = "r.vehicle_type",
            ["operatedRatePerKm"] = "r.operated_rate_per_km_minor",
            ["standingRatePerKm"] = "r.standing_rate_per_km_minor",
            ["effectiveFromDate"] = "r.effective_from_date"
        };

        private const string ELEVATE = "BEGIN; SET LOCAL ROLE kubapc_administrator; ";
        private const string COMMIT = "; COMMIT;";

        private readonly IOperatorAccessService access;

        public ContractRateService(INpgsqlDataAccess dataAccess, IOperatorAccessService access) : base(dataAccess)
        {
            this.access = access;
        }

        public async Task<PagedResponse<ContractRateResponse>> SelectPagedRates(ContractRateListRequest request, Caller caller)
        {
            string sortColumn = SORTABLE_COLUMNS.TryGetValue(request.SortBy ?? string.Empty, out string? mappedColumn) ? mappedColumn : "r.effective_from_date";
            string sortDirection = request.SortDescending ? "DESC" : "ASC";

            string sql = $@"SELECT r.id, r.contract_id, c.contract_number, c.operator_id, o.operator_name, r.vehicle_type,
                                   r.operated_rate_per_km_minor, r.standing_rate_per_km_minor,
                                   r.effective_from_date, r.effective_to_date,
                                   (r.effective_to_date IS NULL) AS is_current, r.is_active,
                                   count(*) OVER() AS total_count
                            FROM contract_rates r
                            JOIN contracts c ON c.id = r.contract_id
                            JOIN operators o ON o.id = c.operator_id
                            WHERE (@ContractId IS NULL OR r.contract_id = @ContractId)
                              AND (@OperatorId IS NULL OR c.operator_id = @OperatorId)
                              AND (@VehicleType IS NULL OR r.vehicle_type = @VehicleType)
                              AND (@Search IS NULL OR c.contract_number ILIKE @SearchPattern OR o.operator_name ILIKE @SearchPattern)
                              AND (@IsActive IS NULL OR r.is_active = @IsActive)
                              {OperatorScope.ForOperatorColumn("c.operator_id")}
                            ORDER BY {sortColumn} {sortDirection}, r.vehicle_type, r.effective_from_date DESC
                            LIMIT @PageSize OFFSET @Offset";

            DynamicParameters parameters = new();
            OperatorScope.AddParameters(parameters, caller);
            parameters.Add("ContractId", request.ContractId, DbType.Guid);
            parameters.Add("OperatorId", request.OperatorId, DbType.Guid);
            parameters.Add("VehicleType", request.VehicleType, DbType.String);
            parameters.Add("Search", request.Search, DbType.String);
            parameters.Add("SearchPattern", $"%{request.Search}%", DbType.String);
            parameters.Add("IsActive", request.IsActive, DbType.Boolean);
            parameters.Add("PageSize", request.PageSize, DbType.Int32);
            parameters.Add("Offset", (request.Page - 1) * request.PageSize, DbType.Int32);

            List<ContractRateRowModel> rows = await _dataAccess.QueryAsync<ContractRateRowModel>(sql, parameters);

            long itemCount = rows.Count > 0 ? rows[0].TotalCount : 0;

            return new PagedResponse<ContractRateResponse>
            {
                Page = request.Page,
                PageSize = request.PageSize,
                ItemCount = itemCount,
                PageCount = (int)Math.Ceiling(itemCount / (double)request.PageSize),
                Data = rows.Select(row => new ContractRateResponse
                {
                    Id = row.Id,
                    ContractId = row.ContractId,
                    ContractNumber = row.ContractNumber,
                    OperatorId = row.OperatorId,
                    OperatorName = row.OperatorName,
                    VehicleType = row.VehicleType,
                    OperatedRatePerKm = Money.ToRands(row.OperatedRatePerKmMinor),
                    StandingRatePerKm = Money.ToRands(row.StandingRatePerKmMinor),
                    EffectiveFromDate = row.EffectiveFromDate,
                    EffectiveToDate = row.EffectiveToDate,
                    IsCurrent = row.IsCurrent,
                    IsActive = row.IsActive
                }).ToList()
            };
        }

        public async Task<CreateResult> CreateRate(CreateContractRateRequest request, Caller caller)
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
                return CreateResult.Failure("The contract is not active, so no rate can be added to it.");
            }

            const string typeSql = @"SELECT count(*) FROM vehicle_types WHERE vehicle_type_value = @VehicleType";
            long typeCount = await _dataAccess.QuerySingleOrDefaultAsync<long>(typeSql, new { request.VehicleType });

            if (typeCount == 0)
            {
                return CreateResult.Failure("Choose a vehicle type from the list.");
            }

            DateOnly? latestFrom = await SelectLatestFromDate(request.ContractId, request.VehicleType, null);

            if (latestFrom is not null && request.EffectiveFromDate <= latestFrom.Value)
            {
                return CreateResult.Failure($"The {request.VehicleType.ToLowerInvariant()} rate in force starts on {latestFrom.Value:yyyy-MM-dd}. A new rate must start after that date.");
            }

            const string sql = ELEVATE + @"
                UPDATE contract_rates
                SET effective_to_date = @EffectiveFromDate::date - 1, modified_at = now(), modified_by = @CreatedBy
                WHERE contract_id = @ContractId AND vehicle_type = @VehicleType AND is_active AND effective_to_date IS NULL;

                INSERT INTO contract_rates (id, contract_id, vehicle_type, operated_rate_per_km_minor, standing_rate_per_km_minor, effective_from_date, created_by)
                VALUES (@Id, @ContractId, @VehicleType, @OperatedRatePerKmMinor, @StandingRatePerKmMinor, @EffectiveFromDate, @CreatedBy)" + COMMIT;

            Guid id = Guid.NewGuid();

            await _dataAccess.ExecuteAsync(sql, new
            {
                Id = id,
                request.ContractId,
                request.VehicleType,
                OperatedRatePerKmMinor = Money.ToMinor(request.OperatedRatePerKm),
                StandingRatePerKmMinor = Money.ToMinor(request.StandingRatePerKm),
                request.EffectiveFromDate,
                CreatedBy = createdBy
            });

            return CreateResult.Success(id);
        }

        public async Task<OperationResult> UpdateRate(UpdateContractRateRequest request, Caller caller)
        {
            OperationResult allowed = await access.CheckContractRate(caller, request.Id);

            if (!allowed.IsSuccess)
            {
                return allowed;
            }

            Guid modifiedBy = caller.AccountId;
            const string rateSql = @"SELECT contract_id, vehicle_type, effective_to_date FROM contract_rates WHERE id = @Id AND is_active";
            ContractRateRowModel? rate = await _dataAccess.QuerySingleOrDefaultAsync<ContractRateRowModel>(rateSql, new { request.Id });

            if (rate is null)
            {
                return OperationResult.Failure("No active rate was found with that id.");
            }

            if (rate.EffectiveToDate is not null)
            {
                return OperationResult.Failure("Only the rate in force can be corrected. A closed rate is what past certificates were priced at.");
            }

            DateOnly? previousFrom = await SelectLatestFromDate(rate.ContractId, rate.VehicleType, request.Id);

            if (previousFrom is not null && request.EffectiveFromDate <= previousFrom.Value)
            {
                return OperationResult.Failure($"The {rate.VehicleType.ToLowerInvariant()} rate before this one starts on {previousFrom.Value:yyyy-MM-dd}. This rate must start after that date.");
            }

            const string sql = ELEVATE + @"
                UPDATE contract_rates
                SET effective_to_date = effective_from_date
                WHERE id = @PreviousId;

                UPDATE contract_rates
                SET operated_rate_per_km_minor = @OperatedRatePerKmMinor,
                    standing_rate_per_km_minor = @StandingRatePerKmMinor,
                    effective_from_date = @EffectiveFromDate,
                    modified_at = now(), modified_by = @ModifiedBy
                WHERE id = @Id AND is_active AND effective_to_date IS NULL;

                UPDATE contract_rates
                SET effective_to_date = @EffectiveFromDate::date - 1, modified_at = now(), modified_by = @ModifiedBy
                WHERE id = @PreviousId" + COMMIT;

            DynamicParameters parameters = new();
            parameters.Add("Id", request.Id, DbType.Guid);
            parameters.Add("PreviousId", await SelectPreviousRateId(rate.ContractId, rate.VehicleType, request.Id), DbType.Guid);
            parameters.Add("OperatedRatePerKmMinor", Money.ToMinor(request.OperatedRatePerKm), DbType.Int64);
            parameters.Add("StandingRatePerKmMinor", Money.ToMinor(request.StandingRatePerKm), DbType.Int64);
            parameters.Add("EffectiveFromDate", request.EffectiveFromDate, DbType.Date);
            parameters.Add("ModifiedBy", modifiedBy, DbType.Guid);

            await _dataAccess.ExecuteAsync(sql, parameters);

            return OperationResult.Success();
        }

        public async Task<OperationResult> DeactivateRate(Guid rateId, Caller caller)
        {
            OperationResult allowed = await access.CheckContractRate(caller, rateId);

            if (!allowed.IsSuccess)
            {
                return allowed;
            }

            Guid modifiedBy = caller.AccountId;
            const string rateSql = @"SELECT contract_id, vehicle_type, effective_to_date FROM contract_rates WHERE id = @RateId AND is_active";
            ContractRateRowModel? rate = await _dataAccess.QuerySingleOrDefaultAsync<ContractRateRowModel>(rateSql, new { RateId = rateId });

            if (rate is null)
            {
                return OperationResult.Failure("No active rate was found with that id.");
            }

            if (rate.EffectiveToDate is not null)
            {
                return OperationResult.Failure("A closed rate cannot be removed. Past certificates were priced at it.");
            }

            const string sql = ELEVATE + @"
                UPDATE contract_rates
                SET is_active = FALSE, modified_at = now(), modified_by = @ModifiedBy
                WHERE id = @RateId AND is_active;

                UPDATE contract_rates
                SET effective_to_date = NULL, modified_at = now(), modified_by = @ModifiedBy
                WHERE id = @PreviousId" + COMMIT;

            DynamicParameters parameters = new();
            parameters.Add("RateId", rateId, DbType.Guid);
            parameters.Add("PreviousId", await SelectPreviousRateId(rate.ContractId, rate.VehicleType, rateId), DbType.Guid);
            parameters.Add("ModifiedBy", modifiedBy, DbType.Guid);

            await _dataAccess.ExecuteAsync(sql, parameters);

            return OperationResult.Success();
        }

        private async Task<Guid?> SelectPreviousRateId(Guid contractId, string vehicleType, Guid rateId)
        {
            const string sql = @"SELECT id
                                 FROM contract_rates
                                 WHERE contract_id = @ContractId AND vehicle_type = @VehicleType AND is_active AND id <> @RateId
                                 ORDER BY effective_from_date DESC
                                 LIMIT 1";

            return await _dataAccess.QuerySingleOrDefaultAsync<Guid?>(sql, new { ContractId = contractId, VehicleType = vehicleType, RateId = rateId });
        }

        public async Task<List<string>> SelectVehicleTypes()
        {
            const string sql = @"SELECT vehicle_type_value FROM vehicle_types ORDER BY display_order";

            return await _dataAccess.QueryAsync<string>(sql, new { });
        }

        private async Task<DateOnly?> SelectLatestFromDate(Guid contractId, string vehicleType, Guid? exceptRateId)
        {
            const string sql = @"SELECT max(effective_from_date)
                                 FROM contract_rates
                                 WHERE contract_id = @ContractId AND vehicle_type = @VehicleType AND is_active
                                   AND (@ExceptRateId IS NULL OR id <> @ExceptRateId)";

            DynamicParameters parameters = new();
            parameters.Add("ContractId", contractId, DbType.Guid);
            parameters.Add("VehicleType", vehicleType, DbType.String);
            parameters.Add("ExceptRateId", exceptRateId, DbType.Guid);

            return await _dataAccess.QuerySingleOrDefaultAsync<DateOnly?>(sql, parameters);
        }

        private sealed class ContractRateRowModel
        {
            public Guid Id { get; set; }
            public Guid ContractId { get; set; }
            public string ContractNumber { get; set; } = string.Empty;
            public Guid OperatorId { get; set; }
            public string OperatorName { get; set; } = string.Empty;
            public string VehicleType { get; set; } = string.Empty;
            public long OperatedRatePerKmMinor { get; set; }
            public long StandingRatePerKmMinor { get; set; }
            public DateOnly EffectiveFromDate { get; set; }
            public DateOnly? EffectiveToDate { get; set; }
            public bool IsCurrent { get; set; }
            public bool IsActive { get; set; }
            public long TotalCount { get; set; }
        }
    }
}
