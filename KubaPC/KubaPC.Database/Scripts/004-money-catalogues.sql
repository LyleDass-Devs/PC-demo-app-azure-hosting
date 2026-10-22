CREATE TABLE IF NOT EXISTS penalty_list_items (
    id                                 uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    contract_id                        uuid NOT NULL REFERENCES contracts (id),
    penalty_code                       text NOT NULL,
    description                        text NOT NULL,
    penalty_formula                    text,
    rand_amount_minor                  bigint,
    revenue_type                       text REFERENCES revenue_types (revenue_type_value),
    penalty_type                       text NOT NULL REFERENCES penalty_types (penalty_type_value),
    repeat_penalty_escalation_enabled  boolean NOT NULL DEFAULT FALSE,
    is_active                          boolean     NOT NULL DEFAULT TRUE,
    created_at                         timestamptz NOT NULL DEFAULT now(),
    created_by                         uuid,
    modified_at                        timestamptz,
    modified_by                        uuid,

    CONSTRAINT ck_penalty_list_items_priced
        CHECK (rand_amount_minor IS NOT NULL OR penalty_formula IS NOT NULL)
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_penalty_list_items_contract_code
    ON penalty_list_items (contract_id, penalty_code) WHERE is_active;

CREATE TABLE IF NOT EXISTS contract_rates (
    id                          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    contract_id                 uuid NOT NULL REFERENCES contracts (id),
    operated_rate_per_km_minor  bigint NOT NULL,
    standing_rate_per_km_minor  bigint NOT NULL,
    effective_from_date         date NOT NULL,
    effective_to_date           date,
    is_active                   boolean     NOT NULL DEFAULT TRUE,
    created_at                  timestamptz NOT NULL DEFAULT now(),
    created_by                  uuid,
    modified_at                 timestamptz,
    modified_by                 uuid
);
CREATE INDEX IF NOT EXISTS ix_contract_rates_contract_effective
    ON contract_rates (contract_id, effective_from_date);

CREATE TABLE IF NOT EXISTS escalation_parameters (
    id                     uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    contract_id            uuid NOT NULL REFERENCES contracts (id),
    labour_weight          numeric(3,2) NOT NULL,
    fuel_weight            numeric(3,2) NOT NULL,
    cpi_weight             numeric(3,2) NOT NULL,
    base_month             integer NOT NULL,
    base_year              integer NOT NULL,
    labour_base_value      numeric(18,6) NOT NULL,
    fuel_base_value        numeric(18,6) NOT NULL,
    cpi_base_value         numeric(18,6) NOT NULL,
    is_active              boolean     NOT NULL DEFAULT TRUE,
    created_at             timestamptz NOT NULL DEFAULT now(),
    created_by             uuid,
    modified_at            timestamptz,
    modified_by            uuid,

    CONSTRAINT ck_escalation_parameters_weights_sum
        CHECK (labour_weight + fuel_weight + cpi_weight = 1.00),
    CONSTRAINT ck_escalation_parameters_base_month
        CHECK (base_month BETWEEN 1 AND 12)
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_escalation_parameters_contract
    ON escalation_parameters (contract_id) WHERE is_active;

CREATE TABLE IF NOT EXISTS reference_urls (
    id           uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    url_key      text NOT NULL REFERENCES url_keys (key_value),
    url_value    text NOT NULL,
    is_active    boolean     NOT NULL DEFAULT TRUE,
    created_at   timestamptz NOT NULL DEFAULT now(),
    created_by   uuid,
    modified_at  timestamptz,
    modified_by  uuid
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_reference_urls_url_key
    ON reference_urls (url_key) WHERE is_active;

CREATE TABLE IF NOT EXISTS dno_reasons (
    id            uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    reason_value  text NOT NULL,
    is_active     boolean     NOT NULL DEFAULT TRUE,
    created_at    timestamptz NOT NULL DEFAULT now(),
    created_by    uuid,
    modified_at   timestamptz,
    modified_by   uuid
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_dno_reasons_reason_value
    ON dno_reasons (reason_value) WHERE is_active;

CREATE TABLE IF NOT EXISTS income_field_configurations (
    id                                 uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    contract_id                        uuid NOT NULL REFERENCES contracts (id),
    income_private_hires_enabled       boolean NOT NULL DEFAULT TRUE,
    income_dedicated_contracts_enabled boolean NOT NULL DEFAULT TRUE,
    is_active                          boolean     NOT NULL DEFAULT TRUE,
    created_at                         timestamptz NOT NULL DEFAULT now(),
    created_by                         uuid,
    modified_at                        timestamptz,
    modified_by                        uuid
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_income_field_configurations_contract
    ON income_field_configurations (contract_id) WHERE is_active;
