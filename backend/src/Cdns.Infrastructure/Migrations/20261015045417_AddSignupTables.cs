using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cdns.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSignupTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "members",
                columns: table => new
                {
                    member_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    first_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    last_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    email = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    notification_email = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    mobile = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    nic = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    profile_image = table.Column<string>(type: "text", nullable: true),
                    designation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    member_type = table.Column<int>(type: "integer", nullable: false),
                    default_subscription_type = table.Column<int>(type: "integer", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_members", x => x.member_id);
                });

            migrationBuilder.CreateTable(
                name: "temporary_members",
                columns: table => new
                {
                    temporary_member_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    first_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    email = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    mobile = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    nic = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    profile_image = table.Column<string>(type: "text", nullable: true),
                    designation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    one_time_password = table.Column<int>(type: "integer", nullable: true),
                    global_identity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    password = table.Column<string>(type: "text", nullable: true),
                    password_salt = table.Column<string>(type: "text", nullable: true),
                    default_subscription_type = table.Column<int>(type: "integer", nullable: true, defaultValue: 1),
                    created_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_temporary_members", x => x.temporary_member_id);
                });

            migrationBuilder.CreateTable(
                name: "member_notifications",
                columns: table => new
                {
                    member_notification_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    member_id = table.Column<long>(type: "bigint", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    message = table.Column<string>(type: "text", nullable: true),
                    notification_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    cus_dec_id = table.Column<long>(type: "bigint", nullable: true),
                    is_seen = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    seen_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    show_on_home = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    notification_type = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_member_notifications", x => x.member_notification_id);
                    table.ForeignKey(
                        name: "fk_member_notifications_members_member_id",
                        column: x => x.member_id,
                        principalTable: "members",
                        principalColumn: "member_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_member_notifications_member_id_is_deleted_is_seen_notificat",
                table: "member_notifications",
                columns: new[] { "member_id", "is_deleted", "is_seen", "notification_date_time" });

            migrationBuilder.CreateIndex(
                name: "ix_members_email",
                table: "members",
                column: "email",
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "member_notifications");

            migrationBuilder.DropTable(
                name: "temporary_members");

            migrationBuilder.DropTable(
                name: "members");
        }
    }
}
