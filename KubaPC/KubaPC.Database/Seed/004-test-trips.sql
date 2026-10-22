INSERT INTO import_batches
    (import_type, source_file_name, period_month_date, import_status,
     row_count_read, row_count_accepted, row_count_rejected, completed_at)
SELECT 'JourneyAnalysis', 'SEED-TEST-DATA-not-a-real-import.xlsx', DATE '2026-08-01', 'Completed',
       0, 0, 0, now()
WHERE NOT EXISTS (
    SELECT 1 FROM import_batches
    WHERE source_file_name = 'SEED-TEST-DATA-not-a-real-import.xlsx' AND is_active
);

INSERT INTO routes (contract_id, route_number, direction, route_name, origin_name, destination_name, route_length_km, is_subsidised)
SELECT c.id, r.route_number, r.direction, r.route_name, r.origin_name, r.destination_name, r.route_length_km, TRUE
FROM contracts c
JOIN (VALUES
        ('ZNT 1129', '1129A', 'Out', 'Depot to Town',  'Depot',   'Town',    41.30),
        ('ZNT 1129', '1129A', 'In',  'Town to Depot',  'Town',    'Depot',   41.30),
        ('ZNT 1129', '1129B', 'Out', 'Depot to Centre','Depot',   'Centre',  27.65),
        ('ZNT 1129', '1129B', 'In',  'Centre to Depot','Centre',  'Depot',   27.65),
        ('MCB-01',   'M01',   'Out', 'North to City',  'North',   'City',    18.40),
        ('MCB-01',   'M01',   'In',  'City to North',  'City',    'North',   18.40)
     ) AS r (contract_number, route_number, direction, route_name, origin_name, destination_name, route_length_km)
  ON r.contract_number = c.contract_number
WHERE c.is_active
  AND NOT EXISTS (
      SELECT 1 FROM routes existing
      WHERE existing.contract_id = c.id
        AND existing.route_number = r.route_number
        AND existing.direction = r.direction
        AND existing.is_active
  );

INSERT INTO vehicles (operator_id, vehicle_number)
SELECT o.id, v.vehicle_number
FROM operators o
JOIN contracts c ON c.operator_id = o.id AND c.is_active
JOIN (VALUES
        ('ZNT 1129', 'CA 411-001'),
        ('ZNT 1129', 'CA 411-002'),
        ('ZNT 1129', 'CA 411-003'),
        ('MCB-01',   'CY 220-001'),
        ('MCB-01',   'CY 220-002'),
        ('MCB-01',   'CY 220-003')
     ) AS v (contract_number, vehicle_number)
  ON v.contract_number = c.contract_number
WHERE o.is_active
  AND NOT EXISTS (
      SELECT 1 FROM vehicles existing
      WHERE existing.operator_id = o.id
        AND existing.vehicle_number = v.vehicle_number
        AND existing.is_active
  );

INSERT INTO trips (route_id, scheduled_date, scheduled_departure, duty_number, trip_number, scheduled_km, source_batch_id)
SELECT r.id,
       day.scheduled_date,
       TIME '05:00' + (trip.trip_index * INTERVAL '2 hours'),
       'D' || lpad(((trip.trip_index % 3) + 1)::text, 2, '0'),
       'T' || trip.trip_index::text,
       r.route_length_km,
       batch.id
FROM routes r
JOIN contracts c ON c.id = r.contract_id AND c.is_active
JOIN import_batches batch
  ON batch.source_file_name = 'SEED-TEST-DATA-not-a-real-import.xlsx' AND batch.is_active
CROSS JOIN LATERAL generate_series(DATE '2026-08-01', DATE '2026-08-31', INTERVAL '1 day') AS day (scheduled_date)
CROSS JOIN LATERAL generate_series(1, CASE WHEN c.contract_number = 'ZNT 1129' THEN 6 ELSE 4 END) AS trip (trip_index)
WHERE r.is_active
  AND c.contract_number IN ('ZNT 1129', 'MCB-01')
  AND NOT EXISTS (
      SELECT 1 FROM trips existing
      WHERE existing.route_id = r.id
        AND existing.scheduled_date = day.scheduled_date::date
        AND existing.trip_number = 'T' || trip.trip_index::text
        AND existing.is_active
  );

INSERT INTO trip_operations
    (trip_id, is_operated, operated_km, is_did_not_operate, dno_reason_id, is_standing_trip,
     vehicle_number, driver_number, actual_departure_at, actual_arrival_at,
     passenger_count, ticket_count, pass_count, transfer_count, revenue_minor, source_batch_id)
SELECT t.id,
       outcome.kind = 'Operated',
       CASE outcome.kind
           WHEN 'NotOperated' THEN 0
           WHEN 'Short'       THEN round(t.scheduled_km * 0.75, 2)
           ELSE t.scheduled_km
       END,
       outcome.kind IN ('NotOperated', 'Standing'),
       CASE WHEN outcome.kind IN ('NotOperated', 'Standing') THEN reason.id END,
       outcome.kind = 'Standing',
       vehicle.vehicle_number,
       'DR' || lpad(((outcome.seed % 7) + 1)::text, 2, '0'),
       CASE WHEN outcome.kind <> 'NotOperated' THEN (t.scheduled_date + t.scheduled_departure) AT TIME ZONE 'Africa/Johannesburg' END,
       CASE WHEN outcome.kind <> 'NotOperated' THEN (t.scheduled_date + t.scheduled_departure + INTERVAL '70 minutes') AT TIME ZONE 'Africa/Johannesburg' END,
       CASE WHEN outcome.kind = 'Operated' THEN 25 + (outcome.seed % 40) END,
       CASE WHEN outcome.kind = 'Operated' THEN 18 + (outcome.seed % 30) END,
       CASE WHEN outcome.kind = 'Operated' THEN 4 + (outcome.seed % 9) END,
       CASE WHEN outcome.kind = 'Operated' THEN outcome.seed % 5 END,
       CASE WHEN outcome.kind = 'Operated' THEN (1200 + (outcome.seed % 40) * 65)::bigint END,
       t.source_batch_id
FROM trips t
JOIN import_batches batch
  ON batch.id = t.source_batch_id
 AND batch.source_file_name = 'SEED-TEST-DATA-not-a-real-import.xlsx'
JOIN routes r ON r.id = t.route_id
JOIN contracts c ON c.id = r.contract_id
JOIN operators o ON o.id = c.operator_id
CROSS JOIN LATERAL (
    SELECT step.seed,
           CASE
               WHEN step.seed % 20 = 0 THEN 'NotOperated'
               WHEN step.seed % 20 = 1 THEN 'Standing'
               WHEN step.seed % 13 = 0 THEN 'Short'
               ELSE 'Operated'
           END AS kind
    FROM (SELECT extract(day FROM t.scheduled_date)::int
                 + replace(t.trip_number, 'T', '')::int AS seed) AS step
) AS outcome
LEFT JOIN LATERAL (
    SELECT d.id
    FROM dno_reasons d
    WHERE d.is_active
    ORDER BY d.reason_value
    OFFSET (outcome.seed % GREATEST((SELECT count(*) FROM dno_reasons WHERE is_active), 1))
    LIMIT 1
) AS reason ON TRUE
LEFT JOIN LATERAL (
    SELECT v.vehicle_number
    FROM vehicles v
    WHERE v.operator_id = o.id AND v.is_active
    ORDER BY v.vehicle_number
    OFFSET (outcome.seed % GREATEST((SELECT count(*) FROM vehicles WHERE operator_id = o.id AND is_active), 1))
    LIMIT 1
) AS vehicle ON TRUE
WHERE t.is_active
  AND NOT EXISTS (
      SELECT 1 FROM trip_operations existing
      WHERE existing.trip_id = t.id AND existing.is_active
  );

UPDATE import_batches
SET row_count_read = counts.trip_count,
    row_count_accepted = counts.trip_count,
    modified_at = now()
FROM (
    SELECT batch.id AS batch_id, count(*) AS trip_count
    FROM import_batches batch
    JOIN trips t ON t.source_batch_id = batch.id AND t.is_active
    WHERE batch.source_file_name = 'SEED-TEST-DATA-not-a-real-import.xlsx' AND batch.is_active
    GROUP BY batch.id
) AS counts
WHERE import_batches.id = counts.batch_id
  AND import_batches.row_count_read <> counts.trip_count;
