-- =========================================================================
--  005 - ops, derived domain tables
--
--  Derived from the landing tables by a transformation step: deduplicated,
--  identifiers resolved, names in plain English. The calculation reads these
--  and only these.
--
--  Run after 004. Safe to re-run.
-- =========================================================================

CREATE TABLE IF NOT EXISTS ops.routes (
    id                uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    contract_id       uuid NOT NULL REFERENCES identity.contracts (id),
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
    ON ops.routes (contract_id, route_number, direction) WHERE is_active;

COMMENT ON COLUMN ops.routes.direction IS
    'In or Out, matching the "In/ Out" column of the source reports.';
COMMENT ON COLUMN ops.routes.route_length_km IS
    'Drives every penalty: penalty equals route_length_km times rate times penalty_percentage.';
COMMENT ON COLUMN ops.routes.is_subsidised IS
    'A route outside the subsidy is operated but never paid for from this contract.';


CREATE TABLE IF NOT EXISTS ops.trips (
    id                   uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    route_id             uuid NOT NULL REFERENCES ops.routes (id),
    scheduled_date       date NOT NULL,
    scheduled_departure  time,
    duty_number          text,
    trip_number          text,
    scheduled_km         numeric(12,2) NOT NULL,
    source_batch_id      uuid REFERENCES ops.import_batches (id),
    is_active            boolean     NOT NULL DEFAULT TRUE,
    created_at           timestamptz NOT NULL DEFAULT now(),
    created_by           uuid,
    modified_at          timestamptz,
    modified_by          uuid
);
CREATE INDEX IF NOT EXISTS ix_trips_route_scheduled_date
    ON ops.trips (route_id, scheduled_date);
CREATE UNIQUE INDEX IF NOT EXISTS ux_trips_route_date_trip_number
    ON ops.trips (route_id, scheduled_date, trip_number) WHERE is_active;

COMMENT ON TABLE ops.trips IS
    'What was supposed to run, derived from the Contract Schedule. Deduplicated, unlike the landing table.';


CREATE TABLE IF NOT EXISTS ops.trip_operations (
    id                       uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    trip_id                  uuid NOT NULL REFERENCES ops.trips (id),
    is_operated              boolean NOT NULL DEFAULT FALSE,
    operated_km              numeric(12,2),
    is_did_not_operate       boolean NOT NULL DEFAULT FALSE,
    non_operation_reason_id  uuid REFERENCES config.non_operation_reasons (id),
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
    source_batch_id          uuid REFERENCES ops.import_batches (id),
    is_active                boolean     NOT NULL DEFAULT TRUE,
    created_at               timestamptz NOT NULL DEFAULT now(),
    created_by               uuid,
    modified_at              timestamptz,
    modified_by              uuid
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_trip_operations_trip
    ON ops.trip_operations (trip_id) WHERE is_active;

COMMENT ON TABLE ops.trip_operations IS
    'What actually ran. Derived from Journey Analysis today, and from the SAEIV exports once those carry real operating data.';
COMMENT ON COLUMN ops.trip_operations.operated_km IS
    'The kilometres Form E pays for. Sums to the operated kilometres figure on the certificate.';
COMMENT ON COLUMN ops.trip_operations.is_standing_trip IS
    'A standing kilometre is paid at the lower standing rate rather than the operated rate.';
COMMENT ON COLUMN ops.trip_operations.source_batch_id IS
    'The import that produced this row. Lets a wrong figure be traced back to the file it came from.';
