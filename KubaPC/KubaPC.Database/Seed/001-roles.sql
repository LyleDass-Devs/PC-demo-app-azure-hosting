-- =========================================================================
--  Seed - the six application roles, exactly as the specification names
--  them (spec User Roles section; KubaOps A.26.1).
--
--  "Operator Admin" is deliberately absent: the spec is explicit that it is
--  not a seventh role but Admin in its capacity as scoped to particular
--  operators via account_operators (spec Terminology; Form G1 RBAC note).
--
--  Safe to re-run: inserts only what is missing.
-- =========================================================================

INSERT INTO roles (role_name, role_description)
SELECT v.role_name, v.role_description
FROM (VALUES
    ('SuperAdmin',
     'Platform governance only in this module: maintains the reference URLs (A.10) and DNO reasons (A.14). No certificate workflow involvement.'),
    ('Admin',
     'Operator-scoped administration: grants Authorisers access to operators (UC23), maintains the penalty list (UC3), escalation parameters (UC15), and income field configuration (A.16). Read-only over the certificate workflow itself. Called "Operator Admin" when acting for a specific operator.'),
    ('Operator Staff',
     'The operator''s own representative: creates and submits certificates (UC1), captures operator penalties (UC4) and Form F1 records (UC8), captures current-month Form B&H operational figures (UC11).'),
    ('Monitor',
     'Independent oversight: captures monitor penalties (UC5) and Form F1 records (UC8), captures monthly escalation rates (UC16). May edit or delete ANY submitted penalty or F1 record, including Operator Staff''s - the asymmetric rule. Read-only elsewhere.'),
    ('Authoriser',
     'Reviews and approves or rejects certificates (UC2), penalties (UC6), and Form F1 records (UC9); generates Form G2 (UC7); captures escalation rates (UC16) and Form B&H financial figures (UC12). Scoped to operators granted by an Admin.'),
    ('DOT',
     'Department of Transport: read-only oversight, plus sole visibility and control of the Form B&H Budget field and the Bus Restructuring Fund contribution (UC12).')
) AS v (role_name, role_description)
WHERE NOT EXISTS (
    SELECT 1 FROM roles r WHERE r.role_name = v.role_name AND r.is_active
);
