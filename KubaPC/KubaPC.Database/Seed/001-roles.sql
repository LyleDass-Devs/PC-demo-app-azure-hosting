-- =========================================================================
--  Seed - application roles
--
--  Safe to re-run: inserts only what is missing.
-- =========================================================================

INSERT INTO identity.roles (role_name, role_description, is_administrator_only)
SELECT v.role_name, v.role_description, v.is_administrator_only
FROM (VALUES
    ('Capturer',      'Enters certificate data and penalties',                    FALSE),
    ('Verifier',      'Checks a captured certificate. May not verify own work',   FALSE),
    ('Authoriser',    'Approves a verified certificate for payment',              FALSE),
    ('Administrator', 'Manages rates, penalty codes, escalation and accounts',    TRUE)
) AS v (role_name, role_description, is_administrator_only)
WHERE NOT EXISTS (
    SELECT 1 FROM identity.roles r
    WHERE r.role_name = v.role_name AND r.is_active
);
