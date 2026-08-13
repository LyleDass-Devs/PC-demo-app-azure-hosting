-- =========================================================================
--  Seed - the known operator, contract, rates, and the example penalty
--  catalogue.
--
--  Sources, and how much to trust each:
--    Operator and contract      1129.xls timetable - real client data.
--    Contract km rates          The ZNT 1129 certificate workbook, Form E:
--                               R8.21 operated, R4.93 standing - real, but
--                               undated; effective_from_date is set to the
--                               contract commencement placeholder below and
--                               must be corrected when the real history is
--                               known.
--    Penalty catalogue          The specification's A.3.0 example table,
--                               which the spec itself calls "illustrative
--                               sample data... not a client-confirmed final
--                               catalogue". Seeded so screens have real
--                               shapes to build against; every amount needs
--                               confirmation before production.
--
--  Safe to re-run: inserts only what is missing.
-- =========================================================================

-- The operator, from the 1129.xls timetable header.
INSERT INTO operators (operator_code, operator_name)
SELECT 'RIPPLE40', 'RIPPLE EFFECT 40 (PTY) LTD'
WHERE NOT EXISTS (
    SELECT 1 FROM operators WHERE operator_code = 'RIPPLE40' AND is_active
);

-- The contract. Commencement date is a PLACEHOLDER: the real date is not in
-- any document provided (the escalation base month of September 1998 in the
-- workbook suggests the contract's era, but that is inference, not fact).
INSERT INTO contracts (operator_id, contract_number, contract_name, commencement_date)
SELECT o.id, 'ZNT 1129', 'ZNT 1129 subsidised service contract', DATE '1998-09-01'
FROM operators o
WHERE o.operator_code = 'RIPPLE40' AND o.is_active
  AND NOT EXISTS (
      SELECT 1 FROM contracts WHERE contract_number = 'ZNT 1129' AND is_active
  );

-- The kilometre rates from the certificate workbook's Form E.
INSERT INTO contract_rates
    (contract_id, operated_rate_per_km_minor, standing_rate_per_km_minor, effective_from_date)
SELECT c.id, 821, 493, c.commencement_date
FROM contracts c
WHERE c.contract_number = 'ZNT 1129' AND c.is_active
  AND NOT EXISTS (
      SELECT 1 FROM contract_rates r WHERE r.contract_id = c.id AND r.is_active
  );

-- The penalty catalogue, spec A.3.0 verbatim. Where the sample leaves the
-- Rand amount blank, the penalty is priced by its free-text formula alone
-- (which the system deliberately does not evaluate - A.3). Code 10 is a
-- parent heading in the sample with neither amount nor formula; it is given
-- an explanatory formula text here so the at-least-one-price constraint
-- holds, and its sub-codes 10.1-10.4 carry the actual amounts.
INSERT INTO penalty_list_items
    (contract_id, penalty_code, description, penalty_formula,
     rand_amount_minor, revenue_type, penalty_type)
SELECT c.id, v.penalty_code, v.description, v.penalty_formula,
       v.rand_amount_minor, v.revenue_type, v.penalty_type
FROM contracts c
CROSS JOIN (VALUES
    ('1',    'Trip failed to Operate',
             '50% or minimum of R500 & no revenue kms', NULL, 'Non-Revenue', 'Trips Not Operated'),
    ('1A',   'Trip failed to Operate - Easter',
             NULL, 400000::bigint, 'Non-Revenue', 'Trips Not Operated'),
    ('1B',   'Trip failed to Operate - Private Hire',
             NULL, 400000::bigint, 'Non-Revenue', 'Trips Not Operated'),
    ('2',    'Trip operated 10-30 min late',
             '30% or minimum of R300', NULL, NULL, 'Time-Related Offences'),
    ('4',    'Trip operated more than 30 min late',
             'Treated as not operated', NULL, 'Non-Revenue', 'Time-Related Offences'),
    ('5',    'Trip operated early',
             '30% or minimum of R300', NULL, NULL, 'Time-Related Offences'),
    ('6',    'Vehicle breakdowns and late replacements',
             '50% or minimum of R500, if no vehicle provided / no revenue kms', NULL, NULL, 'Time-Related Offences'),
    ('8',    'Duty not displayed or displayed incorrectly',
             NULL, 40000::bigint, NULL, 'Duty & Destination Boards'),
    ('9',    'Vehicle non-compliant or expired CRW',
             '50% or minimum of R500 & no revenue kms', NULL, NULL, 'Unroadworthy'),
    ('10',   'Vehicle in an unsatisfactory condition',
             'Priced per sub-code 10.1 to 10.4', NULL, NULL, 'Unsatisfactory Vehicle Condition'),
    ('10.1', 'Vehicle dirty inside / outside',
             'R150 per bus', 15000::bigint, NULL, 'Unsatisfactory Vehicle Condition'),
    ('10.2', 'Vehicle not in a good state of repair',
             'R300 per bus', 30000::bigint, NULL, 'Unsatisfactory Vehicle Condition'),
    ('10.3', 'Missing or broken seats',
             'R300 per bus', 30000::bigint, NULL, 'Unsatisfactory Vehicle Condition'),
    ('10.4', 'Vehicle with missing or broken doors',
             'R300 per bus', 30000::bigint, NULL, 'Unsatisfactory Vehicle Condition'),
    ('11',   'Route deviation',
             'No revenue kms', NULL, 'Non-Revenue', 'Stop & Route Adherence'),
    ('12',   'Operating without headlights',
             NULL, 10000::bigint, NULL, 'Unsatisfactory Vehicle Condition'),
    ('13',   'Failing to pick up or set down passengers',
             'R150 per incident', 15000::bigint, NULL, 'Stop & Route Adherence'),
    ('14',   'Failure to obtain approval for advertising boards',
             'R500 per bus per day', 50000::bigint, NULL, 'Duty & Destination Boards')
) AS v (penalty_code, description, penalty_formula, rand_amount_minor, revenue_type, penalty_type)
WHERE c.contract_number = 'ZNT 1129' AND c.is_active
  AND NOT EXISTS (
      SELECT 1 FROM penalty_list_items p
      WHERE p.contract_id = c.id AND p.penalty_code = v.penalty_code AND p.is_active
  );
