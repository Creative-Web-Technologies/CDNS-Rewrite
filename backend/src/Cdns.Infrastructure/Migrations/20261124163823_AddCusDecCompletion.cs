using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cdns.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCusDecCompletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CusDec_Consignee_Tabs",
                table: "cus_decs");

            migrationBuilder.DropIndex(
                name: "IX_CusDec_Declarant_Tabs",
                table: "cus_decs");

            migrationBuilder.AddColumn<long>(
                name: "consignee_completed_by_id",
                table: "cus_decs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "consignee_completed_date_time",
                table: "cus_decs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "declarant_completed_by_id",
                table: "cus_decs",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "declarant_completed_date_time",
                table: "cus_decs",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_cus_decs_consignee_company_id_consignee_completed_date_time",
                table: "cus_decs",
                columns: new[] { "consignee_company_id", "consignee_completed_date_time" },
                descending: new[] { false, true },
                filter: "consignee_claimed_by_id IS NOT NULL AND consignee_disclaimed_by_id IS NULL AND consignee_completed_date_time IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_cus_decs_consignee_company_id_consignee_disclaimed_date_time",
                table: "cus_decs",
                columns: new[] { "consignee_company_id", "consignee_disclaimed_date_time" },
                descending: new[] { false, true },
                filter: "consignee_disclaimed_by_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_cus_decs_consignee_company_id_cus_dec_date",
                table: "cus_decs",
                columns: new[] { "consignee_company_id", "cus_dec_date" },
                descending: new[] { false, true },
                filter: "consignee_claimed_by_id IS NOT NULL AND consignee_disclaimed_by_id IS NULL AND consignee_completed_date_time IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_cus_decs_consignee_company_id_cus_dec_date1",
                table: "cus_decs",
                columns: new[] { "consignee_company_id", "cus_dec_date" },
                descending: new[] { false, true },
                filter: "consignee_claimed_by_id IS NULL AND consignee_disclaimed_by_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_cus_decs_declarant_company_id_cus_dec_date",
                table: "cus_decs",
                columns: new[] { "declarant_company_id", "cus_dec_date" },
                descending: new[] { false, true },
                filter: "declarant_claimed_by_id IS NOT NULL AND declarant_disclaimed_by_id IS NULL AND declarant_completed_date_time IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_cus_decs_declarant_company_id_cus_dec_date1",
                table: "cus_decs",
                columns: new[] { "declarant_company_id", "cus_dec_date" },
                descending: new[] { false, true },
                filter: "declarant_claimed_by_id IS NULL AND declarant_disclaimed_by_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_cus_decs_declarant_company_id_declarant_completed_date_time",
                table: "cus_decs",
                columns: new[] { "declarant_company_id", "declarant_completed_date_time" },
                descending: new[] { false, true },
                filter: "declarant_claimed_by_id IS NOT NULL AND declarant_disclaimed_by_id IS NULL AND declarant_completed_date_time IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_cus_decs_declarant_company_id_declarant_disclaimed_date_time",
                table: "cus_decs",
                columns: new[] { "declarant_company_id", "declarant_disclaimed_date_time" },
                descending: new[] { false, true },
                filter: "declarant_disclaimed_by_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_cus_decs_consignee_company_id_consignee_completed_date_time",
                table: "cus_decs");

            migrationBuilder.DropIndex(
                name: "ix_cus_decs_consignee_company_id_consignee_disclaimed_date_time",
                table: "cus_decs");

            migrationBuilder.DropIndex(
                name: "ix_cus_decs_consignee_company_id_cus_dec_date",
                table: "cus_decs");

            migrationBuilder.DropIndex(
                name: "ix_cus_decs_consignee_company_id_cus_dec_date1",
                table: "cus_decs");

            migrationBuilder.DropIndex(
                name: "ix_cus_decs_declarant_company_id_cus_dec_date",
                table: "cus_decs");

            migrationBuilder.DropIndex(
                name: "ix_cus_decs_declarant_company_id_cus_dec_date1",
                table: "cus_decs");

            migrationBuilder.DropIndex(
                name: "ix_cus_decs_declarant_company_id_declarant_completed_date_time",
                table: "cus_decs");

            migrationBuilder.DropIndex(
                name: "ix_cus_decs_declarant_company_id_declarant_disclaimed_date_time",
                table: "cus_decs");

            migrationBuilder.DropColumn(
                name: "consignee_completed_by_id",
                table: "cus_decs");

            migrationBuilder.DropColumn(
                name: "consignee_completed_date_time",
                table: "cus_decs");

            migrationBuilder.DropColumn(
                name: "declarant_completed_by_id",
                table: "cus_decs");

            migrationBuilder.DropColumn(
                name: "declarant_completed_date_time",
                table: "cus_decs");

            migrationBuilder.CreateIndex(
                name: "IX_CusDec_Consignee_Tabs",
                table: "cus_decs",
                columns: new[] { "consignee_company_id", "consignee_disclaimed_by_id", "consignee_claimed_by_id", "cus_dec_date" },
                descending: new[] { false, false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_CusDec_Declarant_Tabs",
                table: "cus_decs",
                columns: new[] { "declarant_company_id", "declarant_disclaimed_by_id", "declarant_claimed_by_id", "cus_dec_date" },
                descending: new[] { false, false, false, true });
        }
    }
}
