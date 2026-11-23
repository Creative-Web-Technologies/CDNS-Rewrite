using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cdns.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncCusDecFunction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION sync_cus_dec(p_rows jsonb, p_now timestamp without time zone)
RETURNS void
LANGUAGE plpgsql
AS $$
BEGIN
    WITH source AS (
        SELECT
            (elem->>'CusDecNumber')                            AS cus_dec_number,
            (elem->>'DeclarantTinNumber')                      AS declarant_tin_number,
            (elem->>'ConsigneeTinNumber')                      AS consignee_tin_number,
            (elem->>'InvoiceValue')::numeric                   AS invoice_value,
            TRUNC((elem->>'NoOfItems')::numeric)::int          AS no_of_items,
            (elem->>'BlNumber')                                AS bl_number,
            (elem->>'GlobalIdentity')::uuid                    AS global_identity,
            (elem->>'CusDecExternalSystemNumber')::bigint      AS cus_dec_external_system_number,
            (elem->>'CusDecDate')::timestamp                   AS cus_dec_date,
            (elem->>'Origin')                                  AS origin,
            (elem->>'OfficeCode')                              AS office_code,
            (elem->>'ReleaseDate')::timestamp                  AS release_date,
            (elem->>'RegistrationDate')::timestamp             AS registration_date,
            (elem->>'Exporter')                                AS exporter,
            (elem->>'Incoterm')                                AS incoterm,
            (elem->>'Destination')                             AS destination,
            (elem->>'Channel')                                 AS channel,
            COALESCE((elem->>'NoOfContainers')::int, 0)        AS no_of_containers,
            (elem->>'CusDecType')                              AS cus_dec_type,
            (elem->>'DeclarantCompanyName')                    AS declarant_company_name,
            (elem->>'ConsigneeCompanyName')                    AS consignee_company_name,
            (elem->>'SupplierCompanyName')                     AS supplier_company_name,
            ord
        FROM jsonb_array_elements(p_rows) WITH ORDINALITY AS t(elem, ord)
    ),
    deduped AS (
        SELECT DISTINCT ON (cus_dec_external_system_number) *
        FROM source
        ORDER BY cus_dec_external_system_number, ord DESC
    ),
    resolved AS (
        SELECT
            d.*,
            dc.company_id AS declarant_company_id,
            cc.company_id AS consignee_company_id
        FROM deduped d
        LEFT JOIN LATERAL (
            SELECT c.company_id FROM companies c
            WHERE d.declarant_tin_number IS NOT NULL
              AND c.tin_number = LEFT(d.declarant_tin_number, 9)
            ORDER BY c.company_id LIMIT 1
        ) dc ON TRUE
        LEFT JOIN LATERAL (
            SELECT c.company_id FROM companies c
            WHERE d.consignee_tin_number IS NOT NULL
              AND c.tin_number = LEFT(d.consignee_tin_number, 9)
            ORDER BY c.company_id LIMIT 1
        ) cc ON TRUE
    )
    INSERT INTO cus_decs (
        cus_dec_number, declarant_tin_number, consignee_tin_number,
        invoice_value, no_of_items, bl_number, global_identity,
        cus_dec_external_system_number, cus_dec_date, origin, office_code,
        release_date, registration_date, exporter, incoterm, destination,
        channel, no_of_containers, declarant_company_id, consignee_company_id,
        cus_dec_type, declarant_company_name, consignee_company_name,
        supplier_company_name, last_sync_date_time
    )
    SELECT
        r.cus_dec_number, r.declarant_tin_number, r.consignee_tin_number,
        r.invoice_value, r.no_of_items, r.bl_number, r.global_identity,
        r.cus_dec_external_system_number, r.cus_dec_date, r.origin, r.office_code,
        r.release_date, r.registration_date, r.exporter, r.incoterm, r.destination,
        r.channel, r.no_of_containers, r.declarant_company_id, r.consignee_company_id,
        r.cus_dec_type, r.declarant_company_name, r.consignee_company_name,
        r.supplier_company_name, p_now
    FROM resolved r
    ON CONFLICT (cus_dec_external_system_number) DO UPDATE SET
        release_date           = EXCLUDED.release_date,
        declarant_company_id   = EXCLUDED.declarant_company_id,
        consignee_company_id   = EXCLUDED.consignee_company_id,
        declarant_company_name = EXCLUDED.declarant_company_name,
        consignee_company_name = EXCLUDED.consignee_company_name,
        supplier_company_name  = EXCLUDED.supplier_company_name,
        last_sync_date_time    = EXCLUDED.last_sync_date_time;
END;
$$;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS sync_cus_dec(jsonb, timestamp without time zone);");
        }
    }
}