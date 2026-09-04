-- ============================================================================
--  012 - Error logs: persistent store for application exceptions
--
--  Written by the API's database logger (all ILogger output at Error and
--  above) and by explicit LoggingHelper calls in services and endpoints.
--  Append-only from the application: kubapc_app gets INSERT and SELECT,
--  never UPDATE or DELETE - an error log nobody can rewrite.
--
--  Run after 011. Safe to re-run.
-- ============================================================================

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

COMMENT ON TABLE  error_logs IS 'Application exception log written by the API database logger and LoggingHelper. Append-only for the application role.';
COMMENT ON COLUMN error_logs.log_level IS 'Microsoft.Extensions.Logging level name (Error, Critical, ...).';
COMMENT ON COLUMN error_logs.event_name IS 'Logger event name when set, otherwise the logger category (usually the class that logged).';
COMMENT ON COLUMN error_logs.message IS 'Formatted log message as produced by the logging call.';
COMMENT ON COLUMN error_logs.exception_message IS 'Exception.Message of the logged exception, when one was attached.';
COMMENT ON COLUMN error_logs.inner_exception_message IS 'Message of the innermost wrapped exception, when present.';
COMMENT ON COLUMN error_logs.exception_stack_trace IS 'Full stack trace of the logged exception.';
COMMENT ON COLUMN error_logs.exception_source IS 'Assembly or component the exception originated from (Exception.Source).';
COMMENT ON COLUMN error_logs.request_path IS 'HTTP path being handled when the error was logged, when known.';

CREATE INDEX IF NOT EXISTS ix_error_logs_created_at ON error_logs (created_at);

GRANT INSERT, SELECT ON error_logs TO kubapc_app;
