WITH data_names AS (
    SELECT 
        "DataId" AS data_id,
        "DataName" AS data_name
    FROM "Data"
),
correlations AS (
    SELECT 
        dr."DataIdSource" AS source_id,
        dr."DataIdTarget" AS target_id,
        dr."Correlation" AS correlation_value
    FROM "DataRelation" dr
)
SELECT 
    COALESCE(ds.data_name, 'Total') AS data_name,
    MAX(CASE WHEN dt.data_name = 'GOLD' THEN c.correlation_value END) AS GOLD,
    MAX(CASE WHEN dt.data_name = 'SILVER' THEN c.correlation_value END) AS SILVER,
    MAX(CASE WHEN dt.data_name = 'COPPER' THEN c.correlation_value END) AS COPPER,
    MAX(CASE WHEN dt.data_name = 'DOW JONES' THEN c.correlation_value END) AS "DOW JONES",
    MAX(CASE WHEN dt.data_name = 'S&P 500' THEN c.correlation_value END) AS "S&P 500",
    MAX(CASE WHEN dt.data_name = 'NASDAQ' THEN c.correlation_value END) AS NASDAQ,
    MAX(CASE WHEN dt.data_name = 'CRUDE OIL' THEN c.correlation_value END) AS "CRUDE OIL",
    MAX(CASE WHEN dt.data_name = 'CORN' THEN c.correlation_value END) AS CORN,
    MAX(CASE WHEN dt.data_name = 'LUMBER' THEN c.correlation_value END) AS LUMBER,
    MAX(CASE WHEN dt.data_name = 'TREASURY 10Y' THEN c.correlation_value END) AS "TREASURY 10Y",
    MAX(CASE WHEN dt.data_name = 'TREASURY 1Y' THEN c.correlation_value END) AS "TREASURY 1Y",
    MAX(CASE WHEN dt.data_name = 'TREASURY 30Y' THEN c.correlation_value END) AS "TREASURY 30Y",
    MAX(CASE WHEN dt.data_name = 'INFLATION' THEN c.correlation_value END) AS INFLATION,
    MAX(CASE WHEN dt.data_name = 'UNEMPLOYMENT' THEN c.correlation_value END) AS UNEMPLOYMENT,
    MAX(CASE WHEN dt.data_name = 'DEBT_GDP' THEN c.correlation_value END) AS DEBT_GDP
FROM data_names ds
CROSS JOIN data_names dt
LEFT JOIN correlations c ON c.source_id = ds.data_id AND c.target_id = dt.data_id
GROUP BY ds.data_id, ds.data_name
ORDER BY ds.data_id;