-- =========================================================================
--  007 - Views: the forms as queries
--
--  The forms are mostly presentations of data that already exists. Where
--  the spec says a figure is "calculated live" until approval, that live
--  calculation is a view here; the approval snapshot tables (form_c_records,
--  form_a_records) freeze the same figures permanently.
--
--  CREATE OR REPLACE VIEW makes every statement safely re-runnable.
--
--  Run after 006. Safe to re-run.
-- =========================================================================

-- Form E (live): the Amount for Escalation.
-- Per A.13.1: operated kilometres at the operated rate, less approved
-- non-refundable penalties, plus standing kilometres at the standing rate.
-- The rate row picked is the one in force on the certificate period.
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

COMMENT ON VIEW v_form_e IS
    'The live Form E calculation (A.13.1). Approved formula-priced penalties with no rand amount contribute nothing to approved_penalties_minor - the spec''s own recorded gap. On approval these figures are frozen into form_c_records; this view keeps showing the live values.';


-- Form D (live): the escalation factor per certificate, with its inputs.
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

COMMENT ON VIEW v_form_d IS
    'Form D: per-certificate escalation inputs and the stored factor. The factor itself is calculated and written by the application (6 decimals, A.11), not by this view - a view recomputing it could silently disagree with the stored value.';


-- Form C: snapshot when approved, live otherwise (A.13, confirmed).
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

COMMENT ON VIEW v_form_c IS
    'Form C month row: the permanent snapshot for approved certificates, the live calculation otherwise. is_snapshot says which one you are looking at. The Accumulated Total column belongs to the service layer (a window over approved months), not this row-level view.';


-- Form G2: approved penalties grouped by code and capturing role (UC7).
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

COMMENT ON VIEW v_form_g2_summary IS
    'Form G2 upper section (UC7): approved penalties per certificate grouped by code, counted by capturing role. The refundable-amounts lower section is form_g2_refundable_amounts directly.';


-- Form B&H ledger with its certificate context (UC10).
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

COMMENT ON VIEW v_form_bh_ledger IS
    'Form B&H ledger rows with contract and certificate context. DELIBERATELY EXCLUDES budget_minor: the budget is visible to DOT alone (A.7), so it must never travel through the general-purpose view - the DOT screen queries the table directly under its own authorisation check.';
