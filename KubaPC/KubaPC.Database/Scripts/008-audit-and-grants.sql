-- =========================================================================
--  008 - Audit log and permissions
--
--  Run last, as postgres. Safe to re-run.
-- =========================================================================

-- A.30 Audit Log, including the Reason field added to the shared table for
-- this module (spec: "SHARED TABLE CHANGE MADE - CLIENT NOTIFIED").
CREATE TABLE IF NOT EXISTS audit_logs (
    id             uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    table_name     text NOT NULL,
    record_id      uuid NOT NULL,
    action_name    text NOT NULL,
    action_reason  text,
    changed_by     uuid,
    changed_at     timestamptz NOT NULL DEFAULT now(),
    old_values     jsonb,
    new_values     jsonb,
    is_active      boolean     NOT NULL DEFAULT TRUE,
    created_at     timestamptz NOT NULL DEFAULT now(),
    created_by     uuid,
    modified_at    timestamptz,
    modified_by    uuid,

    CONSTRAINT ck_audit_logs_action
        CHECK (action_name IN ('Create', 'Update', 'Delete'))
);
CREATE INDEX IF NOT EXISTS ix_audit_logs_table_record
    ON audit_logs (table_name, record_id);
CREATE INDEX IF NOT EXISTS ix_audit_logs_changed_at
    ON audit_logs (changed_at);

COMMENT ON TABLE audit_logs IS
    'A.30. Every Create/Update/Delete across every module writes here - confirmed as a blanket rule. Append-only: the application may insert, never update or delete. For the hard deletes the spec requires on penalty_captures and form_f1_records, the Delete entry with old_values IS the surviving record.';
COMMENT ON COLUMN audit_logs.action_reason IS
    'A.30 Reason (the shared-table change this module introduced): mandatory when the action is an Update or Delete by Operator Staff or Monitor on a Form F1 record or penalty capture; optional elsewhere. Application-enforced.';


-- -------------------------------------------------------------------------
--  Permissions
--
--  kubapc_app (ordinary connection):
--    read, insert, update everywhere EXCEPT the six money catalogues,
--    which are read-only; DELETE on exactly the two tables where the spec
--    requires physical deletion (penalty_captures, form_f1_records - UC4
--    4b, UC8 4b); audit_logs insert-only.
--
--  kubapc_administrator (assumed per-transaction via SET LOCAL ROLE):
--    read, insert, update on the six money catalogues. Still no delete -
--    catalogue rows are deactivated (Is_Active = FALSE, UC3 4a), never
--    removed.
--
--  Single-schema note: with everything in public, the money catalogues are
--  protected per table rather than per schema. A NEW money table added by a
--  later script gets kubapc_app write access from the default privileges
--  below UNTIL its own revoke/grant lines are added - adding those two
--  lines is part of adding any new money table. The per-schema version
--  failed closed here; this is the trade for one schema.
-- -------------------------------------------------------------------------

GRANT USAGE ON SCHEMA public TO kubapc_app;
GRANT USAGE ON SCHEMA public TO kubapc_administrator;

GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA public TO kubapc_app;

-- The six money catalogues: read-only for the ordinary connection,
-- writable only under the elevated role.
REVOKE INSERT, UPDATE ON penalty_list_items,
                        contract_rates,
                        escalation_parameters,
                        reference_urls,
                        dno_reasons,
                        income_field_configurations
    FROM kubapc_app;
GRANT SELECT, INSERT, UPDATE ON penalty_list_items,
                                contract_rates,
                                escalation_parameters,
                                reference_urls,
                                dno_reasons,
                                income_field_configurations
    TO kubapc_administrator;

-- The spec's two hard-delete tables, and nothing else.
GRANT DELETE ON penalty_captures TO kubapc_app;
GRANT DELETE ON form_f1_records TO kubapc_app;

-- Audit log: append-only. Rescind the blanket UPDATE granted above.
REVOKE UPDATE ON audit_logs FROM kubapc_app;

ALTER DEFAULT PRIVILEGES IN SCHEMA public
    GRANT SELECT, INSERT, UPDATE ON TABLES TO kubapc_app;
