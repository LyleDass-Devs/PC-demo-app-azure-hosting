-- ============================================================================
--  014 - Forced password change on first login
--
--  Admin-created accounts receive a password chosen by the Admin. Until the
--  owner replaces it with one only they know, the account must not reach the
--  application: login succeeds but every endpoint except change-password
--  refuses the token (enforced in the API by the PasswordChangeRequired
--  claim).
--
--  Existing accounts keep false: their owners already chose their passwords.
--
--  Run after 013. Safe to re-run.
-- ============================================================================

ALTER TABLE accounts
    ADD COLUMN IF NOT EXISTS must_change_password boolean NOT NULL DEFAULT FALSE;

COMMENT ON COLUMN accounts.must_change_password IS
    'True while the account still uses a password set by an Admin (initial creation or reset). The API refuses every endpoint except change-password until the owner sets their own password.';
