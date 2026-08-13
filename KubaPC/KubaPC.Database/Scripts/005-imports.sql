-- =========================================================================
--  005 - Imported report data (landing tables)
--
--  Your requirement: store almost every field from every report, so the
--  data is already there when the app's use cases extend. Each landing
--  table is a faithful copy of one file - every source column typed, plus
--  the whole row as jsonb so a column added in a future export version is
--  captured without a schema change.
--
--  Landing tables keep source column names (a deliberate, documented
--  exception to plain-English naming) so an import can be audited
--  column-by-column against the file. Nothing calculates money from a
--  landing table - the derived tables in 005 do that.
--
--  Run after 004. Safe to re-run.
-- =========================================================================

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

COMMENT ON TABLE import_batches IS
    'One row per file imported. Every landing row points here, so any certificate figure traces to a file, a time, and a person.';
COMMENT ON COLUMN import_batches.source_file_hash IS
    'SHA-256 of the file as received. Detects duplicate imports and proves stored rows match the file sent.';
COMMENT ON COLUMN import_batches.row_count_rejected IS
    'Rows skipped - including the grand-total rows embedded in Journey Analysis data, which would otherwise double a month''s kilometres. Non-zero is expected, not a fault.';


-- Journey Analysis: all 27 source columns.
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

COMMENT ON TABLE journey_analysis_rows IS
    'Faithful copy of the Journey Analysis report. Never calculate money from this table; use trip_operations.';
COMMENT ON COLUMN journey_analysis_rows.dno IS
    'DNO means Did Not Operate. A count in the source file, not a reason.';
COMMENT ON COLUMN journey_analysis_rows.trip_scheduled_operated IS
    'Source heading "Trip S/O" (Trip Scheduled or Operated); renamed because a slash is not a valid identifier character.';
COMMENT ON COLUMN journey_analysis_rows.check_point_sched IS
    'Source heading "CH-P Sched" (Check Point Scheduled). Empty in every sampled row.';
COMMENT ON COLUMN journey_analysis_rows.operated_kms_live IS
    'Kilometres actually operated - the figure Form E multiplies by the contract rate.';
COMMENT ON COLUMN journey_analysis_rows.revenue_minor IS
    'Cash fare value in cents. Inferred to be money, not distance (R13.37 per passenger in the sample); awaiting confirmation.';
COMMENT ON COLUMN journey_analysis_rows.non_revenue_minor IS
    'Value of travel on passes and concessions, in cents. Same caveat as revenue_minor.';
COMMENT ON COLUMN journey_analysis_rows.total_passengers IS
    'Source heading misspelled "Total Passangers"; corrected here.';
COMMENT ON COLUMN journey_analysis_rows.raw_record IS
    'The row exactly as read, keyed by source heading. A column added to a future report version is captured without a migration.';


-- Contract Schedule: all 18 source columns.
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

COMMENT ON COLUMN contract_schedule_rows.days_scheduled IS
    'Eight-character mask such as H_MTWTF_: position 1 public holiday, positions 2-8 Sunday through Saturday, underscore = not scheduled.';
COMMENT ON COLUMN contract_schedule_rows.contract IS
    'Grand-total rows carry the literal string "Total" here; the importer rejects them.';


-- SAEIV trip export (courses): all 24 source columns.
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

COMMENT ON TABLE saeiv_trip_exports IS
    'Faithful copy of the SAEIV course (trip) export. Test sample carried no actual operating data - every "real" field empty, every trip flagged uncovered.';
COMMENT ON COLUMN saeiv_trip_exports.trip_id IS
    'Documented as the trip''s technical identifier but NOT unique in practice (one value repeated four times in the sample). Indexed, deliberately unconstrained; deduplication happens deriving trips.';
COMMENT ON COLUMN saeiv_trip_exports.trip_occurence_date IS
    'Source spelling "occurence" kept so the column matches the file.';
COMMENT ON COLUMN saeiv_trip_exports.trip_direction IS
    'Documented 1=outbound 2=return, but the sample contains 0 and 1. Documentation and file disagree; stored as received.';
COMMENT ON COLUMN saeiv_trip_exports.trip_is_uncovered IS
    '0 = trip ran, 1 = cancelled.';
COMMENT ON COLUMN saeiv_trip_exports.route_hash IS
    'Large numeric itinerary identifier, stored as text because values can exceed bigint.';


-- SAEIV stop export (arrets): all 25 source columns.
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

COMMENT ON TABLE saeiv_stop_exports IS
    'Faithful copy of the SAEIV arret (stop) export. One row per stop served by a trip.';
COMMENT ON COLUMN saeiv_stop_exports.stop_id IS
    'Identifies the parent stop AREA, not the stop: 103 of 111 sample values span several stop codes and coordinates. Never key a stop dimension on this column.';
COMMENT ON COLUMN saeiv_stop_exports.theoretical_stop_code IS
    'The stable identifier of an individual stop - one name, one coordinate across the whole sample. The correct stop key.';
COMMENT ON COLUMN saeiv_stop_exports.fare_stage_id IS
    'Identical to stop_id in every sample row; kept separate because the concepts differ and may diverge in production.';
