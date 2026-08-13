-- =========================================================================
--  002 - identity
--
--  Operators, contracts, accounts, roles and access grants.
--  Run after 001. Safe to re-run.
-- =========================================================================

CREATE TABLE IF NOT EXISTS identity.operators (
    id             uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    operator_code  text NOT NULL,
    operator_name  text NOT NULL,
    is_active      boolean     NOT NULL DEFAULT TRUE,
    created_at     timestamptz NOT NULL DEFAULT now(),
    created_by     uuid,
    modified_at    timestamptz,
    modified_by    uuid
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_operators_operator_code
    ON identity.operators (operator_code) WHERE is_active;

COMMENT ON TABLE identity.operators IS
    'The bus operating company that is paid. Called the societe in the SAEIV exports.';
COMMENT ON COLUMN identity.operators.operator_code IS
    'The short code the operator is known by in the import files.';


CREATE TABLE IF NOT EXISTS identity.contracts (
    id                 uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    operator_id        uuid NOT NULL REFERENCES identity.operators (id),
    contract_number    text NOT NULL,
    contract_name      text,
    commencement_date  date NOT NULL,
    expiry_date        date,
    is_active          boolean     NOT NULL DEFAULT TRUE,
    created_at         timestamptz NOT NULL DEFAULT now(),
    created_by         uuid,
    modified_at        timestamptz,
    modified_by        uuid
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_contracts_contract_number
    ON identity.contracts (contract_number) WHERE is_active;
CREATE INDEX IF NOT EXISTS ix_contracts_operator
    ON identity.contracts (operator_id);

COMMENT ON COLUMN identity.contracts.contract_number IS
    'The public contract reference, for example ZNT 1129 or 22/98T. Appears on every certificate and in every import file.';


CREATE TABLE IF NOT EXISTS identity.accounts (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    email_address       text NOT NULL,
    full_name           text NOT NULL,
    windows_login_name  text,
    is_active           boolean     NOT NULL DEFAULT TRUE,
    created_at          timestamptz NOT NULL DEFAULT now(),
    created_by          uuid,
    modified_at         timestamptz,
    modified_by         uuid
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_accounts_email_address
    ON identity.accounts (lower(email_address)) WHERE is_active;

COMMENT ON COLUMN identity.accounts.email_address IS
    'Unique case-insensitively. Two accounts differing only in capitalisation are the same person.';
COMMENT ON COLUMN identity.accounts.windows_login_name IS
    'Domain login, populated when the site authenticates against Active Directory rather than by password.';


CREATE TABLE IF NOT EXISTS identity.roles (
    id                     uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    role_name              text NOT NULL,
    role_description       text,
    is_administrator_only  boolean NOT NULL DEFAULT FALSE,
    is_active              boolean     NOT NULL DEFAULT TRUE,
    created_at             timestamptz NOT NULL DEFAULT now(),
    created_by             uuid,
    modified_at            timestamptz,
    modified_by            uuid
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_roles_role_name
    ON identity.roles (role_name) WHERE is_active;

COMMENT ON COLUMN identity.roles.is_administrator_only IS
    'Marks a role that may change rates, penalty percentages and escalation constants. Separated because those values decide how much money is paid.';


CREATE TABLE IF NOT EXISTS identity.account_roles (
    id           uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id   uuid NOT NULL REFERENCES identity.accounts (id),
    role_id      uuid NOT NULL REFERENCES identity.roles (id),
    is_active    boolean     NOT NULL DEFAULT TRUE,
    created_at   timestamptz NOT NULL DEFAULT now(),
    created_by   uuid,
    modified_at  timestamptz,
    modified_by  uuid
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_account_roles_account_role
    ON identity.account_roles (account_id, role_id) WHERE is_active;

COMMENT ON TABLE identity.account_roles IS
    'What a person may do.';


CREATE TABLE IF NOT EXISTS identity.account_operators (
    id           uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id   uuid NOT NULL REFERENCES identity.accounts (id),
    operator_id  uuid NOT NULL REFERENCES identity.operators (id),
    is_active    boolean     NOT NULL DEFAULT TRUE,
    created_at   timestamptz NOT NULL DEFAULT now(),
    created_by   uuid,
    modified_at  timestamptz,
    modified_by  uuid
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_account_operators_account_operator
    ON identity.account_operators (account_id, operator_id) WHERE is_active;

COMMENT ON TABLE identity.account_operators IS
    'Which operators a person may see. A role says what someone may do; this says whose money they may do it to. Every certificate, penalty and financial query must filter through this table.';
