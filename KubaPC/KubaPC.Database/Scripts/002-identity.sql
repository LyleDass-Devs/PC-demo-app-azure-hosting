-- =========================================================================
--  002 - Identity: operators, accounts, roles, grants, contracts, vehicles
--
--  Mirrors the shared KubaOps tables this module depends on
--  (spec section 4): A.19 Operator, A.15 Administrative Account, A.26 Role,
--  A.28 Account Role Assignment, A.20 User-Operator Assignment,
--  A.36 Contract, and the Vehicle table penalty captures reference.
--
--  Run after 001. Safe to re-run.
-- =========================================================================

-- A.19 Operator
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

COMMENT ON TABLE operators IS
    'A.19. The transport operator. The specification calls its identifier id_societe; societe is French for company, inherited from the SAEIV system.';


-- A.15 Administrative Account
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

COMMENT ON TABLE accounts IS
    'A.15. One account per person, every non-Driver role. Credentials live with the identity provider, never here.';


-- A.26 Role
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

COMMENT ON TABLE roles IS
    'A.26. Seeded with the six spec roles: SuperAdmin, Admin, Operator Staff, Monitor, Authoriser, DOT. "Operator Admin" is not a seventh role - it is Admin in its capacity as scoped to particular operators (spec Terminology).';


-- A.28 Account Role Assignment
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


-- A.20 User-Operator Assignment
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

COMMENT ON TABLE account_operators IS
    'A.20. Which operators an account may see. A role says what someone may do; this says whose data they may do it to. An Authoriser only reviews certificates for operators granted here, and an Admin may only grant access to operators they themselves hold (Use Case 2 pre-conditions).';


-- A.36 Contract, including the bank details Form A displays
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

COMMENT ON TABLE contracts IS
    'A.36. A payment certificate is always issued against exactly one contract. Bank details are displayed on Form A (looked up live, never snapshotted - A.8).';
COMMENT ON COLUMN contracts.contract_number IS
    'The public reference, for example ZNT 1129 or 22/98T. Appears on every certificate and in every import file.';


-- Vehicle (KubaOps A.1) - the minimum this module needs: penalty captures
-- for Unroadworthy / Unsatisfactory Vehicle Condition must name the vehicle,
-- because repeat-penalty counting for those types is vehicle-scoped.
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

COMMENT ON TABLE vehicles IS
    'Minimal mirror of KubaOps A.1 Vehicle. Exists so vehicle-scoped penalties (Unroadworthy, Unsatisfactory Vehicle Condition) can reference the vehicle for repeat-penalty counting (Use Case 6 business rule).';
