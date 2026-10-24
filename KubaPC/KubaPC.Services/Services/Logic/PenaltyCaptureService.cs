using Dapper;
using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.PenaltyCaptures.Requests;
using KubaPC.Models.Contracts.PenaltyCaptures.Responses;
using KubaPC.Models.Internal;
using KubaPC.Services.Data;
using KubaPC.Services.Security;
using KubaPC.Services.Services.Interfaces;
using System.Data;

namespace KubaPC.Services.Services.Logic
{
    public class PenaltyCaptureService : BaseService, IPenaltyCaptureService
    {
        private const string SUBMITTED_STATUS = "Submitted";
        private const string DISPUTED_STATUS = "Under Dispute";

        private static readonly string[] VEHICLE_SCOPED_TYPES = { "Unroadworthy", "Unsatisfactory Vehicle Condition" };

        private static readonly Dictionary<string, string> SORTABLE_COLUMNS = new(StringComparer.OrdinalIgnoreCase)
        {
            ["penaltyCode"] = "p.penalty_code",
            ["tripDate"] = "t.scheduled_date",
            ["capturedByRole"] = "p.captured_by_role",
            ["captureStatus"] = "p.capture_status"
        };

        private readonly IOperatorAccessService access;

        public PenaltyCaptureService(INpgsqlDataAccess dataAccess, IOperatorAccessService access) : base(dataAccess)
        {
            this.access = access;
        }

        public async Task<PagedResponse<PenaltyCaptureResponse>> SelectPagedCaptures(PenaltyCaptureListRequest request, Caller caller)
        {
            string sortColumn = SORTABLE_COLUMNS.TryGetValue(request.SortBy ?? string.Empty, out string? mapped) ? mapped : "t.scheduled_date";
            string sortDirection = request.SortDescending ? "DESC" : "ASC";

            string sql = $@"SELECT p.id, p.payment_certificate_id, p.route_id, r.route_number, p.trip_id, t.trip_number,
                                   t.scheduled_date AS trip_date, p.penalty_list_item_id, p.penalty_code,
                                   pli.description AS penalty_description, pli.penalty_type, pli.rand_amount_minor,
                                   p.vehicle_id, v.vehicle_number, p.trip_detail, p.captured_by_role,
                                   a.full_name AS captured_by_name, p.created_by AS captured_by_id,

                                   decider.full_name AS status_updated_by_name, p.status_updated_at,
                                   p.capture_status, p.status_reason, p.effective_rand_amount_minor, p.repeat_escalation_applied,
                                   (p.created_by = @Actor) AS is_mine,
                                   count(*) OVER() AS total_count
                            FROM penalty_captures p
                            JOIN routes r ON r.id = p.route_id
                            JOIN trips t ON t.id = p.trip_id
                            JOIN penalty_list_items pli ON pli.id = p.penalty_list_item_id
                            LEFT JOIN vehicles v ON v.id = p.vehicle_id
                            LEFT JOIN accounts a ON a.id = p.created_by

                            LEFT JOIN accounts decider ON decider.id = p.status_updated_by
                            WHERE p.is_active
                              AND (@PaymentCertificateId IS NULL OR p.payment_certificate_id = @PaymentCertificateId)
                              AND (@TripId IS NULL OR p.trip_id = @TripId)

                              AND (@ContractId IS NULL OR r.contract_id = @ContractId)

                              AND (@CapturedByRole IS NULL OR p.captured_by_role = @CapturedByRole)
                              AND (@CaptureStatus IS NULL OR p.capture_status = @CaptureStatus)
                              AND (@Search IS NULL OR p.penalty_code ILIKE @SearchPattern ESCAPE '\' OR pli.description ILIKE @SearchPattern ESCAPE '\')
                              {OperatorScope.ForContractColumn("r.contract_id")}
                            ORDER BY {sortColumn} {sortDirection}, p.penalty_code
                            LIMIT @PageSize OFFSET @Offset";

            DynamicParameters parameters = new();
            OperatorScope.AddParameters(parameters, caller);
            parameters.Add("PaymentCertificateId", request.PaymentCertificateId, DbType.Guid);
            parameters.Add("TripId", request.TripId, DbType.Guid);

            parameters.Add("ContractId", request.ContractId, DbType.Guid);

            parameters.Add("CapturedByRole", request.CapturedByRole, DbType.String);
            parameters.Add("CaptureStatus", request.CaptureStatus, DbType.String);
            parameters.Add("Search", request.Search, DbType.String);
            parameters.Add("SearchPattern", SearchPattern.Build(request.Search), DbType.String);
            parameters.Add("PageSize", request.PageSize, DbType.Int32);
            parameters.Add("Offset", (request.Page - 1) * request.PageSize, DbType.Int32);

            List<PenaltyCaptureRowModel> rows = await _dataAccess.QueryAsync<PenaltyCaptureRowModel>(sql, parameters);

            long itemCount = rows.Count > 0 ? rows[0].TotalCount : 0;

            return new PagedResponse<PenaltyCaptureResponse>
            {
                Page = request.Page,
                PageSize = request.PageSize,
                ItemCount = itemCount,
                PageCount = (int)Math.Ceiling(itemCount / (double)request.PageSize),
                Data = rows.Select(row => new PenaltyCaptureResponse
                {
                    Id = row.Id,
                    PaymentCertificateId = row.PaymentCertificateId,
                    RouteId = row.RouteId,
                    RouteNumber = row.RouteNumber,
                    TripId = row.TripId,
                    TripNumber = row.TripNumber,
                    TripDate = row.TripDate,
                    PenaltyListItemId = row.PenaltyListItemId,
                    PenaltyCode = row.PenaltyCode,
                    PenaltyDescription = row.PenaltyDescription,
                    PenaltyType = row.PenaltyType,
                    RandAmount = row.RandAmountMinor is null ? null : Money.ToRands(row.RandAmountMinor.Value),
                    VehicleId = row.VehicleId,
                    VehicleNumber = row.VehicleNumber,
                    TripDetail = row.TripDetail,
                    CapturedByRole = row.CapturedByRole,
                    CapturedByName = row.CapturedByName ?? string.Empty,
                    CapturedById = row.CapturedById,
                    CaptureStatus = row.CaptureStatus,
                    StatusReason = row.StatusReason,

                    EffectiveRandAmount = row.EffectiveRandAmountMinor is null ? null : Money.ToRands(row.EffectiveRandAmountMinor.Value),

                    IsRepeatEscalationApplied = row.RepeatEscalationApplied,
                    IsMine = row.IsMine,

                    StatusUpdatedByName = row.StatusUpdatedByName,

                    StatusUpdatedAt = row.StatusUpdatedAt
                }).ToList()
            };
        }

        public async Task<CreateResult> CreateCapture(CreatePenaltyCaptureRequest request, Caller caller, string capturedByRole)
        {
            OperationResult allowed = await access.CheckTrip(caller, request.TripId);

            if (!allowed.IsSuccess)
            {
                return CreateResult.From(allowed);
            }

            Guid actor = caller.AccountId;
            PenaltyItemModel? item = await SelectPenaltyItem(request.PenaltyListItemId);

            if (item is null)
            {
                return CreateResult.Failure("That penalty is not on the active penalty list.");
            }

            OperationResult trip = await ValidateTrip(request.RouteId, request.TripId, request.PaymentCertificateId, item.ContractId);

            if (!trip.IsSuccess)
            {
                return CreateResult.Failure(trip.Error!);
            }

            OperationResult vehicle = await ValidateVehicle(item, request.VehicleId, item.ContractId);

            if (!vehicle.IsSuccess)
            {
                return CreateResult.Failure(vehicle.Error!);
            }

            const string sql = @"INSERT INTO penalty_captures
                                     (payment_certificate_id, captured_by_role, route_id, trip_id, trip_detail, vehicle_id,
                                      penalty_list_item_id, penalty_code, capture_status, created_by)
                                 VALUES (@PaymentCertificateId, @CapturedByRole, @RouteId, @TripId, @TripDetail, @VehicleId,
                                         @PenaltyListItemId, @PenaltyCode, @Status, @Actor)
                                 RETURNING id";

            Guid id = await _dataAccess.QuerySingleOrDefaultAsync<Guid>(sql, new
            {
                request.PaymentCertificateId,
                CapturedByRole = capturedByRole,
                request.RouteId,
                request.TripId,
                TripDetail = Clean(request.TripDetail),
                request.VehicleId,
                request.PenaltyListItemId,
                PenaltyCode = item.PenaltyCode,
                Status = SUBMITTED_STATUS,
                Actor = actor
            });

            await WriteAudit(id, "Create", null, actor);

            return CreateResult.Success(id);
        }

        public async Task<OperationResult> UpdateCapture(UpdatePenaltyCaptureRequest request, Caller caller)
        {
            OperationResult allowed = await access.CheckPenaltyCapture(caller, request.Id);

            if (!allowed.IsSuccess)
            {
                return allowed;
            }

            OperationResult targetAllowed = await access.CheckTrip(caller, request.TripId);

            if (!targetAllowed.IsSuccess)
            {
                return targetAllowed;
            }

            Guid actor = caller.AccountId;
            OperationResult owned = await CheckOwnership(request.Id, actor);

            if (!owned.IsSuccess)
            {
                return owned;
            }

            PenaltyItemModel? item = await SelectPenaltyItem(request.PenaltyListItemId);

            if (item is null)
            {
                return OperationResult.Failure("That penalty is not on the active penalty list.");
            }

            const string certificateSql = @"SELECT payment_certificate_id FROM penalty_captures WHERE id = @Id AND is_active";
            Guid? certificateId = await _dataAccess.QuerySingleOrDefaultAsync<Guid?>(certificateSql, new { request.Id });

            OperationResult trip = await ValidateTrip(request.RouteId, request.TripId, certificateId, item.ContractId);

            if (!trip.IsSuccess)
            {
                return trip;
            }

            OperationResult vehicle = await ValidateVehicle(item, request.VehicleId, item.ContractId);

            if (!vehicle.IsSuccess)
            {
                return vehicle;
            }

            const string sql = @"UPDATE penalty_captures
                                 SET route_id = @RouteId, trip_id = @TripId, trip_detail = @TripDetail,
                                     vehicle_id = @VehicleId, penalty_list_item_id = @PenaltyListItemId,
                                     penalty_code = @PenaltyCode, modified_at = now(), modified_by = @Actor
                                 WHERE id = @Id AND is_active AND capture_status IN (@Submitted, @Disputed)";

            await _dataAccess.ExecuteAsync(sql, new
            {
                request.Id,
                request.RouteId,
                request.TripId,
                TripDetail = Clean(request.TripDetail),
                request.VehicleId,
                request.PenaltyListItemId,
                PenaltyCode = item.PenaltyCode,
                Submitted = SUBMITTED_STATUS,
                Disputed = DISPUTED_STATUS,
                Actor = actor
            });

            await WriteAudit(request.Id, "Update", request.Reason.Trim(), actor);

            return OperationResult.Success();
        }

        public async Task<OperationResult> DeleteCapture(DeletePenaltyCaptureRequest request, Caller caller)
        {
            OperationResult allowed = await access.CheckPenaltyCapture(caller, request.Id);

            if (!allowed.IsSuccess)
            {
                return allowed;
            }

            Guid actor = caller.AccountId;
            OperationResult owned = await CheckOwnership(request.Id, actor);

            if (!owned.IsSuccess)
            {
                return owned;
            }

            await WriteAudit(request.Id, "Delete", request.Reason.Trim(), actor);

            const string sql = @"UPDATE penalty_captures
                                 SET is_active = FALSE, modified_at = now(), modified_by = @Actor
                                 WHERE id = @Id AND is_active";

            await _dataAccess.ExecuteAsync(sql, new { request.Id, Actor = actor });

            return OperationResult.Success();
        }

        public async Task<LookupResult<List<DropDownResponse>>> SelectContractRoutes(Guid contractId, Caller caller)
        {
            OperationResult allowed = await access.CheckContract(caller, contractId);

            if (!allowed.IsSuccess)
            {
                return LookupResult<List<DropDownResponse>>.From(allowed);
            }

            const string sql = @"SELECT id, route_number || ' ' || direction AS value
                                 FROM routes
                                 WHERE contract_id = @ContractId AND is_active
                                 ORDER BY route_number, direction";

            return LookupResult<List<DropDownResponse>>.Success(await _dataAccess.QueryAsync<DropDownResponse>(sql, new { ContractId = contractId }));
        }

        public async Task<LookupResult<List<TripOptionResponse>>> SelectRouteTrips(TripLookupRequest request, Caller caller)
        {
            OperationResult allowed = await access.CheckRoute(caller, request.RouteId);

            if (!allowed.IsSuccess)
            {
                return LookupResult<List<TripOptionResponse>>.From(allowed);
            }

            const string sql = @"SELECT t.id, t.trip_number, t.scheduled_date, t.scheduled_departure,
                                        r.route_number, r.direction
                                 FROM trips t
                                 JOIN routes r ON r.id = t.route_id
                                 LEFT JOIN payment_certificates pc ON pc.id = @PaymentCertificateId AND pc.contract_id = r.contract_id AND pc.is_active
                                 WHERE t.route_id = @RouteId AND t.is_active
                                   AND (@TripDate IS NULL OR t.scheduled_date = @TripDate)
                                   AND (pc.id IS NULL OR (t.scheduled_date >= pc.certificate_period
                                                          AND t.scheduled_date < (pc.certificate_period + INTERVAL '1 month')::date))
                                 ORDER BY t.scheduled_date, t.scheduled_departure, t.trip_number
                                 LIMIT 500";

            DynamicParameters parameters = new();
            parameters.Add("RouteId", request.RouteId, DbType.Guid);
            parameters.Add("PaymentCertificateId", request.PaymentCertificateId, DbType.Guid);
            parameters.Add("TripDate", request.TripDate, DbType.Date);

            return LookupResult<List<TripOptionResponse>>.Success(await _dataAccess.QueryAsync<TripOptionResponse>(sql, parameters));
        }

        public async Task<LookupResult<List<DropDownResponse>>> SelectContractVehicles(Guid contractId, Caller caller)
        {
            OperationResult allowed = await access.CheckContract(caller, contractId);

            if (!allowed.IsSuccess)
            {
                return LookupResult<List<DropDownResponse>>.From(allowed);
            }

            const string sql = @"SELECT v.id, v.vehicle_number || ' (' || v.vehicle_type || ')' AS value
                                 FROM vehicles v
                                 JOIN contracts c ON c.operator_id = v.operator_id
                                 WHERE c.id = @ContractId AND v.is_active
                                 ORDER BY v.vehicle_number";

            return LookupResult<List<DropDownResponse>>.Success(await _dataAccess.QueryAsync<DropDownResponse>(sql, new { ContractId = contractId }));
        }

        public async Task<LookupResult<List<DropDownResponse>>> SelectContractPenaltyItems(Guid contractId, string? penaltyType, Caller caller)
        {
            OperationResult allowed = await access.CheckContract(caller, contractId);

            if (!allowed.IsSuccess)
            {
                return LookupResult<List<DropDownResponse>>.From(allowed);
            }

            const string sql = @"SELECT id, penalty_code || ' - ' || description AS value
                                 FROM penalty_list_items
                                 WHERE contract_id = @ContractId AND is_active
                                   AND (@PenaltyType IS NULL OR penalty_type = @PenaltyType)
                                 ORDER BY penalty_code";

            DynamicParameters parameters = new();
            parameters.Add("ContractId", contractId, DbType.Guid);
            parameters.Add("PenaltyType", penaltyType, DbType.String);

            return LookupResult<List<DropDownResponse>>.Success(await _dataAccess.QueryAsync<DropDownResponse>(sql, parameters));
        }

        private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private async Task<OperationResult> CheckOwnership(Guid captureId, Guid actor)
        {
            const string sql = @"SELECT created_by AS captured_by_id, capture_status
                                 FROM penalty_captures
                                 WHERE id = @CaptureId AND is_active";
            PenaltyCaptureRowModel? capture = await _dataAccess.QuerySingleOrDefaultAsync<PenaltyCaptureRowModel>(sql, new { CaptureId = captureId });

            if (capture is null)
            {
                return OperationResult.Failure("No active penalty capture was found with that id.");
            }

            if (capture.CapturedById != actor)
            {
                return OperationResult.Failure("Only the person who captured a penalty may change it.");
            }

            if (capture.CaptureStatus is not (SUBMITTED_STATUS or DISPUTED_STATUS))
            {
                return OperationResult.Failure($"A {capture.CaptureStatus.ToLowerInvariant()} penalty can no longer be changed.");
            }

            return OperationResult.Success();
        }

        private async Task<PenaltyItemModel?> SelectPenaltyItem(Guid penaltyListItemId)
        {
            const string sql = @"SELECT id, contract_id, penalty_code, penalty_type
                                 FROM penalty_list_items
                                 WHERE id = @PenaltyListItemId AND is_active";

            return await _dataAccess.QuerySingleOrDefaultAsync<PenaltyItemModel>(sql, new { PenaltyListItemId = penaltyListItemId });
        }

        private async Task<OperationResult> ValidateVehicle(PenaltyItemModel item, Guid? vehicleId, Guid contractId)
        {
            if (!VEHICLE_SCOPED_TYPES.Contains(item.PenaltyType))
            {
                return OperationResult.Success();
            }

            if (vehicleId is null)
            {
                return OperationResult.Failure($"A {item.PenaltyType.ToLowerInvariant()} penalty needs the vehicle it applies to.");
            }

            const string sql = @"SELECT count(*)
                                 FROM vehicles v
                                 JOIN contracts c ON c.operator_id = v.operator_id
                                 WHERE v.id = @VehicleId AND v.is_active AND c.id = @ContractId";
            long count = await _dataAccess.QuerySingleOrDefaultAsync<long>(sql, new { VehicleId = vehicleId, ContractId = contractId });

            return count == 0
                ? OperationResult.Failure("That vehicle does not belong to the contract's operator.")
                : OperationResult.Success();
        }

        private async Task<OperationResult> ValidateTrip(Guid routeId, Guid tripId, Guid? certificateId, Guid itemContractId)
        {
            const string sql = @"SELECT r.contract_id, t.scheduled_date
                                 FROM trips t
                                 JOIN routes r ON r.id = t.route_id AND r.is_active
                                 WHERE t.id = @TripId AND t.route_id = @RouteId AND t.is_active";
            TripModel? trip = await _dataAccess.QuerySingleOrDefaultAsync<TripModel>(sql, new { TripId = tripId, RouteId = routeId });

            if (trip is null)
            {
                return OperationResult.Failure("That trip does not belong to the chosen route.");
            }

            if (trip.ContractId != itemContractId)
            {
                return OperationResult.Failure("That penalty belongs to a different contract than the trip.");
            }

            if (certificateId is null)
            {
                return OperationResult.Success();
            }

            const string certificateSql = @"SELECT contract_id, certificate_period, is_locked
                                            FROM payment_certificates
                                            WHERE id = @CertificateId AND is_active";
            CertificateModel? certificate = await _dataAccess.QuerySingleOrDefaultAsync<CertificateModel>(certificateSql, new { CertificateId = certificateId });

            if (certificate is null)
            {
                return OperationResult.Failure("No active certificate was found with that id.");
            }

            if (certificate.IsLocked)
            {
                return OperationResult.Failure("That certificate is approved and locked.");
            }

            if (certificate.ContractId != trip.ContractId)
            {
                return OperationResult.Failure("That trip belongs to a different contract than the certificate.");
            }

            if (trip.ScheduledDate < certificate.CertificatePeriod || trip.ScheduledDate >= certificate.CertificatePeriod.AddMonths(1))
            {
                return OperationResult.Failure($"That trip is not in the certificate's month of {certificate.CertificatePeriod:MMMM yyyy}.");
            }

            return OperationResult.Success();
        }

        private async Task WriteAudit(Guid captureId, string action, string? reason, Guid actor)
        {
            const string sql = @"INSERT INTO audit_logs (table_name, record_id, action_name, action_reason, changed_by, new_values)
                                 SELECT 'penalty_captures', @CaptureId, @Action, @Reason, @Actor, to_jsonb(p)
                                 FROM penalty_captures p
                                 WHERE p.id = @CaptureId";

            await _dataAccess.ExecuteAsync(sql, new { CaptureId = captureId, Action = action, Reason = reason, Actor = actor });
        }

        private sealed class PenaltyItemModel
        {
            public Guid Id { get; set; }
            public Guid ContractId { get; set; }
            public string PenaltyCode { get; set; } = string.Empty;
            public string PenaltyType { get; set; } = string.Empty;
        }

        private sealed class TripModel
        {
            public Guid ContractId { get; set; }
            public DateOnly ScheduledDate { get; set; }
        }

        private sealed class CertificateModel
        {
            public Guid ContractId { get; set; }
            public DateOnly CertificatePeriod { get; set; }
            public bool IsLocked { get; set; }
        }

        private sealed class PenaltyCaptureRowModel
        {
            public Guid Id { get; set; }
            public Guid? PaymentCertificateId { get; set; }
            public Guid RouteId { get; set; }
            public string RouteNumber { get; set; } = string.Empty;
            public Guid TripId { get; set; }
            public string TripNumber { get; set; } = string.Empty;
            public DateOnly TripDate { get; set; }
            public Guid PenaltyListItemId { get; set; }
            public string PenaltyCode { get; set; } = string.Empty;
            public string PenaltyDescription { get; set; } = string.Empty;
            public string PenaltyType { get; set; } = string.Empty;
            public long? RandAmountMinor { get; set; }
            public Guid? VehicleId { get; set; }
            public string? VehicleNumber { get; set; }
            public string? TripDetail { get; set; }
            public string CapturedByRole { get; set; } = string.Empty;
            public string? CapturedByName { get; set; }
            public Guid CapturedById { get; set; }
            public string CaptureStatus { get; set; } = string.Empty;
            public string? StatusReason { get; set; }

            public long? EffectiveRandAmountMinor { get; set; }

            public bool RepeatEscalationApplied { get; set; }
            public bool IsMine { get; set; }

            public string? StatusUpdatedByName { get; set; }

            public DateTime? StatusUpdatedAt { get; set; }
            public long TotalCount { get; set; }
        }
    }
}
