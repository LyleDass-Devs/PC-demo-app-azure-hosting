CREATE TABLE IF NOT EXISTS vehicle_types (
    vehicle_type_value text PRIMARY KEY,
    display_order      integer NOT NULL
);

INSERT INTO vehicle_types (vehicle_type_value, display_order) VALUES
    ('Standard bus', 1),
    ('Maxi bus',     2),
    ('Train bus',    3)
ON CONFLICT (vehicle_type_value) DO NOTHING;

ALTER TABLE vehicles
    ADD COLUMN IF NOT EXISTS vehicle_type text REFERENCES vehicle_types (vehicle_type_value);

UPDATE vehicles SET vehicle_type = 'Standard bus' WHERE vehicle_type IS NULL;

ALTER TABLE vehicles ALTER COLUMN vehicle_type SET NOT NULL;
ALTER TABLE vehicles ALTER COLUMN vehicle_type SET DEFAULT 'Standard bus';

ALTER TABLE contract_rates
    ADD COLUMN IF NOT EXISTS vehicle_type text REFERENCES vehicle_types (vehicle_type_value);

UPDATE contract_rates SET vehicle_type = 'Standard bus' WHERE vehicle_type IS NULL;

ALTER TABLE contract_rates ALTER COLUMN vehicle_type SET NOT NULL;

CREATE INDEX IF NOT EXISTS ix_contract_rates_contract_type_effective
    ON contract_rates (contract_id, vehicle_type, effective_from_date);

ALTER TABLE contract_rates DROP CONSTRAINT IF EXISTS ex_contract_rates_no_overlap;

ALTER TABLE contract_rates
    ADD CONSTRAINT ex_contract_rates_no_overlap
    EXCLUDE USING gist (
        contract_id WITH =,
        vehicle_type WITH =,
        daterange(effective_from_date, effective_to_date, '[]') WITH &&
    ) WHERE (is_active);

DROP INDEX IF EXISTS ix_contract_rates_contract_effective;


INSERT INTO url_keys (key_value, display_order) VALUES
    ('Labour Source', 3)
ON CONFLICT (key_value) DO NOTHING;


DROP VIEW IF EXISTS v_grid_contract_rates;

CREATE VIEW v_grid_contract_rates AS
SELECT
    cr.id,
    c.contract_number,
    cr.vehicle_type,
    cr.operated_rate_per_km_minor,
    cr.standing_rate_per_km_minor,
    cr.effective_from_date,
    cr.effective_to_date,
    (cr.effective_to_date IS NULL)           AS is_current,
    cr.is_active,
    COALESCE(am.full_name, ac.full_name)     AS last_changed_by,
    COALESCE(cr.modified_at, cr.created_at)  AS last_changed_at
FROM contract_rates cr
JOIN contracts c  ON c.id = cr.contract_id
LEFT JOIN accounts am ON am.id = cr.modified_by
LEFT JOIN accounts ac ON ac.id = cr.created_by;


DROP VIEW IF EXISTS v_form_c;
DROP VIEW IF EXISTS v_form_e_totals;
DROP VIEW IF EXISTS v_form_e;

CREATE VIEW v_form_e AS
WITH km_by_type AS (
    SELECT
        pc.id AS payment_certificate_id,
        COALESCE(v.vehicle_type, 'Standard bus') AS vehicle_type,
        COALESCE(SUM(op.operated_km) FILTER (WHERE NOT op.is_standing_trip), 0) AS operated_km_total,
        COALESCE(SUM(op.operated_km) FILTER (WHERE op.is_standing_trip), 0)     AS standing_km_total
    FROM payment_certificates pc
    JOIN contracts c        ON c.id = pc.contract_id
    JOIN routes r           ON r.contract_id = pc.contract_id AND r.is_active
    JOIN trips t            ON t.route_id = r.id AND t.is_active
                           AND t.scheduled_date >= pc.certificate_period
                           AND t.scheduled_date <  (pc.certificate_period + INTERVAL '1 month')::date
    JOIN trip_operations op ON op.trip_id = t.id AND op.is_active
    LEFT JOIN vehicles v    ON v.operator_id = c.operator_id
                           AND v.vehicle_number = op.vehicle_number
                           AND v.is_active
    WHERE pc.is_active
    GROUP BY pc.id, COALESCE(v.vehicle_type, 'Standard bus')
),
penalties AS (
    SELECT p.payment_certificate_id, SUM(p.effective_rand_amount_minor) AS approved_penalties_minor
    FROM penalty_captures p
    WHERE p.capture_status = 'Approved' AND p.is_active
    GROUP BY p.payment_certificate_id
)
SELECT
    pc.id                                   AS payment_certificate_id,
    pc.certificate_number,
    pc.certificate_period,
    km.vehicle_type,
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
      + ROUND(km.standing_km_total * cr.standing_rate_per_km_minor)::bigint
                                            AS line_amount_minor
FROM payment_certificates pc
JOIN km_by_type km ON km.payment_certificate_id = pc.id
JOIN contract_rates cr
    ON cr.contract_id = pc.contract_id
   AND cr.vehicle_type = km.vehicle_type
   AND cr.is_active
   AND cr.effective_from_date <= pc.certificate_period
   AND (cr.effective_to_date IS NULL OR cr.effective_to_date >= pc.certificate_period)
LEFT JOIN penalties pen ON pen.payment_certificate_id = pc.id
WHERE pc.is_active;

CREATE VIEW v_form_e_totals AS
SELECT
    e.payment_certificate_id,
    e.certificate_number,
    e.certificate_period,
    SUM(e.operated_amount_minor)::bigint AS operated_amount_minor,
    SUM(e.standing_amount_minor)::bigint AS standing_amount_minor,
    MAX(e.approved_penalties_minor)      AS approved_penalties_minor,
    (SUM(e.line_amount_minor) - MAX(e.approved_penalties_minor))::bigint
                                         AS amount_for_escalation_minor
FROM v_form_e e
GROUP BY e.payment_certificate_id, e.certificate_number, e.certificate_period;

CREATE VIEW v_form_c AS
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
                                       AS escalated_amount_minor
FROM payment_certificates pc
LEFT JOIN form_c_records fc
    ON fc.payment_certificate_id = pc.id AND fc.is_active
LEFT JOIN v_form_e_totals fe
    ON fe.payment_certificate_id = pc.id
LEFT JOIN escalation_records er
    ON er.payment_certificate_id = pc.id AND er.is_active
WHERE pc.is_active;

REVOKE INSERT, UPDATE ON vehicle_types FROM kubapc_app;
GRANT SELECT ON vehicle_types TO kubapc_app;
