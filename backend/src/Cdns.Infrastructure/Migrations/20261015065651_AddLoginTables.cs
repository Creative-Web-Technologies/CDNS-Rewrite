using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cdns.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLoginTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.CreateTable(
                name: "member_activity_logs",
                columns: table => new
                {
                    member_activity_log_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    activity = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    activity_json = table.Column<string>(type: "text", nullable: true),
                    stored_procedure_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    log_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    member_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_member_activity_logs", x => x.member_activity_log_id);
                    table.ForeignKey(
                        name: "fk_member_activity_logs_members_member_id",
                        column: x => x.member_id,
                        principalTable: "members",
                        principalColumn: "member_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "member_login_sessions",
                columns: table => new
                {
                    member_login_session_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    member_id = table.Column<long>(type: "bigint", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_name = table.Column<string>(type: "citext", nullable: false),
                    key = table.Column<string>(type: "text", nullable: true),
                    login_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_member_login_sessions", x => x.member_login_session_id);
                    table.ForeignKey(
                        name: "fk_member_login_sessions_members_member_id",
                        column: x => x.member_id,
                        principalTable: "members",
                        principalColumn: "member_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "member_user_login_details",
                columns: table => new
                {
                    member_user_login_detail_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    member_id = table.Column<long>(type: "bigint", nullable: false),
                    login_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    ip_address = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_member_user_login_details", x => x.member_user_login_detail_id);
                    table.ForeignKey(
                        name: "fk_member_user_login_details_members_member_id",
                        column: x => x.member_id,
                        principalTable: "members",
                        principalColumn: "member_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "member_users",
                columns: table => new
                {
                    member_id = table.Column<long>(type: "bigint", nullable: false),
                    user_name = table.Column<string>(type: "citext", nullable: false),
                    password = table.Column<string>(type: "text", nullable: false),
                    password_salt = table.Column<string>(type: "text", nullable: false),
                    created_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    activation_code = table.Column<int>(type: "integer", nullable: false),
                    global_identity = table.Column<Guid>(type: "uuid", nullable: false),
                    last_otp_generated_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    suspended_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    user_status_type = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_active_otp = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    member_type = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    required_ereg_verification = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_wrong_attempt = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    last_login_attempt_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_member_users", x => x.member_id);
                    table.ForeignKey(
                        name: "fk_member_users_members_member_id",
                        column: x => x.member_id,
                        principalTable: "members",
                        principalColumn: "member_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_member_activity_logs_member_id",
                table: "member_activity_logs",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_member_login_sessions_member_id",
                table: "member_login_sessions",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_member_login_sessions_session_id",
                table: "member_login_sessions",
                column: "session_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_member_user_login_details_member_id",
                table: "member_user_login_details",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_member_users_user_name",
                table: "member_users",
                column: "user_name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "member_activity_logs");

            migrationBuilder.DropTable(
                name: "member_login_sessions");

            migrationBuilder.DropTable(
                name: "member_user_login_details");

            migrationBuilder.DropTable(
                name: "member_users");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");
        }
    }
}
