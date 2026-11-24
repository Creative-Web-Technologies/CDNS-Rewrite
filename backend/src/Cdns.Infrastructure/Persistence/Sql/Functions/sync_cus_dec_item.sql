CREATE OR REPLACE FUNCTION sync_cus_dec_item(p_rows jsonb, p_now timestamp with time zone)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
    WITH source AS (
        SELECT
            (elem->>'CusDecExternalSystemNumber')::bigint AS cus_dec_external_system_number,
            (elem->>'CusDecItemNumber')::int               AS cus_dec_item_number,
            (elem->>'HsCode')                               AS hs_code,
            (elem->>'Description')                          AS description,
            (elem->>'Quantity')                             AS quantity,
            (elem->>'TotalValue')::numeric                  AS total_value,
            (elem->>'UnitPrice')::numeric                   AS unit_price,
            (elem->>'BlNumber')                              AS bl_number,
            (elem->>'GlobalIdentity')::uuid                 AS global_identity,
            ord
        FROM jsonb_array_elements(p_rows) WITH ORDINALITY AS t(elem, ord)
    ),
    deduped AS (
        SELECT DISTINCT ON (cus_dec_external_system_number, cus_dec_item_number) *
        FROM source
        ORDER BY cus_dec_external_system_number, cus_dec_item_number, ord ASC
    ),
    resolved AS (
        SELECT
            d.*,
            cd.cus_dec_id        AS cus_dec_id,
            cd.global_identity   AS cus_dec_global_identity
        FROM deduped d
        LEFT JOIN cus_decs cd
            ON cd.cus_dec_external_system_number = d.cus_dec_external_system_number
    )
    INSERT INTO cus_dec_items (
        cus_dec_id, cus_dec_global_identity, global_identity,
        hs_code, description, quantity, total_value, unit_price,
        cus_dec_external_system_number, cus_dec_item_number, bl_number,
        last_sync_date
    )
    SELECT
        r.cus_dec_id, r.cus_dec_global_identity, r.global_identity,
        r.hs_code, r.description, r.quantity, r.total_value, r.unit_price,
        r.cus_dec_external_system_number, r.cus_dec_item_number, r.bl_number,
        p_now
    FROM resolved r
    ON CONFLICT (cus_dec_external_system_number, cus_dec_item_number) DO UPDATE SET
        unit_price              = EXCLUDED.unit_price,
        bl_number               = EXCLUDED.bl_number,
        cus_dec_id               = COALESCE(cus_dec_items.cus_dec_id, EXCLUDED.cus_dec_id),
        cus_dec_global_identity  = COALESCE(cus_dec_items.cus_dec_global_identity, EXCLUDED.cus_dec_global_identity),
        last_sync_date           = EXCLUDED.last_sync_date;
END;
$$;