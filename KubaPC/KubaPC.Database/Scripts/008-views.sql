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
JOIN contracts c ON c.id = pc.contract_id
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
JOIN contract_rates cr
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
JOIN contracts c ON c.id = pc.contract_id
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
                                       AS escalated_amount_minor
FROM payment_certificates pc
LEFT JOIN form_c_records fc
    ON fc.payment_certificate_id = pc.id AND fc.is_active
LEFT JOIN v_form_e fe
    ON fe.payment_certificate_id = pc.id
LEFT JOIN escalation_records er
    ON er.payment_certificate_id = pc.id AND er.is_active
WHERE pc.is_active;

CREATE OR REPLACE VIEW v_form_g2_summary AS
SELECT
    p.payment_certificate_id,
    p.penalty_code,
    pli.description                                            AS penalty_description,
    COUNT(*) FILTER (WHERE p.captured_by_role = 'Operator Staff') AS operator_staff_count,
    COUNT(*) FILTER (WHERE p.captured_by_role = 'Monitor')        AS monitor_count,
    COUNT(*)                                                      AS total_count,
    SUM(p.effective_rand_amount_minor)                            AS total_amount_minor
FROM penalty_captures p
JOIN penalty_list_items pli ON pli.id = p.penalty_list_item_id
WHERE p.capture_status = 'Approved'
  AND p.is_active
GROUP BY p.payment_certificate_id, p.penalty_code, pli.description;

CREATE OR REPLACE VIEW v_form_bh_ledger AS
SELECT
    c.operator_id,
    c.id                        AS contract_id,
    c.contract_number,
    pc.certificate_number,
    pc.certificate_status,
    bh.ledger_year,
    bh.ledger_month,
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

CREATE OR REPLACE VIEW v_grid_penalty_list_items AS
SELECT
    pli.id,
    c.contract_number,
    pli.penalty_code,
    pli.description,
    pli.penalty_type,
    pli.penalty_formula,
    pli.rand_amount_minor,
    pli.revenue_type,
    pli.repeat_penalty_escalation_enabled,
    pli.is_active,
    COALESCE(am.full_name, ac.full_name)      AS last_changed_by,
    COALESCE(pli.modified_at, pli.created_at) AS last_changed_at
FROM penalty_list_items pli
JOIN contracts c  ON c.id = pli.contract_id
LEFT JOIN accounts am ON am.id = pli.modified_by
LEFT JOIN accounts ac ON ac.id = pli.created_by;

CREATE OR REPLACE VIEW v_grid_contract_rates AS
SELECT
    cr.id,
    c.contract_number,
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

CREATE OR REPLACE VIEW v_grid_escalation_parameters AS
SELECT
    ep.id,
    c.contract_number,
    ep.labour_weight,
    ep.fuel_weight,
    ep.cpi_weight,
    ep.base_month,
    ep.base_year,
    ep.labour_base_value,
    ep.fuel_base_value,
    ep.cpi_base_value,
    ep.is_active,
    COALESCE(am.full_name, ac.full_name)     AS last_changed_by,
    COALESCE(ep.modified_at, ep.created_at)  AS last_changed_at
FROM escalation_parameters ep
JOIN contracts c  ON c.id = ep.contract_id
LEFT JOIN accounts am ON am.id = ep.modified_by
LEFT JOIN accounts ac ON ac.id = ep.created_by;

CREATE OR REPLACE VIEW v_grid_penalty_captures AS
SELECT
    p.id,
    p.payment_certificate_id,
    pc.certificate_number,
    p.penalty_code,
    pli.description                          AS penalty_description,
    r.route_number,
    p.captured_by_role,
    p.capture_status,
    p.repeat_escalation_applied,
    p.effective_rand_amount_minor,
    pli.rand_amount_minor                    AS catalogue_amount_minor,
    p.is_active,
    COALESCE(am.full_name, ac.full_name)     AS last_changed_by,
    COALESCE(p.modified_at, p.created_at)    AS last_changed_at
FROM penalty_captures p
JOIN payment_certificates pc  ON pc.id = p.payment_certificate_id
JOIN penalty_list_items pli   ON pli.id = p.penalty_list_item_id
JOIN routes r                 ON r.id = p.route_id
LEFT JOIN accounts am ON am.id = p.modified_by
LEFT JOIN accounts ac ON ac.id = p.created_by
WHERE p.is_active;

CREATE OR REPLACE VIEW v_grid_form_g2_amounts AS
SELECT
    g2.id,
    g2.payment_certificate_id,
    pc.certificate_number,
    g2.refundable_penalty_type,
    g2.sub_refundable_penalty_type,
    g2.rand_amount_minor,
    g2.is_active,
    COALESCE(am.full_name, ac.full_name)     AS last_changed_by,
    COALESCE(g2.modified_at, g2.created_at)  AS last_changed_at
FROM form_g2_refundable_amounts g2
JOIN payment_certificates pc ON pc.id = g2.payment_certificate_id
LEFT JOIN accounts am ON am.id = g2.modified_by
LEFT JOIN accounts ac ON ac.id = g2.created_by
WHERE g2.is_active;
