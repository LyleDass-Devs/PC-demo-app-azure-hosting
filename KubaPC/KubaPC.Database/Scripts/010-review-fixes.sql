CREATE EXTENSION IF NOT EXISTS btree_gist;

ALTER TABLE audit_logs
    DROP COLUMN IF EXISTS created_at,
    DROP COLUMN IF EXISTS created_by,
    DROP COLUMN IF EXISTS modified_at,
    DROP COLUMN IF EXISTS modified_by,
    DROP COLUMN IF EXISTS is_active;

ALTER TABLE certificate_lock_events
    DROP COLUMN IF EXISTS created_at,
    DROP COLUMN IF EXISTS created_by,
    DROP COLUMN IF EXISTS modified_at,
    DROP COLUMN IF EXISTS modified_by,
    DROP COLUMN IF EXISTS is_active;

ALTER TABLE contracts ADD COLUMN IF NOT EXISTS bank_account_type text;
ALTER TABLE contracts ADD COLUMN IF NOT EXISTS bank_branch_name  text;

ALTER TABLE trip_operations ADD COLUMN IF NOT EXISTS vehicle_id uuid;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint
                   WHERE conname = 'fk_trip_operations_vehicle_id'
                     AND conrelid = 'trip_operations'::regclass) THEN
        ALTER TABLE trip_operations
            ADD CONSTRAINT fk_trip_operations_vehicle_id
            FOREIGN KEY (vehicle_id) REFERENCES vehicles (id);
    END IF;
END $$;

DO $$
DECLARE
    rec record;
    v_constraint_name text;
BEGIN
    FOR rec IN
        SELECT c.table_name, c.column_name
        FROM information_schema.columns c
        JOIN information_schema.tables t
          ON t.table_schema = c.table_schema
         AND t.table_name   = c.table_name
        WHERE c.table_schema = 'public'
          AND t.table_type   = 'BASE TABLE'
          AND c.column_name IN ('created_by', 'modified_by', 'changed_by')
    LOOP
        v_constraint_name := 'fk_' || rec.table_name || '_' || rec.column_name;
        IF NOT EXISTS (SELECT 1 FROM pg_constraint
                       WHERE conname = v_constraint_name
                         AND conrelid = to_regclass(rec.table_name)) THEN
            EXECUTE format(
                'ALTER TABLE %I ADD CONSTRAINT %I FOREIGN KEY (%I) REFERENCES accounts (id)',
                rec.table_name, v_constraint_name, rec.column_name);
        END IF;
    END LOOP;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint
                   WHERE conname = 'ex_contract_rates_no_overlap'
                     AND conrelid = 'contract_rates'::regclass) THEN
        ALTER TABLE contract_rates
            ADD CONSTRAINT ex_contract_rates_no_overlap
            EXCLUDE USING gist (
                contract_id WITH =,
                daterange(effective_from_date, effective_to_date, '[]') WITH &&
            ) WHERE (is_active);
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint
                   WHERE conname = 'ck_payment_certificates_period_first_of_month'
                     AND conrelid = 'payment_certificates'::regclass) THEN
        ALTER TABLE payment_certificates
            ADD CONSTRAINT ck_payment_certificates_period_first_of_month
            CHECK (EXTRACT(DAY FROM certificate_period) = 1);
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint
                   WHERE conname = 'ck_penalty_list_items_amount_positive'
                     AND conrelid = 'penalty_list_items'::regclass) THEN
        ALTER TABLE penalty_list_items
            ADD CONSTRAINT ck_penalty_list_items_amount_positive
            CHECK (rand_amount_minor IS NULL OR rand_amount_minor > 0);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint
                   WHERE conname = 'ck_contract_rates_positive'
                     AND conrelid = 'contract_rates'::regclass) THEN
        ALTER TABLE contract_rates
            ADD CONSTRAINT ck_contract_rates_positive
            CHECK (operated_rate_per_km_minor > 0 AND standing_rate_per_km_minor > 0);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint
                   WHERE conname = 'ck_form_g2_amount_positive'
                     AND conrelid = 'form_g2_refundable_amounts'::regclass) THEN
        ALTER TABLE form_g2_refundable_amounts
            ADD CONSTRAINT ck_form_g2_amount_positive
            CHECK (rand_amount_minor > 0);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint
                   WHERE conname = 'ck_form_bh_kilometres_not_negative'
                     AND conrelid = 'form_bh_records'::regclass) THEN
        ALTER TABLE form_bh_records
            ADD CONSTRAINT ck_form_bh_kilometres_not_negative
            CHECK (   (total_scheduled_kilometres IS NULL OR total_scheduled_kilometres >= 0)
                  AND (total_operated_kilometres  IS NULL OR total_operated_kilometres  >= 0));
    END IF;
END $$;

CREATE UNIQUE INDEX IF NOT EXISTS ux_trips_id_route ON trips (id, route_id);

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint
                   WHERE conname = 'fk_penalty_captures_trip_route'
                     AND conrelid = 'penalty_captures'::regclass) THEN
        ALTER TABLE penalty_captures
            ADD CONSTRAINT fk_penalty_captures_trip_route
            FOREIGN KEY (trip_id, route_id) REFERENCES trips (id, route_id);
    END IF;
    IF NOT EXISTS (SELECT 1 FROM pg_constraint
                   WHERE conname = 'fk_form_f1_records_trip_route'
                     AND conrelid = 'form_f1_records'::regclass) THEN
        ALTER TABLE form_f1_records
            ADD CONSTRAINT fk_form_f1_records_trip_route
            FOREIGN KEY (trip_id, route_id) REFERENCES trips (id, route_id);
    END IF;
END $$;

CREATE UNIQUE INDEX IF NOT EXISTS ux_penalty_list_items_id_code
    ON penalty_list_items (id, penalty_code);

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint
                   WHERE conname = 'fk_penalty_captures_item_code'
                     AND conrelid = 'penalty_captures'::regclass) THEN
        ALTER TABLE penalty_captures
            ADD CONSTRAINT fk_penalty_captures_item_code
            FOREIGN KEY (penalty_list_item_id, penalty_code)
            REFERENCES penalty_list_items (id, penalty_code);
    END IF;
END $$;

CREATE OR REPLACE VIEW v_form_bh_ledger AS
SELECT
    c.operator_id,
    c.id                        AS contract_id,
    c.contract_number,
    pc.certificate_number,
    pc.certificate_status,
    EXTRACT(YEAR  FROM pc.certificate_period)::integer AS ledger_year,
    EXTRACT(MONTH FROM pc.certificate_period)::integer AS ledger_month,
    bh.certificate_value_minor,
    bh.accumulated_certified_value_minor,
    bh.deduction_penalties_ptog_minor,
    bh.release_penalties_ptog_minor,
    bh.amount_paid_to_operator_minor,
    bh.balance_minor,
    bh.bus_restructuring_fund_minor,
    bh.total_scheduled_kilometres,
    bh.total_operated_kilometres,
    bh.income_private_hires_minor,
    bh.income_dedicated_contracts_minor
FROM form_bh_records bh
JOIN payment_certificates pc ON pc.id = bh.payment_certificate_id
JOIN contracts c             ON c.id = pc.contract_id
WHERE bh.is_active AND pc.is_active;

ALTER TABLE form_bh_records
    DROP COLUMN IF EXISTS ledger_month,
    DROP COLUMN IF EXISTS ledger_year;

CREATE OR REPLACE VIEW v_form_e AS
SELECT
    pc.id                                   AS payment_certificate_id,
    pc.certificate_number,
    pc.certificate_period,
    km.operated_km_total,
    cr.operated_rate_per_km_minor,
    ROUND(km.operated_km_total * cr.operated_rate_per_km_minor)::bigint
                                            AS operated_amount_minor,
    km.standing_km_total,
    cr.standing_rate_per_km_minor,
    ROUND(km.standing_km_total * cr.standing_rate_per_km_minor)::bigint
                                            AS standing_amount_minor,
    COALESCE(pen.approved_penalties_minor, 0)
                                            AS approved_penalties_minor,
    ROUND(km.operated_km_total * cr.operated_rate_per_km_minor)::bigint
      - COALESCE(pen.approved_penalties_minor, 0)
      + ROUND(km.standing_km_total * cr.standing_rate_per_km_minor)::bigint
                                            AS amount_for_escalation_minor
FROM payment_certificates pc
JOIN LATERAL (
    SELECT
        COALESCE(SUM(op.operated_km) FILTER (WHERE NOT op.is_standing_trip), 0) AS operated_km_total,
        COALESCE(SUM(op.operated_km) FILTER (WHERE op.is_standing_trip), 0)     AS standing_km_total
    FROM trips t
    JOIN routes r          ON r.id = t.route_id
    JOIN trip_operations op ON op.trip_id = t.id AND op.is_active
    WHERE r.contract_id = pc.contract_id
      AND t.is_active
      AND t.scheduled_date >= pc.certificate_period
      AND t.scheduled_date <  (pc.certificate_period + INTERVAL '1 month')::date
) km ON TRUE
LEFT JOIN contract_rates cr
    ON cr.contract_id = pc.contract_id
   AND cr.is_active
   AND cr.effective_from_date <= pc.certificate_period
   AND (cr.effective_to_date IS NULL OR cr.effective_to_date >= pc.certificate_period)
LEFT JOIN LATERAL (
    SELECT SUM(p.effective_rand_amount_minor) AS approved_penalties_minor
    FROM penalty_captures p
    WHERE p.payment_certificate_id = pc.id
      AND p.capture_status = 'Approved'
      AND p.is_active
) pen ON TRUE
WHERE pc.is_active;

CREATE OR REPLACE VIEW v_form_d AS
SELECT
    pc.id                    AS payment_certificate_id,
    pc.certificate_number,
    pc.certificate_period,
    ep.labour_weight,
    ep.fuel_weight,
    ep.cpi_weight,
    ep.base_month,
    ep.base_year,
    ep.labour_base_value,
    ep.fuel_base_value,
    ep.cpi_base_value,
    er.labour_rate_current,
    er.fuel_rate_current,
    er.cpi_rate_current,
    er.escalation_factor
FROM payment_certificates pc
LEFT JOIN escalation_parameters ep
    ON ep.contract_id = pc.contract_id AND ep.is_active
LEFT JOIN escalation_records er
    ON er.payment_certificate_id = pc.id AND er.is_active
WHERE pc.is_active;

CREATE OR REPLACE VIEW v_form_c AS
SELECT
    pc.id                              AS payment_certificate_id,
    pc.certificate_number,
    pc.certificate_period,
    pc.certificate_status,
    (fc.id IS NOT NULL)                AS is_snapshot,
    COALESCE(fc.amount_for_escalation_minor, fe.amount_for_escalation_minor)
                                       AS amount_for_escalation_minor,
    COALESCE(fc.escalation_factor, er.escalation_factor)
                                       AS escalation_factor,
    COALESCE(fc.escalated_amount_minor,
             ROUND(fe.amount_for_escalation_minor * er.escalation_factor)::bigint)
                                       AS escalated_amount_minor,
    fc.accumulated_total_minor         AS accumulated_total_minor
FROM payment_certificates pc
LEFT JOIN form_c_records fc
    ON fc.payment_certificate_id = pc.id AND fc.is_active
LEFT JOIN v_form_e fe
    ON fe.payment_certificate_id = pc.id
LEFT JOIN escalation_records er
    ON er.payment_certificate_id = pc.id AND er.is_active
WHERE pc.is_active;

REVOKE UPDATE ON form_c_records, form_a_records, certificate_lock_events FROM kubapc_app;
GRANT UPDATE (is_active, modified_at, modified_by)
    ON form_c_records, form_a_records TO kubapc_app;

REVOKE INSERT, UPDATE ON roles FROM kubapc_app;

GRANT SELECT ON ALL TABLES IN SCHEMA public TO kubapc_administrator;
ALTER DEFAULT PRIVILEGES IN SCHEMA public
    GRANT SELECT ON TABLES TO kubapc_administrator;
