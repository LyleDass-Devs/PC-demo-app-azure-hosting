DROP INDEX IF EXISTS ux_form_f1_certificate_trip_open;

CREATE INDEX IF NOT EXISTS ix_form_f1_records_trip_type_open
    ON form_f1_records (trip_id, record_type)
    WHERE record_status <> 'Rejected' AND is_active;
