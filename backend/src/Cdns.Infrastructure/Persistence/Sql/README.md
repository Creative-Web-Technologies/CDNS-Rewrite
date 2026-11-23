# Raw SQL objects (functions, procedures, views, triggers)

## Why this directory exists

EF Core generates migrations **only** from the C# model (entities + `IEntityTypeConfiguration`
classes). It has no knowledge of database objects that aren't modelled as tables/columns —
**functions, stored procedures, views, and triggers**. Those objects therefore:

1. Live here as readable, reviewable source-of-truth `.sql` files, and
2. Must be created **by hand** inside an EF migration via `migrationBuilder.Sql(...)`.

Because they are invisible to the model, they are **not** recreated automatically when the
`Migrations/` folder is reset and `InitialSchema` is regenerated. After any such reset you must
re-add every object listed below (see the **Reset checklist**).

## Inventory

| Object | Type | Source file | Created by migration | Used by (file:line) |
|--------|------|-------------|----------------------|---------------------|
| `check_tin_signup_status(text)` | Function (plpgsql, returns `integer`) | `Functions/check_tin_signup_status.sql` | `20261122151153_AddCheckTinSignupStatusFunction` | `src/Cdns.Infrastructure/Services/SignupService.cs:37` (`SqlQueryRaw<int>("SELECT check_tin_signup_status({0}) …")`) |

## `check_tin_signup_status(text)`

Checks whether a company TIN is in a state that allows new member signup. It is a 4-table join
(`companies → company_member_mappings → members → member_users`) expressed as set-based SQL.

**Signature:** `check_tin_signup_status(p_tin text) RETURNS integer`

**Return codes** (consumed by `SignupService.TinStatusToError`):

| Code | Meaning | Signup outcome |
|------|---------|----------------|
| 1 | company found, master active | member may proceed |
| 2 | company found, master pending eReg | block |
| 3 | company found, master pending activation | block |
| 4 | TIN shorter than 9 characters | validation error |
| 5 | company found, master account incomplete | block |
| 6 | company not found | block |

**How it must be managed:**

- Keep the **readable source** in `Functions/check_tin_signup_status.sql`.
- To create it in the DB, paste that SQL into a migration's `Up()`
  (`migrationBuilder.Sql(@"…")`), **after** the tables it depends on (`companies`,
  `company_member_mappings`, `members`, `member_users`) — all created in `InitialSchema`.
- Give the migration a matching `Down()`:
  `migrationBuilder.Sql("DROP FUNCTION IF EXISTS check_tin_signup_status(text);")`.
- The SQL uses `CREATE OR REPLACE FUNCTION`, so it is safe to re-apply.
- **Change it only through a NEW migration** (another `CREATE OR REPLACE FUNCTION`). Never edit a
  migration that has already been applied, and keep the `.sql` source file in step with the
  latest version.

## Reset checklist

If `Migrations/` is deleted and `InitialSchema` is regenerated, EF will recreate the tables but
**none** of the objects in the inventory above. After regenerating `InitialSchema`:

1. `dotnet ef migrations add AddCheckTinSignupStatusFunction`
   (run from `backend/` with
   `--project src/Cdns.Infrastructure --startup-project src/Cdns.Api`).
2. Confirm the generated `Up()`/`Down()` are **empty** (empty = no model drift). If not, the
   model drifted — `dotnet ef migrations remove`, fix the drift, and retry.
3. Fill `Up()` with the `CREATE OR REPLACE FUNCTION` body from
   `Functions/check_tin_signup_status.sql`, and `Down()` with the `DROP FUNCTION IF EXISTS …`.
4. Add a header comment pointing back to the `.sql` source and the "new migration only" rule.
5. Repeat for every row in the inventory table above (currently just the one function).
