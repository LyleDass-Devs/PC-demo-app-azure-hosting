-- =========================================================================
--  010 - Review fixes
--
--  Implements the accepted findings of the three-model design review
--  (docs/database-design-review.md, 18 August 2026). One finding per
--  section, labelled F1..F20 to match the review document. F4/F13/F16/F21
--  are documentation-only and live in the edited files, not here.
--
--  Everything is additive or touches empty tables, so this runs cleanly on
--  the live database. Run as postgres, after 009. Safe to re-run: every
--  ALTER is guarded, and CREATE OR REPLACE VIEW is idempotent.
-- =========================================================================

-- Needed by F2's exclusion constraint (btree equality inside a gist index).
CREATE EXTENSION IF NOT EXISTS btree_gist;

-- Small helper pattern used throughout: a constraint is added only if a
-- constraint of that name does not already exist, making re-runs no-ops.


-- -------------------------------------------------------------------------
--  F15 - drop the unreachable boilerplate columns FIRST, so the F1 loop
--  below does not add foreign keys to columns that are about to go.
--
--  audit_logs: UPDATE is revoked (009), so modified_at/modified_by/is_active
--  can never be set, and created_at/created_by duplicate changed_at/
--  changed_by for the same fact. certificate_lock_events: performed_by/
--  performed_at are the real pair; the trail is append-only by contract
--  (and now by grant - F6), so the same five columns are dead there.
-- -------------------------------------------------------------------------
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


-- -------------------------------------------------------------------------
--  F5 - the two missing Form A bank fields (spec A.36: Account_Type and
--  Branch_Name; the other four were already present).
-- -------------------------------------------------------------------------
ALTER TABLE contracts ADD COLUMN IF NOT EXISTS bank_account_type text;
ALTER TABLE contracts ADD COLUMN IF NOT EXISTS bank_branch_name  text;

COMMENT ON COLUMN contracts.bank_account_type IS
    'A.36 Account_Type (for example cheque or savings), displayed read-only in Form A''s bank-details block.';
COMMENT ON COLUMN contracts.bank_branch_name IS
    'A.36 Branch_Name, displayed read-only in Form A''s bank-details block alongside the branch code.';


-- -------------------------------------------------------------------------
--  F10 - the vehicle on a trip operation becomes a reference, not a string.
--  vehicle_number stays as the unresolved source value from the report;
--  vehicle_id is resolved at import time where possible.
-- -------------------------------------------------------------------------
ALTER TABLE trip_operations ADD COLUMN IF NOT EXISTS vehicle_id uuid;

-- FK added separately (named, and guarded independently of the column) so
-- a partial earlier run can never leave the column unconstrained.
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

COMMENT ON COLUMN trip_operations.vehicle_id IS
    'Resolved from vehicle_number against vehicles at import time, when the number matches. Vehicle-scoped repeat-penalty counting (UC6) should use this, not the text.';
COMMENT ON COLUMN trip_operations.vehicle_number IS
    'The vehicle exactly as named by the source report - kept as evidence even once resolved to vehicle_id. May stay unresolved: the spec defers automatic vehicle-to-trip derivation pending AFC integration (UC6 assumption).';

-- F11 - the second DNO-reason home gets its missing explanation.
COMMENT ON COLUMN trip_operations.dno_reason_id IS
    'Set by the import from the AFC determination when that feed exists. The human conversion decision lives on the Form F1 record (A.6, UC8 2b); where both are present the Form F1 record is the override and wins.';


-- -------------------------------------------------------------------------
--  F1 - every created_by / modified_by / changed_by column references
--  accounts (id), as the spec requires ("Must reference a valid
--  Account_ID") and as approved_by / performed_by / status_updated_by
--  already do. Nullable columns, empty-or-seed-only tables: the ADDs are
--  instant. The loop keeps the migration honest - a future table with
--  these columns gets the constraint by re-running this script.
-- -------------------------------------------------------------------------
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


-- -------------------------------------------------------------------------
--  F2 - overlapping contract-rate windows become impossible. Two active
--  rows for one contract whose [from, to] ranges intersect would have
--  silently doubled Form E; now the second insert fails. A NULL
--  effective_to_date means open-ended, which the range handles natively.
-- -------------------------------------------------------------------------
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


-- -------------------------------------------------------------------------
--  F3 - the certificate period is always the first of the month, so the
--  unique index on (contract_id, certificate_period) really means one
--  certificate per contract per month (UC1 3a).
-- -------------------------------------------------------------------------
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


-- -------------------------------------------------------------------------
--  F17 - the positive-value rules the spec states, as CHECKs. NULL passes
--  a CHECK, so optional columns stay optional.
-- -------------------------------------------------------------------------
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


-- -------------------------------------------------------------------------
--  F9 - the spec's own validation rule "Trip_ID must belong to Route_ID"
--  (A.4, A.6), enforced with a composite foreign key. The supporting
--  unique index on trips (id, route_id) costs nothing - id alone is
--  already unique.
-- -------------------------------------------------------------------------
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


-- -------------------------------------------------------------------------
--  F18 - the penalty code copied onto a capture is provably the code of
--  the item it references. Consequence (verifier note): the code of an
--  item any capture references can no longer be edited in place - changing
--  a code becomes retire-the-item-and-create-a-successor, which the
--  catalogue's partial unique index already supports. Existing captures
--  keep pointing at the retired item, history intact.
-- -------------------------------------------------------------------------
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


-- -------------------------------------------------------------------------
--  F8 - Form B&H no longer stores its own month and year. They restated
--  the linked certificate's period with nothing keeping them in step; the
--  ledger view now derives them, so the ledger can never disagree with
--  Forms A and C about which month a row describes. (The A.7 Month/Year
--  fields remain available - through v_form_bh_ledger, same names.)
--
--  The view is replaced FIRST so the column drop has no dependants.
-- -------------------------------------------------------------------------
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

COMMENT ON VIEW v_form_bh_ledger IS
    'Form B&H ledger rows with contract and certificate context. ledger_year/ledger_month are derived from the certificate''s period (review fix F8), so the ledger cannot disagree with Forms A and C about the month. DELIBERATELY EXCLUDES budget_minor: the budget is visible to DOT alone (A.7), so it must never travel through the general-purpose view - the DOT screen queries the table directly under its own authorisation check.';


-- -------------------------------------------------------------------------
--  F2 (view half) + F7 - Form E survives a missing rate row instead of
--  silently dropping the certificate (LEFT JOIN; a rate-less certificate
--  now shows NULL amounts, which the screens display as pending), and the
--  dead join to contracts is gone. The comment records the F7 decision
--  point for Kuba.
-- -------------------------------------------------------------------------
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

COMMENT ON VIEW v_form_e IS
    'The live Form E calculation (A.13.1). A certificate with no rate row in force shows NULL amounts (pending) rather than disappearing - review fix F2. The penalty deduction DELIBERATELY sums each capture''s effective_rand_amount_minor, not A.3''s catalogue amount, so the repeat-penalty doubling (UC6) reaches the certificate total; a literal reading of A.13.1 names the catalogue amount, and the choice is with Kuba to confirm (review F7, open question 6). Approved formula-priced penalties with no rand amount contribute nothing - the spec''s own recorded gap. On approval these figures are frozen into form_c_records; this view keeps showing the live values.';


-- Form D loses the same dead contracts join (review note).
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


-- -------------------------------------------------------------------------
--  F14 - Form C's fourth spec column. The stored Accumulated Total is now
--  visible through the view for approved (snapshotted) certificates; live
--  certificates show NULL, and the running live total remains the service
--  layer's window calculation as before.
-- -------------------------------------------------------------------------
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

COMMENT ON VIEW v_form_c IS
    'Form C month row: the permanent snapshot for approved certificates, the live calculation otherwise. is_snapshot says which one you are looking at. accumulated_total_minor is the frozen snapshot value (A.13), NULL for live rows - the live running total stays a service-layer window over approved months.';


-- -------------------------------------------------------------------------
--  F19 - the PTOG abbreviation, documented at last (house rule: every
--  abbreviation documented at the object and in the schema doc).
-- -------------------------------------------------------------------------
COMMENT ON COLUMN form_bh_records.deduction_penalties_ptog_minor IS
    'A.7 Deduction Penalties (PTOG). PTOG = Public Transport Operations Grant, the national subsidy programme these contracts are paid from. Penalties deducted from the grant payment, in cents.';
COMMENT ON COLUMN form_bh_records.release_penalties_ptog_minor IS
    'A.7 Release Penalties (PTOG). PTOG = Public Transport Operations Grant. Previously deducted penalty amounts released back to the operator, in cents.';


-- -------------------------------------------------------------------------
--  F6 - tables that promise immutability get it from the database, the way
--  audit_logs already did: the snapshots (A.8 "permanent", A.13 "never
--  edited") and the lock trail ("append-only"). A correction after an
--  unlock SUPERSEDES: deactivate the old snapshot (is_active = FALSE),
--  then insert the successor - the partial unique index frees the slot.
--  Deactivation is itself an UPDATE, so the app keeps COLUMN-LEVEL update
--  on exactly is_active/modified_at/modified_by (verifier finding): the
--  money columns accept no UPDATE from any role. The lock trail needs no
--  supersede path and stays fully append-only.
--  F20 - roles carries the six spec-fixed names; it freezes like the
--  reference lists (a new role is a new script, through review).
--  F12 - the elevated role can read everything (its writes stay narrow:
--  still only the six money catalogues). Without this, an elevated
--  transaction could not read the grid views for the very screens it
--  serves, nor validate a contract by query.
-- -------------------------------------------------------------------------
REVOKE UPDATE ON form_c_records, form_a_records, certificate_lock_events FROM kubapc_app;
GRANT UPDATE (is_active, modified_at, modified_by)
    ON form_c_records, form_a_records TO kubapc_app;

REVOKE INSERT, UPDATE ON roles FROM kubapc_app;

GRANT SELECT ON ALL TABLES IN SCHEMA public TO kubapc_administrator;
ALTER DEFAULT PRIVILEGES IN SCHEMA public
    GRANT SELECT ON TABLES TO kubapc_administrator;

-- Grant-hygiene note (review): 009's GRANT ... ON ALL TABLES also matched
-- the views. Harmless - none is auto-updatable, so INSERT/UPDATE on them
-- cannot be exercised - recorded here so the "read, insert, update on
-- tables" summary in 009 is not read as a hole.
