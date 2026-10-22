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

CREATE TABLE IF NOT EXISTS trip_completion_statuses (
    status_value  text PRIMARY KEY,
    display_order integer NOT NULL,
    description   text NOT NULL
);

INSERT INTO trip_completion_statuses (status_value, display_order, description) VALUES
    ('Completed',     1, 'Verified as completed per the trip completion logic.'),
    ('Not Completed', 2, 'Scheduled but not verified as completed.')
ON CONFLICT (status_value) DO NOTHING;

CREATE TABLE IF NOT EXISTS revenue_types (
    revenue_type_value text PRIMARY KEY,
    display_order      integer NOT NULL
);

INSERT INTO revenue_types (revenue_type_value, display_order) VALUES
    ('Revenue',     1),
    ('Non-Revenue', 2)
ON CONFLICT (revenue_type_value) DO NOTHING;

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

CREATE TABLE IF NOT EXISTS captured_by_roles (
    role_value    text PRIMARY KEY,
    display_order integer NOT NULL
);

INSERT INTO captured_by_roles (role_value, display_order) VALUES
    ('Operator Staff', 1),
    ('Monitor',        2)
ON CONFLICT (role_value) DO NOTHING;

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

CREATE TABLE IF NOT EXISTS refundable_penalty_types (
    type_value    text PRIMARY KEY,
    display_order integer NOT NULL
);

INSERT INTO refundable_penalty_types (type_value, display_order) VALUES
    ('Deductible Refundable', 1),
    ('Released Refundable',   2)
ON CONFLICT (type_value) DO NOTHING;

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

CREATE TABLE IF NOT EXISTS form_f1_record_types (
    type_value    text PRIMARY KEY,
    display_order integer NOT NULL
);

INSERT INTO form_f1_record_types (type_value, display_order) VALUES
    ('Breakdown',                  1),
    ('Roadworthy Non-Conformance', 2),
    ('Standing Trip',              3)
ON CONFLICT (type_value) DO NOTHING;

CREATE TABLE IF NOT EXISTS url_keys (
    key_value     text PRIMARY KEY,
    display_order integer NOT NULL
);

INSERT INTO url_keys (key_value, display_order) VALUES
    ('Fuel Price Source', 1),
    ('CPI Source',        2)
ON CONFLICT (key_value) DO NOTHING;
