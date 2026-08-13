-- =========================================================================
--  009 - Audit log and permissions
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
    'A.30. Every Create/Update/Delete across every module writes here - confirmed as a blanket rule. Append-only: the application may insert, never update or delete. A Delete action means a soft delete (is_active set FALSE); no table grants physical DELETE.';
COMMENT ON COLUMN audit_logs.action_reason IS
    'A.30 Reason (the shared-table change this module introduced): mandatory when the action is an Update or Delete by Operator Staff or Monitor on a Form F1 record or penalty capture; optional elsewhere. Application-enforced.';


-- -------------------------------------------------------------------------
--  Permissions
--
--  kubapc_app (ordinary connection):
--    read, insert, update everywhere EXCEPT the six money catalogues,
--    which are read-only; audit_logs insert-only. NO DELETE anywhere.
--
--    Owner's decision, recorded as a deviation from the spec: UC4 4b and
--    UC8 4b say a Submitted penalty or F1 record is "permanently removed".
--    Here deletion is soft (is_active = FALSE) instead. Every observable
--    spec behaviour is preserved - the record disappears from screens, the
--    audit log gets its Delete entry, and the trip/code uniqueness indexes
--    filter on is_active so the combination is freed for a new record -
--    but the row itself survives as evidence.
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

-- The reference lists (script 003) carry the specification's fixed values.
-- Nobody writes them at runtime - not even the elevated role. A new value
-- is a new script, through review.
REVOKE INSERT, UPDATE ON certificate_statuses,
                        trip_completion_statuses,
                        revenue_types,
                        penalty_types,
                        captured_by_roles,
                        review_statuses,
                        refundable_penalty_types,
                        sub_refundable_penalty_types,
                        form_f1_record_types,
                        url_keys
    FROM kubapc_app;

-- Audit log: append-only. Rescind the blanket UPDATE granted above.
REVOKE UPDATE ON audit_logs FROM kubapc_app;

ALTER DEFAULT PRIVILEGES IN SCHEMA public
    GRANT SELECT, INSERT, UPDATE ON TABLES TO kubapc_app;
