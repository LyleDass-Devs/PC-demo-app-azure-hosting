-- =========================================================================
--  005 - Derived operational data
--
--  These stand in for the shared KubaOps tables the spec references:
--  routes for A.31 GTFS Route, trips for A.32 GTFS Trip. Derived from the
--  landing tables by the import's transformation step - deduplicated,
--  identifiers resolved, names plain English. The certificate and every
--  form read these, never the landing tables.
--
--  Run after 004. Safe to re-run.
-- =========================================================================

-- A.31 GTFS Route equivalent.
CREATE TABLE IF NOT EXISTS routes (
    id                uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    contract_id       uuid NOT NULL REFERENCES contracts (id),
    route_number      text NOT NULL,
    direction         text NOT NULL,
    route_name        text,
    origin_name       text,
    destination_name  text,
    route_length_km   numeric(12,2) NOT NULL,
    is_subsidised     boolean NOT NULL DEFAULT TRUE,
    is_active         boolean     NOT NULL DEFAULT TRUE,
    created_at        timestamptz NOT NULL DEFAULT now(),
    created_by        uuid,
    modified_at       timestamptz,
    modified_by       uuid
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_routes_contract_number_direction
    ON routes (contract_id, route_number, direction) WHERE is_active;

COMMENT ON TABLE routes IS
    'Stands in for KubaOps A.31 GTFS Route. Derived from the Contract Schedule and timetable imports.';
COMMENT ON COLUMN routes.direction IS
    'In or Out, matching the "In/ Out" column of the source reports.';


-- A.32 GTFS Trip equivalent - what was scheduled to run.
CREATE TABLE IF NOT EXISTS trips (
    id                   uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    route_id             uuid NOT NULL REFERENCES routes (id),
    scheduled_date       date NOT NULL,
    scheduled_departure  time,
    duty_number          text,
    trip_number          text,
    scheduled_km         numeric(12,2) NOT NULL,
    source_batch_id      uuid REFERENCES import_batches (id),
    is_active            boolean     NOT NULL DEFAULT TRUE,
    created_at           timestamptz NOT NULL DEFAULT now(),
    created_by           uuid,
    modified_at          timestamptz,
    modified_by          uuid
);
CREATE INDEX IF NOT EXISTS ix_trips_route_scheduled_date
    ON trips (route_id, scheduled_date);
CREATE UNIQUE INDEX IF NOT EXISTS ux_trips_route_date_trip_number
    ON trips (route_id, scheduled_date, trip_number) WHERE is_active;

COMMENT ON TABLE trips IS
    'Stands in for KubaOps A.32 GTFS Trip: what was scheduled to run, deduplicated from the Contract Schedule landing table. The trips a certificate summarises (A.1 Total_Trips_Scheduled) and its trip lines reference (A.2).';


-- What actually ran - one row per trip, from Journey Analysis (and SAEIV
-- once that export carries real operating data).
CREATE TABLE IF NOT EXISTS trip_operations (
    id                       uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    trip_id                  uuid NOT NULL REFERENCES trips (id),
    is_operated              boolean NOT NULL DEFAULT FALSE,
    operated_km              numeric(12,2),
    is_did_not_operate       boolean NOT NULL DEFAULT FALSE,
    dno_reason_id            uuid REFERENCES dno_reasons (id),
    is_standing_trip         boolean NOT NULL DEFAULT FALSE,
    vehicle_number           text,
    driver_number            text,
    actual_departure_at      timestamptz,
    actual_arrival_at        timestamptz,
    passenger_count          integer,
    ticket_count             integer,
    pass_count               integer,
    transfer_count           integer,
    revenue_minor            bigint,
    non_revenue_minor        bigint,
    source_batch_id          uuid REFERENCES import_batches (id),
    is_active                boolean     NOT NULL DEFAULT TRUE,
    created_at               timestamptz NOT NULL DEFAULT now(),
    created_by               uuid,
    modified_at              timestamptz,
    modified_by              uuid
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_trip_operations_trip
    ON trip_operations (trip_id) WHERE is_active;

COMMENT ON TABLE trip_operations IS
    'What actually ran. operated_km sums into Form E''s operated kilometres; is_operated feeds A.1 Total_Trips_Completed and A.2 Completion_Status.';
COMMENT ON COLUMN trip_operations.operated_km IS
    'The kilometres Form E pays for at the operated rate.';
COMMENT ON COLUMN trip_operations.is_standing_trip IS
    'Standing kilometres are paid at the lower standing rate (Form E). Also settable via an approved Form F1 Standing Trip record.';
