using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cdns.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SplitCusDecStatusIntoInspectStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cus_dec_inspect_statuses",
                columns: table => new
                {
                    cus_dec_inspect_status_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cus_dec_id = table.Column<long>(type: "bigint", nullable: true),
                    cus_dec_global_identity = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    status_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_sync_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    external_system_number = table.Column<long>(type: "bigint", nullable: true),
                    cus_dec_status_number = table.Column<int>(type: "integer", nullable: true),
                    cus_dec_external_system_number = table.Column<long>(type: "bigint", nullable: true),
                    table_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cus_dec_inspect_statuses", x => x.cus_dec_inspect_status_id);
                    table.ForeignKey(
                        name: "fk_cus_dec_inspect_statuses_cus_decs_cus_dec_id",
                        column: x => x.cus_dec_id,
                        principalTable: "cus_decs",
                        principalColumn: "cus_dec_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_cus_dec_statuses_external_system_number_cus_dec_status_numb",
                table: "cus_dec_statuses",
                columns: new[] { "external_system_number", "cus_dec_status_number", "cus_dec_external_system_number", "table_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cus_dec_inspect_statuses_cus_dec_id",
                table: "cus_dec_inspect_statuses",
                column: "cus_dec_id");

            migrationBuilder.CreateIndex(
                name: "ix_cus_dec_inspect_statuses_external_system_number_cus_dec_ext",
                table: "cus_dec_inspect_statuses",
                columns: new[] { "external_system_number", "cus_dec_external_system_number", "table_name", "status" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cus_dec_inspect_statuses");

            migrationBuilder.DropIndex(
                name: "ix_cus_dec_statuses_external_system_number_cus_dec_status_numb",
                table: "cus_dec_statuses");
        }
    }
}
