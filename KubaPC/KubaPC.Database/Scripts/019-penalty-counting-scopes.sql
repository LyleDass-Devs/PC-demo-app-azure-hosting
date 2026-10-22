UPDATE penalty_types SET escalation_counting_scope = 'Trip'
WHERE penalty_type_value IN ('Trips Not Operated', 'Time-Related Offences', 'Stop & Route Adherence')
  AND escalation_counting_scope <> 'Trip';

UPDATE penalty_types SET escalation_counting_scope = 'Contract'
WHERE penalty_type_value = 'Duty & Destination Boards'
  AND escalation_counting_scope <> 'Contract';

UPDATE penalty_types SET escalation_counting_scope = 'Vehicle'
WHERE penalty_type_value IN ('Unroadworthy', 'Unsatisfactory Vehicle Condition')
  AND escalation_counting_scope <> 'Vehicle';
