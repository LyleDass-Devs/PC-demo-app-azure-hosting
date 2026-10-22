ALTER TABLE accounts
    ADD COLUMN IF NOT EXISTS must_change_password boolean NOT NULL DEFAULT FALSE;
