COMMENT ON TABLE vehicle_types IS NULL;
COMMENT ON COLUMN vehicles.vehicle_type IS NULL;
COMMENT ON COLUMN contract_rates.vehicle_type IS NULL;
COMMENT ON VIEW v_grid_contract_rates IS NULL;

INSERT INTO vehicle_types (vehicle_type_value, display_order) VALUES
    ('Bus',       1),
    ('Train-Bus', 2),
    ('Taxi',      3),
    ('Other',     4)
ON CONFLICT (vehicle_type_value) DO NOTHING;

UPDATE contract_rates SET vehicle_type = CASE vehicle_type
        WHEN 'Standard bus' THEN 'Bus'
        WHEN 'Train bus'    THEN 'Train-Bus'
        WHEN 'Maxi bus'     THEN 'Other'
    END
WHERE vehicle_type IN ('Standard bus', 'Train bus', 'Maxi bus');

UPDATE vehicles SET vehicle_type = CASE vehicle_type
        WHEN 'Standard bus' THEN 'Bus'
        WHEN 'Train bus'    THEN 'Train-Bus'
        WHEN 'Maxi bus'     THEN 'Other'
    END
WHERE vehicle_type IN ('Standard bus', 'Train bus', 'Maxi bus');

ALTER TABLE vehicles ALTER COLUMN vehicle_type SET DEFAULT 'Bus';

DELETE FROM vehicle_types WHERE vehicle_type_value IN ('Standard bus', 'Train bus', 'Maxi bus');

UPDATE vehicle_types SET display_order = 1 WHERE vehicle_type_value = 'Bus';
UPDATE vehicle_types SET display_order = 2 WHERE vehicle_type_value = 'Train-Bus';
UPDATE vehicle_types SET display_order = 3 WHERE vehicle_type_value = 'Taxi';
UPDATE vehicle_types SET display_order = 4 WHERE vehicle_type_value = 'Other';


CREATE TABLE IF NOT EXISTS operator_vehicle_types (
    id                 uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    operator_id        uuid NOT NULL REFERENCES operators (id),
    vehicle_type_value text NOT NULL REFERENCES vehicle_types (vehicle_type_value),
    is_enabled         boolean NOT NULL DEFAULT TRUE,
    is_active          boolean     NOT NULL DEFAULT TRUE,
    created_at         timestamptz NOT NULL DEFAULT now(),
    created_by         uuid,
    modified_at        timestamptz,
    modified_by        uuid
);

CREATE UNIQUE INDEX IF NOT EXISTS ux_operator_vehicle_types_operator_type
    ON operator_vehicle_types (operator_id, vehicle_type_value) WHERE is_active;


INSERT INTO operator_vehicle_types (operator_id, vehicle_type_value)
SELECT o.id, t.vehicle_type_value
FROM operators o
CROSS JOIN vehicle_types t
WHERE o.is_active
  AND NOT EXISTS (
      SELECT 1 FROM operator_vehicle_types existing
      WHERE existing.operator_id = o.id
        AND existing.vehicle_type_value = t.vehicle_type_value
        AND existing.is_active
  );

GRANT SELECT, INSERT, UPDATE ON operator_vehicle_types TO kubapc_app;


ALTER TABLE penalty_captures ALTER COLUMN payment_certificate_id DROP NOT NULL;

CREATE INDEX IF NOT EXISTS ix_penalty_captures_unattached
    ON penalty_captures (trip_id) WHERE payment_certificate_id IS NULL AND is_active;


DROP VIEW IF EXISTS v_form_c;
DROP VIEW IF EXISTS v_form_e_totals;
DROP VIEW IF EXISTS v_form_e;

CREATE VIEW v_form_e AS
WITH km_by_type AS (
    SELECT
        pc.id AS payment_certificate_id,
        COALESCE(v.vehicle_type, 'Bus') AS vehicle_type,
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
    GROUP BY pc.id, COALESCE(v.vehicle_type, 'Bus')
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
