# FK Audit Report — CDNS-Rewrite backend
**Branch:** `feature/company/subscription` (HEAD `728863a`)  
**Date:** 2026-10-21  
**Scope:** All 40 entities across 5 sections; all 40 EF configurations; `CdnsDbContext`; 1 migration.  
**Rule set applied:** Domain rules 1-5 as specified in the audit brief.

---

## 1. Section Summary

| Section | Entities | Configured FKs | FKs with issues | Other issues | Total issues |
|---|---|---|---|---|---|
| AdminPortal | 11 | 10 | 3 wrong delete behavior, 7 missing indexes | 1 incomplete migration | 11 |
| MemberSignup | 3 | 1 (MemberNotification→Member) | 0 | 1 missing CusDec FK | 1 |
| MemberLogin | 4 | 4 | 0 | 3 missing indexes | 3 |
| CompanySubscription | 7 | 11 | 4 wrong delete behavior | 5 missing FKs, 13 missing indexes | 22 |
| CusDec | 13 | 8 | 7 sub-optimal delete behavior | 2 missing cross-section FKs, 1 missing DbSet, 10 missing indexes, 2 naming | 22 |
| **Total** | **38** | **34** | **14** | **35** | **59** |

> Note: `CusDecInspectionMapping` and `CusDecInspectionSectionTabStatusMapping` are standalone lookup tables with no FK columns; they contribute 0 FK issues.

---

## 2. Full Issue Table

### HIGH Severity

| # | Section | Entity | Property | Category | What is wrong | Recommended fix | Sev |
|---|---|---|---|---|---|---|---|
| H1 | CompanySubscription | `MemberSubscription` | `MemberId` | DELETE_BEHAVIOR | `OnDelete(Cascade)` at `MemberSubscriptionConfiguration.cs:32`. Deleting a `Member` would cascade-erase all their subscription history. Violates domain rule 3. | Change to `Restrict`. | **High** |
| H2 | CompanySubscription | `MemberSubscription` | `SubscriptionId` | DELETE_BEHAVIOR | `OnDelete(Cascade)` at `MemberSubscriptionConfiguration.cs:36`. Deleting a `Subscription` plan cascades to all member subscription records. Violates domain rule 3. | Change to `Restrict`. | **High** |
| H3 | CompanySubscription | `MemberSubscriptionStatusHistory` | `MemberSubscriptionId` | DELETE_BEHAVIOR | `OnDelete(Cascade)` at `MemberSubscriptionStatusHistoryConfiguration.cs:28`. Status history is audit data; cascade-deleting it with the parent `MemberSubscription` violates domain rule 3. | Change to `Restrict`. | **High** |
| H4 | CompanySubscription | `CompanyStatusDetail` | `CompanyId` | DELETE_BEHAVIOR | `OnDelete(Cascade)` at `CompanyStatusDetailConfiguration.cs:29`. Company status-change history is audit data; should survive the parent. Violates domain rule 3. | Change to `Restrict`. | **High** |
| H5 | AdminPortal | `LoginUserActivity` | `UserId` | DELETE_BEHAVIOR | `OnDelete(Cascade)` at `LoginUserActivityConfiguration.cs:27`. Activity logs are history data. Deleting a `User` would erase their login audit trail. Violates domain rule 3. | Change to `Restrict`. | **High** |
| H6 | CusDec | `CusDec` | `DeclarantCompanyId` | CROSS-SECTION | Column `int?` exists on entity (`CusDec.cs:11`) and comment at `CusDecConfiguration.cs:61` defers the FK to "once Company section is built." Company is now merged. No `HasOne`, no FK constraint, no index. Domain rule 2: must be nullable, non-cascade. | Add `HasOne`→`Company` with `HasForeignKey(x => x.DeclarantCompanyId).IsRequired(false).OnDelete(Restrict)` and `HasIndex(x => x.DeclarantCompanyId)` in `CusDecConfiguration.cs`. | **High** |
| H7 | CusDec | `CusDec` | `ConsigneeCompanyId` | CROSS-SECTION | Same situation as H6 (`CusDec.cs:12`, `CusDecConfiguration.cs:61`). Column `int?`, no FK. Domain rule 2. | Same fix pattern as H6. | **High** |
| H8 | CusDec | `CusDecInspectionSectionTab` | *(DbSet)* | MISSING | Entity `CusDecInspectionSectionTab.cs` and config `CusDecInspectionSectionTabConfiguration.cs` both exist, but there is **no `DbSet<CusDecInspectionSectionTab>`** in `CdnsDbContext.cs`. `ApplyConfigurationsFromAssembly` will still build the table in migrations, but the entity is inaccessible without `context.Set<>()`. | Add `public DbSet<CusDecInspectionSectionTab> CusDecInspectionSectionTabs => Set<CusDecInspectionSectionTab>();` to `CdnsDbContext.cs`. | **High** |

---

### MEDIUM Severity

| # | Section | Entity | Property | Category | What is wrong | Recommended fix | Sev |
|---|---|---|---|---|---|---|---|
| M1 | AdminPortal | `AccessControl` | `GroupId` | DELETE_BEHAVIOR | `OnDelete(Cascade)` at `AccessControlConfiguration.cs:24`. Deleting a `Group` silently removes all its access-control entries. Permissions data should be preserved or blocked. | Change to `Restrict` (or `SetNull` if orphaned rows are tolerable). | Med |
| M2 | CompanySubscription | `CompanyMemberMapping` | `CompanyId` | DELETE_BEHAVIOR | `OnDelete(Cascade)` at `CompanyMemberMappingConfiguration.cs:24`. Deleting a `Company` cascades to all member-company membership rows. Should be prevented while members still exist. | Change to `Restrict`. | Med |
| M3 | CompanySubscription | `CompanyStatusDetail` | `EmployeeId` | MISSING | `EmployeeId long?` on entity (`CompanyStatusDetail.cs:13`) records which `Employee` changed company status. No navigation property, no FK configured. `Employee` entity is merged. | Add navigation `Employee? Employee`, FK `HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).IsRequired(false).OnDelete(Restrict)`, and `HasIndex(x => x.EmployeeId)` in `CompanyStatusDetailConfiguration.cs`. | Med |
| M4 | MemberSignup | `MemberNotification` | `CusDecId` | MISSING | `CusDecId long?` on entity (`MemberNotification.cs:17`) with comment "FK added once CusDec entity exists." `CusDec` is now merged. No FK configured in `MemberNotificationConfiguration.cs`. | Add navigation `CusDec? CusDec`, FK `HasOne`→`CusDec` with `.IsRequired(false).OnDelete(SetNull)`, and `HasIndex(x => x.CusDecId)`. | Med |
| M5 | CompanySubscription | `MemberSubscription` | `CompanyMasterMemberId` | MISSING | `CompanyMasterMemberId long` at `MemberSubscription.cs:45`. Non-nullable column, no navigation property, no FK configured. Appears to reference the company's master `Member`. | Confirm target is `Member.MemberId`, then add navigation and FK with appropriate delete behavior. See Questions §4. | Med |
| M6 | CompanySubscription | `SubscriptionRequest` | `ResponderId` | MISSING | `ResponderId long?` at `SubscriptionRequest.cs:16`. No navigation, no FK. Likely references an `Employee` or a `Member`. | Confirm target, add nullable FK with `Restrict`. See Questions §5. | Med |
| M7 | AdminPortal | *(migration)* | `Employee`, `Group` | OTHER | `InitialAdminPortalSchema.cs`: the `employee` table is created with **only** `employee_id`; all other columns (`first_name`, `last_name`, `department_id`, etc.) are absent. Same for `group` (only `group_id`). The current entity definitions are inconsistent with the applied migration. | Regenerate or add a second migration to add the missing columns. Do not apply to prod until reconciled. | Med |

---

### LOW Severity — Missing FKs

| # | Section | Entity | Property | Category | What is wrong | Recommended fix | Sev |
|---|---|---|---|---|---|---|---|
| L1 | CompanySubscription | `Subscription` | `CreatedById` | MISSING | `long?` at `Subscription.cs:27`. No navigation, no FK. Likely FK to `Employee.EmployeeId`. | Confirm, add nullable FK with `Restrict`, add index. See Questions §6. | Low |
| L2 | CompanySubscription | `Subscription` | `UpdatedById` | MISSING | `long?` at `Subscription.cs:30`. Same situation as L1. | Same as L1. | Low |

---

### LOW Severity — Missing Indexes on FK Columns

Every FK column below has a configured or implied FK but no `HasIndex` call. EF Core does **not** automatically create indexes on FK columns for PostgreSQL in all cases; missing them causes sequential scans on joins and child lookups.

| # | Section | Entity | FK column | File |
|---|---|---|---|---|
| L3 | AdminPortal | `AccessControl` | `GroupId` | `AccessControlConfiguration.cs` |
| L4 | AdminPortal | `AccessControl` | `MenuItemFunctionId` | `AccessControlConfiguration.cs` |
| L5 | AdminPortal | `Employee` | `DepartmentId` | `EmployeeConfiguration.cs` |
| L6 | AdminPortal | `LoginUserActivity` | `UserId` | `LoginUserActivityConfiguration.cs` |
| L7 | AdminPortal | `Menu` | `ModuleId` | `MenuConfiguration.cs` |
| L8 | AdminPortal | `MenuItem` | `MenuId` | `MenuItemConfiguration.cs` |
| L9 | AdminPortal | `MenuItemFunction` | `MenuItemId` | `MenuItemFunctionConfiguration.cs` |
| L10 | MemberLogin | `MemberLoginSession` | `MemberId` | `MemberLoginSessionConfiguration.cs` |
| L11 | MemberLogin | `MemberUserLoginDetail` | `MemberId` | `MemberUserLoginDetailConfiguration.cs` |
| L12 | MemberLogin | `MemberActivityLog` | `MemberId` | `MemberActivityLogConfiguration.cs` |
| L13 | CompanySubscription | `CompanyMemberMapping` | `CompanyId` | `CompanyMemberMappingConfiguration.cs` |
| L14 | CompanySubscription | `CompanyMemberMapping` | `MemberId` | `CompanyMemberMappingConfiguration.cs` |
| L15 | CompanySubscription | `CompanyStatusDetail` | `CompanyId` | `CompanyStatusDetailConfiguration.cs` |
| L16 | CompanySubscription | `CompanyStatusDetail` | `EmployeeId` | `CompanyStatusDetailConfiguration.cs` |
| L17 | CompanySubscription | `SubscriptionRequest` | `MemberId` | `SubscriptionRequestConfiguration.cs` |
| L18 | CompanySubscription | `SubscriptionRequest` | `CompanyId` | `SubscriptionRequestConfiguration.cs` |
| L19 | CompanySubscription | `MemberSubscription` | `MemberId` | `MemberSubscriptionConfiguration.cs` |
| L20 | CompanySubscription | `MemberSubscription` | `SubscriptionId` | `MemberSubscriptionConfiguration.cs` |
| L21 | CompanySubscription | `MemberSubscription` | `SubscriptionRequestId` | `MemberSubscriptionConfiguration.cs` |
| L22 | CompanySubscription | `MemberSubscription` | `CompanyId` | `MemberSubscriptionConfiguration.cs` |
| L23 | CompanySubscription | `MemberSubscription` | `CompanyMasterMemberId` | `MemberSubscriptionConfiguration.cs` |
| L24 | CompanySubscription | `MemberSubscriptionStatusHistory` | `MemberSubscriptionId` | `MemberSubscriptionStatusHistoryConfiguration.cs` |
| L25 | CompanySubscription | `MemberSubscriptionStatusHistory` | `MasterMemberId` | `MemberSubscriptionStatusHistoryConfiguration.cs` |
| L26 | CompanySubscription | `MemberSubscriptionStatusHistory` | `EmployeeId` | `MemberSubscriptionStatusHistoryConfiguration.cs` |
| L27 | CusDec | `CusDec` | `DeclarantCompanyId` | `CusDecConfiguration.cs` |
| L28 | CusDec | `CusDec` | `ConsigneeCompanyId` | `CusDecConfiguration.cs` |
| L29 | CusDec | `CusDecContainer` | `CusDecId` | `CusDecContainerConfiguration.cs` |
| L30 | CusDec | `CusDecContainerStatus` | `CusDecContainerId` | `CusDecContainerStatusConfiguration.cs` |
| L31 | CusDec | `CusDecInspection` | `CusDecId` | `CusDecInspectionConfiguration.cs` |
| L32 | CusDec | `CusDecInspectionSectionTab` | `CusDecId` | `CusDecInspectionSectionTabConfiguration.cs` |
| L33 | CusDec | `CusDecItem` | `CusDecId` | `CusDecItemConfiguration.cs` |
| L34 | CusDec | `CusDecStatus` | `CusDecId` | `CusDecStatusConfiguration.cs` |
| L35 | CusDec | `CusDecTax` | `CusDecId` | `CusDecTaxConfiguration.cs` |
| L36 | CusDec | `ReadyToProcessConsignee` | `CusDecReadyToProcessSettingId` | `ReadyToProcessConsigneeConfiguration.cs` |

---

### LOW Severity — Delete Behavior (CusDec owned children)

Domain rule 3 explicitly permits `Cascade` for "true owned children (e.g. CusDec → CusDecItem/Container/Tax rows)." All seven child-of-`CusDec` relationships currently use `Restrict`, which means a `CusDec` row cannot be deleted without manually removing all children first. While `Restrict` is safe, it conflicts with the stated rule and will complicate any sync-driven cleanup scripts.

| # | Section | Entity | FK | Current | Recommended |
|---|---|---|---|---|---|
| L37 | CusDec | `CusDecContainer` | `CusDecId` | `Restrict` | `Cascade` |
| L38 | CusDec | `CusDecContainerStatus` | `CusDecContainerId` | `Restrict` | `Cascade` |
| L39 | CusDec | `CusDecInspection` | `CusDecId` | `Restrict` | `Cascade` |
| L40 | CusDec | `CusDecInspectionSectionTab` | `CusDecId` | `Restrict` | `Cascade` |
| L41 | CusDec | `CusDecItem` | `CusDecId` | `Restrict` | `Cascade` |
| L42 | CusDec | `CusDecStatus` | `CusDecId` | `Restrict` | `Cascade` |
| L43 | CusDec | `CusDecTax` | `CusDecId` | `Restrict` | `Cascade` |

---

### LOW Severity — Naming / Other

| # | Section | Entity | Property | Category | What is wrong | Recommended fix | Sev |
|---|---|---|---|---|---|---|---|
| L44 | CusDec | `CusDecPaymentSummary` | *(namespace)* | NAMING | `CusDecPaymentSummaryConfiguration.cs:5` declares namespace `Cdns.Infrastructure.Persistence.Configurations.CusdecTables` — every other CusDec config uses `Cdns.Infrastructure.Persistence.Configurations.Cusdec`. `ApplyConfigurationsFromAssembly` is unaffected but IDEs and grep are confused. | Change namespace to `Cusdec`. | Low |
| L45 | CusDec | `CusDecInspectionMapping` | *(file name)* | NAMING | Configuration file is `Configurations/Cusdec/CusDecInspectionMapping.cs` (no "Configuration" suffix). All other config files follow `*Configuration.cs`. | Rename file to `CusDecInspectionMappingConfiguration.cs`. | Low |
| L46 | CompanySubscription | `SubscriptionRequest` | `IsDeleted` | OTHER | `SubscriptionRequest.cs:29` declares `bool? IsDeleted`. Every other entity uses `bool IsDeleted`. A nullable bool column changes EF Core's `HasDefaultValue(false)` semantics and makes null-checks in queries inconsistent. | Change to `bool IsDeleted` and add `HasDefaultValue(false)` in config. | Low |

---

## 3. CusDec ↔ Company / Subscription / Member Cross-Section Links

| Relationship | Direction | Current state | Notes |
|---|---|---|---|
| `CusDec.DeclarantCompanyId` → `Company` | CusDec → Company | **MISSING** (issue H6) | Column `int?` exists; comment in `CusDecConfiguration.cs:61` says "comes with Company section." Company is merged. Must be nullable + `Restrict`/`SetNull`. No index. |
| `CusDec.ConsigneeCompanyId` → `Company` | CusDec → Company | **MISSING** (issue H7) | Same. |
| `MemberNotification.CusDecId` → `CusDec` | MemberSignup → CusDec | **MISSING** (issue M4) | Column `long?` exists; comment deferred FK until CusDec merged. CusDec is merged. No FK, no index. |
| `CompanyMemberMapping.MemberId` → `Member` | CompanySubscription → MemberSignup | **Done** — `CompanyMemberMappingConfiguration.cs:27`, `Restrict`. | Type: `long` FK → `long` PK ✓. Missing index (L14). |
| `CompanyMemberMapping.CompanyId` → `Company` | internal | **Done** — `CompanyMemberMappingConfiguration.cs:21`, `Cascade` (issue M2). | Type: `int` FK → `int` PK ✓. Wrong delete behavior + missing index. |
| `SubscriptionRequest.MemberId` → `Member` | CompanySubscription → MemberSignup | **Done** — `SubscriptionRequestConfiguration.cs:41`, `Restrict`. | Type ✓. Missing index (L17). |
| `SubscriptionRequest.CompanyId` → `Company` | internal | **Done** — `SubscriptionRequestConfiguration.cs:47`, `SetNull`. | Type `int?` FK → `int` PK ✓. Missing index (L18). |
| `MemberSubscription.MemberId` → `Member` | CompanySubscription → MemberSignup | **Done** — `MemberSubscriptionConfiguration.cs:29`, but `Cascade` (issue H1). | Type ✓. Wrong delete behavior + missing index. |
| `MemberSubscription.SubscriptionId` → `Subscription` | internal | **Done** — `MemberSubscriptionConfiguration.cs:35`, but `Cascade` (issue H2). | Wrong delete behavior + missing index. |
| `MemberSubscription.CompanyId` → `Company` | internal | **Done** — `MemberSubscriptionConfiguration.cs:47`, `Restrict`. | Type ✓. Missing index (L22). |
| `MemberSubscription.CompanyMasterMemberId` → `Member`(?) | CompanySubscription → MemberSignup | **MISSING** (issue M5) | Column `long` (non-nullable) exists; no FK/nav. |
| `MemberSubscriptionStatusHistory.MasterMemberId` → `Member` | CompanySubscription → MemberSignup | **Done** — `MemberSubscriptionStatusHistoryConfiguration.cs:31`, `Restrict`. | Type `long?` FK → `long` PK ✓. Missing index (L25). |
| `MemberSubscriptionStatusHistory.EmployeeId` → `Employee` | CompanySubscription → AdminPortal | **Done** — `MemberSubscriptionStatusHistoryConfiguration.cs:37`, `Restrict`. | Type `long?` FK → `long` PK ✓. Missing index (L26). |

---

## 4. Questions to Answer Before Fixing

**Q1. `CusDec.DeclarantCompanyId` / `ConsigneeCompanyId` — delete behavior**  
Domain rule 2 says "must not use Cascade." Should it be `Restrict` (prevent deleting a Company while CusDec rows point to it) or `SetNull` (null out the FK when the Company is deleted)? Given that CusDec rows arrive from ASYCUDA and may not have a matching Company initially, `SetNull` seems safer for cleanup scenarios. Needs product decision.

**Q2. `MemberNotification.CusDecId` — delete behavior**  
If a `CusDec` row is ever removed, should the linked notification be: (a) orphaned via `SetNull`, or (b) blocked by `Restrict`? In practice CusDec rows are never hard-deleted, but the migration constraint must be explicit.

**Q3. `CusDec` owned children (L37–L43) — Restrict vs Cascade**  
Is it intentional that `CusDecContainer`, `CusDecItem`, `CusDecTax`, etc. block `CusDec` deletion? If ASYCUDA sync can send a "re-import" that requires deleting + reinserting a `CusDec`, `Restrict` would force manual child-row cleanup first. Confirm whether `Cascade` is acceptable for all seven, or only some.

**Q4. `MemberSubscription.CompanyMasterMemberId`**  
`long` non-nullable column on `MemberSubscription.cs:45`. Is it a FK to `Member.MemberId` (the company's master/admin member)? If so, which delete behavior applies — can a master member be removed while subscriptions exist? Is `CompanyMasterMemberId` nullable in the legacy schema (it is non-nullable here, which may break the subscription-creation flow if the master member is not yet known at insert time).

**Q5. `SubscriptionRequest.ResponderId`**  
`long?` on `SubscriptionRequest.cs:16`. Does this point to `Employee.EmployeeId` (an admin approving the request), `User.EmployeeId` (same table), or `Member.MemberId` (a member approving for another member)? Without the legacy schema this is ambiguous.

**Q6. `Subscription.CreatedById` / `UpdatedById`**  
`long?` on `Subscription.cs:27,30`. Same ambiguity — Employee, User, or Member? Given `Subscription` is an admin-configured plan, `Employee.EmployeeId` is most likely but needs confirmation.

**Q7. `CompanyStatusDetail.EmployeeId` vs `User.EmployeeId`**  
`Employee` and `User` share the same `EmployeeId` PK. The FK target for `CompanyStatusDetail.EmployeeId` should be `Employee` (not `User`), but confirming with the business rule is important before adding the constraint.

**Q8. `CusDecReadyToProcessSetting.CreatedById` / `UpdatedById`**  
`long` and `long?` on `CusDecReadyToProcessSetting.cs:11,13`. Same as Q6 — which table?

**Q9. `Company.TinNumber` — unique index?**  
There is no unique constraint on `Company.TinNumber` (`CompanyConfiguration.cs`). The eReg and ASYCUDA syncs both key on TIN to backfill `DeclarantCompanyId` / `ConsigneeCompanyId`. Without a unique constraint, two companies could share a TIN and make the lookup ambiguous. Should `TinNumber` have a unique index (possibly partial: `WHERE is_deleted = false`)?

**Q10. Migration strategy for unbuilt sections**  
Only `InitialAdminPortalSchema` exists. No migration covers MemberSignup, MemberLogin, CompanySubscription, or CusDec. Is the plan to add one combined migration per feature branch before merging to Dev, or a single migration on Dev? The incomplete `employee` and `group` tables in the existing migration also need reconciling before adding more.

---

## 5. Suggested Fix Order

Fix in this order to keep migrations clean and avoid re-running the same migration twice.

| Step | What | Rationale |
|---|---|---|
| **1** | Add `DbSet<CusDecInspectionSectionTab>` to `CdnsDbContext.cs` (H8) | Zero-migration code fix; unblocks the entity being queryable. |
| **2** | Fix 5 `Cascade → Restrict` on history/audit tables: `MemberSubscription.MemberId`, `MemberSubscription.SubscriptionId`, `MemberSubscriptionStatusHistory.MemberSubscriptionId`, `CompanyStatusDetail.CompanyId`, `LoginUserActivity.UserId` (H1–H5) | These go into the next migration as `ALTER TABLE … ALTER CONSTRAINT`. Getting delete behavior right before any data exists is cheapest. |
| **3** | Add `CusDec → Company` FKs for `DeclarantCompanyId` and `ConsigneeCompanyId` (H6, H7) | After Q1 and Q2 are answered. Nullable + `Restrict` or `SetNull`. One migration, two FK constraints + two indexes. |
| **4** | Fix `AccessControl.GroupId` Cascade → Restrict (M1) and `CompanyMemberMapping.CompanyId` Cascade → Restrict (M2) | Group with access-control fixes; two FK alterations, one migration. |
| **5** | Add missing FK for `CompanyStatusDetail.EmployeeId` (M3) | Needs Q7 answer. One new FK + index. |
| **6** | Add missing FK for `MemberNotification.CusDecId` (M4) | Needs Q2 answer. One new FK + index. |
| **7** | Investigate and add FKs for ambiguous columns: `ResponderId`, `CompanyMasterMemberId`, `Subscription.CreatedById`, `UpdatedById` (M5, M6, L1, L2) | After Q4, Q5, Q6 are answered. Batch into one migration. |
| **8** | Add all missing FK column indexes (L3–L36) | Batch all 34 indexes into one migration per section to minimise migration count. Index creation is non-blocking on PostgreSQL with `CONCURRENTLY` (add `migrationBuilder.Sql` with `CREATE INDEX CONCURRENTLY` if table already has data). |
| **9** | Fix CusDec owned-children `Restrict → Cascade` (L37–L43) | After Q3 is answered. Low urgency while tables are empty. |
| **10** | Fix `SubscriptionRequest.IsDeleted bool? → bool` (L46) | Schema + entity change; trivial column type migration. |
| **11** | Reconcile `InitialAdminPortalSchema` migration (M7) — regenerate or add new migration for missing `Employee` / `Group` columns | Requires coordination with whoever applied the migration to non-dev environments. |
| **12** | Rename `CusDecInspectionMapping.cs` → `CusDecInspectionMappingConfiguration.cs` and fix `CusDecPaymentSummary` namespace (L44, L45) | Pure rename / namespace change, no migration needed. |

---

*End of FK audit report.*
