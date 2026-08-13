-- =========================================================================
--  004 - ops, landing tables
--
--  A landing table is a faithful copy of a source file. One row in, one row
--  out, column names matching the source headings so the import can be
--  checked column by column against the original.
--
--  This is a deliberate exception to the plain-English naming rule and it
--  applies to these tables only. Nothing here calculates money; the derived
--  tables in 005 do that.
--
--  Run after 003. Safe to re-run.
-- =========================================================================

CREATE TABLE IF NOT EXISTS ops.import_batches (
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
    modified_by         uuid
);
CREATE INDEX IF NOT EXISTS ix_import_batches_type_period
    ON ops.import_batches (import_type, period_month_date);

COMMENT ON TABLE ops.import_batches IS
    'One row per file imported. Every landing row points back here, so any figure on a certificate can be traced to a file, a time and a person.';
COMMENT ON COLUMN ops.import_batches.import_type IS
    'One of JourneyAnalysis, ContractSchedule, SaeivTrip, SaeivStop.';
COMMENT ON COLUMN ops.import_batches.source_file_hash IS
    'SHA-256 of the file as received. Detects the same file being imported twice, and proves the stored rows came from the file the operator sent.';
COMMENT ON COLUMN ops.import_batches.row_count_rejected IS
    'Rows skipped, including the grand-total rows that appear inside Journey Analysis data. A non-zero value here is expected, not a fault.';
COMMENT ON COLUMN ops.import_batches.import_status IS
    'Started, Completed or Failed.';


-- -------------------------------------------------------------------------
--  Journey Analysis. All 27 source columns.
-- -------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS ops.journey_analysis_rows (
    id                       uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    import_batch_id          uuid NOT NULL REFERENCES ops.import_batches (id),
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
    ON ops.journey_analysis_rows (contract, sched_date);
CREATE INDEX IF NOT EXISTS ix_journey_analysis_rows_batch
    ON ops.journey_analysis_rows (import_batch_id);

COMMENT ON TABLE ops.journey_analysis_rows IS
    'Faithful copy of the Journey Analysis report. Do not calculate money from this table; use ops.trip_operations.';
COMMENT ON COLUMN ops.journey_analysis_rows.dno IS
    'DNO means Did Not Operate. Present in the source file as a count rather than a reason.';
COMMENT ON COLUMN ops.journey_analysis_rows.trip_scheduled_operated IS
    'Source heading "Trip S/O", meaning Trip Scheduled or Operated. Renamed only because a slash is not valid in an identifier.';
COMMENT ON COLUMN ops.journey_analysis_rows.check_point_sched IS
    'Source heading "CH-P Sched", meaning Check Point Scheduled. Empty in every row of the April 2026 sample.';
COMMENT ON COLUMN ops.journey_analysis_rows.operated_kms_live IS
    'Kilometres actually operated. This is the figure Form E multiplies by the contract rate.';
COMMENT ON COLUMN ops.journey_analysis_rows.revenue_minor IS
    'Cash fare value in cents. Read as money rather than distance; this is an inference from the arithmetic and awaits confirmation.';
COMMENT ON COLUMN ops.journey_analysis_rows.non_revenue_minor IS
    'Value of travel on passes and concessions, in cents. Same caveat as revenue_minor.';
COMMENT ON COLUMN ops.journey_analysis_rows.total_passengers IS
    'Source heading is misspelled "Total Passangers". Corrected here.';
COMMENT ON COLUMN ops.journey_analysis_rows.raw_record IS
    'The source row exactly as read, keyed by source heading. Guarantees a column added in a future version of the report is captured without a schema change.';


-- -------------------------------------------------------------------------
--  Contract Schedule. All 18 source columns.
-- -------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS ops.contract_schedule_rows (
    id                 uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    import_batch_id    uuid NOT NULL REFERENCES ops.import_batches (id),
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
    ON ops.contract_schedule_rows (contract, route);
CREATE INDEX IF NOT EXISTS ix_contract_schedule_rows_batch
    ON ops.contract_schedule_rows (import_batch_id);

COMMENT ON TABLE ops.contract_schedule_rows IS
    'Faithful copy of the Contract Schedule report.';
COMMENT ON COLUMN ops.contract_schedule_rows.days_scheduled IS
    'An eight-character mask such as H_MTWTF_ where position one is a public holiday and positions two to eight are Sunday through Saturday. An underscore means not scheduled.';
COMMENT ON COLUMN ops.contract_schedule_rows.contract IS
    'The literal string "Total" appears in this column on the grand-total rows, which the importer rejects.';


-- -------------------------------------------------------------------------
--  SAEIV trip export (courses). All 24 source columns.
-- -------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS ops.saeiv_trip_exports (
    id                                uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    import_batch_id                   uuid NOT NULL REFERENCES ops.import_batches (id),
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
    ON ops.saeiv_trip_exports (trip_id);
CREATE INDEX IF NOT EXISTS ix_saeiv_trip_exports_batch
    ON ops.saeiv_trip_exports (import_batch_id);

COMMENT ON TABLE ops.saeiv_trip_exports IS
    'Faithful copy of the SAEIV course export. SAEIV is the French acronym for the vehicle location and passenger information system; a course is one trip.';
COMMENT ON COLUMN ops.saeiv_trip_exports.trip_id IS
    'Documented as the technical identifier of the trip, but not unique in practice: the August 2026 sample repeats one value four times. Indexed, deliberately not constrained unique. Deduplication happens when deriving ops.trips.';
COMMENT ON COLUMN ops.saeiv_trip_exports.trip_occurence_date IS
    'Source spelling "occurence" is retained so the column matches the file. The correct spelling is occurrence.';
COMMENT ON COLUMN ops.saeiv_trip_exports.trip_direction IS
    'Documented as 1 for outbound and 2 for return. The sample contains 0 and 1, so the documentation and the file disagree.';
COMMENT ON COLUMN ops.saeiv_trip_exports.trip_is_uncovered IS
    'Zero means the trip ran, one means it was cancelled. Every row of the sample is one, because the sample tenant had no vehicles operating.';
COMMENT ON COLUMN ops.saeiv_trip_exports.route_hash IS
    'A large numeric identifier for the itinerary. Stored as text because the values can exceed the range of bigint.';
COMMENT ON COLUMN ops.saeiv_trip_exports.tenant IS
    'Not documented in the export specification but present in the file. Identifies the client instance.';


-- -------------------------------------------------------------------------
--  SAEIV stop export (arrets). All 25 source columns.
-- -------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS ops.saeiv_stop_exports (
    id                          uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    import_batch_id             uuid NOT NULL REFERENCES ops.import_batches (id),
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
    ON ops.saeiv_stop_exports (trip_id, stop_rank);
CREATE INDEX IF NOT EXISTS ix_saeiv_stop_exports_stop_code
    ON ops.saeiv_stop_exports (theoretical_stop_code);
CREATE INDEX IF NOT EXISTS ix_saeiv_stop_exports_batch
    ON ops.saeiv_stop_exports (import_batch_id);

COMMENT ON TABLE ops.saeiv_stop_exports IS
    'Faithful copy of the SAEIV stop export. An arret is a stop. One row per stop served by a trip.';
COMMENT ON COLUMN ops.saeiv_stop_exports.stop_id IS
    'Documented as the identifier of the logical stop, but it identifies the parent stop area rather than the individual stop: in the sample, 103 of 111 values cover more than one theoretical_stop_code and more than one coordinate. Never key a stop dimension on this column.';
COMMENT ON COLUMN ops.saeiv_stop_exports.theoretical_stop_code IS
    'The stable identifier of an individual stop. Maps to exactly one name and one coordinate across the whole sample. This is the correct key for a stop dimension.';
COMMENT ON COLUMN ops.saeiv_stop_exports.stop_rank IS
    'Position of the stop within the trip, starting at one. Together with trip_id this is unique across all 1508 sample rows.';
COMMENT ON COLUMN ops.saeiv_stop_exports.fare_stage_id IS
    'Identical to stop_id in every row of the sample. Stored separately because the two are different concepts and may diverge in production data.';
COMMENT ON COLUMN ops.saeiv_stop_exports.real_delay_on_time IS
    'Seconds early or late at the stop. Negative is early. Empty throughout the sample because no vehicles were operating.';
