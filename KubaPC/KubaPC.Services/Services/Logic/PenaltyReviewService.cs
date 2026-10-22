using Dapper;
using KubaPC.Models.Contracts.PenaltyCaptures.Requests;
using KubaPC.Models.Contracts.PenaltyCaptures.Responses;
using KubaPC.Models.Internal;
using KubaPC.Services.Data;
using KubaPC.Services.Services.Interfaces;
using System.Data;

namespace KubaPC.Services.Services.Logic
{
    public class PenaltyReviewService : BaseService, IPenaltyReviewService
    {
        private const string SUBMITTED_STATUS = "Submitted";
        private const string APPROVED_STATUS = "Approved";
        private const string REJECTED_STATUS = "Rejected";
        private const string DISPUTED_STATUS = "Under Dispute";

        private const int MONTHS_BEFORE_DOUBLING = 3;

        public PenaltyReviewService(INpgsqlDataAccess dataAccess) : base(dataAccess)
        {
        }

        public async Task<MatchingCaptureResponse> SelectMatchingUndecided(Guid captureId)
        {
            const string sql = @"SELECT other.id, other.captured_by_role, a.full_name AS captured_by_name, other.capture_status,
                                        other.penalty_code, other.created_at AS captured_at,
                                        (other.capture_status = @Submitted) AS is_undecided
                                 FROM penalty_captures self
                                 JOIN penalty_captures other
                                      ON other.trip_id = self.trip_id
                                     AND other.penalty_list_item_id = self.penalty_list_item_id
                                     AND other.captured_by_role <> self.captured_by_role
                                     AND other.is_active
                                 LEFT JOIN accounts a ON a.id = other.created_by
                                 WHERE self.id = @CaptureId AND self.is_active
                                 ORDER BY (other.capture_status = @Submitted) DESC, other.created_at DESC
                                 LIMIT 1";

            MatchingCaptureResponse? matching = await _dataAccess.QuerySingleOrDefaultAsync<MatchingCaptureResponse>(
                sql, new { CaptureId = captureId, Submitted = SUBMITTED_STATUS });

            return matching ?? new MatchingCaptureResponse();
        }

        public async Task<OperationResult> ReviewCapture(ReviewPenaltyCaptureRequest request, Guid actor)
        {
            if (request.CaptureStatus is not (APPROVED_STATUS or REJECTED_STATUS or DISPUTED_STATUS))
            {
                return OperationResult.Failure("A penalty can only be set to Approved, Rejected or Under Dispute.");
            }

            if (request.CaptureStatus != APPROVED_STATUS && string.IsNullOrWhiteSpace(request.StatusReason))
            {
                return OperationResult.Failure($"A {request.CaptureStatus.ToLowerInvariant()} decision needs a reason.");
            }

            CaptureModel? capture = await SelectCapture(request.Id);

            if (capture is null)
            {
                return OperationResult.Failure("No active penalty capture was found with that id.");
            }

            if (capture.IsCertificateLocked)
            {
                return OperationResult.Failure("The certificate this penalty belongs to is approved, so the penalty is locked.");
            }

            List<Guid> decided = new() { capture.Id };

            if (request.CascadeToMatching)
            {
                MatchingCaptureResponse matching = await SelectMatchingUndecided(capture.Id);

                if (matching.Id is Guid matchingId && matching.IsUndecided)
                {
                    decided.Add(matchingId);
                }
            }

            foreach (Guid captureId in decided)
            {
                await ApplyDecision(captureId, capture, request, actor);
            }

            await WriteOutcome(capture, request, decided, actor);

            return OperationResult.Success();
        }

        private async Task ApplyDecision(Guid captureId, CaptureModel capture, ReviewPenaltyCaptureRequest request, Guid actor)
        {
            long? effectiveAmount = null;
            bool repeatApplied = false;

            if (request.CaptureStatus == APPROVED_STATUS && capture.RandAmountMinor is long amount)
            {
                repeatApplied = capture.RepeatPenaltyEscalationEnabled && await IsQualifyingRepeat(capture);
                effectiveAmount = repeatApplied ? amount * 2 : amount;
            }

            const string sql = @"UPDATE penalty_captures
                                 SET capture_status = @Status,
                                     status_reason = @Reason,
                                     status_updated_by = @Actor,
                                     status_updated_at = now(),
                                     repeat_escalation_applied = @RepeatApplied,
                                     effective_rand_amount_minor = @EffectiveAmount,
                                     modified_at = now(),
                                     modified_by = @Actor
                                 WHERE id = @CaptureId AND is_active";

            DynamicParameters parameters = new();
            parameters.Add("CaptureId", captureId, DbType.Guid);
            parameters.Add("Status", request.CaptureStatus, DbType.String);
            parameters.Add("Reason", string.IsNullOrWhiteSpace(request.StatusReason) ? null : request.StatusReason.Trim(), DbType.String);
            parameters.Add("Actor", actor, DbType.Guid);
            parameters.Add("RepeatApplied", repeatApplied, DbType.Boolean);
            parameters.Add("EffectiveAmount", effectiveAmount, DbType.Int64);

            await _dataAccess.ExecuteAsync(sql, parameters);

            await WriteAudit(captureId, request.CaptureStatus, request.StatusReason, actor);
        }

        private async Task WriteOutcome(CaptureModel capture, ReviewPenaltyCaptureRequest request, List<Guid> decided, Guid actor)
        {
            if (request.CaptureStatus == DISPUTED_STATUS)
            {
                const string clearSql = @"UPDATE penalty_outcomes
                                          SET is_active = FALSE, modified_at = now(), modified_by = @Actor
                                          WHERE trip_id = @TripId AND penalty_list_item_id = @PenaltyListItemId AND is_active";

                await _dataAccess.ExecuteAsync(clearSql, new { capture.TripId, capture.PenaltyListItemId, Actor = actor });
                return;
            }

            const string amountSql = @"SELECT max(effective_rand_amount_minor)
                                       FROM penalty_captures
                                       WHERE trip_id = @TripId AND penalty_list_item_id = @PenaltyListItemId
                                         AND capture_status = @Approved AND is_active";
            long? effectiveAmount = await _dataAccess.QuerySingleOrDefaultAsync<long?>(
                amountSql, new { capture.TripId, capture.PenaltyListItemId, Approved = APPROVED_STATUS });

            const string sql = @"
                WITH updated AS (
                    UPDATE penalty_outcomes
                    SET outcome_status = @Status,
                        effective_rand_amount_minor = @EffectiveAmount,
                        source_penalty_capture_ids = @SourceIds,
                        is_cascade_applied = @Cascade,
                        payment_certificate_id = @PaymentCertificateId,
                        decided_by = @Actor,
                        decided_at = now(),
                        modified_at = now(),
                        modified_by = @Actor
                    WHERE trip_id = @TripId AND penalty_list_item_id = @PenaltyListItemId AND is_active
                    RETURNING id
                )
                INSERT INTO penalty_outcomes
                    (payment_certificate_id, trip_id, penalty_list_item_id, outcome_status, effective_rand_amount_minor,
                     source_penalty_capture_ids, is_cascade_applied, decided_by, created_by)
                SELECT @PaymentCertificateId, @TripId, @PenaltyListItemId, @Status, @EffectiveAmount,
                       @SourceIds, @Cascade, @Actor, @Actor
                WHERE NOT EXISTS (SELECT 1 FROM updated)";

            DynamicParameters parameters = new();
            parameters.Add("PaymentCertificateId", capture.PaymentCertificateId, DbType.Guid);
            parameters.Add("TripId", capture.TripId, DbType.Guid);
            parameters.Add("PenaltyListItemId", capture.PenaltyListItemId, DbType.Guid);
            parameters.Add("Status", request.CaptureStatus, DbType.String);
            parameters.Add("EffectiveAmount", request.CaptureStatus == APPROVED_STATUS ? effectiveAmount : null, DbType.Int64);
            parameters.Add("SourceIds", string.Join(",", decided), DbType.String);
            parameters.Add("Cascade", decided.Count > 1, DbType.Boolean);
            parameters.Add("Actor", actor, DbType.Guid);

            await _dataAccess.ExecuteAsync(sql, parameters);
        }

        private async Task<bool> IsQualifyingRepeat(CaptureModel capture)
        {
            if (capture.CertificatePeriod is not DateOnly period)
            {
                return false;
            }

            string scopeFilter = capture.EscalationCountingScope switch
            {
                "Vehicle" => "AND p.vehicle_id = @VehicleId",
                "Contract" => string.Empty,
                _ => "AND t.trip_number = @TripNumber"
            };

            if (capture.EscalationCountingScope == "Vehicle" && capture.VehicleId is null)
            {
                return false;
            }

            string sql = $@"SELECT DISTINCT pc.certificate_period
                            FROM penalty_captures p
                            JOIN trips t ON t.id = p.trip_id
                            JOIN routes r ON r.id = p.route_id
                            JOIN payment_certificates pc ON pc.id = p.payment_certificate_id AND pc.is_active
                            WHERE p.is_active
                              AND p.penalty_list_item_id = @PenaltyListItemId
                              AND p.capture_status <> @Rejected
                              AND r.contract_id = @ContractId
                              AND pc.certificate_period <= @Period
                              {scopeFilter}
                            ORDER BY pc.certificate_period DESC";

            DynamicParameters parameters = new();
            parameters.Add("PenaltyListItemId", capture.PenaltyListItemId, DbType.Guid);
            parameters.Add("Rejected", REJECTED_STATUS, DbType.String);
            parameters.Add("ContractId", capture.ContractId, DbType.Guid);
            parameters.Add("Period", period, DbType.Date);
            parameters.Add("VehicleId", capture.VehicleId, DbType.Guid);
            parameters.Add("TripNumber", capture.TripNumber, DbType.String);

            List<DateOnly> months = await _dataAccess.QueryAsync<DateOnly>(sql, parameters);

            int streak = 0;
            DateOnly expected = period;

            foreach (DateOnly month in months)
            {
                if (month != expected)
                {
                    break;
                }

                streak++;
                expected = expected.AddMonths(-1);
            }

            return streak >= MONTHS_BEFORE_DOUBLING;
        }

        private async Task<CaptureModel?> SelectCapture(Guid captureId)
        {
            const string sql = @"SELECT p.id, p.payment_certificate_id, p.trip_id, p.penalty_list_item_id, p.vehicle_id,
                                        p.captured_by_role, t.trip_number, r.contract_id,
                                        pli.rand_amount_minor, pli.repeat_penalty_escalation_enabled,
                                        pt.escalation_counting_scope,
                                        pc.certificate_period, COALESCE(pc.is_locked, FALSE) AS is_certificate_locked
                                 FROM penalty_captures p
                                 JOIN trips t ON t.id = p.trip_id
                                 JOIN routes r ON r.id = p.route_id
                                 JOIN penalty_list_items pli ON pli.id = p.penalty_list_item_id
                                 JOIN penalty_types pt ON pt.penalty_type_value = pli.penalty_type
                                 LEFT JOIN payment_certificates pc ON pc.id = p.payment_certificate_id AND pc.is_active
                                 WHERE p.id = @CaptureId AND p.is_active";

            return await _dataAccess.QuerySingleOrDefaultAsync<CaptureModel>(sql, new { CaptureId = captureId });
        }

        private async Task WriteAudit(Guid captureId, string status, string? reason, Guid actor)
        {
            const string sql = @"INSERT INTO audit_logs (table_name, record_id, action_name, action_reason, changed_by, new_values)
                                 SELECT 'penalty_captures', @CaptureId, 'Update', @Reason, @Actor, to_jsonb(p)
                                 FROM penalty_captures p
                                 WHERE p.id = @CaptureId";

            await _dataAccess.ExecuteAsync(sql, new
            {
                CaptureId = captureId,
                Reason = string.IsNullOrWhiteSpace(reason) ? status : $"{status}: {reason.Trim()}",
                Actor = actor
            });
        }

        private sealed class CaptureModel
        {
            public Guid Id { get; set; }
            public Guid? PaymentCertificateId { get; set; }
            public Guid TripId { get; set; }
            public Guid PenaltyListItemId { get; set; }
            public Guid? VehicleId { get; set; }
            public string CapturedByRole { get; set; } = string.Empty;
            public string TripNumber { get; set; } = string.Empty;
            public Guid ContractId { get; set; }
            public long? RandAmountMinor { get; set; }
            public bool RepeatPenaltyEscalationEnabled { get; set; }
            public string EscalationCountingScope { get; set; } = string.Empty;
            public DateOnly? CertificatePeriod { get; set; }
            public bool IsCertificateLocked { get; set; }
        }
    }
}
