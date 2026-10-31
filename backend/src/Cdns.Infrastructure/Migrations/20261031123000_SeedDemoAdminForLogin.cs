using Cdns.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cdns.Infrastructure.Migrations;

[DbContext(typeof(CdnsDbContext))]
[Migration("20261031123000_SeedDemoAdminForLogin")]
public sealed class SeedDemoAdminForLogin : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DO $seed$
            DECLARE
                demo_email constant text := 'admin.demo@cdns.local';
                demo_user_name constant text := 'admin.demo@cdns.local';
                demo_employee_id bigint;
                demo_group_id integer;
                matching_employee_count integer;
            BEGIN
                IF current_database() <> 'cdns_dev' THEN
                    RAISE NOTICE 'Skipping demo admin seed outside the cdns_dev database.';
                    RETURN;
                END IF;

                SELECT count(*), min(employee_id)
                INTO matching_employee_count, demo_employee_id
                FROM employees
                WHERE lower(email) = lower(demo_email)
                  AND is_deleted = false;

                IF matching_employee_count > 1 THEN
                    RAISE EXCEPTION 'Cannot seed demo admin: multiple active employees use %', demo_email;
                END IF;

                IF matching_employee_count = 1 THEN
                    IF EXISTS (SELECT 1 FROM "user" WHERE employee_id = demo_employee_id) THEN
                        RAISE NOTICE 'Demo admin account already exists; skipping seed.';
                        RETURN;
                    END IF;
                ELSE
                    INSERT INTO employees (first_name, last_name, employee_code, email, is_deleted)
                    VALUES ('Demo', 'Administrator', 'CDNS-DEMO-ADMIN-SEED', demo_email, false)
                    RETURNING employee_id INTO demo_employee_id;
                END IF;

                IF EXISTS (SELECT 1 FROM "user" WHERE user_name = demo_user_name) THEN
                    RAISE EXCEPTION 'Cannot seed demo admin: username % is already in use', demo_user_name;
                END IF;

                SELECT group_id
                INTO demo_group_id
                FROM "group"
                WHERE group_name = 'ADMIN'
                  AND is_deleted = false
                ORDER BY group_id
                LIMIT 1;

                IF demo_group_id IS NULL THEN
                    INSERT INTO "group" (group_name, is_deleted)
                    VALUES ('ADMIN', false)
                    RETURNING group_id INTO demo_group_id;
                END IF;

                INSERT INTO "user" (
                    employee_id,
                    user_name,
                    password,
                    password_salt,
                    is_active,
                    is_suspended
                )
                VALUES (
                    demo_employee_id,
                    demo_user_name,
                    'p7iKU83JHg75JKZDTC6GZV+xbAp23jfiEfDLnJs6ODUB7uXLY1B0w0jXfSq971OafNGjE1dxkCYS2YoZpWm9PQ==',
                    'oc0uusdEff4hjC2l+PbWY+EqLPyM+93RcoI+QgwRHNovEQ==',
                    true,
                    false
                );

                INSERT INTO user_group_mappings (employee_id, group_id)
                VALUES (demo_employee_id, demo_group_id);
            END
            $seed$;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DELETE FROM user_group_mappings
            WHERE employee_id IN (
                SELECT employee_id
                FROM "user"
                WHERE user_name = 'admin.demo@cdns.local'
            );

            DELETE FROM "user"
            WHERE user_name = 'admin.demo@cdns.local';

            DELETE FROM employees
            WHERE employee_code = 'CDNS-DEMO-ADMIN-SEED'
              AND email = 'admin.demo@cdns.local'
              AND NOT EXISTS (
                  SELECT 1
                  FROM "user"
                  WHERE "user".employee_id = employees.employee_id
              );
            """);
    }
}
