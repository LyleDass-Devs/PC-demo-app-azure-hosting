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

CREATE UNIQUE INDEX IF NOT EXISTS ux_penalty_captures_trip_code_open
    ON penalty_captures (trip_id, penalty_code)
    WHERE capture_status <> 'Rejected' AND is_active;
CREATE INDEX IF NOT EXISTS ix_penalty_captures_certificate
    ON penalty_captures (payment_certificate_id);

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

    CONSTRAINT ck_form_f1_dno_pair
        CHECK (dno_reason_id IS NULL OR dno_explanation IS NOT NULL)
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_form_f1_certificate_trip_open
    ON form_f1_records (payment_certificate_id, trip_id)
    WHERE record_status <> 'Rejected' AND is_active;
CREATE INDEX IF NOT EXISTS ix_form_f1_records_certificate
    ON form_f1_records (payment_certificate_id);

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
