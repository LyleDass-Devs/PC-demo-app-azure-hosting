ALTER TABLE accounts
    ADD COLUMN IF NOT EXISTS token_version integer NOT NULL DEFAULT 1,
    ADD COLUMN IF NOT EXISTS failed_login_count integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS locked_until timestamptz,
    ADD COLUMN IF NOT EXISTS temporary_password_expires_at timestamptz;

ALTER TABLE accounts DROP COLUMN IF EXISTS windows_login_name;

REVOKE UPDATE ON error_logs FROM kubapc_app;

ALTER DEFAULT PRIVILEGES IN SCHEMA public REVOKE INSERT, UPDATE ON TABLES FROM kubapc_app;

REVOKE CREATE ON SCHEMA public FROM PUBLIC;
