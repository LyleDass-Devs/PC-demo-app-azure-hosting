-- =========================================================================
--  003 - config  (administrator-only reference data)
--
--  Rates, penalty codes and escalation constants. Everything in this schema
--  decides how much money is paid, which is why it is separated: permissions
--  are granted on the schema, so a table added next year is protected without
--  anyone having to remember.
--
--  Run after 002. Safe to re-run.
-- =========================================================================

CREATE TABLE IF NOT EXISTS config.contract_rates (
    id                          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    contract_id                 uuid NOT NULL REFERENCES identity.contracts (id),
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
    ON config.contract_rates (contract_id, effective_from_date);

COMMENT ON TABLE config.contract_rates IS
    'The rate per kilometre. Form E multiplies kilometres by these figures, and no rate field exists anywhere in the functional specification, so without this table the system cannot produce a single amount.';
COMMENT ON COLUMN config.contract_rates.operated_rate_per_km_minor IS
    'Rate per operated revenue kilometre, in cents. R8.21 is stored as 821.';
COMMENT ON COLUMN config.contract_rates.standing_rate_per_km_minor IS
    'Rate per standing kilometre, in cents. R4.93 is stored as 493.';
COMMENT ON COLUMN config.contract_rates.effective_to_date IS
    'NULL means currently in force. Rates are never updated in place; a new row supersedes, so an older certificate always recalculates to the figure it was issued at.';


CREATE TABLE IF NOT EXISTS config.penalty_codes (
    id                         uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    contract_id                uuid NOT NULL REFERENCES identity.contracts (id),
    code                       text NOT NULL,
    code_description           text NOT NULL,
    penalty_percentage         numeric(6,4),
    minimum_penalty_minor      bigint,
    is_double_penalty_allowed  boolean NOT NULL DEFAULT FALSE,
    is_refundable              boolean NOT NULL DEFAULT FALSE,
    is_active                  boolean     NOT NULL DEFAULT TRUE,
    created_at                 timestamptz NOT NULL DEFAULT now(),
    created_by                 uuid,
    modified_at                timestamptz,
    modified_by                uuid
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_penalty_codes_contract_code
    ON config.penalty_codes (contract_id, code) WHERE is_active;

COMMENT ON TABLE config.penalty_codes IS
    'The catalogue of penalties. The functional specification models a free-text penalty formula, which no program can evaluate; the client workbook uses fixed percentages, which this table holds.';
COMMENT ON COLUMN config.penalty_codes.penalty_percentage IS
    'Fraction of the route value charged as a penalty. 0.25 means 25 percent, per workbook Form G1.';
COMMENT ON COLUMN config.penalty_codes.is_refundable IS
    'A refundable penalty may be reversed on a later certificate once the operator supplies evidence. Drives Form G2.';


CREATE TABLE IF NOT EXISTS config.escalation_parameters (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    contract_id         uuid NOT NULL REFERENCES identity.contracts (id),
    fixed_portion_x     numeric(6,4)  NOT NULL,
    labour_weight_a     numeric(6,4)  NOT NULL,
    vehicle_weight_b    numeric(6,4)  NOT NULL,
    fuel_weight_c       numeric(6,4)  NOT NULL,
    base_month_date     date          NOT NULL,
    labour_base_value   numeric(18,6) NOT NULL,
    vehicle_base_value  numeric(18,6) NOT NULL,
    fuel_base_value     numeric(18,6) NOT NULL,
    is_active           boolean     NOT NULL DEFAULT TRUE,
    created_at          timestamptz NOT NULL DEFAULT now(),
    created_by          uuid,
    modified_at         timestamptz,
    modified_by         uuid
);
CREATE INDEX IF NOT EXISTS ix_escalation_parameters_contract
    ON config.escalation_parameters (contract_id);

COMMENT ON TABLE config.escalation_parameters IS
    'Escalation constants from clause 37 of the GCC, the General Conditions of Contract. For the sample contract: x is 0.15, a is 0.35, b is 0.45, c is 0.20, base month September 1998.';
COMMENT ON COLUMN config.escalation_parameters.fixed_portion_x IS
    'The share of the tendered rate that does not escalate at all.';
COMMENT ON COLUMN config.escalation_parameters.base_month_date IS
    'Always the first day of the base month. September 1998 for the sample contract.';
COMMENT ON COLUMN config.escalation_parameters.labour_base_value IS
    'Lo in the escalation formula. The labour index at the base month, 19 for the sample contract.';
COMMENT ON COLUMN config.escalation_parameters.vehicle_base_value IS
    'Bo in the escalation formula. The Bus and Truck index from the PPI, the Producer Price Index report, at the base month. 49.4 for the sample contract.';
COMMENT ON COLUMN config.escalation_parameters.fuel_base_value IS
    'Fo in the escalation formula. The diesel price at the base month, 6.9 for the sample contract.';


CREATE TABLE IF NOT EXISTS config.escalation_index_values (
    id                       uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    escalation_parameter_id  uuid NOT NULL REFERENCES config.escalation_parameters (id),
    index_month_date         date          NOT NULL,
    labour_index_value       numeric(18,6) NOT NULL,
    vehicle_index_value      numeric(18,6) NOT NULL,
    fuel_index_value         numeric(18,6) NOT NULL,
    source_note              text,
    is_active                boolean     NOT NULL DEFAULT TRUE,
    created_at               timestamptz NOT NULL DEFAULT now(),
    created_by               uuid,
    modified_at              timestamptz,
    modified_by              uuid
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_escalation_index_values_parameter_month
    ON config.escalation_index_values (escalation_parameter_id, index_month_date)
    WHERE is_active;

COMMENT ON TABLE config.escalation_index_values IS
    'The published index values for one month. The escalation formula compares these against the base values on escalation_parameters, so without this table the formula cannot be evaluated for any month at all.';
COMMENT ON COLUMN config.escalation_index_values.index_month_date IS
    'Always the first day of the month the indices describe.';
COMMENT ON COLUMN config.escalation_index_values.source_note IS
    'Where the figure was read from, for example the SEIFSA report issue or the published diesel price date. An auditor will ask.';


CREATE TABLE IF NOT EXISTS config.non_operation_reasons (
    id                    uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    reason_code           text NOT NULL,
    reason_description    text NOT NULL,
    is_operator_at_fault  boolean NOT NULL,
    is_active             boolean     NOT NULL DEFAULT TRUE,
    created_at            timestamptz NOT NULL DEFAULT now(),
    created_by            uuid,
    modified_at           timestamptz,
    modified_by           uuid
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_non_operation_reasons_code
    ON config.non_operation_reasons (reason_code) WHERE is_active;

COMMENT ON TABLE config.non_operation_reasons IS
    'Why a scheduled trip did not operate. Called DNO, meaning Did Not Operate, in the Journey Analysis report.';
COMMENT ON COLUMN config.non_operation_reasons.is_operator_at_fault IS
    'Decides the money. A road closure is not the operator fault and the kilometres are still paid; a breakdown is, and they are not.';
