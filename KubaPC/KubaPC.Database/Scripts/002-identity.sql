CREATE TABLE IF NOT EXISTS operators (
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
    ON operators (operator_code) WHERE is_active;

CREATE TABLE IF NOT EXISTS accounts (
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
    ON accounts (lower(email_address)) WHERE is_active;

CREATE TABLE IF NOT EXISTS roles (
    id                uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    role_name         text NOT NULL,
    role_description  text,
    is_active         boolean     NOT NULL DEFAULT TRUE,
    created_at        timestamptz NOT NULL DEFAULT now(),
    created_by        uuid,
    modified_at       timestamptz,
    modified_by       uuid
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_roles_role_name ON roles (role_name) WHERE is_active;

CREATE TABLE IF NOT EXISTS account_roles (
    id           uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id   uuid NOT NULL REFERENCES accounts (id),
    role_id      uuid NOT NULL REFERENCES roles (id),
    is_active    boolean     NOT NULL DEFAULT TRUE,
    created_at   timestamptz NOT NULL DEFAULT now(),
    created_by   uuid,
    modified_at  timestamptz,
    modified_by  uuid
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_account_roles_account_role
    ON account_roles (account_id, role_id) WHERE is_active;

CREATE TABLE IF NOT EXISTS account_operators (
    id           uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    account_id   uuid NOT NULL REFERENCES accounts (id),
    operator_id  uuid NOT NULL REFERENCES operators (id),
    is_active    boolean     NOT NULL DEFAULT TRUE,
    created_at   timestamptz NOT NULL DEFAULT now(),
    created_by   uuid,
    modified_at  timestamptz,
    modified_by  uuid
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_account_operators_account_operator
    ON account_operators (account_id, operator_id) WHERE is_active;

CREATE TABLE IF NOT EXISTS contracts (
    id                   uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    operator_id          uuid NOT NULL REFERENCES operators (id),
    contract_number      text NOT NULL,
    contract_name        text,
    commencement_date    date NOT NULL,
    expiry_date          date,
    bank_name            text,
    bank_branch_code     text,
    bank_account_name    text,
    bank_account_number  text,
    is_active            boolean     NOT NULL DEFAULT TRUE,
    created_at           timestamptz NOT NULL DEFAULT now(),
    created_by           uuid,
    modified_at          timestamptz,
    modified_by          uuid
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_contracts_contract_number
    ON contracts (contract_number) WHERE is_active;
CREATE INDEX IF NOT EXISTS ix_contracts_operator ON contracts (operator_id);

CREATE TABLE IF NOT EXISTS vehicles (
    id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    operator_id     uuid NOT NULL REFERENCES operators (id),
    vehicle_number  text NOT NULL,
    is_active       boolean     NOT NULL DEFAULT TRUE,
    created_at      timestamptz NOT NULL DEFAULT now(),
    created_by      uuid,
    modified_at     timestamptz,
    modified_by     uuid
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_vehicles_operator_number
    ON vehicles (operator_id, vehicle_number) WHERE is_active;
