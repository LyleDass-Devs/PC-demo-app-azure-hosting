using Dapper;
using KubaPC.Models.Contracts.FormF1.Requests;
using KubaPC.Models.Contracts.FormF1.Responses;
using KubaPC.Models.Internal;
using KubaPC.Services.Data;
using KubaPC.Services.Services.Interfaces;
using System.Data;

namespace KubaPC.Services.Services.Logic
{
    public class FormF1ReviewService : BaseService, IFormF1ReviewService
    {
        private const string SUBMITTED_STATUS = "Submitted";
        private const string APPROVED_STATUS = "Approved";
        private const string REJECTED_STATUS = "Rejected";
        private const string DISPUTED_STATUS = "Under Dispute";

        private readonly IOperatorAccessService access;

        public FormF1ReviewService(INpgsqlDataAccess dataAccess, IOperatorAccessService access) : base(dataAccess)
        {
            this.access = access;
        }

        public async Task<LookupResult<MatchingFormF1Response>> SelectMatchingUndecided(Guid recordId, Caller caller)
        {
            OperationResult allowed = await access.CheckFormF1Record(caller, recordId);

            if (!allowed.IsSuccess)
            {
                return LookupResult<MatchingFormF1Response>.From(allowed);
            }

            return LookupResult<MatchingFormF1Response>.Success(await SelectMatching(recordId));
        }

        private async Task<MatchingFormF1Response> SelectMatching(Guid recordId)
        {
            const string sql = @"SELECT other.id, other.captured_by_role, a.full_name AS captured_by_name, other.record_status,
                                        other.record_type, other.created_at AS captured_at,
                                        (other.record_status = @Submitted) AS is_undecided
                                 FROM form_f1_records self
                                 JOIN form_f1_records other
                                      ON other.trip_id = self.trip_id
                                     AND other.record_type = self.record_type
                                     AND other.captured_by_role <> self.captured_by_role
                                     AND other.is_active
                                 LEFT JOIN accounts a ON a.id = other.created_by
                                 WHERE self.id = @RecordId AND self.is_active
                                 ORDER BY (other.record_status = @Submitted) DESC, other.created_at DESC
                                 LIMIT 1";

            MatchingFormF1Response? matching = await _dataAccess.QuerySingleOrDefaultAsync<MatchingFormF1Response>(
                sql, new { RecordId = recordId, Submitted = SUBMITTED_STATUS });

            return matching ?? new MatchingFormF1Response();
        }

        public async Task<OperationResult> ReviewRecord(ReviewFormF1RecordRequest request, Caller caller)
        {
            OperationResult allowed = await access.CheckFormF1Record(caller, request.Id);

            if (!allowed.IsSuccess)
            {
                return allowed;
            }

            Guid actor = caller.AccountId;
            if (request.RecordStatus is not (APPROVED_STATUS or REJECTED_STATUS or DISPUTED_STATUS))
            {
                return OperationResult.Failure("A Form F1 record can only be set to Approved, Rejected or Under Dispute.");
            }

            if (request.RecordStatus != APPROVED_STATUS && string.IsNullOrWhiteSpace(request.StatusReason))
            {
                return OperationResult.Failure($"A {request.RecordStatus.ToLowerInvariant()} decision needs a reason.");
            }

            RecordModel? record = await SelectRecord(request.Id);

            if (record is null)
            {
                return OperationResult.Failure("No active Form F1 record was found with that id.");
            }

            if (record.IsCertificateLocked)
            {
                return OperationResult.Failure("The certificate this record belongs to is approved, so the record is locked.");
            }

            List<Guid> decided = new() { record.Id };

            if (request.CascadeToMatching)
            {
                MatchingFormF1Response matching = await SelectMatching(record.Id);

                if (matching.Id is Guid matchingId && matching.IsUndecided)
                {
                    decided.Add(matchingId);
                }
            }

            foreach (Guid recordId in decided)
            {
                await ApplyDecision(recordId, request, actor);
            }

            await WriteOutcome(record, request, decided, actor);

            return OperationResult.Success();
        }

        private async Task ApplyDecision(Guid recordId, ReviewFormF1RecordRequest request, Guid actor)
        {
            const string sql = @"UPDATE form_f1_records
                                 SET record_status = @Status,
                                     status_reason = @Reason,
                                     status_updated_by = @Actor,
                                     status_updated_at = now(),
                                     modified_at = now(),
                                     modified_by = @Actor
                                 WHERE id = @RecordId AND is_active";

            await _dataAccess.ExecuteAsync(sql, new
            {
                RecordId = recordId,
                Status = request.RecordStatus,
                Reason = string.IsNullOrWhiteSpace(request.StatusReason) ? null : request.StatusReason.Trim(),
                Actor = actor
            });

            await WriteAudit(recordId, request.RecordStatus, request.StatusReason, actor);
        }

        private async Task WriteOutcome(RecordModel record, ReviewFormF1RecordRequest request, List<Guid> decided, Guid actor)
        {
            if (request.RecordStatus == DISPUTED_STATUS)
            {
                const string clearSql = @"UPDATE form_f1_outcomes
                                          SET is_active = FALSE, modified_at = now(), modified_by = @Actor
                                          WHERE trip_id = @TripId AND record_type = @RecordType AND is_active";

                await _dataAccess.ExecuteAsync(clearSql, new { record.TripId, record.RecordType, Actor = actor });
                return;
            }

            const string sql = @"
                WITH updated AS (
                    UPDATE form_f1_outcomes
                    SET outcome_status = @Status,
                        source_form_f1_record_ids = @SourceIds,
                        is_cascade_applied = @Cascade,
                        payment_certificate_id = @PaymentCertificateId,
                        decided_by = @Actor,
                        decided_at = now(),
                        modified_at = now(),
                        modified_by = @Actor
                    WHERE trip_id = @TripId AND record_type = @RecordType AND is_active
                    RETURNING id
                )
                INSERT INTO form_f1_outcomes
                    (payment_certificate_id, trip_id, record_type, outcome_status,
                     source_form_f1_record_ids, is_cascade_applied, decided_by, created_by)
                SELECT @PaymentCertificateId, @TripId, @RecordType, @Status,
                       @SourceIds, @Cascade, @Actor, @Actor
                WHERE NOT EXISTS (SELECT 1 FROM updated)";

            DynamicParameters parameters = new();
            parameters.Add("PaymentCertificateId", record.PaymentCertificateId, DbType.Guid);
            parameters.Add("TripId", record.TripId, DbType.Guid);
            parameters.Add("RecordType", record.RecordType, DbType.String);
            parameters.Add("Status", request.RecordStatus, DbType.String);
            parameters.Add("SourceIds", string.Join(",", decided), DbType.String);
            parameters.Add("Cascade", decided.Count > 1, DbType.Boolean);
            parameters.Add("Actor", actor, DbType.Guid);

            await _dataAccess.ExecuteAsync(sql, parameters);
        }

        private async Task<RecordModel?> SelectRecord(Guid recordId)
        {
            const string sql = @"SELECT f.id, f.payment_certificate_id, f.trip_id, f.record_type,
                                        COALESCE(pc.is_locked, FALSE) AS is_certificate_locked
                                 FROM form_f1_records f
                                 LEFT JOIN payment_certificates pc ON pc.id = f.payment_certificate_id AND pc.is_active
                                 WHERE f.id = @RecordId AND f.is_active";

            return await _dataAccess.QuerySingleOrDefaultAsync<RecordModel>(sql, new { RecordId = recordId });
        }

        private async Task WriteAudit(Guid recordId, string status, string? reason, Guid actor)
        {
            const string sql = @"INSERT INTO audit_logs (table_name, record_id, action_name, action_reason, changed_by, new_values)
                                 SELECT 'form_f1_records', @RecordId, 'Update', @Reason, @Actor, to_jsonb(f)
                                 FROM form_f1_records f
                                 WHERE f.id = @RecordId";

            await _dataAccess.ExecuteAsync(sql, new
            {
                RecordId = recordId,
                Reason = string.IsNullOrWhiteSpace(reason) ? status : $"{status}: {reason.Trim()}",
                Actor = actor
            });
        }

        private sealed class RecordModel
        {
            public Guid Id { get; set; }
            public Guid? PaymentCertificateId { get; set; }
            public Guid TripId { get; set; }
            public string RecordType { get; set; } = string.Empty;
            public bool IsCertificateLocked { get; set; }
        }
    }
}
