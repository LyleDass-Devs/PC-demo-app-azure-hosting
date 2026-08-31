-- =========================================================================
--  011 - Authentication columns
--
--  Adds local-password login support to accounts. The application stores a
--  BCrypt hash (BCrypt.Net-Next on the C# side); the salt and cost factor
--  live inside the hash string itself, so one column is enough.
--
--  No users are seeded here. The first super user is inserted manually in
--  pgAdmin (template in docs/guides/07-authentication.md) using pgcrypto's
--  crypt(..., gen_salt('bf', 12)), which produces a BCrypt hash the C#
--  verifier accepts.
--
--  Run as postgres, after 010. Safe to re-run: every statement is guarded.
--  No grant changes needed: kubapc_app already holds table-level
--  SELECT/INSERT/UPDATE on accounts (009), which covers the new columns.
-- =========================================================================

-- Provides crypt() and gen_salt('bf') for the manual super-user insert and
-- any future SQL-side password work. Harmless if already installed.
CREATE EXTENSION IF NOT EXISTS pgcrypto;

ALTER TABLE accounts
    ADD COLUMN IF NOT EXISTS password_hash text,
    ADD COLUMN IF NOT EXISTS password_changed_at timestamptz,
    ADD COLUMN IF NOT EXISTS last_login_at timestamptz;

COMMENT ON COLUMN accounts.password_hash IS
    'BCrypt hash of the account password (modular crypt format: version, cost, salt and hash in one string, e.g. $2a$12$...). NULL means the account cannot log in with a local password. Never store a plain-text or reversibly encrypted password here.';

COMMENT ON COLUMN accounts.password_changed_at IS
    'When the password was last set or changed. Set by the application whenever password_hash changes.';

COMMENT ON COLUMN accounts.last_login_at IS
    'When the account last authenticated successfully. Stamped by the login service after each successful login.';
