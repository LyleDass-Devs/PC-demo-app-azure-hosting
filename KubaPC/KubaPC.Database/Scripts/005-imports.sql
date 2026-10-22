CREATE TABLE IF NOT EXISTS import_batches (
    id                  uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    import_type         text NOT NULL,
    source_file_name    text NOT NULL,
    source_file_hash    text,
    period_month_date   date,
    row_count_read      integer NOT NULL DEFAULT 0,
    row_count_accepted  integer NOT NULL DEFAULT 0,
    row_count_rejected  integer NOT NULL DEFAULT 0,
    import_status       text NOT NULL DEFAULT 'Started',
    failure_reason      text,
    started_at          timestamptz NOT NULL DEFAULT now(),
    completed_at        timestamptz,
    is_active           boolean     NOT NULL DEFAULT TRUE,
    created_at          timestamptz NOT NULL DEFAULT now(),
    created_by          uuid,
    modified_at         timestamptz,
    modified_by         uuid,

    CONSTRAINT ck_import_batches_import_type
        CHECK (import_type IN ('JourneyAnalysis', 'ContractSchedule', 'SaeivTrip', 'SaeivStop')),
    CONSTRAINT ck_import_batches_import_status
        CHECK (import_status IN ('Started', 'Completed', 'Failed'))
);
CREATE INDEX IF NOT EXISTS ix_import_batches_type_period
    ON import_batches (import_type, period_month_date);

CREATE TABLE IF NOT EXISTS journey_analysis_rows (
    id                       uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    import_batch_id          uuid NOT NULL REFERENCES import_batches (id),
    source_row_number        integer NOT NULL,

    sched_date               date,
    contract                 text,
    trip                     text,
    in_out                   text,
    line_no                  text,
    duty                     text,
    driver_no                text,
    route_name               text,
    driver                   text,
    bus                      text,
    trip_start               time,
    trip_stop                time,
    funded                   text,
    check_point_sched        text,
    single_trip_kms          numeric(12,2),
    scheduled_trips          integer,
    scheduled_kms            numeric(14,2),
    operated_trips           integer,
    operated_kms_live        numeric(14,2),
    dno                      text,
    trip_scheduled_operated  text,
    non_revenue_minor        bigint,
    revenue_minor            bigint,
    tickets                  integer,
    passes                   integer,
    transfers                integer,
    total_passengers         integer,

    raw_record               jsonb NOT NULL,
    is_active                boolean     NOT NULL DEFAULT TRUE,
    created_at               timestamptz NOT NULL DEFAULT now(),
    created_by               uuid,
    modified_at              timestamptz,
    modified_by              uuid
);
CREATE INDEX IF NOT EXISTS ix_journey_analysis_rows_contract_date
    ON journey_analysis_rows (contract, sched_date);
CREATE INDEX IF NOT EXISTS ix_journey_analysis_rows_batch
    ON journey_analysis_rows (import_batch_id);

CREATE TABLE IF NOT EXISTS contract_schedule_rows (
    id                 uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    import_batch_id    uuid NOT NULL REFERENCES import_batches (id),
    source_row_number  integer NOT NULL,

    contract           text,
    schedule_year      integer,
    schedule_month     text,
    schedule_day       integer,
    scheduled_time     text,
    duty               text,
    trip_no            text,
    in_out             text,
    days_scheduled     text,
    trips_scheduled    integer,
    total_km           numeric(14,2),
    route              text,
    line               text,
    check_point        text,
    checkpoint_name    text,
    monitor_time       text,
    line_no            text,
    rank               text,

    raw_record         jsonb NOT NULL,
    is_active          boolean     NOT NULL DEFAULT TRUE,
    created_at         timestamptz NOT NULL DEFAULT now(),
    created_by         uuid,
    modified_at        timestamptz,
    modified_by        uuid
);
CREATE INDEX IF NOT EXISTS ix_contract_schedule_rows_contract_route
    ON contract_schedule_rows (contract, route);
CREATE INDEX IF NOT EXISTS ix_contract_schedule_rows_batch
    ON contract_schedule_rows (import_batch_id);

CREATE TABLE IF NOT EXISTS saeiv_trip_exports (
    id                                uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    import_batch_id                   uuid NOT NULL REFERENCES import_batches (id),
    source_row_number                 integer NOT NULL,

    tenant                            text,
    operator                          text,
    trip_id                           uuid,
    trip_occurence_date               timestamptz,
    trip_direction                    integer,
    theoretical_trip_start_timestamp  timestamptz,
    real_trip_start_timestamp         timestamptz,
    real_trip_end_timestamp           timestamptz,
    trip_delay_on_start               integer,
    theoretical_trip_code             text,
    real_trip_code                    text,
    service_number                    text,
    route_code                        text,
    route_hash                        text,
    network_code                      text,
    line_shortname                    text,
    line_fullname                     text,
    vehicle_number                    text,
    vehicle_company                   text,
    theoretical_trip_count_stops      integer,
    real_trip_count_stops             integer,
    driver_id                         text,
    driver_company                    text,
    trip_is_uncovered                 integer,

    raw_record                        jsonb NOT NULL,
    is_active                         boolean     NOT NULL DEFAULT TRUE,
    created_at                        timestamptz NOT NULL DEFAULT now(),
    created_by                        uuid,
    modified_at                       timestamptz,
    modified_by                       uuid
);
CREATE INDEX IF NOT EXISTS ix_saeiv_trip_exports_trip_id
    ON saeiv_trip_exports (trip_id);
CREATE INDEX IF NOT EXISTS ix_saeiv_trip_exports_batch
    ON saeiv_trip_exports (import_batch_id);

CREATE TABLE IF NOT EXISTS saeiv_stop_exports (
    id                          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    import_batch_id             uuid NOT NULL REFERENCES import_batches (id),
    source_row_number           integer NOT NULL,

    tenant                      text,
    operator_code               text,
    trip_start_date             timestamptz,
    trip_id                     uuid,
    theoretical_trip_code       text,
    real_trip_code              text,
    line_shortname              text,
    line_fullname               text,
    line_operator_name          text,
    stop_rank                   integer,
    stop_id                     uuid,
    theoretical_stop_code       text,
    real_stop_code              text,
    stop_shortname              text,
    stop_fullname               text,
    theoretical_stop_timestamp  timestamptz,
    real_stop_timestamp         timestamptz,
    real_delay_on_time          integer,
    stop_longitude              numeric(12,7),
    stop_latitude               numeric(12,7),
    fare_stage_id               uuid,
    fare_stage_code             text,
    fare_stage_name             text,
    zone_id                     uuid,
    zone_label                  text,

    raw_record                  jsonb NOT NULL,
    is_active                   boolean     NOT NULL DEFAULT TRUE,
    created_at                  timestamptz NOT NULL DEFAULT now(),
    created_by                  uuid,
    modified_at                 timestamptz,
    modified_by                 uuid
);
CREATE INDEX IF NOT EXISTS ix_saeiv_stop_exports_trip_rank
    ON saeiv_stop_exports (trip_id, stop_rank);
CREATE INDEX IF NOT EXISTS ix_saeiv_stop_exports_stop_code
    ON saeiv_stop_exports (theoretical_stop_code);
CREATE INDEX IF NOT EXISTS ix_saeiv_stop_exports_batch
    ON saeiv_stop_exports (import_batch_id);
