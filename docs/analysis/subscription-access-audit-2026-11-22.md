# Subscription access audit — B01 / B02 / B13

**Date:** 2026-11-22 · **Mode:** read-only (no source, migration, seed, or DB changes)

Legacy reference (`Core_Bll/MemberServices/MemberSubscriptionService.cs:187-214`) is
**not present in this repo**; all legacy comparisons below are against the format/rules
quoted in the task brief, not against legacy source. Points that cannot be confirmed
without the legacy file are flagged **⚠ verify-legacy**.

---

## Part A — B01: every place a subscription is treated as "active/usable"

| # | File:line | Method | Used by | Status check? | Expiry check (clock) | Company-active? | Owner (MemberId)? | HMAC? | Soft-delete handling |
|---|-----------|--------|---------|---------------|----------------------|-----------------|-------------------|-------|----------------------|
| 1 | `MemberSubscriptionService.cs:28-33` | `GetActiveSubscriptionsAsync` | GET `/api/member/subscriptions` (CusDec switcher list) | `== Activate` | `ExpiryDate >= _clock.StartOfToday()` | **No** — checks `!c.IsDeleted` only | Yes `s.MemberId == memberId` | **No** | company `IsDeleted` filtered; sub soft-delete n/a |
| 2 | `MasterSubscriptionService.cs:211-214` (`IsActive`) | `GetSubscriptionsAsync` / `GetSubscriptionAsync` | GET `/api/master/subscriptions`, `/{id}` | `== Activate` | `expiry >= today` | **No** | scoped by `CompanyMasterMemberId` | **No** | none |
| 3 | `DashboardCalculator.cs:145-147` (`SubscriptionCounts`) | active count | member + company dashboards | `== Activate` | `ExpiryDate >= today` | **No** | caller pre-scopes rows | **No** | caller-dependent |
| 4 | `DashboardCalculator.cs:149-151` | expired count | dashboards | `== Expired` OR `expiry < today` | `< today` | No | pre-scoped | No | — |
| 5 | `MemberDashboardService.cs:57-60` (`activeSubs`) | CusDec visibility scopes + nearest-expiry tile | GET member dashboard | `== Activate` | `ExpiryDate >= today` | **No** | `s.MemberId == memberId` | **No** | company joined, **no `IsDeleted` filter** |
| 6 | `CompanyDashboardService.cs:66-69` (`activeSubs`) | CusDec visibility scopes | GET company dashboard | `== Activate` | `ExpiryDate >= today` | **No** | scoped by `companyIds` | **No** | company joined, **no `IsDeleted` filter** |

**Distinct copies:** the predicate `status == Activate && ExpiryDate >= today` is inlined in
**5 independent places** (#1, #2, #3, #5, #6; #4 is its inverse). They agree on status+expiry
but **disagree on company handling**: only #1 filters `company.IsDeleted`; #5/#6 ignore it; none
check `CompanyStatusEnum == Active`.

**Disagreement with legacy (brief):** legacy requires **5** conditions (status=1, expiry>=today,hartihhhh
`Company.CompanyStatusEnum==1`, owner match, **HMAC valid**). Every rewrite copy is missing the
**Company-active** check and the **HMAC** check. Company entity *does* expose
`CompanyStatusEnum` (`CompanyStatusEnum.Active = 1`) — it is simply never consulted. The
rewrite substitutes `IsDeleted` (a different notion) in only one of the six sites.

**ACCESS decisions** (gate what a user may do/see): #1 (switcher list), #5, #6 (CusDec
visibility). **DISPLAY only**: #2 (`isActive` badge in Master list/detail), #3, #4 (dashboard
tile counts). There is **no CusDec access-gate service** — nothing downstream re-checks a
subscription before serving declaration data; visibility is decided entirely by the dashboard
scope queries (#5/#6) and the switcher list (#1).

---

## Part B — B02: HMAC key

### 1. WRITE paths
Only **one** site computes/saves `Key`:

`MasterSubscriptionRequestService.cs:276-283` (approve):
```csharp
subscription.Key = _keyService.Generate(
    subscription.MemberId, subscription.SubscriptionId, subscription.MemberSubscriptionId,
    expiryDate /*Colombo-offset*/, subscription.PaidAmount /*0m*/,
    subscription.SubscriptionActionTypeEnum, subscription.SubscriptionTypeEnum);
```
Generation itself: `MemberSubscriptionKeyService.cs:28-85`.
- **Data string** (`:77-78`): `"{memberId}!{subscriptionId}!{memberSubscriptionId}!{expiryStr}!{paidAmount}!{actionType}!{subType}"`
- **Date format**: `yyyy-MM-dd`, converted **UTC→Asia/Colombo** first (`:80-84`).
- **PaidAmount**: `decimal.ToString(CultureInfo.InvariantCulture)` (`:78`). Approve always passes `0m` → `"0"`.
- **Enums**: emitted as **`int`** (`actionTypeEnum`, `typeEnum` are `int` params).
- **Algorithm**: `HMACSHA512` (`:64`).
- **HMAC key bytes**: `UTF8(_secret + expiryStr)` (`:62`).
- **Output encoding**: `Convert.ToBase64String` — Base64, mixed case (`:66`).

### 2. Character-level diff vs legacy brief
| Element | Legacy (brief) | Rewrite | Match? |
|---------|----------------|---------|--------|
| Field order / separators | `Id!Sub!MSub!Expiry!Paid!Action!Type` with `!` | identical | ✅ |
| ExpiryDate | `yyyy-MM-dd` | `yyyy-MM-dd` (Colombo-local) | ✅ if legacy date was Colombo wall-clock ⚠ verify-legacy |
| PaidAmount | `{PaidAmount}` (culture unspecified) | `InvariantCulture` | ⚠ verify-legacy (trailing zeros / separators, e.g. `1500.00` vs `1500`) |
| ActionType enum | `{SubscriptionActionTypeEnum}` | **int** | ⚠ **verify-legacy** — if legacy interpolated an enum *type* it emitted the **name** ("General"), not the int |
| SubType enum | `{SubscriptionTypeEnum}` | **int** | ⚠ **verify-legacy** — same risk ("Declarant"/"Consignee" vs 1/2) |
| HMAC key | `secret + ExpiryDate` | `secret + expiryStr` | ✅ if legacy appended the `yyyy-MM-dd` string ⚠ verify-legacy |
| Algorithm | HMAC-SHA512 | HMAC-SHA512 | ✅ |
| Output | (brief unspecified) | Base64 | ⚠ verify-legacy (hex vs base64, case) |

The **enum int-vs-name** question is the single largest parity risk and cannot be settled
from this repo.

### 3. READ paths
**The key is never verified anywhere.** `.Verify(` has zero production call sites
(`grep`: only `MemberSubscriptionKeyService` defines it and tests call it). No access decision
in Part A consults `Key`. It is write-only today.

### 4. Secret loading
- Config key: `Cdns:MemberSubscriptionSecret`.
- Read in `MemberSubscriptionKeyService` ctor (`:23`) — throws `InvalidOperationException` if null.
- **Startup validation**: `Program.cs:43-47` throws if null/whitespace (checks `Cdns:MemberSubscriptionSecret`, hints `Cdns__MemberSubscriptionSecret` env var).
- Registered **scoped**: `DependencyInjection.cs:62` (`AddScoped`). Not an `IOptions`; raw `IConfiguration` read. Dev value present in `appsettings.Development.json:19`.

### 5. Comparison method
`string.Equals(expected, key, StringComparison.Ordinal)` (`:55`). **Not** `CryptographicOperations.FixedTimeEquals` — not constant-time. Low risk while unused, but note before it gates access.

### 6. Paths that change a hashed field after creation
Hashed fields: ExpiryDate, PaidAmount, Action/SubType, SubscriptionId, MemberId. After the
approve insert, the only mutation found is `MasterSubscriptionService.DeactivateAsync:194`,
which changes **`MemberSubscriptionStatusEnum`** — **not hashed**, so no recompute needed.
No code path updates ExpiryDate, PaidAmount, PaidDate, or the enums post-creation ⇒ **no
stale-key risk from edits today** (because nothing edits them, and nothing verifies them).

### 7. Seed data
`MemberPortalSeedData.NewMemberSubscription` (`:491-518`) sets `PaidAmount = plan.Amount`,
`PaidDate = start`, and **never sets `Key`** ⇒ all three seeded `member_subscriptions`
(`MS-A-0001`, `MS-B-0001`, `MS-A-0000`) have **`Key == NULL`**. They would **fail** any future
verification. Note also seed `PaidAmount = plan.Amount` (non-zero) vs approve's `0m`, so even a
recomputed key would differ on the PaidAmount field.

Read-only SQL to inspect (run yourself in DBeaver):
```sql
SELECT member_subscription_id,
       (key IS NULL) AS key_is_null,
       expiry_date,
       member_subscription_status_enum
FROM   member_subscriptions
ORDER  BY member_subscription_id;
```

---

## Part C — B13: duration / Lifetime

### 1. Enum members
`SubscriptionDurationTypeEnum.cs:5-15`: `Days = 0`, `Months = 1`, `Years = 2`.
Legacy: `0–3` with `3 = Lifetime`.

| Value | Legacy | Rewrite | |
|-------|--------|---------|--|
| 0 | Days | Days | ✅ |
| 1 | Months | Months | ✅ |
| 2 | Years | Years | ✅ |
| 3 | **Lifetime (+500y)** | **absent** | ❌ missing |

0/1/2 align, so **no renumbering hazard** — but Lifetime is unrepresented.

### 2. Expiry calculation
`SubscriptionExpiry.ComputeExpiryDate:30-35` — `switch` on duration type with Months/Years cases
and a catch-all `_ => AddDays(n)`. A row with `durationType == 3` (or any unknown) silently
falls into **AddDays(n)** ⇒ a legacy Lifetime plan would get `expiry = start + n days` instead of
+500 years. No exception, **wrong date**. Same default-to-Days logic is duplicated in the display
formatter `MemberSubscriptionService.FormatDuration:60-68` and `MasterSubscriptionRequestService.DurationLabel:457-462`.

### 3. Plans created/seeded with a duration type
Seed `MemberPortalSeedData` creates all plans with `SubscriptionDurationTypeEnum.Months`
(`:61,73,85`). No seed/plan uses value 3; nothing would shift meaning under the current 0/1/2.

### 4. Frontend
`frontend/src/pages/company/requests/planUtils.js:8-12` (`formatDuration`): Years/Months else
**Days** fallback. `PlanCard.jsx:7-10` (`toDays`): Years×365, Months×30, else raw. A Lifetime
(type 3) plan renders as `"{n} Days"` and sorts as n days — **wrong**. Member switcher shows
`sub.planDuration` precomputed by the backend formatter (same Days fallback).

---

## Part D — Proposed design (not implemented)

**`ISubscriptionAccessService`** (Application interface, Infrastructure impl):

- `Task<SubscriptionAccess> CheckAsync(long memberSubscriptionId, long callerMemberId, ct)`
  returning a result with explicit failure code:
  `NotFound | NotOwner | Inactive | Expired | CompanyInactive | KeyMismatch | KeyMissing | Ok`.
  Order: load → owner → status==Activate → `ExpiryDate >= clock.StartOfToday()` →
  `Company.CompanyStatusEnum == Active` → `Key != null` (else `KeyMissing`) →
  `_keyService.Verify(...)` with `CryptographicOperations.FixedTimeEquals` (else `KeyMismatch`).
- `IQueryable<MemberSubscription> UsableFilter(IQueryable<...>, long memberId, DateTimeOffset today)`
  applying the SQL-translatable predicate (owner + status + expiry + company active/not-deleted);
  **HMAC verified in memory** after materialisation (HMAC can't run in SQL).
- Uses the existing `IBusinessClock` for the Colombo date (reuse `StartOfToday()`).

**Switch to the service (ACCESS):** Part A #1 (`GetActiveSubscriptionsAsync`), #5 and #6
(dashboard `activeSubs` scope building). **Stay as-is (DISPLAY):** #2 `IsActive` badge, #3/#4
dashboard counts — these summarise history and should not drop rows on HMAC failure (optionally
reuse the status+expiry predicate without HMAC). New CusDec data endpoints should call
`CheckAsync` before serving.

**Tests needing updates:** `MemberSubscriptionServiceTests` (4 active-filter tests would gain a
company-active + key dimension), dashboard scope expectations in
`MemberSubscriptionServiceTests`/dashboard tests, and `MasterSubscriptionRequestServiceTests`
approve tests (now the produced key must satisfy `Verify`).

**Risks**
- **Seeded keys are NULL** → every seeded sub returns `KeyMissing` the moment verification goes
  live (lists/dashboards would empty out). Needs a seed key backfill or a grace path.
- **PaidAmount mismatch**: approve hashes `0m`, seed stores `plan.Amount`; a backfill must hash the
  *stored* PaidAmount or standardise it.
- **Secret rotation**: key embeds `secret + expiryStr`; rotating the secret invalidates all
  existing keys (no versioning). Consider a key-version column before rotation is possible.
- **Rows created before key generation** / partial failures (key set in a second `SaveChanges`
  after insert) could leave `Key == NULL`; `KeyMissing` handling must be deliberate.
- **Enum int-vs-name** and **PaidAmount culture** parity (Part B.2) must be locked down before any
  legacy-issued key is trusted.
- `string.Equals`/`Ordinal` comparison is not constant-time (switch to `FixedTimeEquals`).

---

## Part E — Tests that exist today

| Test file | Facts | Covers |
|-----------|-------|--------|
| `Cdns.Infrastructure.Tests/MasterSubscriptionRequestServiceTests.cs` | 18 | approve/reject/plans/self-create; uses **real** `MemberSubscriptionKeyService` via `FakeKeyServiceFactory` (key generated, never asserted against `Verify`) |
| `Cdns.Infrastructure.Tests/MemberSubscriptionServiceTests.cs` | 13 | active list: excludes expired, expiry-today included, yesterday excluded, excludes other member |
| `Cdns.Infrastructure.Tests/MasterSubscriptionServiceTests.cs` | 6 | deactivate state guard, `IsActive` flag, date-range filter |
| `Cdns.Infrastructure.Tests/MemberSubscriptionRequestServiceTests.cs` | 10 | member request list/create/cancel/duplicate |
| `Cdns.Application.Tests/SubscriptionExpiryTests.cs` | 9 | expiry edge cases (31Jan+1mo, 29Feb+1yr, Colombo-day) **+ HMAC round-trip / tamper / determinism** |
| `Cdns.Application.Tests/DashboardCalculatorTests.cs` | 9 | dashboard counts incl. subscription active/expired/pending |

**Key-generation coverage caveat:** `SubscriptionExpiryTests` HMAC tests (`:131,156,174`) run
against a **re-implemented** `TestKeyService` (`:208-243`), *not* the production
`MemberSubscriptionKeyService` — the two formats are hand-kept in sync and can silently drift.
No test asserts a **Lifetime/duration-type-3** path, a **company-active** gate, or **HMAC
verification inside an access decision** (because none exists).
