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
