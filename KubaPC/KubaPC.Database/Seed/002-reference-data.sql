INSERT INTO dno_reasons (reason_value)
SELECT v.reason_value
FROM (VALUES ('Unrest'), ('Weather'), ('Other')) AS v (reason_value)
WHERE NOT EXISTS (
    SELECT 1 FROM dno_reasons d
    WHERE d.reason_value = v.reason_value AND d.is_active
);

INSERT INTO reference_urls (url_key, url_value)
SELECT v.url_key, v.url_value
FROM (VALUES
    ('Fuel Price Source',
     'https://fuelsindustry.org.za/consumer-information/fuel-prices-current-past/'),
    ('CPI Source',
     'https://www.statssa.gov.za/publications/P0141/CPIHistory.pdf')
) AS v (url_key, url_value)
WHERE NOT EXISTS (
    SELECT 1 FROM reference_urls u
    WHERE u.url_key = v.url_key AND u.is_active
);
