-- =========================================================================
--  003 - Reference lists
--
--  Every "Reference List" section in the specification, as a table with its
--  values inserted, exactly as the spec names them:
--
--    A.1.1  Status (certificate)        certificate_statuses
--    A.2.1  Completion Status           trip_completion_statuses
--    A.3.1  Revenue Type                revenue_types
--    A.3.2  Penalty Type                penalty_types
--    A.4.1  Captured By Role            captured_by_roles
--    A.4.2  Status (penalty capture)    review_statuses
--    A.5.1  Refundable Penalty Type     refundable_penalty_types
--    A.5.2  Sub Refundable Penalty Type sub_refundable_penalty_types
--    A.6.1  Record Type                 form_f1_record_types
--    A.6.2  Captured By Role (F1)       captured_by_roles (same two values,
--                                       one table serves both A.4.1 and A.6.2)
--    A.6.3  Status (F1)                 review_statuses (same four values as
--                                       A.4.2; the terminality rules differ by
--                                       module and are enforced in the app)
--    A.10.1 URL Key                     url_keys
--    A.14.1 DNO Reason                  dno_reasons (in 004 - the spec makes
--                                       it a SuperAdmin-editable table, not a
--                                       fixed list)
--
--  The transactional tables reference these by value, so the database
--  enforces membership and the UI reads its dropdowns from the same rows.
--  The values are part of the schema, not seed data: the spec fixes them,
--  so they are inserted here, and neither application role may write these
--  tables (see 009). A new value arrives as a new script, through review.
--
--  Values are the primary keys. The spec's own English is the identity of
--  each entry; a surrogate uuid would only put a join between every status
--  check and its answer.
--
--  Run after 002. Safe to re-run.
-- =========================================================================

-- A.1.1 - certificate lifecycle.
CREATE TABLE IF NOT EXISTS certificate_statuses (
    status_value  text PRIMARY KEY,
    display_order integer NOT NULL,
    description   text NOT NULL
);

INSERT INTO certificate_statuses (status_value, display_order, description) VALUES
    ('Draft',     1, 'Editable by the Operator Staff who created it; not yet visible to the Authoriser.'),
    ('Submitted', 2, 'Awaiting review (UC2). Still editable or withdrawable by Operator Staff until the Authoriser acts.'),
    ('Approved',  3, 'Approved by the Authoriser. Locks the certificate and every linked record; triggers the Form C and Form A snapshots.'),
    ('Rejected',  4, 'Rejected by the Authoriser with a reason. Returns to Operator Staff for correction and resubmission.')
ON CONFLICT (status_value) DO NOTHING;

COMMENT ON TABLE certificate_statuses IS
    'A.1.1. Fixed by the specification; neither application role may write here.';


-- A.2.1 - was the trip completed.
CREATE TABLE IF NOT EXISTS trip_completion_statuses (
    status_value  text PRIMARY KEY,
    display_order integer NOT NULL,
    description   text NOT NULL
);

INSERT INTO trip_completion_statuses (status_value, display_order, description) VALUES
    ('Completed',     1, 'Verified as completed per the trip completion logic.'),
    ('Not Completed', 2, 'Scheduled but not verified as completed.')
ON CONFLICT (status_value) DO NOTHING;

COMMENT ON TABLE trip_completion_statuses IS 'A.2.1.';


-- A.3.1 - revenue classification of a penalty.
CREATE TABLE IF NOT EXISTS revenue_types (
    revenue_type_value text PRIMARY KEY,
    display_order      integer NOT NULL
);

INSERT INTO revenue_types (revenue_type_value, display_order) VALUES
    ('Revenue',     1),
    ('Non-Revenue', 2)
ON CONFLICT (revenue_type_value) DO NOTHING;

COMMENT ON TABLE revenue_types IS 'A.3.1.';


-- A.3.2 - the six penalty categories. The counting scope for repeat-penalty
-- escalation is derived from the type (UC6 business rule), so it lives here
-- as a column rather than as prose someone must remember.
CREATE TABLE IF NOT EXISTS penalty_types (
    penalty_type_value        text PRIMARY KEY,
    display_order             integer NOT NULL,
    escalation_counting_scope text NOT NULL,

    CONSTRAINT ck_penalty_types_scope
        CHECK (escalation_counting_scope IN ('Trip', 'Contract', 'Vehicle'))
);

INSERT INTO penalty_types (penalty_type_value, display_order, escalation_counting_scope) VALUES
    ('Trips Not Operated',               1, 'Trip'),
    ('Time-Related Offences',            2, 'Trip'),
    ('Unsatisfactory Vehicle Condition', 3, 'Vehicle'),
    ('Unroadworthy',                     4, 'Vehicle'),
    ('Stop & Route Adherence',           5, 'Trip'),
    ('Duty & Destination Boards',        6, 'Contract')
ON CONFLICT (penalty_type_value) DO NOTHING;

COMMENT ON TABLE penalty_types IS
    'A.3.2. escalation_counting_scope drives the repeat-penalty streak (UC6): Trip-level types count the same trip across months, Contract-level any occurrence under the contract, Vehicle-level the same vehicle - which is why vehicle-scoped captures require a vehicle.';


-- A.4.1 / A.6.2 - who captured a record. Identical two values in both lists;
-- one table serves both.
CREATE TABLE IF NOT EXISTS captured_by_roles (
    role_value    text PRIMARY KEY,
    display_order integer NOT NULL
);

INSERT INTO captured_by_roles (role_value, display_order) VALUES
    ('Operator Staff', 1),
    ('Monitor',        2)
ON CONFLICT (role_value) DO NOTHING;

COMMENT ON TABLE captured_by_roles IS
    'A.4.1 and A.6.2 (identical lists). Set from the actor''s role at capture time, never user-editable.';


-- A.4.2 / A.6.3 - review outcome of a penalty or Form F1 record. Identical
-- four values; the modules differ only in which outcomes are terminal, and
-- that rule is the application''s (penalties: nothing terminal until the
-- certificate is approved; F1: Approved and Rejected immediately terminal).
CREATE TABLE IF NOT EXISTS review_statuses (
    status_value  text PRIMARY KEY,
    display_order integer NOT NULL,
    description   text NOT NULL
);

INSERT INTO review_statuses (status_value, display_order, description) VALUES
    ('Submitted',     1, 'Awaiting review. Editable and deletable by the capturing roles under the asymmetric rule.'),
    ('Approved',      2, 'Approved by the Authoriser. Terminal for Form F1; re-reviewable for penalties until the certificate is approved.'),
    ('Rejected',      3, 'Rejected with a reason. Terminal for Form F1; re-reviewable for penalties. Frees the uniqueness rule for a new record.'),
    ('Under Dispute', 4, 'Requires a reason; blocks certificate approval while any penalty holds it. Never terminal.')
ON CONFLICT (status_value) DO NOTHING;

COMMENT ON TABLE review_statuses IS
    'A.4.2 and A.6.3 (identical values). Terminality differs by module and is enforced in the application, not here.';


-- A.5.1 - refundable penalty classification on Form G2.
CREATE TABLE IF NOT EXISTS refundable_penalty_types (
    type_value    text PRIMARY KEY,
    display_order integer NOT NULL
);

INSERT INTO refundable_penalty_types (type_value, display_order) VALUES
    ('Deductible Refundable', 1),
    ('Released Refundable',   2)
ON CONFLICT (type_value) DO NOTHING;

COMMENT ON TABLE refundable_penalty_types IS 'A.5.1.';


-- A.5.2 - the five named refundable penalties.
CREATE TABLE IF NOT EXISTS sub_refundable_penalty_types (
    type_value    text PRIMARY KEY,
    display_order integer NOT NULL
);

INSERT INTO sub_refundable_penalty_types (type_value, display_order) VALUES
    ('Approved Vehicles Not Operated',                     1),
    ('Failure to Provide Information',                     2),
    ('Failure to Provide Information (<80% of Patronage)', 3),
    ('For Approved Ticket Machines Not Used',              4),
    ('Failure to Provide Customer Complaints Policy',      5)
ON CONFLICT (type_value) DO NOTHING;

COMMENT ON TABLE sub_refundable_penalty_types IS
    'A.5.2. A fixed list independent of the contract penalty catalogue - the spec records the relationship between the two as unenforced and awaiting confirmation.';


-- A.6.1 - the three Form F1 exception types.
CREATE TABLE IF NOT EXISTS form_f1_record_types (
    type_value    text PRIMARY KEY,
    display_order integer NOT NULL
);

INSERT INTO form_f1_record_types (type_value, display_order) VALUES
    ('Breakdown',                  1),
    ('Roadworthy Non-Conformance', 2),
    ('Standing Trip',              3)
ON CONFLICT (type_value) DO NOTHING;

COMMENT ON TABLE form_f1_record_types IS 'A.6.1.';


-- A.10.1 - which figure a reference URL documents.
CREATE TABLE IF NOT EXISTS url_keys (
    key_value     text PRIMARY KEY,
    display_order integer NOT NULL
);

INSERT INTO url_keys (key_value, display_order) VALUES
    ('Fuel Price Source', 1),
    ('CPI Source',        2)
ON CONFLICT (key_value) DO NOTHING;

COMMENT ON TABLE url_keys IS
    'A.10.1. A Labour source would be a third row once the client names one - the spec records that gap.';
