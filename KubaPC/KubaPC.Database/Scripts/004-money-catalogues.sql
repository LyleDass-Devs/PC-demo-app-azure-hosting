-- =========================================================================
--  004 - The money catalogues
--
--  Everything here is edited through the application by specific roles
--  (spec RBAC), under the elevated kubapc_administrator database role
--  (read-only to the ordinary connection - see 008):
--
--    penalty_list_items          Admin (as Operator Admin)     Use Case 3
--    contract_rates              Admin (spec leaves owner open; A.13.1)
--    escalation_parameters       Admin (as Operator Admin)     Use Case 15
--    reference_urls              SuperAdmin only               A.10
--    dno_reasons                 SuperAdmin only               A.14
--    income_field_configurations Admin                          A.16
--
--  Run after 003. Safe to re-run.
-- =========================================================================

-- A.3 Penalty List Item - the per-contract penalty catalogue.
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

    -- A.3: at least one of the two pricing mechanisms must be present.
    CONSTRAINT ck_penalty_list_items_priced
        CHECK (rand_amount_minor IS NOT NULL OR penalty_formula IS NOT NULL)
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_penalty_list_items_contract_code
    ON penalty_list_items (contract_id, penalty_code) WHERE is_active;

COMMENT ON TABLE penalty_list_items IS
    'A.3. The contract''s penalty catalogue, maintained by Admin (Use Case 3). Deactivating an item hides it from new captures but leaves historical captures intact.';
COMMENT ON COLUMN penalty_list_items.penalty_formula IS
    'Free text, for example "R500 x number of days late". Deliberately NOT evaluated by the system (A.3 assumption, confirmed): there is no calculation engine. An approved capture priced only by formula contributes nothing to the non-refundable penalties total until priced - a known spec gap (A.13.1).';
COMMENT ON COLUMN penalty_list_items.rand_amount_minor IS
    'Fixed penalty value in cents. Spec Decimal Rand; stored as integer minor units, so R4000 is 400000.';
COMMENT ON COLUMN penalty_list_items.repeat_penalty_escalation_enabled IS
    'Use Case 6 business rule: when TRUE, the 3rd and every later consecutive monthly capture of this item (same trip, contract, or vehicle depending on penalty_type) doubles - always exactly 2x the base, never compounding. A Rejected capture resets the streak.';
COMMENT ON COLUMN penalty_list_items.penalty_type IS
    'A.3.2. Also determines the repeat-escalation counting scope: Trip-level for Trips Not Operated / Time-Related / Stop & Route Adherence; Contract-level for Duty & Destination Boards; Vehicle-level for Unroadworthy / Unsatisfactory Vehicle Condition.';


-- Contract rates. NOT in the specification: A.13.1 records that the operated
-- and standing kilometre rates Form E needs are "reference data" whose home,
-- owner, and scope are unspecified. The certificate workbook proves they
-- exist (R8.21 operated, R4.93 standing). Modelled here, dated, superseded
-- rather than updated, so historical certificates recalculate unchanged.
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

COMMENT ON TABLE contract_rates IS
    'Not in the spec (gap recorded at A.13.1): the per-kilometre rates Form E multiplies by. From the sample workbook: R8.21 operated (stored 821), R4.93 standing (stored 493). Never update a row - close it with effective_to_date and insert a successor.';


-- A.9 Escalation Parameter - one row per contract.
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

    -- A.9: the three weights must sum to exactly 1.00, so that at the base
    -- month (every ratio = 1) the factor f is exactly 1.00.
    CONSTRAINT ck_escalation_parameters_weights_sum
        CHECK (labour_weight + fuel_weight + cpi_weight = 1.00),
    CONSTRAINT ck_escalation_parameters_base_month
        CHECK (base_month BETWEEN 1 AND 12)
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_escalation_parameters_contract
    ON escalation_parameters (contract_id) WHERE is_active;

COMMENT ON TABLE escalation_parameters IS
    'A.9. One row per contract. Maintained by Admin as Operator Admin (Use Case 15). The Form D formula, corrected and confirmed: f = ((a x (Lt/Lbm)) + (b x (Ft/Fbm)) + (c x (CPIt/Cbm)) - 1) + 1.';
COMMENT ON COLUMN escalation_parameters.labour_weight IS
    'a in the formula. Source: SA Road Passenger Bargaining Council labour component.';
COMMENT ON COLUMN escalation_parameters.fuel_weight IS
    'b in the formula. Source: DOENG diesel price (reference URL in reference_urls).';
COMMENT ON COLUMN escalation_parameters.cpi_weight IS
    'c in the formula. Source: STATSSA CPI (reference URL in reference_urls). NOTE: the sample workbook uses an older four-part GCC formula (fixed portion x plus labour, vehicle PPI, fuel weights, base September 1998). The spec formula above supersedes it; the divergence is recorded in guide 06.';


-- A.10 Reference URL - system-wide, SuperAdmin only.
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

COMMENT ON TABLE reference_urls IS
    'A.10. The two reference links shown on Form D. Editable only by SuperAdmin. No Labour URL exists because the spec never names a source for the labour figure (open item).';


-- A.14 DNO Reason - system-wide, SuperAdmin only.
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

COMMENT ON TABLE dno_reasons IS
    'A.14. Why an AFC-determined DNO (Did Not Operate) is converted to a Standing Trip (Use Case 8, 2b). Seeded Unrest, Weather, Other; extensible by SuperAdmin only.';


-- A.16 Income Field Configuration - one row per contract.
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

COMMENT ON TABLE income_field_configurations IS
    'A.16. Per-contract switch controlling whether the two Form B&H income fields are capturable at all. Managed by Admin. Both default to enabled.';
