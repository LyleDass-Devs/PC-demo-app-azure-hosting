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

GRANT USAGE ON SCHEMA public TO kubapc_app;
GRANT USAGE ON SCHEMA public TO kubapc_administrator;

GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA public TO kubapc_app;

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

REVOKE UPDATE ON audit_logs FROM kubapc_app;

ALTER DEFAULT PRIVILEGES IN SCHEMA public
    GRANT SELECT, INSERT, UPDATE ON TABLES TO kubapc_app;
