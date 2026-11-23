# Schema Reset Readiness Report

**Date:** 2026-11-20  
**Branch:** feature/cusdec-page  
**Purpose:** Pre-flight audit before dropping cdns_dev, deleting all migration files, and regenerating one clean InitialSchema migration.

---

## 1. Schema Change Summary

### Commit range

The most recent merge that brought Dinuka's work in is a **single merge commit**:

| SHA | Message | Date |
|-----|---------|------|
| `6723917` | Feature/asycuda sync cusdec (#32) | 2026-11-20 |

Previous tip (auth UI redesign) was `c0720f1`. All schema changes described below arrived in `6723917`.

### Changed files under Domain and Persistence

**Cdns.Domain/Entities/CusdecTables/**

| File | Change |
|------|--------|
| `CusDecInspectStatus.cs` | **New entity** — mirrors CusDecStatus structure, used for inspection-specific statuses |
| `CusDec.cs` | No nullability changes; `CusDecExternalSystemNumber` was already required |
| `CusDecContainer.cs` | `CusDecExternalSystemNumber` → required (`long`); `ContainerKey` → required (`int`) |
| `CusDecContainerStatus.cs` | `ExternalSystemNumber` → required (`long`); `ContainerStatusNumber` → required (`int`) |
| `CusDecInspection.cs` | `CusDecExternalSystemNumber`, `InspectionInstanceId`, `KeyFrdNumber` → required |
| `CusDecInspectionSectionTab.cs` | `CusDecExternalSystemNumber`, `InsTabExternalSystemNumber`, `KeySecNumber` → required |
| `CusDecItem.cs` | `CusDecExternalSystemNumber`, `CusDecItemNumber` → required |
| `CusDecStatus.cs` | `ExternalSystemNumber`, `CusDecStatusNumber`, `CusDecExternalSystemNumber`, `TableName` → required |
| `CusDecTax.cs` | `CusDecExternalSystemNumber`, `ItemNumber`, `TaxRank` → required |

**Cdns.Domain/Entities/SystemInfrastructureAuditing/**

| File | Change |
|------|--------|
| `TaskScheduleAppDetail.cs` | Added `IsSuccess bool` and `Errors string?` |

**Cdns.Infrastructure/Persistence/CdnsDbContext.cs** — added `DbSet<CusDecInspectStatus> CusDecInspectStatuses`.

**Cdns.Infrastructure/Persistence/Configurations/Cusdec/** — updated or created:  
`CusDecConfiguration.cs`, `CusDecContainerConfiguration.cs`, `CusDecContainerStatusConfiguration.cs`, `CusDecInspectStatusConfiguration.cs` (new), `CusDecInspectionConfiguration.cs`, `CusDecInspectionSectionTabConfiguration.cs`, `CusDecItemConfiguration.cs`, `CusDecStatusConfiguration.cs`, `CusDecTaxConfiguration.cs`

**Cdns.Infrastructure/Persistence/Configurations/SystemInfrastructureAuditing/**  
`TaskScheduleAppDetailConfiguration.cs`

### Per-table schema changes

#### `cus_decs`

| Column / constraint | Change | Old | New | Source |
|---------------------|--------|-----|-----|--------|
| `cus_dec_external_system_number` unique index | Added | — | `ix_cus_decs_cus_dec_external_system_number` UNIQUE | Migration 20261113 / `CusDecConfiguration.cs:81` |

No column type, nullability, or FK changes on `cus_decs` itself.

#### `cus_dec_taxes`

| Column | Change | Old | New | Source |
|--------|--------|-----|-----|--------|
| `cus_dec_external_system_number` | Nullability | nullable `bigint` | NOT NULL `bigint` | Migration 20261116 / `CusDecTaxConfiguration.cs` |
| `item_number` | Nullability | nullable `int` | NOT NULL `int` | Migration 20261116 / entity |
| `tax_rank` | Nullability | nullable `int` | NOT NULL `int` | Migration 20261116 / entity |
| `(cus_dec_external_system_number, item_number, tax_rank)` unique index | Added | — | `ix_cus_dec_taxes_...` UNIQUE | Migration 20261113 / `CusDecTaxConfiguration.cs` |

#### `cus_dec_items`

| Column | Change | Old | New | Source |
|--------|--------|-----|-----|--------|
| `cus_dec_external_system_number` | Nullability | nullable `bigint` | NOT NULL `bigint` | Migration 20261116 / `CusDecItemConfiguration.cs` |
| `cus_dec_item_number` | Nullability | nullable `int` | NOT NULL `int` | Migration 20261116 / entity |
| `(cus_dec_external_system_number, cus_dec_item_number)` unique index | Added | — | UNIQUE | Migration 20261113 / `CusDecItemConfiguration.cs` |

#### `cus_dec_inspections`

| Column | Change | Old | New | Source |
|--------|--------|-----|-----|--------|
| `cus_dec_external_system_number` | Nullability | nullable `bigint` | NOT NULL `bigint` | Migration 20261116 |
| `inspection_instance_id` | Nullability | nullable `bigint` | NOT NULL `bigint` | Migration 20261116 |
| `key_frd_number` | Nullability | nullable `int` | NOT NULL `int` | Migration 20261116 |
| `(cus_dec_external_system_number, inspection_instance_id, key_frd_number)` unique index | Added | — | UNIQUE | Migration 20261113 / `CusDecInspectionConfiguration.cs` |

#### `cus_dec_inspection_section_tab`

| Column | Change | Old | New | Source |
|--------|--------|-----|-----|--------|
| `cus_dec_external_system_number` | Nullability | nullable `bigint` | NOT NULL `bigint` | Migration 20261116 |
| `ins_tab_external_system_number` | Nullability | nullable `bigint` | NOT NULL `bigint` | Migration 20261116 |
| `key_sec_number` | Nullability | nullable `int` | NOT NULL `int` | Migration 20261116 |
| `(cus_dec_external_system_number, ins_tab_external_system_number, key_sec_number)` unique index | Added | — | UNIQUE | Migration 20261113 / `CusDecInspectionSectionTabConfiguration.cs` |

#### `cus_dec_containers`

| Column | Change | Old | New | Source |
|--------|--------|-----|-----|--------|
| `cus_dec_external_system_number` | Nullability | nullable `bigint` | NOT NULL `bigint` | Migration 20261116 / `CusDecContainerConfiguration.cs` |
| `container_key` | Nullability | nullable `int` | NOT NULL `int` | Migration 20261116 / `CusDecContainerConfiguration.cs` |
| `(cus_dec_external_system_number, container_key)` unique index | Added | — | UNIQUE | Migration 20261113 / `CusDecContainerConfiguration.cs` |

#### `cus_dec_container_statuses`

| Column | Change | Old | New | Source |
|--------|--------|-----|-----|--------|
| `external_system_number` | Nullability | nullable `bigint` | NOT NULL `bigint` | Migration 20261116 |
| `container_status_number` | Nullability | nullable `int` | NOT NULL `int` | Migration 20261116 |
| `(external_system_number, container_status_number)` unique index | Added | — | UNIQUE | Migration 20261113 |

#### `cus_dec_statuses`

| Column | Change | Old | New | Source |
|--------|--------|-----|-----|--------|
| `external_system_number` | Nullability | nullable `bigint` | NOT NULL `bigint` | Migration 20261116 / `CusDecStatus.cs` |
| `cus_dec_status_number` | Nullability | nullable `int` | NOT NULL `int` | Migration 20261116 / entity |
| `cus_dec_external_system_number` | Nullability | nullable `bigint` | NOT NULL `bigint` | Migration 20261116 / entity |
| `table_name` | Nullability | nullable `varchar(100)` | NOT NULL `varchar(100)` | Migration 20261116 / entity |
| `(external_system_number, cus_dec_status_number, cus_dec_external_system_number, table_name)` unique index | Added | — | UNIQUE | Migration 20261115 / `CusDecStatusConfiguration.cs:35` |

#### `cus_dec_inspect_statuses` — NEW TABLE

Created in migration 20261115. Configured in `CusDecInspectStatusConfiguration.cs`.

| Column | Type | Nullable | Notes |
|--------|------|----------|-------|
| `cus_dec_inspect_status_id` | `bigint` | NOT NULL | PK, identity |
| `cus_dec_id` | `bigint` | nullable | FK → cus_decs (Restrict) |
| `cus_dec_global_identity` | `uuid` | nullable | |
| `status` | `varchar(500)` | NOT NULL | Required per config |
| `description` | `text` | nullable | |
| `status_date_time` | `timestamp with time zone` | nullable | |
| `last_sync_date_time` | `timestamp with time zone` | NOT NULL | default CURRENT_TIMESTAMP |
| `external_system_number` | `bigint` | NOT NULL | Required per config |
| `cus_dec_status_number` | `int` | nullable | (not in unique key) |
| `cus_dec_external_system_number` | `bigint` | NOT NULL | Required per config |
| `table_name` | `varchar(100)` | NOT NULL | Required per config |

Indexes:
- `ix_cus_dec_inspect_statuses_cus_dec_id` — non-unique, on `cus_dec_id`
- `ix_cus_dec_inspect_statuses_external_system_number_cus_dec_ext` — UNIQUE on `(external_system_number, cus_dec_external_system_number, table_name, status)`

#### `task_schedule_app_details`

| Column | Change | Old | New | Source |
|--------|--------|-----|-----|--------|
| `is_success` | Added | — | `boolean` NOT NULL default `false` | Migration 20261117 / `TaskScheduleAppDetailConfiguration.cs` |
| `errors` | Added | — | `varchar(4000)` nullable | Migration 20261117 / `TaskScheduleAppDetailConfiguration.cs` |

---

## 2. Current Migrations Inventory

**Folder:** `backend/src/Cdns.Infrastructure/Migrations/`

| File | Date | What it does |
|------|------|--------------|
| `20261026075640_InitialSchema.cs` | 2026-10-26 | Full initial schema — all tables, FKs, indexes, `citext` extension |
| `20261103051539_AddCheckTinSignupStatusFunction.cs` | 2026-11-03 | Creates `check_tin_signup_status(p_tin text)` PL/pgSQL function via `migrationBuilder.Sql()` |
| `20261113034925_AddUniqueConstraintsToCusdecSyncTables.cs` | 2026-11-13 | Adds 7 unique composite indexes on CusDec sync tables |
| `20261115032533_SplitCusDecStatusIntoInspectStatus.cs` | 2026-11-15 | Creates `cus_dec_inspect_statuses` table; adds unique index on `cus_dec_statuses` |
| `20261116053355_AddNotNullConstraintsToCusdecSyncKeys.cs` | 2026-11-16 | Makes business-key columns NOT NULL across 8 CusDec sync tables |
| `20261117061425_AddSyncStatusToTaskScheduleAppDetail.cs` | 2026-11-17 | Adds `is_success` + `errors` columns to `task_schedule_app_details` |
| `CdnsDbContextModelSnapshot.cs` | (auto) | EF model snapshot |

**`dotnet ef migrations has-pending-model-changes` result:**

```
Exit code 1
Unable to create a 'DbContext' of type 'CdnsDbContext'. The exception
'ConnectionStrings:Default not found. Copy appsettings.Development.example.json
to appsettings.Development.json...' was thrown.
```

The command requires a live connection string to build the design-time host. It could not run because `appsettings.Development.json` is absent (gitignored). **This is expected behavior.** You should re-run this check after creating your local appsettings before regenerating.

---

## 3. What a Regenerate Would LOSE

### Raw SQL — `migrationBuilder.Sql()` calls

| Migration | SQL content | Also in EF model? | Verdict |
|-----------|-------------|-------------------|---------|
| `20261103051539_AddCheckTinSignupStatusFunction.cs` | Creates `check_tin_signup_status(p_tin text)` PL/pgSQL function | **NO** — EF has no concept of PL/pgSQL functions | **MUST RE-ADD BY HAND** |

No other `migrationBuilder.Sql()` calls exist in any migration, including `InitialSchema`.

### Exact SQL that must be re-added

The canonical text lives at `backend/src/Cdns.Infrastructure/Persistence/Sql/Functions/check_tin_signup_status.sql`, but EF never executes that file — it is documentation only. After regeneration, you must include this in a new migration (or the new `InitialSchema` itself) as a `migrationBuilder.Sql()` call:

```sql
CREATE OR REPLACE FUNCTION check_tin_signup_status(p_tin text)
RETURNS integer
LANGUAGE plpgsql
AS $$
DECLARE
  v_company_id  integer;
  v_member_id   bigint;
  v_user_status integer;
  v_req_ereg    boolean;
BEGIN
  IF length(trim(p_tin)) < 9 THEN
    RETURN 4;
  END IF;
  SELECT company_id INTO v_company_id FROM companies
  WHERE tin_number = p_tin AND is_deleted = false;
  IF NOT FOUND THEN RETURN 6; END IF;
  SELECT m.member_id INTO v_member_id
  FROM company_member_mappings cmm
  JOIN members m ON m.member_id = cmm.member_id
  WHERE cmm.company_id = v_company_id
    AND m.member_type = 2
    AND cmm.is_deleted = false AND m.is_deleted = false
  LIMIT 1;
  IF NOT FOUND THEN RETURN 5; END IF;
  IF NOT EXISTS (SELECT 1 FROM member_users WHERE member_id = v_member_id) THEN
    RETURN 5;
  END IF;
  SELECT user_status_type, required_ereg_verification
  INTO v_user_status, v_req_ereg
  FROM member_users WHERE member_id = v_member_id;
  IF v_user_status = 1 THEN RETURN 1; END IF;
  IF v_req_ereg = true THEN RETURN 2; END IF;
  RETURN 3;
END;
$$;
```

The corresponding `Down()` drop is:
```sql
DROP FUNCTION IF EXISTS check_tin_signup_status(text);
```

### HasData seeding in configurations

None. All seed data lives in runtime seeders (`AuthSeedData`, `MemberPortalSeedData`), not in `HasData()` calls inside any `IEntityTypeConfiguration`. A regenerated migration will contain no data rows.

---

## 4. EF Model Features Check

This section confirms which Postgres-specific features are expressed in `IEntityTypeConfiguration` (and will therefore survive regeneration) versus which are absent entirely.

| Feature | Present in EF config? | Evidence | Will survive regenerate? |
|---------|-----------------------|----------|--------------------------|
| Unique index on `cus_dec_statuses (external_system_number, cus_dec_status_number, cus_dec_external_system_number, table_name)` | **YES** | `CusDecStatusConfiguration.cs:35` — `HasIndex(...).IsUnique()` | ✓ |
| Unique index on `cus_dec_inspect_statuses (external_system_number, cus_dec_external_system_number, table_name, status)` | **YES** | `CusDecInspectStatusConfiguration.cs:36` — `HasIndex(...).IsUnique()` | ✓ |
| **NULLS NOT DISTINCT** on any index | **NO** | No `AreNullsDistinct(false)` call found anywhere in Persistence/ | n/a — feature absent |
| **Partial (HasFilter) indexes** on CusDec tables | **NO** | No `HasFilter` in any CusDec configuration. The only `HasFilter` in the codebase is in `MemberConfiguration.cs` (`is_deleted = false`) — unrelated | n/a — feature absent from CusDec |
| **CHECK constraint on `table_name`** | **NO** | No `HasCheckConstraint` call found anywhere | n/a — feature absent |
| `timestamp without time zone` on CusDec date columns | **YES** | `CusDecConfiguration.cs` — `.HasColumnType("timestamp without time zone")` on `LastSyncDateTime`, `CusDecDate`, `ReleaseDate`, `RegistrationDate`, `DeclarantClaimedDateTime`, `ConsigneeClaimedDateTime`, `DeclarantDisclaimedDateTime`, `ConsigneeDisclaimedDateTime`, `ReadyToProcessMarkDateTime`, `CanNotProcessMarkDateTime`, `CanProcessMarkDateTime` | ✓ |
| `citext` extension | **YES** | `CdnsDbContext.OnModelCreating` — `modelBuilder.HasPostgresExtension("citext")` | ✓ |
| Custom index names `IX_CusDec_DeclarantCompanyId` / `IX_CusDec_ConsigneeCompanyId` | **YES** | `CusDecConfiguration.cs:77-78` — `.HasDatabaseName(...)` | ✓ |
| Partial unique index on `members` (`is_deleted = false`) | **YES** | `MemberConfiguration.cs:28` — `.HasFilter("is_deleted = false")` | ✓ |

**Summary for section 4:** No NULLS NOT DISTINCT, partial index (HasFilter), or CHECK constraint exists on any CusDec table in either the EF configurations or the migration files. These features are absent from the schema entirely. If they were intended, they have not yet been added.

---

## 5. Seed Data Impact

### How seeding runs

Both seeders are called unconditionally on every startup in the **Development** environment:

```csharp
// Program.cs:90-91
await AuthSeedData.SeedAsync(db, hasher);
await MemberPortalSeedData.SeedAsync(db);
```

There is no environment variable gate, feature flag, or CLI argument. The seeders are idempotent via guard checks (`if (await db.Companies.AnyAsync(...)) return;` and `if (await db.Subscriptions.AnyAsync()) return;`), so they no-op after the first run. After a DB drop + migrate, the first API start will re-seed automatically.

### NOT NULL constraint compatibility

All new NOT NULL business-key columns are explicitly set in `MemberPortalSeedData.cs`:

| Column (new NOT NULL) | Set in seed? | Location |
|-----------------------|-------------|----------|
| `CusDecExternalSystemNumber` on CusDec | Yes — `externalSeq` incremented per loop | `MemberPortalSeedData.cs` section 6 |
| `CusDecExternalSystemNumber` on CusDecItem | Yes — `CusDecExternalSystemNumber = externalSeq` | section 6, items list |
| `CusDecItemNumber` | Yes — `CusDecItemNumber = 1` / `= 2` | section 6 |
| `CusDecExternalSystemNumber` on CusDecContainer | Yes — `CusDecExternalSystemNumber = externalSeq` | section 6, containers loop |
| `ContainerKey` | Yes — `ContainerKey = c + 1` | section 6 |
| `ContainerStatusNumber` on CusDecContainerStatus | Yes — `ContainerStatusNumber = step` | section 6 |
| `ExternalSystemNumber` on CusDecContainerStatus | Yes — `CusDecExternalSystemNumber = externalSeq` | section 6 |
| `CusDecExternalSystemNumber` on CusDecStatus | Yes — `CusDecExternalSystemNumber = externalSeq` | sections 6 and 7 |
| `CusDecStatusNumber` | Yes — `CusDecStatusNumber = statusStep` / `n` | sections 6 and 7 |
| `TableName` on CusDecStatus | Yes — `TableName = "cus_dec_statuses"` | sections 6 and 7 |
| `ExternalSystemNumber` on CusDecStatus | Yes — set via `externalSeq` / `completedExternalSeq` | sections 6 and 7 |

No NOT NULL violations expected. `CusDecInspectStatus` is not seeded at all — no issue.

### DateTime kind / timestamptz compatibility

| Table / column | Type in DB | Seed value | Kind | Compatible? |
|----------------|-----------|-----------|------|-------------|
| `cus_decs.last_sync_date_time` | `timestamp without time zone` | `DateTime.SpecifyKind(nowUtc, DateTimeKind.Unspecified)` | Unspecified | ✓ (comment in seed explains this) |
| `cus_decs.*_date_time` (claimed, disclaimed, etc.) | `timestamp without time zone` | same as above | Unspecified | ✓ |
| `cus_dec_statuses.last_sync_date_time` | `timestamp with time zone` | `DateTime.UtcNow` (Kind=Utc) | Utc | ✓ |
| All `DateTimeOffset` columns (subscriptions, members, etc.) | `timestamp with time zone` | `DateTimeOffset.UtcNow` | Utc | ✓ |

No seed writes to `cus_dec_inspect_statuses` → no timezone concerns there.

### External ID collision check

Seed uses ranges `900_000_001` → `900_000_012` and `950_000_001`+. The integration test suite was specifically moved to a range outside these after a prior collision (noted in commit `6723917` message). No conflict expected.

---

## 6. Build Status

### Backend — `dotnet build` (backend/)

```
Build succeeded.
2 Warning(s)   [NU1903: Microsoft.OpenApi 2.0.0 high severity vulnerability — in test project only]
0 Error(s)
Time Elapsed 00:00:09.87
```

All projects build: `Cdns.Domain`, `Cdns.Application`, `Cdns.Infrastructure`, `Cdns.Worker`, `Cdns.Api`, `Cdns.Infrastructure.Tests`.

### Frontend — `npm run build` (frontend/)

```
vite v8.3.3 — ✓ built in 787ms
2986 modules transformed.
1 Warning: index.js chunk > 500 kB (chunking advisory only)
0 Errors
```

Both builds are clean. No action needed before the DB reset.

---

## 7. Risks and Questions Before You Drop the DB

### R1 — `check_tin_signup_status` function (HIGH — mandatory re-add)

**Risk:** After regeneration, the EF-generated `InitialSchema` will not contain the `check_tin_signup_status` PL/pgSQL function. EF has no mechanism to infer database functions. The signup TIN-check endpoint (`POST /api/auth/check-tin`) will throw a Postgres error (`function check_tin_signup_status does not exist`) on first call.

**Action required:** After `dotnet ef migrations add InitialSchema`, open the generated file and add a `migrationBuilder.Sql(...)` call with the function body from Section 3 above (or from `Persistence/Sql/Functions/check_tin_signup_status.sql`). Add the corresponding `DROP FUNCTION IF EXISTS` to `Down()`.

---

### R2 — `has-pending-model-changes` unverified

**Risk:** The design-time command could not run because `appsettings.Development.json` is absent. It is therefore unconfirmed whether the current EF model snapshot exactly matches the C# entity+configuration state (i.e., there are no un-migrated model changes that would cause the new InitialSchema to diverge from what you expect).

**Action required:** Before regenerating, run:
```bash
cd backend
dotnet ef migrations has-pending-model-changes \
  --project src/Cdns.Infrastructure \
  --startup-project src/Cdns.Api
```
This must return exit code 0 (no pending changes). If it returns exit code 2 (pending changes), there is a model/migration gap that the new InitialSchema will silently absorb — confirm whether that is intentional.

---

### R3 — NULLS NOT DISTINCT / partial indexes / CHECK on `table_name` are absent

**Risk:** These Postgres features do not exist on `cus_dec_statuses` or `cus_dec_inspect_statuses` in the current schema (not in migration files, not in EF configs). If they were part of an agreed design intent, they have never been implemented and will not appear in the regenerated migration either.

**Question for you:** Were NULLS NOT DISTINCT / a filtered unique index / a CHECK constraint on `table_name` planned but not yet built, or is the current plain UNIQUE index design correct?

---

### R4 — Seed runs on first startup, no gate

**Risk:** With a fresh DB and the new migration applied, the first time the API starts in Development mode it will execute both seeders automatically. This is by design (idempotent) but means there is no "clean schema before seed" window for manual inspection without temporarily removing the seed calls.

**No action required** unless you want a window to inspect the empty schema before seeding.

---

### R5 — `Microsoft.OpenApi` vulnerability warning in test project (LOW)

The test project (`Cdns.Infrastructure.Tests`) references `Microsoft.OpenApi 2.0.0` with a known high-severity CVE. Unrelated to the DB reset, but worth upgrading before the next release cut.
