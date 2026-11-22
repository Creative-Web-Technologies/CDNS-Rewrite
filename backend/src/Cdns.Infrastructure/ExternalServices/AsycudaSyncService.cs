using Cdns.Application.DTOs.Sync;
using Cdns.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;

namespace Cdns.Infrastructure.ExternalServices;

public class AsycudaSyncService : IAsycudaSyncService
{
    private readonly string _connectionString;
    private readonly ILogger<AsycudaSyncService> _logger;

    public AsycudaSyncService(IConfiguration configuration, ILogger<AsycudaSyncService> logger)
    {
        _connectionString = configuration.GetConnectionString("AsycudaDb")
            ?? throw new InvalidOperationException("Missing 'AsycudaDb' connection string.");
        _logger = logger;
    }

    public async Task<IReadOnlyList<CusDecSyncRow>> GetChangedCusDecsAsync(DateTime since, CancellationToken cancellationToken)
    {
        var results = new List<CusDecSyncRow>();
        const int pageSize = 3000;
        var pageNumber = 1;
        var endDate = SriLankaTime.ToColombo(DateTime.UtcNow);
        var sinceColombo = SriLankaTime.ToColombo(since);

        await using var connection = new OracleConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        var hasMore = true;
        while (hasMore)
        {
            var offset = (pageNumber - 1) * pageSize;

            const string query = @"
                SELECT *
                FROM (
                    SELECT
                        g.INSTANCEID AS Instance_ID,
                        (IDE_CUO_COD ||'-' ||IDE_REG_SER||'-' || g.IDE_REG_NBR||'-'||DEC_REF_YER) AS CusDec_Number,
                        DEC_COD AS Declarent_TIN,
                        CAST(REPLACE(REPLACE(g.DEC_NAM, CHR(10), ' '), CHR(13), ' ') AS VARCHAR2(100)) AS Declarant_Name,
                        CMP_CON_COD AS Consignee_TIN,
                        CAST(REPLACE(REPLACE(g.CMP_CON_NAM, CHR(10), ' '), CHR(13), ' ') AS VARCHAR2(100)) AS Consignee_Name,
                        CMP_EXP_COD AS Exporter_TIN,
                        CAST(REPLACE(REPLACE(g.CMP_EXP_NAM, CHR(10), ' '), CHR(13), ' ') AS VARCHAR2(100)) AS Exporter_Name,
                        DEC_REF_YER AS Reg_Year,
                        IDE_CUO_COD AS Office_Code,
                        IDE_REG_SER AS Ser,
                        IDE_REG_NBR AS Reg_No,
                        IDE_REG_DAT AS Reg_Date,
                        IDE_RCP_DAT AS Rcp_Date,
                        RLO_DAT AS Release_Date,
                        IDE_TYP_SAD AS Sad_Type,
                        FIN_TOP_COD AS Terms_of_Payment,
                        TPT_LOP_COD AS Load_Port,
                        TPT_TOD_COD AS INCOTERM,
                        g.PTY_COL_DSC AS channel,
                        g.WHS_COD AS Warehouse_Code,
                        COALESCE(GEN_CAP, '0') AS Container_Count,
                        COALESCE(GEN_CTY_TRD, 'XX') AS Trade_Cty,
                        COALESCE(g.GEN_CTY_EPT_COD, 'XX') AS Export_Cty,
                        COALESCE(g.GEN_CTY_DES_COD, 'XX') AS Destination_Cty,
                        g.GEN_CTY_ORG AS Country_Of_Origin,
                        (VGS_INV_CUR_RAT * VGS_INV_AMT_FCX) AS Invoice_Value
                    FROM awunadm.sad_general_Segment g
                    WHERE
                        EXISTS (
                            SELECT *
                            FROM awunadm.UN_ASYBRK_TRACK k
                            JOIN awunadm.UN_ASYBRK_IED e ON e.ied_id = k.ied_id
                            WHERE
                                g.instanceid = e.instance_id AND
                                k.OP_DATE_TIME >= TO_DATE(:startDate, 'YYYY-MM-DD HH24:MI:SS') AND
                                k.OP_DATE_TIME <= TO_DATE(:endDate, 'YYYY-MM-DD HH24:MI:SS') AND
                                e.BINDER_ID = 'B_asysad'
                        )
                    ORDER BY g.INSTANCEID
                )
                OFFSET :offset ROWS FETCH NEXT :pageSize ROWS ONLY";

            await using var command = new OracleCommand(query, connection);
            command.Parameters.Add("startDate", sinceColombo.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.Add("endDate", endDate.ToString("yyyy-MM-dd HH:mm:ss"));
            command.Parameters.Add("offset", offset);
            command.Parameters.Add("pageSize", pageSize);

            var rowsThisPage = 0;
            await using var reader = (OracleDataReader)await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                rowsThisPage++;
                try
                {
                    results.Add(MapRow(reader));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Skipping a CusDec row from Oracle due to a mapping error.");
                }
            }

            hasMore = rowsThisPage == pageSize;
            pageNumber++;
        }

        return results;
    }

    private static CusDecSyncRow MapRow(OracleDataReader reader)
    {
        var sadType = GetString(reader, "Sad_Type");

        string? consigneeCompanyName = null;
        string? supplierCompanyName = null;
        if (sadType == "IM")
        {
            consigneeCompanyName = GetString(reader, "Consignee_Name");
            supplierCompanyName = GetString(reader, "Exporter_Name");
        }
        else if (sadType == "EX")
        {
            consigneeCompanyName = GetString(reader, "Exporter_Name");
            supplierCompanyName = GetString(reader, "Consignee_Name");
        }

        var consigneeTin = GetString(reader, "Consignee_TIN");
        if (string.IsNullOrEmpty(consigneeTin))
        {
            consigneeTin = GetString(reader, "Exporter_TIN");
        }

        var registrationDate = SriLankaTime.FromColomboToUtc(GetNullableDateTime(reader, "Reg_Date"));

        return new CusDecSyncRow
        {
            CusDecExternalSystemNumber = Convert.ToInt64(reader["Instance_ID"]),
            CusDecNumber = GetString(reader, "CusDec_Number"),
            DeclarantTinNumber = GetString(reader, "Declarent_TIN"),
            ConsigneeTinNumber = consigneeTin,
            ConsigneeCompanyName = consigneeCompanyName,
            SupplierCompanyName = supplierCompanyName,
            Channel = GetString(reader, "channel"),
            OfficeCode = GetString(reader, "Office_Code"),
            RegistrationDate = registrationDate,
            ReleaseDate = SriLankaTime.FromColomboToUtc(GetNullableDateTime(reader, "Release_Date")),
            CusDecDate = registrationDate,
            InvoiceValue = GetNullableDecimal(reader, "Invoice_Value") ?? 0,
            NoOfContainers = GetNullableInt(reader, "Container_Count") ?? 0,
            Origin = GetString(reader, "Country_Of_Origin"),
            Incoterm = GetString(reader, "INCOTERM"),
            Destination = GetString(reader, "Destination_Cty"),
            CusDecType = sadType,
            GlobalIdentity = Guid.NewGuid(),

            // Intentionally left null/0 here — this matches real legacy production behavior,
            // not just a gap on our side. Confirmed by reading the legacy source directly:
            //  - DeclarantCompanyName: SyncCusDec.sql takes it from the JSON payload (not a
            //    DB-side TIN lookup, unlike DeclarantCompanyId), but the production Oracle
            //    sync never assigns it on the C# side either. Always null in legacy today.
            //  - BlNumber: real column, accepted by SyncCusDec's JSON, but the production
            //    Oracle sync never sets it on the CusDec object. (CusDecItem has its own,
            //    correctly-populated BlNumber from Oracle's LNK_TPT column, but the SQL that
            //    would roll it up to CusDec.BlNumber is commented out in SyncCusDecItem.sql.)
            //  - NoOfItems: same story as BlNumber — real column, never assigned. Always 0.
            //  - Exporter: real column, never assigned, and has no Oracle source at all.
            // Tracked as a follow-up improvement once this sync is verified working end-to-end.
            DeclarantCompanyName = null,
            BlNumber = null,
            NoOfItems = null,
            Exporter = null,
        };
    }

    private static string? GetString(OracleDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    private static DateTime? GetNullableDateTime(OracleDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
    }

    private static decimal? GetNullableDecimal(OracleDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
    }

    private static int? GetNullableInt(OracleDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : Convert.ToInt32(reader.GetValue(ordinal));
    }
}