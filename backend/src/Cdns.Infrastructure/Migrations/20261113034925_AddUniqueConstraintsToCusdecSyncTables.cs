using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cdns.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueConstraintsToCusdecSyncTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_cus_decs_cus_dec_external_system_number",
                table: "cus_decs",
                column: "cus_dec_external_system_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cus_dec_taxes_cus_dec_external_system_number_item_number_ta",
                table: "cus_dec_taxes",
                columns: new[] { "cus_dec_external_system_number", "item_number", "tax_rank" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cus_dec_items_cus_dec_external_system_number_cus_dec_item_n",
                table: "cus_dec_items",
                columns: new[] { "cus_dec_external_system_number", "cus_dec_item_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cus_dec_inspections_cus_dec_external_system_number_inspecti",
                table: "cus_dec_inspections",
                columns: new[] { "cus_dec_external_system_number", "inspection_instance_id", "key_frd_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cus_dec_inspection_section_tab_cus_dec_external_system_numb",
                table: "cus_dec_inspection_section_tab",
                columns: new[] { "cus_dec_external_system_number", "ins_tab_external_system_number", "key_sec_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cus_dec_containers_cus_dec_external_system_number_container",
                table: "cus_dec_containers",
                columns: new[] { "cus_dec_external_system_number", "container_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cus_dec_container_statuses_external_system_number_container",
                table: "cus_dec_container_statuses",
                columns: new[] { "external_system_number", "container_status_number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_cus_decs_cus_dec_external_system_number",
                table: "cus_decs");

            migrationBuilder.DropIndex(
                name: "ix_cus_dec_taxes_cus_dec_external_system_number_item_number_ta",
                table: "cus_dec_taxes");

            migrationBuilder.DropIndex(
                name: "ix_cus_dec_items_cus_dec_external_system_number_cus_dec_item_n",
                table: "cus_dec_items");

            migrationBuilder.DropIndex(
                name: "ix_cus_dec_inspections_cus_dec_external_system_number_inspecti",
                table: "cus_dec_inspections");

            migrationBuilder.DropIndex(
                name: "ix_cus_dec_inspection_section_tab_cus_dec_external_system_numb",
                table: "cus_dec_inspection_section_tab");

            migrationBuilder.DropIndex(
                name: "ix_cus_dec_containers_cus_dec_external_system_number_container",
                table: "cus_dec_containers");

            migrationBuilder.DropIndex(
                name: "ix_cus_dec_container_statuses_external_system_number_container",
                table: "cus_dec_container_statuses");
        }
    }
}
