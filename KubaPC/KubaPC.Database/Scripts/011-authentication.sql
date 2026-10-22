CREATE EXTENSION IF NOT EXISTS pgcrypto;

ALTER TABLE accounts
    ADD COLUMN IF NOT EXISTS password_hash text,
    ADD COLUMN IF NOT EXISTS password_changed_at timestamptz,
    ADD COLUMN IF NOT EXISTS last_login_at timestamptz;
