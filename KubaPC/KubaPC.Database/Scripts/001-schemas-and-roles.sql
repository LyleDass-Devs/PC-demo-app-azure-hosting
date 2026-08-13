-- =========================================================================
--  001 - Schemas and database roles
--
--  Run once, as a superuser, against the kubapc database.
--  Safe to re-run: every statement is guarded.
-- =========================================================================

CREATE SCHEMA IF NOT EXISTS identity;
CREATE SCHEMA IF NOT EXISTS config;
CREATE SCHEMA IF NOT EXISTS ops;
CREATE SCHEMA IF NOT EXISTS cert;
CREATE SCHEMA IF NOT EXISTS audit;

COMMENT ON SCHEMA identity IS
    'Operators, contracts, user accounts, roles and access grants.';
COMMENT ON SCHEMA config IS
    'Administrator-only reference data. Rates, penalty percentages and escalation constants. Read access is granted separately from every other schema because these values decide how much money is paid.';
COMMENT ON SCHEMA ops IS
    'Imported reports. Tables ending _rows or _exports are faithful copies of a source file and must never be used to calculate money; use the derived tables instead.';
COMMENT ON SCHEMA cert IS
    'Payment certificates and their forms.';
COMMENT ON SCHEMA audit IS
    'Append-only record of who changed what.';


-- -------------------------------------------------------------------------
--  Application roles
--
--  The application never connects as a superuser. kubapc_app owns no objects
--  and can be revoked without affecting the schema.
-- -------------------------------------------------------------------------

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'kubapc_app') THEN
        -- Password is set separately so it never appears in a tracked file.
        CREATE ROLE kubapc_app LOGIN;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'kubapc_administrator') THEN
        CREATE ROLE kubapc_administrator NOLOGIN;
    END IF;
END
$$;

COMMENT ON ROLE kubapc_app IS
    'The role the web application connects as. Reads and writes everything except config, which it may only read.';
COMMENT ON ROLE kubapc_administrator IS
    'Granted to kubapc_app only where the deployment allows rates and penalty percentages to be edited in the application.';
