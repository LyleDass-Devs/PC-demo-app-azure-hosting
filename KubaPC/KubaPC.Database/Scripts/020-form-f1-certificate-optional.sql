ALTER TABLE form_f1_records ALTER COLUMN payment_certificate_id DROP NOT NULL;

CREATE INDEX IF NOT EXISTS ix_form_f1_records_unattached
    ON form_f1_records (trip_id) WHERE payment_certificate_id IS NULL AND is_active;

CREATE INDEX IF NOT EXISTS ix_form_f1_records_trip
    ON form_f1_records (trip_id);
