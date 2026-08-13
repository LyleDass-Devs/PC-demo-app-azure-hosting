-- =========================================================================
--  007 - The payment certificate and its forms
--
--  Spec tables A.1 (certificate), A.2 (trip lines), A.4 (penalty captures),
--  A.5 (Form G2 amounts), A.6 (Form F1 records), A.7 (Form B&H ledger),
--  A.8 (Form A snapshot), A.11 (escalation record), A.13 (Form C snapshot),
--  plus certificate_lock_events for the lock/unlock requirement.
--
--  Locking model (spec + one extension):
--    - Spec: approval of the certificate (Use Case 2) permanently locks the
--      certificate, every linked penalty capture (Use Case 6, 3b), the
--      escalation record (A.11), and snapshots Form C (A.13) and Form A's
--      Total_Amount_Due (A.8).
--    - Extension you asked for: a user holding the unlock permission may
--      unlock an approved certificate from the application. Every lock and
--      unlock is a row in certificate_lock_events with who, when, and why.
--      Enforcement is application-side; the trail is database-side.
--
--  Run after 006. Safe to re-run.
-- =========================================================================

-- A.1 Payment Certificate.
CREATE TABLE IF NOT EXISTS payment_certificates (
    id                      uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    contract_id             uuid NOT NULL REFERENCES contracts (id),
    certificate_number      text NOT NULL,
    certificate_period      date NOT NULL,
    certificate_status      text NOT NULL DEFAULT 'Draft'
                            REFERENCES certificate_statuses (status_value),
    total_trips_scheduled   integer NOT NULL DEFAULT 0,
    total_trips_completed   integer NOT NULL DEFAULT 0,
    notes                   text,
    submitted_at            timestamptz,
    approved_by             uuid REFERENCES accounts (id),
    approved_at             timestamptz,
    rejection_reason        text,
    is_locked               boolean NOT NULL DEFAULT FALSE,
    is_active               boolean     NOT NULL DEFAULT TRUE,
    created_at              timestamptz NOT NULL DEFAULT now(),
    created_by              uuid,
    modified_at             timestamptz,
    modified_by             uuid
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_payment_certificates_contract_period
    ON payment_certificates (contract_id, certificate_period) WHERE is_active;
CREATE UNIQUE INDEX IF NOT EXISTS ux_payment_certificates_number
    ON payment_certificates (certificate_number) WHERE is_active;

COMMENT ON TABLE payment_certificates IS
    'A.1. One monthly certificate per contract per period. Status flow: Draft -> Submitted -> Approved/Rejected; Submitted may be withdrawn back to Draft, and Rejected is editable and resubmittable (Use Case 1, 3c/3d; Use Case 2, 3b).';
COMMENT ON COLUMN payment_certificates.certificate_number IS
    'A.1 Certificate_Number: the human-readable number used in reporting and correspondence, distinct from id. Unique among active certificates; must support data migration of historical numbers, hence text and no generation rule in the database.';
COMMENT ON COLUMN payment_certificates.certificate_period IS
    'First day of the calendar month the certificate covers (A.1 Certificate_Period ''2026-07'' stored as 2026-07-01).';
COMMENT ON COLUMN payment_certificates.total_trips_scheduled IS
    'A.1: calculated from trips for the contract and period; never user-edited.';
COMMENT ON COLUMN payment_certificates.total_trips_completed IS
    'A.1: calculated from trip_operations; never user-edited.';
COMMENT ON COLUMN payment_certificates.is_locked IS
    'TRUE from the moment of approval. Cleared only by the application''s unlock action, which requires the unlock permission and writes a certificate_lock_events row. While TRUE the application refuses every edit to the certificate and its linked records.';
COMMENT ON COLUMN payment_certificates.modified_by IS
    'A.1 Last_Modified_By, confirmed: updated by EVERY change from any actor - a field edit or withdrawal by Operator Staff, or an approve/reject by an Authoriser.';


-- Lock/unlock trail (your requirement; not in the spec).
CREATE TABLE IF NOT EXISTS certificate_lock_events (
    id                       uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    payment_certificate_id   uuid NOT NULL REFERENCES payment_certificates (id),
    lock_action              text NOT NULL,
    action_reason            text,
    performed_by             uuid NOT NULL REFERENCES accounts (id),
    performed_at             timestamptz NOT NULL DEFAULT now(),
    is_active                boolean     NOT NULL DEFAULT TRUE,
    created_at               timestamptz NOT NULL DEFAULT now(),
    created_by               uuid,
    modified_at              timestamptz,
    modified_by              uuid,

    CONSTRAINT ck_certificate_lock_events_action
        CHECK (lock_action IN ('Locked', 'Unlocked'))
);
CREATE INDEX IF NOT EXISTS ix_certificate_lock_events_certificate
    ON certificate_lock_events (payment_certificate_id, performed_at);

COMMENT ON TABLE certificate_lock_events IS
    'Extension beyond the spec: the spec locks permanently at approval; the requirement here is that a user with the unlock permission may unlock from the application. Append-only trail of every lock and unlock, with the reason. A certificate that was unlocked, corrected, and relocked shows the full sequence.';
COMMENT ON COLUMN certificate_lock_events.action_reason IS
    'Mandatory for Unlocked (the application enforces it): unlocking an approved certificate reopens agreed money and must say why.';


-- A.2 Payment Certificate Trip Line.
CREATE TABLE IF NOT EXISTS certificate_trip_lines (
    id                      uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    payment_certificate_id  uuid NOT NULL REFERENCES payment_certificates (id),
    trip_id                 uuid NOT NULL REFERENCES trips (id),
    trip_date               date NOT NULL,
    completion_status       text NOT NULL
                            REFERENCES trip_completion_statuses (status_value),
    is_active               boolean     NOT NULL DEFAULT TRUE,
    created_at              timestamptz NOT NULL DEFAULT now(),
    created_by              uuid,
    modified_at             timestamptz,
    modified_by             uuid
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_certificate_trip_lines_certificate_trip
    ON certificate_trip_lines (payment_certificate_id, trip_id) WHERE is_active;
CREATE INDEX IF NOT EXISTS ix_certificate_trip_lines_certificate
    ON certificate_trip_lines (payment_certificate_id);

COMMENT ON TABLE certificate_trip_lines IS
    'A.2. One row per trip included in the certificate''s summary, so the aggregate counts on the certificate can be drilled into. trip_date must fall within the certificate''s period (application-validated).';


-- A.4 Penalty Capture.
CREATE TABLE IF NOT EXISTS penalty_captures (
    id                          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    payment_certificate_id      uuid NOT NULL REFERENCES payment_certificates (id),
    captured_by_role            text NOT NULL
                                REFERENCES captured_by_roles (role_value),
    route_id                    uuid NOT NULL REFERENCES routes (id),
    trip_id                     uuid NOT NULL REFERENCES trips (id),
    trip_detail                 text,
    vehicle_id                  uuid REFERENCES vehicles (id),
    penalty_list_item_id        uuid NOT NULL REFERENCES penalty_list_items (id),
    penalty_code                text NOT NULL,
    capture_status              text NOT NULL DEFAULT 'Submitted'
                                REFERENCES review_statuses (status_value),
    status_reason               text,
    status_updated_by           uuid REFERENCES accounts (id),
    status_updated_at           timestamptz,
    repeat_escalation_applied   boolean NOT NULL DEFAULT FALSE,
    effective_rand_amount_minor bigint,
    is_active                   boolean     NOT NULL DEFAULT TRUE,
    created_at                  timestamptz NOT NULL DEFAULT now(),
    created_by                  uuid,
    modified_at                 timestamptz,
    modified_by                 uuid
);
-- Use Case 4, 2a: at most one non-Rejected capture per trip and penalty code,
-- regardless of capturing role. The database enforces exactly the spec rule.
CREATE UNIQUE INDEX IF NOT EXISTS ux_penalty_captures_trip_code_open
    ON penalty_captures (trip_id, penalty_code)
    WHERE capture_status <> 'Rejected' AND is_active;
CREATE INDEX IF NOT EXISTS ix_penalty_captures_certificate
    ON penalty_captures (payment_certificate_id);

COMMENT ON TABLE penalty_captures IS
    'A.4. One penalty recorded against a certificate by Operator Staff (UC4) or Monitor (UC5), reviewed by an Authoriser (UC6). While Submitted: Operator Staff edits/deletes own records only, Monitor edits/deletes any (asymmetric, confirmed). Deletion is soft (is_active FALSE) by owner decision - the spec''s UC4 4b says permanently removed, but the audit Delete entry and the is_active filter on the uniqueness index preserve every observable behaviour while keeping the row as evidence. Rejected/Under Dispute stay re-reviewable until the certificate is Approved, at which point every capture locks regardless of status (UC6 3b - the only permanent lock).';
COMMENT ON COLUMN penalty_captures.penalty_code IS
    'A.4: copied from the penalty list item at capture time for display and reporting without a join, and so the trip/code uniqueness rule survives a catalogue edit.';
COMMENT ON COLUMN penalty_captures.vehicle_id IS
    'A.4: mandatory when the linked item''s penalty_type is Unroadworthy or Unsatisfactory Vehicle Condition (application-enforced conditional), because repeat-penalty counting for those types is vehicle-scoped.';
COMMENT ON COLUMN penalty_captures.status_reason IS
    'A.4: mandatory for Rejected and Under Dispute; optional for Approved - a confirmed, intentional divergence from Form F1, where a reason accompanies every status change.';
COMMENT ON COLUMN penalty_captures.repeat_escalation_applied IS
    'Use Case 6 rule: set on approval when this is the 3rd+ consecutive qualifying monthly occurrence for the same trip, contract, or vehicle. A Rejected capture in the sequence resets the streak.';
COMMENT ON COLUMN penalty_captures.effective_rand_amount_minor IS
    'A.4: the final amount in cents, system-calculated at approval - the item''s base amount, or exactly double it when repeat_escalation_applied (never compounding). NULL until approved, and NULL for items priced only by formula (known spec gap, A.13.1).';


-- A.5 Form G2 Refundable Penalty Amount - at most 10 rows per certificate.
CREATE TABLE IF NOT EXISTS form_g2_refundable_amounts (
    id                          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    payment_certificate_id      uuid NOT NULL REFERENCES payment_certificates (id),
    refundable_penalty_type     text NOT NULL
                                REFERENCES refundable_penalty_types (type_value),
    sub_refundable_penalty_type text NOT NULL
                                REFERENCES sub_refundable_penalty_types (type_value),
    rand_amount_minor           bigint NOT NULL,
    is_active                   boolean     NOT NULL DEFAULT TRUE,
    created_at                  timestamptz NOT NULL DEFAULT now(),
    created_by                  uuid,
    modified_at                 timestamptz,
    modified_by                 uuid
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_form_g2_certificate_combination
    ON form_g2_refundable_amounts
       (payment_certificate_id, refundable_penalty_type, sub_refundable_penalty_type)
    WHERE is_active;

COMMENT ON TABLE form_g2_refundable_amounts IS
    'A.5. Captured by the Authoriser on Form G2 (UC7). 2 types x 5 sub-types = at most 10 rows per certificate, enforced by the unique index. Amounts entered in Rand, stored as cents.';


-- A.6 Form F1 Record.
CREATE TABLE IF NOT EXISTS form_f1_records (
    id                      uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    payment_certificate_id  uuid NOT NULL REFERENCES payment_certificates (id),
    route_id                uuid NOT NULL REFERENCES routes (id),
    trip_id                 uuid NOT NULL REFERENCES trips (id),
    record_type             text NOT NULL
                            REFERENCES form_f1_record_types (type_value),
    reason                  text NOT NULL,
    dno_reason_id           uuid REFERENCES dno_reasons (id),
    dno_explanation         text,
    captured_by_role        text NOT NULL
                            REFERENCES captured_by_roles (role_value),
    record_status           text NOT NULL DEFAULT 'Submitted'
                            REFERENCES review_statuses (status_value),
    status_reason           text,
    status_updated_by       uuid REFERENCES accounts (id),
    status_updated_at       timestamptz,
    is_active               boolean     NOT NULL DEFAULT TRUE,
    created_at              timestamptz NOT NULL DEFAULT now(),
    created_by              uuid,
    modified_at             timestamptz,
    modified_by             uuid,

    -- A.6: a DNO conversion must carry both the reason and the explanation.
    CONSTRAINT ck_form_f1_dno_pair
        CHECK (dno_reason_id IS NULL OR dno_explanation IS NOT NULL)
);
-- Use Case 8, 2a: one record per certificate/trip, excluding Rejected.
CREATE UNIQUE INDEX IF NOT EXISTS ux_form_f1_certificate_trip_open
    ON form_f1_records (payment_certificate_id, trip_id)
    WHERE record_status <> 'Rejected' AND is_active;
CREATE INDEX IF NOT EXISTS ix_form_f1_records_certificate
    ON form_f1_records (payment_certificate_id);

COMMENT ON TABLE form_f1_records IS
    'A.6. Trip-level exception: Breakdown, Roadworthy Non-Conformance, or Standing Trip. Unlike penalties, Approved and Rejected are TERMINAL here (UC9 2a) and a status reason is mandatory on every change including Approved - both confirmed, intentional divergences. Soft delete while Submitted, same as penalty_captures.';
COMMENT ON COLUMN form_f1_records.dno_reason_id IS
    'Populated only when the record was created by converting an AFC-determined DNO to a Standing Trip (UC8 2b). The AFC feed itself does not exist yet; the reference list and this column are ready for it.';


-- A.11 Escalation Record - one per certificate.
CREATE TABLE IF NOT EXISTS escalation_records (
    id                      uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    payment_certificate_id  uuid NOT NULL REFERENCES payment_certificates (id),
    labour_rate_current     numeric(18,6),
    fuel_rate_current       numeric(18,6),
    cpi_rate_current        numeric(18,6),
    escalation_factor       numeric(12,6),
    is_active               boolean     NOT NULL DEFAULT TRUE,
    created_at              timestamptz NOT NULL DEFAULT now(),
    created_by              uuid,
    modified_at             timestamptz,
    modified_by             uuid
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_escalation_records_certificate
    ON escalation_records (payment_certificate_id) WHERE is_active;

COMMENT ON TABLE escalation_records IS
    'A.11. The month''s captured rates (Lt, Ft, CPIt) and calculated factor f, captured by Monitor or Authoriser (UC16). Editable only until the certificate is Approved. f = ((a x (Lt/Lbm)) + (b x (Ft/Fbm)) + (c x (CPIt/Cbm)) - 1) + 1, rounded to 6 decimals, never editable by any role.';


-- A.13 Form C Record - snapshot written at the moment of approval.
CREATE TABLE IF NOT EXISTS form_c_records (
    id                            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    payment_certificate_id        uuid NOT NULL REFERENCES payment_certificates (id),
    amount_for_escalation_minor   bigint NOT NULL,
    escalation_factor             numeric(12,6) NOT NULL,
    escalated_amount_minor        bigint NOT NULL,
    accumulated_total_minor       bigint NOT NULL,
    is_active                     boolean     NOT NULL DEFAULT TRUE,
    created_at                    timestamptz NOT NULL DEFAULT now(),
    created_by                    uuid,
    modified_at                   timestamptz,
    modified_by                   uuid
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_form_c_records_certificate
    ON form_c_records (payment_certificate_id) WHERE is_active;

COMMENT ON TABLE form_c_records IS
    'A.13. Written ONLY when the certificate is approved (UC2 step 3), freezing that month''s Amount for Escalation (Form E), Escalation Factor (Form D), Escalated Amount, and running Accumulated Total. Never edited after creation. Before approval, Form C displays live figures from the view instead and no row exists here.';
COMMENT ON COLUMN form_c_records.amount_for_escalation_minor IS
    'From Form E: operated kilometres at the operated rate, less approved non-refundable penalties, plus standing kilometres at the standing rate (A.13.1).';


-- A.8 Form A Record - a single permanent figure per certificate.
CREATE TABLE IF NOT EXISTS form_a_records (
    id                      uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    payment_certificate_id  uuid NOT NULL REFERENCES payment_certificates (id),
    total_amount_due_minor  bigint NOT NULL,
    is_active               boolean     NOT NULL DEFAULT TRUE,
    created_at              timestamptz NOT NULL DEFAULT now(),
    created_by              uuid,
    modified_at             timestamptz,
    modified_by             uuid
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_form_a_records_certificate
    ON form_a_records (payment_certificate_id) WHERE is_active;

COMMENT ON TABLE form_a_records IS
    'A.8. Stores exactly one thing: Total Amount Due, snapshotted at approval (same pattern as Form C, confirmed). Everything else Form A shows - accumulated escalated amount (= Form C''s accumulated total), less-accumulated values, sub-totals, bank details - is calculated or looked up live, never stored here.';


-- A.7 Form B&H Record - one ledger row per contract per month.
CREATE TABLE IF NOT EXISTS form_bh_records (
    id                                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    payment_certificate_id                uuid NOT NULL REFERENCES payment_certificates (id),
    ledger_month                          integer NOT NULL,
    ledger_year                           integer NOT NULL,
    certificate_value_minor               bigint,
    budget_minor                          bigint,
    accumulated_certified_value_minor     bigint,
    deduction_penalties_ptog_minor        bigint,
    release_penalties_ptog_minor          bigint,
    amount_paid_to_operator_minor         bigint,
    balance_minor                         bigint GENERATED ALWAYS AS
        (COALESCE(certificate_value_minor, 0) - COALESCE(amount_paid_to_operator_minor, 0)) STORED,
    bus_restructuring_fund_minor          bigint,
    total_scheduled_kilometres            numeric(12,2),
    total_operated_kilometres             numeric(12,2),
    income_private_hires_minor            bigint,
    income_dedicated_contracts_minor      bigint,
    is_active                             boolean     NOT NULL DEFAULT TRUE,
    created_at                            timestamptz NOT NULL DEFAULT now(),
    created_by                            uuid,
    modified_at                           timestamptz,
    modified_by                           uuid,

    CONSTRAINT ck_form_bh_month CHECK (ledger_month BETWEEN 1 AND 12)
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_form_bh_records_certificate
    ON form_bh_records (payment_certificate_id) WHERE is_active;

COMMENT ON TABLE form_bh_records IS
    'A.7. One row per contract per month (the contract is reached through the certificate). Field-level rights vary by role and recency: only the current and previous month are ever editable, everything earlier is permanently locked history (application-enforced). Budget is visible to DOT only - the operator must never see the budgeted amount; enforce in every query and endpoint.';
COMMENT ON COLUMN form_bh_records.certificate_value_minor IS
    'A.7 Cert Value: entered by Operator Staff from the certificate report (UC11), editable by Authoriser or DOT (UC12), current month only.';
COMMENT ON COLUMN form_bh_records.budget_minor IS
    'DOT only - both visibility and writes. Hidden entirely from Operator Staff, Monitor, and Authoriser.';
COMMENT ON COLUMN form_bh_records.balance_minor IS
    'A.7 Balance, confirmed: certificate value less amount paid, treating unpaid as zero. Generated column - the database computes it; no role can set it.';
COMMENT ON COLUMN form_bh_records.bus_restructuring_fund_minor IS
    'Captured by DOT only.';
COMMENT ON COLUMN form_bh_records.income_private_hires_minor IS
    'Capturable only while income_field_configurations enables it for the contract (A.16); defaults to Operator Staff, current month.';
