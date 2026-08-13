-- =========================================================================
--  006 - cert
--
--  The payment certificate and its forms. The money chain runs
--  Form E, then escalation, then Form C, then Form A.
--
--  Run after 005. Safe to re-run.
-- =========================================================================

CREATE TABLE IF NOT EXISTS cert.payment_certificates (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    contract_id         uuid NOT NULL REFERENCES identity.contracts (id),
    certificate_number  integer NOT NULL,
    period_month_date   date NOT NULL,
    certificate_status  text NOT NULL DEFAULT 'Draft',
    captured_by         uuid REFERENCES identity.accounts (id),
    captured_at         timestamptz,
    verified_by         uuid REFERENCES identity.accounts (id),
    verified_at         timestamptz,
    authorised_by       uuid REFERENCES identity.accounts (id),
    authorised_at       timestamptz,
    is_active           boolean     NOT NULL DEFAULT TRUE,
    created_at          timestamptz NOT NULL DEFAULT now(),
    created_by          uuid,
    modified_at         timestamptz,
    modified_by         uuid
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_payment_certificates_contract_period
    ON cert.payment_certificates (contract_id, period_month_date) WHERE is_active;
CREATE UNIQUE INDEX IF NOT EXISTS ux_payment_certificates_contract_number
    ON cert.payment_certificates (contract_id, certificate_number) WHERE is_active;

COMMENT ON TABLE cert.payment_certificates IS
    'One monthly claim for one contract.';
COMMENT ON COLUMN cert.payment_certificates.certificate_number IS
    'Sequential per contract. The sample workbook is certificate 320 for May 2025.';
COMMENT ON COLUMN cert.payment_certificates.period_month_date IS
    'Always the first day of the month being claimed for.';
COMMENT ON COLUMN cert.payment_certificates.certificate_status IS
    'Draft, Captured, Verified, Authorised or Rejected.';


CREATE TABLE IF NOT EXISTS cert.certificate_status_history (
    id                      uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    payment_certificate_id  uuid NOT NULL REFERENCES cert.payment_certificates (id),
    from_status             text,
    to_status               text NOT NULL,
    changed_by              uuid NOT NULL REFERENCES identity.accounts (id),
    changed_at              timestamptz NOT NULL DEFAULT now(),
    change_note             text,
    is_active               boolean     NOT NULL DEFAULT TRUE,
    created_at              timestamptz NOT NULL DEFAULT now(),
    created_by              uuid,
    modified_at             timestamptz,
    modified_by             uuid
);
CREATE INDEX IF NOT EXISTS ix_certificate_status_history_certificate
    ON cert.certificate_status_history (payment_certificate_id, changed_at);

COMMENT ON TABLE cert.certificate_status_history IS
    'Every movement through the approval workflow. The rejection reason lives in change_note, which is what an operator asks for first.';


CREATE TABLE IF NOT EXISTS cert.form_e_records (
    id                          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    payment_certificate_id      uuid NOT NULL REFERENCES cert.payment_certificates (id),
    operated_kilometres         numeric(14,2) NOT NULL,
    operated_rate_per_km_minor  bigint NOT NULL,
    operated_amount_minor       bigint NOT NULL,
    standing_kilometres         numeric(14,2) NOT NULL DEFAULT 0,
    standing_rate_per_km_minor  bigint NOT NULL,
    standing_amount_minor       bigint NOT NULL DEFAULT 0,
    deduction_amount_minor      bigint NOT NULL DEFAULT 0,
    gross_amount_minor          bigint NOT NULL,
    is_active                   boolean     NOT NULL DEFAULT TRUE,
    created_at                  timestamptz NOT NULL DEFAULT now(),
    created_by                  uuid,
    modified_at                 timestamptz,
    modified_by                 uuid
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_form_e_records_certificate
    ON cert.form_e_records (payment_certificate_id) WHERE is_active;

COMMENT ON TABLE cert.form_e_records IS
    'Where the money starts. The functional specification states Form E does not exist; it is present in the client workbook and every later form depends on it.';
COMMENT ON COLUMN cert.form_e_records.operated_rate_per_km_minor IS
    'The rate copied in at calculation time rather than read through a join. Renegotiating a rate must not silently alter a certificate already issued.';
COMMENT ON COLUMN cert.form_e_records.gross_amount_minor IS
    'operated_amount_minor plus standing_amount_minor less deduction_amount_minor. For the May 2025 sample: 677566.37 plus 1153.62 less 1889.34 equals 676830.65.';


CREATE TABLE IF NOT EXISTS cert.escalation_records (
    id                       uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    payment_certificate_id   uuid NOT NULL REFERENCES cert.payment_certificates (id),
    escalation_parameter_id  uuid NOT NULL REFERENCES config.escalation_parameters (id),
    index_month_date         date NOT NULL,
    labour_index_value       numeric(18,6) NOT NULL,
    vehicle_index_value      numeric(18,6) NOT NULL,
    fuel_index_value         numeric(18,6) NOT NULL,
    escalation_factor        numeric(20,12) NOT NULL,
    base_amount_minor        bigint NOT NULL,
    escalated_amount_minor   bigint NOT NULL,
    is_active                boolean     NOT NULL DEFAULT TRUE,
    created_at               timestamptz NOT NULL DEFAULT now(),
    created_by               uuid,
    modified_at              timestamptz,
    modified_by              uuid
);
CREATE INDEX IF NOT EXISTS ix_escalation_records_certificate
    ON cert.escalation_records (payment_certificate_id);

COMMENT ON COLUMN cert.escalation_records.escalation_factor IS
    'Twelve decimal places, to reproduce workbook values such as 1.0250429995463988 exactly. A ratio rather than money, so numeric rather than integer cents.';
COMMENT ON COLUMN cert.escalation_records.labour_index_value IS
    'The index values are copied in rather than joined, for the same reason as the rates on form_e_records: a restated index must not change a certificate already paid.';


CREATE TABLE IF NOT EXISTS cert.form_c_records (
    id                      uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    payment_certificate_id  uuid NOT NULL REFERENCES cert.payment_certificates (id),
    escalated_amount_minor  bigint NOT NULL,
    penalty_total_minor     bigint NOT NULL DEFAULT 0,
    refund_total_minor      bigint NOT NULL DEFAULT 0,
    other_adjustment_minor  bigint NOT NULL DEFAULT 0,
    total_claim_minor       bigint NOT NULL,
    is_active               boolean     NOT NULL DEFAULT TRUE,
    created_at              timestamptz NOT NULL DEFAULT now(),
    created_by              uuid,
    modified_at             timestamptz,
    modified_by             uuid
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_form_c_records_certificate
    ON cert.form_c_records (payment_certificate_id) WHERE is_active;

COMMENT ON TABLE cert.form_c_records IS
    'Totals the claim: the escalated amount less penalties, plus any refunds and adjustments.';


CREATE TABLE IF NOT EXISTS cert.form_a_records (
    id                              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    payment_certificate_id          uuid NOT NULL REFERENCES cert.payment_certificates (id),
    cumulative_to_date_minor        bigint NOT NULL,
    previous_certificate_minor      bigint NOT NULL,
    amount_due_minor                bigint NOT NULL,
    value_added_tax_minor           bigint NOT NULL DEFAULT 0,
    amount_due_including_tax_minor  bigint NOT NULL,
    is_active                       boolean     NOT NULL DEFAULT TRUE,
    created_at                      timestamptz NOT NULL DEFAULT now(),
    created_by                      uuid,
    modified_at                     timestamptz,
    modified_by                     uuid
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_form_a_records_certificate
    ON cert.form_a_records (payment_certificate_id) WHERE is_active;

COMMENT ON TABLE cert.form_a_records IS
    'The concluding form. Cumulative less previously certified equals the amount due. For the May 2025 sample: 625678195.34 less 622369360.38 equals 3308834.96.';
COMMENT ON COLUMN cert.form_a_records.value_added_tax_minor IS
    'VAT is Value Added Tax. Held separately because the rate changes independently of anything else on the certificate.';


CREATE TABLE IF NOT EXISTS cert.penalty_captures (
    id                          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    payment_certificate_id      uuid NOT NULL REFERENCES cert.payment_certificates (id),
    penalty_code_id             uuid NOT NULL REFERENCES config.penalty_codes (id),
    route_id                    uuid REFERENCES ops.routes (id),
    trip_id                     uuid REFERENCES ops.trips (id),
    occurrence_date             date NOT NULL,
    occurrence_count            integer NOT NULL DEFAULT 1,
    route_length_km             numeric(12,2) NOT NULL,
    rate_per_km_minor           bigint NOT NULL,
    penalty_percentage          numeric(6,4) NOT NULL,
    penalty_amount_minor        bigint NOT NULL,
    is_double_penalty_applied   boolean NOT NULL DEFAULT FALSE,
    is_refunded                 boolean NOT NULL DEFAULT FALSE,
    refunded_on_certificate_id  uuid REFERENCES cert.payment_certificates (id),
    capture_note                text,
    is_active                   boolean     NOT NULL DEFAULT TRUE,
    created_at                  timestamptz NOT NULL DEFAULT now(),
    created_by                  uuid,
    modified_at                 timestamptz,
    modified_by                 uuid
);
CREATE INDEX IF NOT EXISTS ix_penalty_captures_certificate
    ON cert.penalty_captures (payment_certificate_id);

COMMENT ON TABLE cert.penalty_captures IS
    'One penalty raised against one certificate. The inputs are copied in, not joined, so renegotiating a rate in a later year leaves this year penalties exactly as issued.';
COMMENT ON COLUMN cert.penalty_captures.penalty_amount_minor IS
    'route_length_km times rate_per_km_minor times penalty_percentage. Worked example from the sample: 70.3 km at R8.21 at 25 percent equals R144.29.';
COMMENT ON COLUMN cert.penalty_captures.refunded_on_certificate_id IS
    'The later certificate that reversed this penalty once evidence was accepted. Drives Form G2.';
