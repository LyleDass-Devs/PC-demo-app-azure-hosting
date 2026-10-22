using Dapper;
using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.FormF1.Requests;
using KubaPC.Models.Contracts.FormF1.Responses;
using KubaPC.Models.Internal;
using KubaPC.Services.Data;
using KubaPC.Services.Services.Interfaces;
using System.Data;

namespace KubaPC.Services.Services.Logic
{
    public class FormF1Service : BaseService, IFormF1Service
    {
        private const string SUBMITTED_STATUS = "Submitted";
        private const string DISPUTED_STATUS = "Under Dispute";
        private const string STANDING_TRIP_TYPE = "Standing Trip";

        private static readonly Dictionary<string, string> SORTABLE_COLUMNS = new(StringComparer.OrdinalIgnoreCase)
        {
            ["tripDate"] = "t.scheduled_date",
            ["recordType"] = "f.record_type",
            ["recordStatus"] = "f.record_status",
            ["capturedByRole"] = "f.captured_by_role"
        };

        public FormF1Service(INpgsqlDataAccess dataAccess) : base(dataAccess)
        {
        }

        public async Task<PagedResponse<FormF1RecordResponse>> SelectPaged(FormF1ListRequest request, Guid actor)
        {
            string sortColumn = SORTABLE_COLUMNS.TryGetValue(request.SortBy ?? string.Empty, out string? mapped) ? mapped : "t.scheduled_date";
            string sortDirection = request.SortDescending ? "DESC" : "ASC";

            string sql = $@"SELECT f.id, f.payment_certificate_id, f.route_id, r.route_number, f.trip_id, t.trip_number,
                                   t.scheduled_date AS trip_date, f.record_type, f.reason,
                                   f.dno_reason_id, d.reason_value AS dno_reason_value, f.dno_explanation,
                                   f.captured_by_role, a.full_name AS captured_by_name, f.created_by AS captured_by_id,
                                   f.record_status, f.status_reason,
                                   decider.full_name AS status_updated_by_name, f.status_updated_at,
                                   (f.created_by = @Actor) AS is_mine,
                                   count(*) OVER() AS total_count
                            FROM form_f1_records f
                            JOIN routes r ON r.id = f.route_id
                            JOIN trips t ON t.id = f.trip_id
                            LEFT JOIN dno_reasons d ON d.id = f.dno_reason_id
                            LEFT JOIN accounts a ON a.id = f.created_by
                            LEFT JOIN accounts decider ON decider.id = f.status_updated_by
                            WHERE f.is_active
                              AND (@PaymentCertificateId IS NULL OR f.payment_certificate_id = @PaymentCertificateId)
                              AND (@TripId IS NULL OR f.trip_id = @TripId)
                              AND (@ContractId IS NULL OR r.contract_id = @ContractId)
                              AND (@RecordType IS NULL OR f.record_type = @RecordType)
                              AND (@RecordStatus IS NULL OR f.record_status = @RecordStatus)
                              AND (@CapturedByRole IS NULL OR f.captured_by_role = @CapturedByRole)
                              AND (@Search IS NULL OR f.reason ILIKE @SearchPattern OR f.dno_explanation ILIKE @SearchPattern)
                            ORDER BY {sortColumn} {sortDirection}, t.trip_number, f.record_type
                            LIMIT @PageSize OFFSET @Offset";

            DynamicParameters parameters = new();
            parameters.Add("Actor", actor, DbType.Guid);
            parameters.Add("PaymentCertificateId", request.PaymentCertificateId, DbType.Guid);
            parameters.Add("TripId", request.TripId, DbType.Guid);
            parameters.Add("ContractId", request.ContractId, DbType.Guid);
            parameters.Add("RecordType", request.RecordType, DbType.String);
            parameters.Add("RecordStatus", request.RecordStatus, DbType.String);
            parameters.Add("CapturedByRole", request.CapturedByRole, DbType.String);
            parameters.Add("Search", request.Search, DbType.String);
            parameters.Add("SearchPattern", $"%{request.Search}%", DbType.String);
            parameters.Add("PageSize", request.PageSize, DbType.Int32);
            parameters.Add("Offset", (request.Page - 1) * request.PageSize, DbType.Int32);

            List<FormF1RowModel> rows = await _dataAccess.QueryAsync<FormF1RowModel>(sql, parameters);

            long itemCount = rows.Count > 0 ? rows[0].TotalCount : 0;

            return new PagedResponse<FormF1RecordResponse>
            {
                Page = request.Page,
                PageSize = request.PageSize,
                ItemCount = itemCount,
                PageCount = (int)Math.Ceiling(itemCount / (double)request.PageSize),
                Data = rows.Select(row => new FormF1RecordResponse
                {
                    Id = row.Id,
                    PaymentCertificateId = row.PaymentCertificateId,
                    RouteId = row.RouteId,
                    RouteNumber = row.RouteNumber,
                    TripId = row.TripId,
                    TripNumber = row.TripNumber,
                    TripDate = row.TripDate,
                    RecordType = row.RecordType,
                    Reason = row.Reason,
                    DnoReasonId = row.DnoReasonId,
                    DnoReasonValue = row.DnoReasonValue,
                    DnoExplanation = row.DnoExplanation,
                    CapturedByRole = row.CapturedByRole,
                    CapturedByName = row.CapturedByName ?? string.Empty,
                    CapturedById = row.CapturedById,
                    RecordStatus = row.RecordStatus,
                    StatusReason = row.StatusReason,
                    StatusUpdatedByName = row.StatusUpdatedByName,
                    StatusUpdatedAt = row.StatusUpdatedAt,
                    IsMine = row.IsMine
                }).ToList()
            };
        }

        public async Task<FormF1LookupsResponse> SelectLookups()
        {
            const string typesSql = @"SELECT type_value FROM form_f1_record_types ORDER BY display_order";
            const string reasonsSql = @"SELECT id, reason_value AS value FROM dno_reasons WHERE is_active ORDER BY reason_value";

            return new FormF1LookupsResponse
            {
                RecordTypes = await _dataAccess.QueryAsync<string>(typesSql, new { }),
                DnoReasons = await _dataAccess.QueryAsync<DropDownResponse>(reasonsSql, new { })
            };
        }

        public async Task<CreateResult> Create(CreateFormF1RecordRequest request, Guid actor, string capturedByRole)
        {
            OperationResult shape = await ValidateRecord(request.RecordType, request.DnoReasonId, request.DnoExplanation, request.RouteId, request.TripId, request.PaymentCertificateId);

            if (!shape.IsSuccess)
            {
                return CreateResult.Failure(shape.Error!);
            }

            const string sql = @"INSERT INTO form_f1_records
                                     (payment_certificate_id, route_id, trip_id, record_type, reason,
                                      dno_reason_id, dno_explanation, captured_by_role, record_status, created_by)
                                 VALUES (@PaymentCertificateId, @RouteId, @TripId, @RecordType, @Reason,
                                         @DnoReasonId, @DnoExplanation, @CapturedByRole, @Status, @Actor)
                                 RETURNING id";

            Guid id = await _dataAccess.QuerySingleOrDefaultAsync<Guid>(sql, new
            {
                request.PaymentCertificateId,
                request.RouteId,
                request.TripId,
                request.RecordType,
                Reason = request.Reason.Trim(),
                DnoReasonId = request.RecordType == STANDING_TRIP_TYPE ? request.DnoReasonId : null,
                DnoExplanation = request.RecordType == STANDING_TRIP_TYPE ? Clean(request.DnoExplanation) : null,
                CapturedByRole = capturedByRole,
                Status = SUBMITTED_STATUS,
                Actor = actor
            });

            await WriteAudit(id, "Create", null, actor);

            return CreateResult.Success(id);
        }

        public async Task<OperationResult> Update(UpdateFormF1RecordRequest request, Guid actor)
        {
            OperationResult owned = await CheckOwnership(request.Id, actor);

            if (!owned.IsSuccess)
            {
                return owned;
            }

            const string certificateSql = @"SELECT payment_certificate_id FROM form_f1_records WHERE id = @Id AND is_active";
            Guid? certificateId = await _dataAccess.QuerySingleOrDefaultAsync<Guid?>(certificateSql, new { request.Id });

            OperationResult shape = await ValidateRecord(request.RecordType, request.DnoReasonId, request.DnoExplanation, request.RouteId, request.TripId, certificateId);

            if (!shape.IsSuccess)
            {
                return shape;
            }

            const string sql = @"UPDATE form_f1_records
                                 SET route_id = @RouteId, trip_id = @TripId, record_type = @RecordType,
                                     reason = @Reason, dno_reason_id = @DnoReasonId, dno_explanation = @DnoExplanation,
                                     modified_at = now(), modified_by = @Actor
                                 WHERE id = @Id AND is_active AND record_status IN (@Submitted, @Disputed)";

            await _dataAccess.ExecuteAsync(sql, new
            {
                request.Id,
                request.RouteId,
                request.TripId,
                request.RecordType,
                Reason = request.Reason.Trim(),
                DnoReasonId = request.RecordType == STANDING_TRIP_TYPE ? request.DnoReasonId : null,
                DnoExplanation = request.RecordType == STANDING_TRIP_TYPE ? Clean(request.DnoExplanation) : null,
                Submitted = SUBMITTED_STATUS,
                Disputed = DISPUTED_STATUS,
                Actor = actor
            });

            await WriteAudit(request.Id, "Update", request.ChangeReason.Trim(), actor);

            return OperationResult.Success();
        }

        public async Task<OperationResult> Delete(DeleteFormF1RecordRequest request, Guid actor)
        {
            OperationResult owned = await CheckOwnership(request.Id, actor);

            if (!owned.IsSuccess)
            {
                return owned;
            }

            await WriteAudit(request.Id, "Delete", request.Reason.Trim(), actor);

            const string sql = @"UPDATE form_f1_records
                                 SET is_active = FALSE, modified_at = now(), modified_by = @Actor
                                 WHERE id = @Id AND is_active";

            await _dataAccess.ExecuteAsync(sql, new { request.Id, Actor = actor });

            return OperationResult.Success();
        }

        private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private async Task<OperationResult> ValidateRecord(string recordType, Guid? dnoReasonId, string? dnoExplanation, Guid routeId, Guid tripId, Guid? certificateId)
        {
            const string typeSql = @"SELECT count(*) FROM form_f1_record_types WHERE type_value = @RecordType";
            long typeCount = await _dataAccess.QuerySingleOrDefaultAsync<long>(typeSql, new { RecordType = recordType });

            if (typeCount == 0)
            {
                return OperationResult.Failure("Choose a record type from the list.");
            }

            if (recordType == STANDING_TRIP_TYPE)
            {
                if (dnoReasonId is null)
                {
                    return OperationResult.Failure("A standing trip conversion needs the reason the tracking system's did-not-operate finding is being overridden.");
                }

                if (string.IsNullOrWhiteSpace(dnoExplanation))
                {
                    return OperationResult.Failure("A standing trip conversion needs an explanation alongside the reason.");
                }

                const string reasonSql = @"SELECT count(*) FROM dno_reasons WHERE id = @DnoReasonId AND is_active";
                long reasonCount = await _dataAccess.QuerySingleOrDefaultAsync<long>(reasonSql, new { DnoReasonId = dnoReasonId });

                if (reasonCount == 0)
                {
                    return OperationResult.Failure("That reason is not on the active list.");
                }
            }

            return await ValidateTrip(routeId, tripId, certificateId);
        }

        private async Task<OperationResult> ValidateTrip(Guid routeId, Guid tripId, Guid? certificateId)
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

        private async Task<OperationResult> CheckOwnership(Guid recordId, Guid actor)
        {
            const string sql = @"SELECT created_by AS captured_by_id, record_status
                                 FROM form_f1_records
                                 WHERE id = @RecordId AND is_active";
            FormF1RowModel? record = await _dataAccess.QuerySingleOrDefaultAsync<FormF1RowModel>(sql, new { RecordId = recordId });

            if (record is null)
            {
                return OperationResult.Failure("No active Form F1 record was found with that id.");
            }

            if (record.CapturedById != actor)
            {
                return OperationResult.Failure("Only the person who captured a record may change it.");
            }

            if (record.RecordStatus is not (SUBMITTED_STATUS or DISPUTED_STATUS))
            {
                return OperationResult.Failure($"A {record.RecordStatus.ToLowerInvariant()} record can no longer be changed.");
            }

            return OperationResult.Success();
        }

        private async Task WriteAudit(Guid recordId, string action, string? reason, Guid actor)
        {
            const string sql = @"INSERT INTO audit_logs (table_name, record_id, action_name, action_reason, changed_by, new_values)
                                 SELECT 'form_f1_records', @RecordId, @Action, @Reason, @Actor, to_jsonb(f)
                                 FROM form_f1_records f
                                 WHERE f.id = @RecordId";

            await _dataAccess.ExecuteAsync(sql, new { RecordId = recordId, Action = action, Reason = reason, Actor = actor });
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

        private sealed class FormF1RowModel
        {
            public Guid Id { get; set; }
            public Guid? PaymentCertificateId { get; set; }
            public Guid RouteId { get; set; }
            public string RouteNumber { get; set; } = string.Empty;
            public Guid TripId { get; set; }
            public string TripNumber { get; set; } = string.Empty;
            public DateOnly TripDate { get; set; }
            public string RecordType { get; set; } = string.Empty;
            public string Reason { get; set; } = string.Empty;
            public Guid? DnoReasonId { get; set; }
            public string? DnoReasonValue { get; set; }
            public string? DnoExplanation { get; set; }
            public string CapturedByRole { get; set; } = string.Empty;
            public string? CapturedByName { get; set; }
            public Guid CapturedById { get; set; }
            public string RecordStatus { get; set; } = string.Empty;
            public string? StatusReason { get; set; }
            public string? StatusUpdatedByName { get; set; }
            public DateTime? StatusUpdatedAt { get; set; }
            public bool IsMine { get; set; }
            public long TotalCount { get; set; }
        }
    }
}
