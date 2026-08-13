-- =========================================================================
--  001 - Database roles
--
--  Target database: "KubaPC" (already created; case-sensitive name).
--  Run as postgres. Safe to re-run.
--
--  Everything lives in the public schema. Two database roles:
--
--    kubapc_app            The application's ordinary connection.
--    kubapc_administrator  Write access to the six money catalogues
--                          (penalty_list_items, contract_rates,
--                          escalation_parameters, reference_urls,
--                          dno_reasons, income_field_configurations).
--
--  kubapc_app is NOINHERIT: it holds membership of kubapc_administrator but
--  none of its privileges until it explicitly elevates with
--  SET LOCAL ROLE kubapc_administrator inside a transaction. Only the
--  application's administrative services do that, so an injection flaw in an
--  ordinary endpoint cannot rewrite a rate or a penalty amount.
-- =========================================================================

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'kubapc_app') THEN
        -- Password is set manually afterwards, never in a tracked file.
        CREATE ROLE kubapc_app LOGIN NOINHERIT;
    ELSE
        -- Re-runs must still guarantee NOINHERIT; elevation being explicit
        -- is the entire point of the two-role design.
        ALTER ROLE kubapc_app NOINHERIT;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'kubapc_administrator') THEN
        CREATE ROLE kubapc_administrator NOLOGIN;
    END IF;
END
$$;

-- Membership without inheritance: the application may elevate, but holds no
-- catalogue write privilege until it executes SET LOCAL ROLE inside a
-- transaction. SET LOCAL reverts when the transaction ends, which makes it
-- safe under connection pooling.
GRANT kubapc_administrator TO kubapc_app;

COMMENT ON ROLE kubapc_app IS
    'The role the application connects as. NOINHERIT: money-catalogue writes require SET LOCAL ROLE kubapc_administrator inside a transaction.';
COMMENT ON ROLE kubapc_administrator IS
    'Write access to the money catalogues. Assumed per-transaction by the application''s administrative services only.';
