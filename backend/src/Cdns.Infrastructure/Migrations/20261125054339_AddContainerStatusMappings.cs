using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cdns.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddContainerStatusMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "container_status_mappings",
                columns: table => new
                {
                    container_status_mapping_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    asycuda_status = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    asycuda_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    cdns_status = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    cdns_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_show = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    asycuda_status_key = table.Column<string>(type: "text", nullable: false, computedColumnSql: "lower(btrim(asycuda_status))", stored: true),
                    asycuda_description_key = table.Column<string>(type: "text", nullable: false, computedColumnSql: "lower(btrim(asycuda_description))", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_container_status_mappings", x => x.container_status_mapping_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_container_status_mappings_asycuda_status_key_asycuda_descri",
                table: "container_status_mappings",
                columns: new[] { "asycuda_status_key", "asycuda_description_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "container_status_mappings");
        }
    }
}
