using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cdns.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncStatusToTaskScheduleAppDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "errors",
                table: "task_schedule_app_details",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_success",
                table: "task_schedule_app_details",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "errors",
                table: "task_schedule_app_details");

            migrationBuilder.DropColumn(
                name: "is_success",
                table: "task_schedule_app_details");
        }
    }
}
