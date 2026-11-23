# ASYCUDA Sync — Status Investigation (2026-11-22)

Read-only investigation. No code changed. No database or migration commands were run.

## Summary

**1 of 12 legacy Oracle queries is implemented** — the CusDec header query (A04) — on branch
**`Dev`** (the main dev branch), commit **`6723917`** ("Feature/asycuda sync cusdec (#32)"),
which is **identical to `feature/cusdec-page`** (same SHA) and therefore merged. The pipeline
was added in a single commit **`2d14bf9`** (2026-11-19, ChirathCreative) via
`feature/asycuda-sync-cusdec` → PR #32 → `Dev`.

**The parity audit is correct; the "sync is complete and fetches all data" claim is not.**
Hard evidence:
- Exactly **one** `OracleConnection` / `OracleCommand` / `ExecuteReaderAsync` exists in the
  whole codebase, on every branch — all three counts = 1 in
  `AsycudaSyncService.cs` (`git grep -c` on `origin/Dev`).
- Of the 14 legacy Oracle table names searched across **all** branches, only
  `SAD_GENERAL_SEGMENT`, `UN_ASYBRK_TRACK` (the header query + its change-gate) and `LNK_TPT`
  (in a **comment** only) appear. `SAD_ITEM`, `SAD_TAX`, `SAD_CONTAINERS`,
  `SAD_SUPPLEMENTARY_UNIT`, `INSP_ACT_TAB`, `INS_FRD_TAB`, `INS_SEC_TAB`,
  `EXIT_NOTE_DETAIL_GENERAL_SEG`, `CUSDEC_DISPATCH_TAB`, `CARGO_DISPATCH_TAB`, `CONTAINER_PASS`
  appear **nowhere** as queries. (`CONTAINER_PASS` matched only a Postgres column name,
  `container_pass_number`, in `InitialSchema.cs:697` — not an Oracle query.)
- **No stored procedure or function exists for the sync** on any branch. The only DB function
  in the repo is `check_tin_signup_status` — a signup TIN gate, unrelated to sync.

The child tables (`cus_dec_items`, `cus_dec_taxes`, `cus_dec_containers`,
`cus_dec_container_statuses`, `cus_dec_statuses`, `cus_dec_inspect_statuses`,
`cus_dec_inspections`, `cus_dec_inspection_section_tabs`, `cus_dec_payment_summaries`) exist
as EF entities/tables with unique constraints, but **nothing writes to them**. The schema is
ready; the ingestion for 11 of 12 queries is not built.

---

## Step 1 — Where the work is

### Branches touched in the last 3 weeks (since 2026-11-01)

Only commits touching the **sync worker, migrations, or `.sql`** are listed.

| Commit | Date | Author | Subject | Touches |
|--------|------|--------|---------|---------|
| `2d14bf9` | 2026-11-19 | ChirathCreative | Add CusDec ASYCUDA sync pipeline (Oracle read → Postgres write) | **worker + sync + 1 migration** |
| `92b3c4f` | 2026-11-19 | ChirathCreative | Merge `origin/Dev` into `feature/asycuda-sync-cusdec` | merge |
| `6723917` | 2026-11-20 | ChirathCreative | Feature/asycuda sync cusdec (#32) — **merge into Dev** | merge to Dev |
| `47a375b` | 2026-11-16 | ChirathCreative | Make sync-table business-key columns required (close null-key gap) | migration |
| `72d3d4e` | 2026-11-15 | ChirathCreative | Split CusDecStatus into CusDecStatus + CusDecInspectStatus | migration |
| `86838ea` | 2026-11-13 | ChirathCreative | Add unique constraints on business keys for 7 CusDec sync tables | migration |
| `8627a1d` | 2026-11-15 | TenukaCreative | Add member/company dashboard services, ICurrentUser, portal seed | migration/seed |
| `93d1812` | 2026-11-16 | TenukaCreative | scripts added (`scripts/dev/seed_dev_data.sql`) | dev seed sql |
| `02ab305` | 2026-11-06 | TenukaCreative | added migration files for pg function (`check_tin_signup_status`) | migration (signup) |
| `b573fd3`, `90ceae8`, `8305302`, `18bd09b`, `72e0efd` | 2026-11-02…10 | mixed | auth / signup-flow work | migrations/seed (not sync) |

All sync-schema migration work is by **ChirathCreative** between 2026-11-13 and 2026-11-19;
the sync **code** is the single commit `2d14bf9`.

`2d14bf9` file stat (what "the sync" actually is):

```
 Cdns.Application/DTOs/Sync/CusDecSyncRow.cs            |   27 +
 Cdns.Application/Interfaces/IAsycudaSyncService.cs     |    8 +
 .../SystemInfrastructureAuditing/TaskScheduleAppDetail.cs |  4 +
 Cdns.Infrastructure/DependencyInjection.cs            |    3 +
 Cdns.Infrastructure/ExternalServices/AsycudaSyncService.cs | 200 ++   <- the ONE Oracle query
 .../Migrations/..._AddSyncStatusToTaskScheduleAppDetail.cs |  40 +
 Cdns.Infrastructure/Sync/CusDecSyncProcessor.cs       |  117 +
 Cdns.Worker/Worker.cs                                 |   92 +-
 tests/Cdns.Infrastructure.Tests/Sync/CusDecSyncProcessorTests.cs | 386 +
```

### Branches that contain the sync code

- `origin/Dev` **= `origin/feature/cusdec-page`** → both at `6723917`. **Merged.**
- `origin/feature/asycuda-sync-cusdec` at `05de80e` (pre-PR tip; its content is in Dev via the
  PR merge `6723917`).
- No other branch contains `AsycudaSyncService.cs` (confirmed via `git grep -l SAD_GENERAL_SEGMENT`
  across all remotes → only `Dev`, `feature/asycuda-sync-cusdec`, `feature/cusdec-page`).

`origin/main` is stale (`efc4b32`, 2026-10-28) and has **no** sync code.

### Repo-wide search for SPs / functions / raw SQL (all branches)

| Construct | Where found | Sync-related? |
|-----------|-------------|---------------|
| `OracleConnection` / `OracleCommand` / `ExecuteReaderAsync` | `AsycudaSyncService.cs` (Dev, cusdec-page, asycuda-sync-cusdec) — **1 each** | Yes — the header query only |
| `CREATE OR REPLACE FUNCTION` | `check_tin_signup_status.sql` + migration `20261103051539_AddCheckTinSignupStatusFunction.cs` | **No** — signup TIN gate |
| `SqlQueryRaw` | `SignupService.cs` (calls `check_tin_signup_status`) | No |
| `migrationBuilder.Sql(...)` | `20261103051539_...` (the function), `feature/admin-portal:20261031123000_SeedDemoAdminForLogin.cs` (seed) | No |
| `CREATE PROCEDURE` | **none anywhere** | — |
| Dapper (`using Dapper`, `QueryAsync`) | **none anywhere** | — |
| `.sql` files | `check_tin_signup_status.sql`, `scripts/dev/seed_dev_data.sql` | Neither is sync |

---

## Step 2 — Stored procedures / functions found

### `check_tin_signup_status(p_tin text) → integer`  *(the only DB function in the repo; NOT sync)*

- **File / line:** `backend/src/Cdns.Infrastructure/Persistence/Sql/Functions/check_tin_signup_status.sql:16`.
- **Created in DB by migration:** **Yes** —
  `backend/src/Cdns.Infrastructure/Migrations/20261103051539_AddCheckTinSignupStatusFunction.cs:14`
  runs the `CREATE OR REPLACE FUNCTION` via `migrationBuilder.Sql(...)`; `Down()` drops it.
- **Parameter:** `p_tin text`.
- **Reads:** `companies`, `company_member_mappings`, `members`, `member_users`. **Writes:** nothing (read-only).
- **Returns:** `1` master active / `2` pending eReg / `3` not activated / `4` TIN < 9 chars /
  `5` master account incomplete / `6` company not found.
- **Upsert key / ON CONFLICT:** N/A (no writes).
- **Filters / date window / timezone:** `is_deleted = false` on companies/mappings/members;
  `member_type = 2` (CompanyMasterMember); `length(trim(p_tin)) < 9` gate. No dates, no timezone.
- **Called by:** `SignupService.GetTinStatusAsync` →
  `backend/src/Cdns.Infrastructure/Services/SignupService.cs:36-38`
  (`SqlQueryRaw<int>("SELECT check_tin_signup_status({0}) …")`), used by verify-tin & register.
- **Transaction / errors:** plain function call; no explicit transaction; errors surface as the
  default `_ => UnexpectedError` branch in `TinStatusToError`.

### Sync stored procedure / function

**None exists.** There is no SP or function that fetches or writes any ASYCUDA/CusDec data.
The sync is implemented entirely in C#:
- `AsycudaSyncService.GetChangedCusDecsAsync` — one Oracle `SELECT` (the header query), read via
  `OracleDataReader` (`AsycudaSyncService.cs:21-111`).
- `CusDecSyncProcessor.ProcessAsync` — EF-side upsert into `cus_decs`
  (`CusDecSyncProcessor.cs:21-105`). The legacy `MERGE` is emulated as "load existing dict →
  add/update" in C#, not SQL.

---

## Step 3 — Coverage matrix vs legacy (12 queries + post-run)

Status + evidence (`file:line` on `Dev` / working tree, which match for these files).

| Row | Legacy unit → target | Status | Evidence |
|-----|----------------------|--------|----------|
| **A04** | CusDec header (`SAD_GENERAL_SEGMENT`, `UN_ASYBRK_TRACK` gate) → `cus_decs` | **Implemented** | Query `AsycudaSyncService.cs:37-82`; gate `:70-79` (`k.OP_DATE_TIME BETWEEN`, `e.BINDER_ID='B_asysad'`); upsert `CusDecSyncProcessor.cs:54-104`. Header-only: `DeclarantCompanyName`/`BlNumber`/`NoOfItems`/`Exporter` left null by design (`:159-175`). |
| **A05** | Items (`SAD_ITEM` + `SAD_SUPPLEMENTARY_UNIT`, BL from `LNK_TPT`) → `cus_dec_items` | **Missing** | No query. `LNK_TPT` appears only in a comment (`AsycudaSyncService.cs:166-167`). `cus_dec_items` table exists, never written. |
| **A06** | Taxes (`SAD_TAX`, `MOP='1' AND amount>0`, rank key) → `cus_dec_taxes` | **Missing** | No `SAD_TAX` anywhere. |
| **A07** | Containers (`SAD_CONTAINERS` ⟕ `CONTAINER_PASS`) → `cus_dec_containers` | **Missing** | No `SAD_CONTAINERS`/`CONTAINER_PASS` query (`CONTAINER_PASS` match = column name only). |
| **A08** | Dispatch containers (`CUSDEC_DISPATCH_TAB` + `CARGO_DISPATCH_TAB`) | **Missing** | Neither table referenced anywhere. |
| **A09a/b** | Container status (`B_ContainerPass` + `B_CDN` binders) → `cus_dec_container_statuses` | **Missing** | No query; table unwritten. |
| **A10** | CusDec status (`UN_ASYBRK_TRACK`/`IED`) → `cus_dec_statuses` | **Missing** | `UN_ASYBRK_TRACK` is used **only as the A04 change-gate**, never to populate `cus_dec_statuses`. |
| **A11** | Exit-note status (`EXIT_NOTE_DETAIL_GENERAL_SEG`, `Status='Exit'`) | **Missing** | Table referenced nowhere. |
| **A12** | Inspect status (`INSP_ACT_TAB`: APR/STF/DIS_DTN/FRD_TYP) → `cus_dec_statuses` | **Missing** | `INSP_ACT_TAB` referenced nowhere. |
| **A13** | Inspection (`INS_FRD_TAB`, `FRD_NFR NOT IN (…)`) → `cus_dec_inspections` | **Missing** | `INS_FRD_TAB` referenced nowhere. |
| **A14** | Inspection section (`INS_SEC_TAB`, `SEC_DAT` gate) → `cus_dec_inspection_section_tabs` | **Missing** | `INS_SEC_TAB` referenced nowhere. |
| **A15** | Payment summary (CIF, tax, recoveries) → `cus_dec_payment_summaries` | **Missing** | No aggregation query. Table read by dashboard (`CompanyDashboardService.cs:104-115`) but never populated → always zero. |
| **A17** | Post-run: child `cus_dec_id` resolution | **Missing** | No children synced, so nothing to resolve. |
| **A18** | Post-run: `NoOfItems` | **Missing** | Always null by design (`AsycudaSyncService.cs:168,173`; `CusDecSyncProcessor.cs:79`). |
| **A19** | Post-run: `BlNumber` `'Many'` rule | **Missing** | `BlNumber` never set (`AsycudaSyncService.cs:166-167,172`). |
| **A20** | Post-run: item global identity | **Missing** (CusDec-level only) | Only `CusDec.GlobalIdentity = Guid.NewGuid()` (`AsycudaSyncService.cs:157`); no item-level identity (no items). |
| **A21** | Post-run: company re-match `LEFT(tin,9)` for all NULL ids | **Partial** | `LEFT(tin,9)` lookup implemented (`CusDecSyncProcessor.cs:107-116`), rebuilt every cycle, applied to declarant+consignee on insert **and** update (`:56-57,63-64`). **Gap:** re-match only happens for CusDecs that re-enter the Oracle change window; there is **no standalone sweep** re-matching all historically-NULL company ids. |
| **A02** | 26h look-back overlap | **Partial** | 26h fallback is **first-run only** (`Worker.cs:65`, `AddHours(-26)`). Subsequent runs use the exact previous `runStartTime` as the lower bound with **no overlap subtracted**, so there is no rolling 26h/overlap safety window. Watermark advances only on success (`:76-78`). |
| **A27** | Failure email notification | **Missing** | Failures are logged and written to `task_schedule_app_details.Errors` (`Worker.cs:88-92`); **no email/alert** is sent. |

**Score: A04 Implemented; A21 & A02 Partial; everything else Missing → 1 of 12 source queries.**

---

## Step 4 — Proof it runs

- **Build:** `dotnet build backend/Cdns.slnx` → **Build succeeded, 0 errors** (2 warnings). .NET SDK 10.0.203.
- **Sync processor unit tests:** `CusDecSyncProcessorTests` (5 tests) **could not run** — they
  require a live PostgreSQL at `127.0.0.1:5432`, which is not running here
  (`Npgsql … Failed to connect to 127.0.0.1:5432: Connection refused`). I did **not** provision a
  database (project rules protect the dev DB; the test target `5432` is also not the local
  `5433` instance). So the tests are **unverified**, not failing on logic.
- **End-to-end Oracle → Postgres sync:** **cannot be run** here — there is no Oracle instance or
  `ConnectionStrings:AsycudaDb` credentials available, and running it is out of scope for a
  read-only check. Per-table row counts therefore **cannot be reported**.

The build result confirms the sync code compiles; it does **not** demonstrate that all data is
fetched — and by static evidence (Step 1–3) it is not.

---

## Verdict

The teammate's statement is inaccurate. The ASYCUDA sync currently implements **one** of the
twelve legacy Oracle queries — the CusDec header — plus partial company-id matching and a
first-run-only 26h fallback. The remaining eleven queries (items, taxes, containers, dispatch,
container statuses, CusDec statuses, exit-note, inspect status, inspections, inspection
sections, payment summary) and the failure-email are **not implemented on any branch**, and no
stored procedure or function does this work. The destination schema exists and is merged into
`Dev`; the ingestion is not.
