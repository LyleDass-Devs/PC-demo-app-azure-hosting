DROP INDEX IF EXISTS ux_penalty_captures_trip_code_open;

CREATE TABLE IF NOT EXISTS penalty_outcomes (
    id                          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    payment_certificate_id      uuid REFERENCES payment_certificates (id),
    trip_id                     uuid NOT NULL REFERENCES trips (id),
    penalty_list_item_id        uuid NOT NULL REFERENCES penalty_list_items (id),
    outcome_status              text NOT NULL REFERENCES review_statuses (status_value),
    effective_rand_amount_minor bigint,
    source_penalty_capture_ids  text NOT NULL,
    is_cascade_applied          boolean NOT NULL DEFAULT FALSE,
    decided_by                  uuid NOT NULL REFERENCES accounts (id),
    decided_at                  timestamptz NOT NULL DEFAULT now(),
    is_active                   boolean     NOT NULL DEFAULT TRUE,
    created_at                  timestamptz NOT NULL DEFAULT now(),
    created_by                  uuid,
    modified_at                 timestamptz,
    modified_by                 uuid,

    CONSTRAINT ck_penalty_outcomes_status
        CHECK (outcome_status IN ('Approved', 'Rejected')),
    CONSTRAINT ck_penalty_outcomes_amount
        CHECK (outcome_status <> 'Approved' OR effective_rand_amount_minor IS NOT NULL)
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_penalty_outcomes_trip_item
    ON penalty_outcomes (trip_id, penalty_list_item_id) WHERE is_active;
CREATE INDEX IF NOT EXISTS ix_penalty_outcomes_certificate
    ON penalty_outcomes (payment_certificate_id);

CREATE TABLE IF NOT EXISTS form_f1_outcomes (
    id                          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    payment_certificate_id      uuid REFERENCES payment_certificates (id),
    trip_id                     uuid NOT NULL REFERENCES trips (id),
    record_type                 text NOT NULL REFERENCES form_f1_record_types (type_value),
    outcome_status              text NOT NULL REFERENCES review_statuses (status_value),
    source_form_f1_record_ids   text NOT NULL,
    is_cascade_applied          boolean NOT NULL DEFAULT FALSE,
    decided_by                  uuid NOT NULL REFERENCES accounts (id),
    decided_at                  timestamptz NOT NULL DEFAULT now(),
    is_active                   boolean     NOT NULL DEFAULT TRUE,
    created_at                  timestamptz NOT NULL DEFAULT now(),
    created_by                  uuid,
    modified_at                 timestamptz,
    modified_by                 uuid,

    CONSTRAINT ck_form_f1_outcomes_status
        CHECK (outcome_status IN ('Approved', 'Rejected'))
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_form_f1_outcomes_trip_type
    ON form_f1_outcomes (trip_id, record_type) WHERE is_active;
CREATE INDEX IF NOT EXISTS ix_form_f1_outcomes_certificate
    ON form_f1_outcomes (payment_certificate_id);

CREATE TABLE IF NOT EXISTS public_holidays (
    id           uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    holiday_date date NOT NULL,
    holiday_name text NOT NULL,
    is_active    boolean     NOT NULL DEFAULT TRUE,
    created_at   timestamptz NOT NULL DEFAULT now(),
    created_by   uuid,
    modified_at  timestamptz,
    modified_by  uuid
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_public_holidays_date
    ON public_holidays (holiday_date) WHERE is_active;

CREATE TABLE IF NOT EXISTS form_i_records (
    id                          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    payment_certificate_id      uuid NOT NULL REFERENCES payment_certificates (id),
    operator_id                 uuid NOT NULL REFERENCES operators (id),
    route_id                    uuid NOT NULL REFERENCES routes (id),
    monday_trips                integer NOT NULL DEFAULT 0,
    tuesday_trips               integer NOT NULL DEFAULT 0,
    wednesday_trips             integer NOT NULL DEFAULT 0,
    thursday_trips              integer NOT NULL DEFAULT 0,
    friday_trips                integer NOT NULL DEFAULT 0,
    saturday_trips              integer NOT NULL DEFAULT 0,
    sunday_trips                integer NOT NULL DEFAULT 0,
    public_holiday_trips        integer NOT NULL DEFAULT 0,
    total_scheduled_trips       integer NOT NULL DEFAULT 0,
    route_length_km             numeric(12,2) NOT NULL,
    total_scheduled_kilometres  numeric(14,2) NOT NULL,
    calculated_by               uuid REFERENCES accounts (id),
    calculated_at               timestamptz NOT NULL DEFAULT now(),
    is_active                   boolean     NOT NULL DEFAULT TRUE,
    created_at                  timestamptz NOT NULL DEFAULT now(),
    created_by                  uuid,
    modified_at                 timestamptz,
    modified_by                 uuid
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_form_i_records_certificate_route
    ON form_i_records (payment_certificate_id, route_id) WHERE is_active;

UPDATE penalty_types SET escalation_counting_scope = 'Trip'
WHERE penalty_type_value IN ('Time-Related Offences', 'Unsatisfactory Vehicle Condition', 'Duty & Destination Boards')
  AND escalation_counting_scope <> 'Trip';

GRANT SELECT, INSERT, UPDATE ON penalty_outcomes TO kubapc_app;
GRANT SELECT, INSERT, UPDATE ON form_f1_outcomes TO kubapc_app;
GRANT SELECT, INSERT, UPDATE ON public_holidays TO kubapc_app;
GRANT SELECT, INSERT, UPDATE ON form_i_records TO kubapc_app;
