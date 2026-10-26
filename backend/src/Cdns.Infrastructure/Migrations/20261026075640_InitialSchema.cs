using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cdns.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.CreateTable(
                name: "companies",
                columns: table => new
                {
                    company_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    tin_number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    e_reg_system_reference_number = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    company_status_enum = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_companies", x => x.company_id);
                });

            migrationBuilder.CreateTable(
                name: "cus_dec_inspection_mappings",
                columns: table => new
                {
                    cus_dec_inspection_mapping_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    asycuda_frd_nfr = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_show = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cus_dec_inspection_mappings", x => x.cus_dec_inspection_mapping_id);
                });

            migrationBuilder.CreateTable(
                name: "cus_dec_inspection_section_tab_status_mappings",
                columns: table => new
                {
                    cus_dec_inspection_section_tab_status_mapping_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    asycuda_section_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    cdns_section_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_show = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    office_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cus_dec_inspection_section_tab_status_mappings", x => x.cus_dec_inspection_section_tab_status_mapping_id);
                });

            migrationBuilder.CreateTable(
                name: "cus_dec_payment_summaries",
                columns: table => new
                {
                    cus_dec_payment_summary_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    declarant_tin_number = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    consignee_tin_number = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    total_cif = table.Column<decimal>(type: "numeric(28,2)", precision: 28, scale: 2, nullable: true),
                    total_tax_paid = table.Column<decimal>(type: "numeric(28,2)", precision: 28, scale: 2, nullable: true),
                    inspection_recoveries = table.Column<decimal>(type: "numeric(28,2)", precision: 28, scale: 2, nullable: true),
                    last_sync_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cus_dec_payment_summaries", x => x.cus_dec_payment_summary_id);
                });

            migrationBuilder.CreateTable(
                name: "cus_dec_status_mappings",
                columns: table => new
                {
                    cus_dec_status_mapping_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    asycuda_status = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    asycuda_description = table.Column<string>(type: "text", nullable: true),
                    cdns_status = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    cdns_description = table.Column<string>(type: "text", nullable: true),
                    is_show = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    office_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cus_dec_status_mappings", x => x.cus_dec_status_mapping_id);
                });

            migrationBuilder.CreateTable(
                name: "department",
                columns: table => new
                {
                    department_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    department_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_department", x => x.department_id);
                });

            migrationBuilder.CreateTable(
                name: "group",
                columns: table => new
                {
                    group_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    group_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_group", x => x.group_id);
                });

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
                name: "module",
                columns: table => new
                {
                    module_id = table.Column<int>(type: "integer", nullable: false),
                    module_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    area = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    controller = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    action = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    icon_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    small_icon_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    bg_color_class = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    module_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_module", x => x.module_id);
                });

            migrationBuilder.CreateTable(
                name: "office_codes",
                columns: table => new
                {
                    office_code_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    office_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    code = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_office_codes", x => x.office_code_id);
                });

            migrationBuilder.CreateTable(
                name: "smtp_mail_settings",
                columns: table => new
                {
                    smtp_mail_setting_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    smtp_mail_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    from_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    from_address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    smtp_username = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    smtp_password = table.Column<string>(type: "text", nullable: false),
                    smtp_uri = table.Column<string>(type: "text", nullable: false),
                    smtp_port = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_smtp_mail_settings", x => x.smtp_mail_setting_id);
                });

            migrationBuilder.CreateTable(
                name: "subscriptions",
                columns: table => new
                {
                    subscription_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    subscription_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    no_of_active_duration = table.Column<int>(type: "integer", nullable: false),
                    subscription_duration_type_enum = table.Column<int>(type: "integer", nullable: true),
                    subscription_type_enum = table.Column<int>(type: "integer", nullable: true, defaultValue: 0),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    subscription_action_type_enum = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    subscription_status_enum = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()"),
                    created_by_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_by_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscriptions", x => x.subscription_id);
                });

            migrationBuilder.CreateTable(
                name: "system_serials",
                columns: table => new
                {
                    system_serial_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    auto_increment_serial = table.Column<long>(type: "bigint", nullable: true, defaultValue: 0L),
                    daily_increment_serial = table.Column<long>(type: "bigint", nullable: true, defaultValue: 0L),
                    monthly_increment_serial = table.Column<long>(type: "bigint", nullable: true, defaultValue: 0L),
                    yearly_increment_serial = table.Column<long>(type: "bigint", nullable: true, defaultValue: 0L),
                    serial_format_expression = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    serial_name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    document_type_enum = table.Column<int>(type: "integer", nullable: true, defaultValue: 0),
                    generated_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false),
                    is_default_serial = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_system_serials", x => x.system_serial_id);
                });

            migrationBuilder.CreateTable(
                name: "task_schedule_app_details",
                columns: table => new
                {
                    task_schedule_app_detail_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    app_id = table.Column<Guid>(type: "uuid", nullable: false),
                    app_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    last_run_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_task_schedule_app_details", x => x.task_schedule_app_detail_id);
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
                name: "company_status_details",
                columns: table => new
                {
                    company_status_detail_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<int>(type: "integer", nullable: false),
                    company_status_enum = table.Column<int>(type: "integer", nullable: false),
                    created_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    employee_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_company_status_details", x => x.company_status_detail_id);
                    table.ForeignKey(
                        name: "fk_company_status_details_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "company_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cus_decs",
                columns: table => new
                {
                    cus_dec_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cus_dec_number = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    declarant_tin_number = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    consignee_tin_number = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    declarant_company_id = table.Column<int>(type: "integer", nullable: true),
                    consignee_company_id = table.Column<int>(type: "integer", nullable: true),
                    invoice_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    no_of_items = table.Column<int>(type: "integer", nullable: true),
                    bl_number = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    global_identity = table.Column<Guid>(type: "uuid", nullable: false),
                    cus_dec_external_system_number = table.Column<long>(type: "bigint", nullable: false),
                    last_sync_date_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "now()"),
                    declarant_claimed_by_id = table.Column<long>(type: "bigint", nullable: true),
                    consignee_claimed_by_id = table.Column<long>(type: "bigint", nullable: true),
                    declarant_disclaimed_by_id = table.Column<long>(type: "bigint", nullable: true),
                    consignee_disclaimed_by_id = table.Column<long>(type: "bigint", nullable: true),
                    declarant_claimed_date_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    consignee_claimed_date_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    declarant_disclaimed_date_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    consignee_disclaimed_date_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    is_ready_to_process = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ready_to_process_mark_by_id = table.Column<long>(type: "bigint", nullable: true),
                    ready_to_process_mark_date_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    cus_dec_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    origin = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    office_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    release_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    registration_date = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    exporter = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    incoterm = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    destination = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    channel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    no_of_containers = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    cus_dec_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    can_not_process = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    can_not_process_by_id = table.Column<long>(type: "bigint", nullable: true),
                    can_not_process_mark_date_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    can_not_process_reason = table.Column<string>(type: "text", nullable: true),
                    can_process = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    can_process_by_id = table.Column<long>(type: "bigint", nullable: true),
                    can_process_mark_date_time = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    declarant_company_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    consignee_company_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    supplier_company_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cus_decs", x => x.cus_dec_id);
                    table.ForeignKey(
                        name: "fk_cus_decs_companies_consignee_company_id",
                        column: x => x.consignee_company_id,
                        principalTable: "companies",
                        principalColumn: "company_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_cus_decs_companies_declarant_company_id",
                        column: x => x.declarant_company_id,
                        principalTable: "companies",
                        principalColumn: "company_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "employees",
                columns: table => new
                {
                    employee_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    department_id = table.Column<int>(type: "integer", nullable: true),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    employee_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    profile_image_url = table.Column<string>(type: "text", nullable: true),
                    email = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_employees", x => x.employee_id);
                    table.ForeignKey(
                        name: "fk_employees_department_department_id",
                        column: x => x.department_id,
                        principalTable: "department",
                        principalColumn: "department_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "company_member_mappings",
                columns: table => new
                {
                    company_member_mapping_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<int>(type: "integer", nullable: false),
                    member_id = table.Column<long>(type: "bigint", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_company_member_mappings", x => x.company_member_mapping_id);
                    table.ForeignKey(
                        name: "fk_company_member_mappings_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "company_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_company_member_mappings_members_member_id",
                        column: x => x.member_id,
                        principalTable: "members",
                        principalColumn: "member_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "customer_support_tickets",
                columns: table => new
                {
                    customer_support_ticket_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    member_id = table.Column<long>(type: "bigint", nullable: false),
                    ticket_number = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    contact_email = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    contact_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    message = table.Column<string>(type: "text", nullable: true),
                    created_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    is_completed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_customer_support_tickets", x => x.customer_support_ticket_id);
                    table.ForeignKey(
                        name: "fk_customer_support_tickets_members_member_id",
                        column: x => x.member_id,
                        principalTable: "members",
                        principalColumn: "member_id",
                        onDelete: ReferentialAction.Restrict);
                });

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

            migrationBuilder.CreateTable(
                name: "subscription_requests",
                columns: table => new
                {
                    subscription_request_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    member_id = table.Column<long>(type: "bigint", nullable: false),
                    company_id = table.Column<int>(type: "integer", nullable: true),
                    requested_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    respond_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    responder_id = table.Column<long>(type: "bigint", nullable: true),
                    comment = table.Column<string>(type: "text", nullable: true),
                    responder_comment = table.Column<string>(type: "text", nullable: true),
                    subscription_request_number = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    subscription_request_status_enum = table.Column<int>(type: "integer", nullable: true, defaultValue: 0),
                    subscription_type_enum = table.Column<int>(type: "integer", nullable: true, defaultValue: 0),
                    global_identity = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: true, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_subscription_requests", x => x.subscription_request_id);
                    table.ForeignKey(
                        name: "fk_subscription_requests_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "company_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_subscription_requests_members_member_id",
                        column: x => x.member_id,
                        principalTable: "members",
                        principalColumn: "member_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "menus",
                columns: table => new
                {
                    menu_id = table.Column<int>(type: "integer", nullable: false),
                    module_id = table.Column<int>(type: "integer", nullable: true),
                    menu_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    area = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    controller = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    action = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    icon_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    menu_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_menus", x => x.menu_id);
                    table.ForeignKey(
                        name: "fk_menus_module_module_id",
                        column: x => x.module_id,
                        principalTable: "module",
                        principalColumn: "module_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cus_dec_ready_to_process_settings",
                columns: table => new
                {
                    cus_dec_ready_to_process_setting_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    office_code_id = table.Column<int>(type: "integer", nullable: true),
                    no_of_items = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    all_items = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    all_consignees = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_by_id = table.Column<long>(type: "bigint", nullable: false),
                    created_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    updated_by_id = table.Column<long>(type: "bigint", nullable: true),
                    updated_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cus_dec_ready_to_process_settings", x => x.cus_dec_ready_to_process_setting_id);
                    table.ForeignKey(
                        name: "fk_cus_dec_ready_to_process_settings_office_codes_office_code_",
                        column: x => x.office_code_id,
                        principalTable: "office_codes",
                        principalColumn: "office_code_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cus_dec_containers",
                columns: table => new
                {
                    cus_dec_container_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cus_dec_id = table.Column<long>(type: "bigint", nullable: true),
                    cus_dec_global_identity = table.Column<Guid>(type: "uuid", nullable: true),
                    global_identity = table.Column<Guid>(type: "uuid", nullable: true),
                    container = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    location = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    current_status = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_sync_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    size = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    arrival_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    voyage = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    vessel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    cus_dec_external_system_number = table.Column<long>(type: "bigint", nullable: true),
                    container_key = table.Column<int>(type: "integer", nullable: true),
                    container_pass_number = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cus_dec_containers", x => x.cus_dec_container_id);
                    table.ForeignKey(
                        name: "fk_cus_dec_containers_cus_decs_cus_dec_id",
                        column: x => x.cus_dec_id,
                        principalTable: "cus_decs",
                        principalColumn: "cus_dec_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cus_dec_inspection_section_tab",
                columns: table => new
                {
                    cus_dec_inspection_section_tab_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cus_dec_id = table.Column<long>(type: "bigint", nullable: true),
                    cus_dec_external_system_number = table.Column<long>(type: "bigint", nullable: true),
                    ins_tab_external_system_number = table.Column<long>(type: "bigint", nullable: true),
                    key_sec_number = table.Column<int>(type: "integer", nullable: true),
                    section_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    section_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_sync_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cus_dec_inspection_section_tab", x => x.cus_dec_inspection_section_tab_id);
                    table.ForeignKey(
                        name: "fk_cus_dec_inspection_section_tab_cus_decs_cus_dec_id",
                        column: x => x.cus_dec_id,
                        principalTable: "cus_decs",
                        principalColumn: "cus_dec_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cus_dec_inspections",
                columns: table => new
                {
                    cus_dec_inspection_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    issue_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    cus_dec_id = table.Column<long>(type: "bigint", nullable: true),
                    cus_dec_external_system_number = table.Column<long>(type: "bigint", nullable: true),
                    last_sync_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    inspection_instance_id = table.Column<long>(type: "bigint", nullable: true),
                    key_frd_number = table.Column<int>(type: "integer", nullable: true),
                    frd_nfr = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    frd_uni = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    frd_ubr = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cus_dec_inspections", x => x.cus_dec_inspection_id);
                    table.ForeignKey(
                        name: "fk_cus_dec_inspections_cus_decs_cus_dec_id",
                        column: x => x.cus_dec_id,
                        principalTable: "cus_decs",
                        principalColumn: "cus_dec_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cus_dec_items",
                columns: table => new
                {
                    cus_dec_item_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cus_dec_id = table.Column<long>(type: "bigint", nullable: true),
                    cus_dec_global_identity = table.Column<Guid>(type: "uuid", nullable: true),
                    global_identity = table.Column<Guid>(type: "uuid", nullable: true),
                    hs_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    last_sync_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    quantity = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    unit_price = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    total_value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    cus_dec_external_system_number = table.Column<long>(type: "bigint", nullable: true),
                    cus_dec_item_number = table.Column<int>(type: "integer", nullable: true),
                    bl_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cus_dec_items", x => x.cus_dec_item_id);
                    table.ForeignKey(
                        name: "fk_cus_dec_items_cus_decs_cus_dec_id",
                        column: x => x.cus_dec_id,
                        principalTable: "cus_decs",
                        principalColumn: "cus_dec_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cus_dec_statuses",
                columns: table => new
                {
                    cus_dec_status_id = table.Column<long>(type: "bigint", nullable: false)
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
                    table.PrimaryKey("pk_cus_dec_statuses", x => x.cus_dec_status_id);
                    table.ForeignKey(
                        name: "fk_cus_dec_statuses_cus_decs_cus_dec_id",
                        column: x => x.cus_dec_id,
                        principalTable: "cus_decs",
                        principalColumn: "cus_dec_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cus_dec_taxes",
                columns: table => new
                {
                    cus_dec_tax_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cus_dec_id = table.Column<long>(type: "bigint", nullable: true),
                    tax_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    tax_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    last_sync_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    cus_dec_external_system_number = table.Column<long>(type: "bigint", nullable: true),
                    item_number = table.Column<int>(type: "integer", nullable: true),
                    tax_rank = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cus_dec_taxes", x => x.cus_dec_tax_id);
                    table.ForeignKey(
                        name: "fk_cus_dec_taxes_cus_decs_cus_dec_id",
                        column: x => x.cus_dec_id,
                        principalTable: "cus_decs",
                        principalColumn: "cus_dec_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "system_activity_logs",
                columns: table => new
                {
                    system_activity_log_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    activity = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    activity_json = table.Column<string>(type: "text", nullable: true),
                    stored_procedure_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    log_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()"),
                    employee_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_system_activity_logs", x => x.system_activity_log_id);
                    table.ForeignKey(
                        name: "fk_system_activity_logs_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "employee_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "system_error_logs",
                columns: table => new
                {
                    system_error_log_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    activity = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    error = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    stored_procedure_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    log_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()"),
                    employee_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_system_error_logs", x => x.system_error_log_id);
                    table.ForeignKey(
                        name: "fk_system_error_logs_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "employee_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "user",
                columns: table => new
                {
                    employee_id = table.Column<long>(type: "bigint", nullable: false),
                    user_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    password = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    password_salt = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    activation_code = table.Column<int>(type: "integer", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_suspended = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    suspended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    user_global_identity = table.Column<Guid>(type: "uuid", nullable: true),
                    last_otp_generated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    otp_sequence = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user", x => x.employee_id);
                    table.ForeignKey(
                        name: "fk_user_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "employee_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_group_mappings",
                columns: table => new
                {
                    user_group_mapping_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    employee_id = table.Column<long>(type: "bigint", nullable: false),
                    group_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_group_mappings", x => x.user_group_mapping_id);
                    table.ForeignKey(
                        name: "fk_user_group_mappings_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "employee_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_group_mappings_group_group_id",
                        column: x => x.group_id,
                        principalTable: "group",
                        principalColumn: "group_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "member_subscriptions",
                columns: table => new
                {
                    member_subscription_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    subscription_request_id = table.Column<long>(type: "bigint", nullable: false),
                    member_id = table.Column<long>(type: "bigint", nullable: false),
                    subscription_id = table.Column<int>(type: "integer", nullable: false),
                    start_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expiry_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    paid_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false, defaultValue: 0m),
                    paid_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    member_subscription_status_enum = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    member_subscription_number = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    subscription_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    no_of_active_duration = table.Column<int>(type: "integer", nullable: false),
                    subscription_duration_type_enum = table.Column<int>(type: "integer", nullable: true),
                    subscription_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    transaction_reference_number = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    global_identity = table.Column<Guid>(type: "uuid", nullable: false),
                    subscription_type_enum = table.Column<int>(type: "integer", nullable: false),
                    subscription_action_type_enum = table.Column<int>(type: "integer", nullable: false),
                    company_id = table.Column<int>(type: "integer", nullable: false),
                    company_master_member_id = table.Column<long>(type: "bigint", nullable: false),
                    key = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_member_subscriptions", x => x.member_subscription_id);
                    table.ForeignKey(
                        name: "fk_member_subscriptions_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "company_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_member_subscriptions_members_member_id",
                        column: x => x.member_id,
                        principalTable: "members",
                        principalColumn: "member_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_member_subscriptions_subscription_requests_subscription_req",
                        column: x => x.subscription_request_id,
                        principalTable: "subscription_requests",
                        principalColumn: "subscription_request_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_member_subscriptions_subscriptions_subscription_id",
                        column: x => x.subscription_id,
                        principalTable: "subscriptions",
                        principalColumn: "subscription_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "menu_items",
                columns: table => new
                {
                    menu_item_id = table.Column<int>(type: "integer", nullable: false),
                    menu_id = table.Column<int>(type: "integer", nullable: true),
                    menu_item_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    area = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    controller = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    action = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    icon_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    menu_item_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    work_space_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    report_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_power_bi_dashboard = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_menu_items", x => x.menu_item_id);
                    table.ForeignKey(
                        name: "fk_menu_items_menus_menu_id",
                        column: x => x.menu_id,
                        principalTable: "menus",
                        principalColumn: "menu_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ready_to_process_consignees",
                columns: table => new
                {
                    ready_to_process_consignee_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cus_dec_ready_to_process_setting_id = table.Column<int>(type: "integer", nullable: true),
                    consignee_tin_number = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ready_to_process_consignees", x => x.ready_to_process_consignee_id);
                    table.ForeignKey(
                        name: "fk_ready_to_process_consignees_cus_dec_ready_to_process_settin",
                        column: x => x.cus_dec_ready_to_process_setting_id,
                        principalTable: "cus_dec_ready_to_process_settings",
                        principalColumn: "cus_dec_ready_to_process_setting_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cus_dec_container_statuses",
                columns: table => new
                {
                    cus_dec_container_status_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    cus_dec_container_id = table.Column<long>(type: "bigint", nullable: true),
                    status = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    status_date_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_sync_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    cus_dec_external_system_number = table.Column<long>(type: "bigint", nullable: true),
                    container_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    external_system_number = table.Column<long>(type: "bigint", nullable: true),
                    container_status_number = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cus_dec_container_statuses", x => x.cus_dec_container_status_id);
                    table.ForeignKey(
                        name: "fk_cus_dec_container_statuses_cus_dec_containers_cus_dec_conta",
                        column: x => x.cus_dec_container_id,
                        principalTable: "cus_dec_containers",
                        principalColumn: "cus_dec_container_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "login_user_activities",
                columns: table => new
                {
                    login_user_activity_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    global_identity = table.Column<Guid>(type: "uuid", nullable: false),
                    logged_ip_address = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    logged_device = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    logged_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_current_session = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_login_user_activities", x => x.login_user_activity_id);
                    table.ForeignKey(
                        name: "fk_login_user_activities_user_user_id",
                        column: x => x.user_id,
                        principalTable: "user",
                        principalColumn: "employee_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "member_subscription_status_histories",
                columns: table => new
                {
                    member_subscription_status_history_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    member_subscription_id = table.Column<long>(type: "bigint", nullable: false),
                    member_subscription_status_enum = table.Column<int>(type: "integer", nullable: false),
                    comment = table.Column<string>(type: "text", nullable: true),
                    master_member_id = table.Column<long>(type: "bigint", nullable: true),
                    employee_id = table.Column<long>(type: "bigint", nullable: true),
                    created_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_member_subscription_status_histories", x => x.member_subscription_status_history_id);
                    table.ForeignKey(
                        name: "fk_member_subscription_status_histories_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "employee_id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_member_subscription_status_histories_member_subscriptions_m",
                        column: x => x.member_subscription_id,
                        principalTable: "member_subscriptions",
                        principalColumn: "member_subscription_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_member_subscription_status_histories_members_master_member_",
                        column: x => x.master_member_id,
                        principalTable: "members",
                        principalColumn: "member_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "menu_item_function",
                columns: table => new
                {
                    menu_item_function_id = table.Column<int>(type: "integer", nullable: false),
                    menu_item_id = table.Column<int>(type: "integer", nullable: true),
                    menu_item_function_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    area = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    controller = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    action = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    icon_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    menu_item_function_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_menu_item_function", x => x.menu_item_function_id);
                    table.ForeignKey(
                        name: "fk_menu_item_function_menu_items_menu_item_id",
                        column: x => x.menu_item_id,
                        principalTable: "menu_items",
                        principalColumn: "menu_item_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "access_control",
                columns: table => new
                {
                    access_control_id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    group_id = table.Column<int>(type: "integer", nullable: true),
                    menu_item_function_id = table.Column<int>(type: "integer", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_access_control", x => x.access_control_id);
                    table.ForeignKey(
                        name: "fk_access_control_group_group_id",
                        column: x => x.group_id,
                        principalTable: "group",
                        principalColumn: "group_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_access_control_menu_item_function_menu_item_function_id",
                        column: x => x.menu_item_function_id,
                        principalTable: "menu_item_function",
                        principalColumn: "menu_item_function_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_access_control_group_id",
                table: "access_control",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_access_control_menu_item_function_id",
                table: "access_control",
                column: "menu_item_function_id");

            migrationBuilder.CreateIndex(
                name: "ix_company_member_mappings_company_id",
                table: "company_member_mappings",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_company_member_mappings_member_id",
                table: "company_member_mappings",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_company_status_details_company_id",
                table: "company_status_details",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_cus_dec_container_statuses_cus_dec_container_id",
                table: "cus_dec_container_statuses",
                column: "cus_dec_container_id");

            migrationBuilder.CreateIndex(
                name: "ix_cus_dec_containers_cus_dec_id",
                table: "cus_dec_containers",
                column: "cus_dec_id");

            migrationBuilder.CreateIndex(
                name: "ix_cus_dec_inspection_section_tab_cus_dec_id",
                table: "cus_dec_inspection_section_tab",
                column: "cus_dec_id");

            migrationBuilder.CreateIndex(
                name: "ix_cus_dec_inspections_cus_dec_id",
                table: "cus_dec_inspections",
                column: "cus_dec_id");

            migrationBuilder.CreateIndex(
                name: "ix_cus_dec_items_cus_dec_id",
                table: "cus_dec_items",
                column: "cus_dec_id");

            migrationBuilder.CreateIndex(
                name: "ix_cus_dec_payment_summaries_declarant_tin_number_consignee_ti",
                table: "cus_dec_payment_summaries",
                columns: new[] { "declarant_tin_number", "consignee_tin_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cus_dec_ready_to_process_settings_office_code_id",
                table: "cus_dec_ready_to_process_settings",
                column: "office_code_id");

            migrationBuilder.CreateIndex(
                name: "ix_cus_dec_statuses_cus_dec_id",
                table: "cus_dec_statuses",
                column: "cus_dec_id");

            migrationBuilder.CreateIndex(
                name: "ix_cus_dec_taxes_cus_dec_id",
                table: "cus_dec_taxes",
                column: "cus_dec_id");

            migrationBuilder.CreateIndex(
                name: "IX_CusDec_ConsigneeCompanyId",
                table: "cus_decs",
                column: "consignee_company_id");

            migrationBuilder.CreateIndex(
                name: "IX_CusDec_DeclarantCompanyId",
                table: "cus_decs",
                column: "declarant_company_id");

            migrationBuilder.CreateIndex(
                name: "ix_customer_support_tickets_member_id",
                table: "customer_support_tickets",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_employees_department_id",
                table: "employees",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "ix_login_user_activities_user_id",
                table: "login_user_activities",
                column: "user_id");

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
                name: "ix_member_notifications_member_id_is_deleted_is_seen_notificat",
                table: "member_notifications",
                columns: new[] { "member_id", "is_deleted", "is_seen", "notification_date_time" });

            migrationBuilder.CreateIndex(
                name: "ix_member_subscription_status_histories_employee_id",
                table: "member_subscription_status_histories",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_member_subscription_status_histories_master_member_id",
                table: "member_subscription_status_histories",
                column: "master_member_id");

            migrationBuilder.CreateIndex(
                name: "ix_member_subscription_status_histories_member_subscription_id",
                table: "member_subscription_status_histories",
                column: "member_subscription_id");

            migrationBuilder.CreateIndex(
                name: "ix_member_subscriptions_company_id",
                table: "member_subscriptions",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_member_subscriptions_member_id",
                table: "member_subscriptions",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_member_subscriptions_subscription_id",
                table: "member_subscriptions",
                column: "subscription_id");

            migrationBuilder.CreateIndex(
                name: "ix_member_subscriptions_subscription_request_id",
                table: "member_subscriptions",
                column: "subscription_request_id");

            migrationBuilder.CreateIndex(
                name: "ix_member_user_login_details_member_id",
                table: "member_user_login_details",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_member_users_user_name",
                table: "member_users",
                column: "user_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_members_email",
                table: "members",
                column: "email",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_menu_item_function_menu_item_id",
                table: "menu_item_function",
                column: "menu_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_menu_items_menu_id",
                table: "menu_items",
                column: "menu_id");

            migrationBuilder.CreateIndex(
                name: "ix_menus_module_id",
                table: "menus",
                column: "module_id");

            migrationBuilder.CreateIndex(
                name: "ix_ready_to_process_consignees_cus_dec_ready_to_process_settin",
                table: "ready_to_process_consignees",
                column: "cus_dec_ready_to_process_setting_id");

            migrationBuilder.CreateIndex(
                name: "ix_subscription_requests_company_id",
                table: "subscription_requests",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "ix_subscription_requests_member_id",
                table: "subscription_requests",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "ix_system_activity_logs_employee_id",
                table: "system_activity_logs",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_system_error_logs_employee_id",
                table: "system_error_logs",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_user_name",
                table: "user",
                column: "user_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_group_mappings_employee_id",
                table: "user_group_mappings",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_group_mappings_group_id",
                table: "user_group_mappings",
                column: "group_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "access_control");

            migrationBuilder.DropTable(
                name: "company_member_mappings");

            migrationBuilder.DropTable(
                name: "company_status_details");

            migrationBuilder.DropTable(
                name: "cus_dec_container_statuses");

            migrationBuilder.DropTable(
                name: "cus_dec_inspection_mappings");

            migrationBuilder.DropTable(
                name: "cus_dec_inspection_section_tab");

            migrationBuilder.DropTable(
                name: "cus_dec_inspection_section_tab_status_mappings");

            migrationBuilder.DropTable(
                name: "cus_dec_inspections");

            migrationBuilder.DropTable(
                name: "cus_dec_items");

            migrationBuilder.DropTable(
                name: "cus_dec_payment_summaries");

            migrationBuilder.DropTable(
                name: "cus_dec_status_mappings");

            migrationBuilder.DropTable(
                name: "cus_dec_statuses");

            migrationBuilder.DropTable(
                name: "cus_dec_taxes");

            migrationBuilder.DropTable(
                name: "customer_support_tickets");

            migrationBuilder.DropTable(
                name: "login_user_activities");

            migrationBuilder.DropTable(
                name: "member_activity_logs");

            migrationBuilder.DropTable(
                name: "member_login_sessions");

            migrationBuilder.DropTable(
                name: "member_notifications");

            migrationBuilder.DropTable(
                name: "member_subscription_status_histories");

            migrationBuilder.DropTable(
                name: "member_user_login_details");

            migrationBuilder.DropTable(
                name: "member_users");

            migrationBuilder.DropTable(
                name: "ready_to_process_consignees");

            migrationBuilder.DropTable(
                name: "smtp_mail_settings");

            migrationBuilder.DropTable(
                name: "system_activity_logs");

            migrationBuilder.DropTable(
                name: "system_error_logs");

            migrationBuilder.DropTable(
                name: "system_serials");

            migrationBuilder.DropTable(
                name: "task_schedule_app_details");

            migrationBuilder.DropTable(
                name: "temporary_members");

            migrationBuilder.DropTable(
                name: "user_group_mappings");

            migrationBuilder.DropTable(
                name: "menu_item_function");

            migrationBuilder.DropTable(
                name: "cus_dec_containers");

            migrationBuilder.DropTable(
                name: "user");

            migrationBuilder.DropTable(
                name: "member_subscriptions");

            migrationBuilder.DropTable(
                name: "cus_dec_ready_to_process_settings");

            migrationBuilder.DropTable(
                name: "group");

            migrationBuilder.DropTable(
                name: "menu_items");

            migrationBuilder.DropTable(
                name: "cus_decs");

            migrationBuilder.DropTable(
                name: "employees");

            migrationBuilder.DropTable(
                name: "subscription_requests");

            migrationBuilder.DropTable(
                name: "subscriptions");

            migrationBuilder.DropTable(
                name: "office_codes");

            migrationBuilder.DropTable(
                name: "menus");

            migrationBuilder.DropTable(
                name: "department");

            migrationBuilder.DropTable(
                name: "companies");

            migrationBuilder.DropTable(
                name: "members");

            migrationBuilder.DropTable(
                name: "module");
        }
    }
}
