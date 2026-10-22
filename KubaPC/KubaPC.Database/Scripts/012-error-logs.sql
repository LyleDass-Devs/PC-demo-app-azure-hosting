CREATE TABLE IF NOT EXISTS error_logs (
    id                       uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    log_level                text NOT NULL,
    event_name               text,
    message                  text,
    exception_message        text,
    inner_exception_message  text,
    exception_stack_trace    text,
    exception_source         text,
    request_path             text,
    created_at               timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_error_logs_created_at ON error_logs (created_at);

GRANT INSERT, SELECT ON error_logs TO kubapc_app;
