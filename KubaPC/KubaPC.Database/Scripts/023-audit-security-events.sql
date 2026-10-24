ALTER TABLE audit_logs DROP CONSTRAINT IF EXISTS ck_audit_logs_action;

ALTER TABLE audit_logs
    ADD CONSTRAINT ck_audit_logs_action
        CHECK (action_name IN ('Create', 'Update', 'Delete', 'Security'));
