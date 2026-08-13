-- =========================================================================
--  007 - audit schema and permissions
--
--  Run last, as a superuser. Safe to re-run.
-- =========================================================================

CREATE TABLE IF NOT EXISTS audit.audit_logs (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    table_schema  text NOT NULL,
    table_name    text NOT NULL,
    record_id     uuid NOT NULL,
    action_name   text NOT NULL,
    changed_by    uuid,
    changed_at    timestamptz NOT NULL DEFAULT now(),
    old_values    jsonb,
    new_values    jsonb,
    is_active     boolean     NOT NULL DEFAULT TRUE,
    created_at    timestamptz NOT NULL DEFAULT now(),
    created_by    uuid,
    modified_at   timestamptz,
    modified_by   uuid
);
CREATE INDEX IF NOT EXISTS ix_audit_logs_table_record
    ON audit.audit_logs (table_schema, table_name, record_id);
CREATE INDEX IF NOT EXISTS ix_audit_logs_changed_at
    ON audit.audit_logs (changed_at);

COMMENT ON TABLE audit.audit_logs IS
    'Append-only. Nothing in the application updates or deletes a row here. The is_active column exists only because the project convention requires it on every table.';
COMMENT ON COLUMN audit.audit_logs.action_name IS
    'Insert, Update or Delete, where Delete means a soft delete.';


-- -------------------------------------------------------------------------
--  Permissions
--
--  The application role may read and write the working schemas, but may only
--  read config. Changing a rate is an administrative act performed under a
--  separate role, because a wrong rate silently changes every certificate.
--
--  ALTER DEFAULT PRIVILEGES matters as much as the GRANT: without it, a table
--  added by a later migration has no permissions and the application fails at
--  runtime, on that table only, in a way that is easy to miss in testing.
-- -------------------------------------------------------------------------

GRANT USAGE ON SCHEMA identity, config, ops, cert, audit TO kubapc_app;

GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA identity TO kubapc_app;
GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA ops      TO kubapc_app;
GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA cert     TO kubapc_app;

-- Read-only. Rates and penalty percentages are not editable by the application.
GRANT SELECT ON ALL TABLES IN SCHEMA config TO kubapc_app;

-- Append-only. No UPDATE, no DELETE: an audit row that can be changed is not an audit row.
GRANT SELECT, INSERT ON ALL TABLES IN SCHEMA audit TO kubapc_app;

-- The same rules, applied automatically to tables created later.
ALTER DEFAULT PRIVILEGES IN SCHEMA identity
    GRANT SELECT, INSERT, UPDATE ON TABLES TO kubapc_app;
ALTER DEFAULT PRIVILEGES IN SCHEMA ops
    GRANT SELECT, INSERT, UPDATE ON TABLES TO kubapc_app;
ALTER DEFAULT PRIVILEGES IN SCHEMA cert
    GRANT SELECT, INSERT, UPDATE ON TABLES TO kubapc_app;
ALTER DEFAULT PRIVILEGES IN SCHEMA config
    GRANT SELECT ON TABLES TO kubapc_app;
ALTER DEFAULT PRIVILEGES IN SCHEMA audit
    GRANT SELECT, INSERT ON TABLES TO kubapc_app;

-- The administrator role adds write access to config, and nothing else.
GRANT USAGE ON SCHEMA config TO kubapc_administrator;
GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA config TO kubapc_administrator;
ALTER DEFAULT PRIVILEGES IN SCHEMA config
    GRANT SELECT, INSERT, UPDATE ON TABLES TO kubapc_administrator;

-- No DELETE anywhere. Deletion is setting is_active to false.
