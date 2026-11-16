---
name: project-subscriptions-requests
description: feature/member-subscriptions-requests and master subscriptions feature — services, UTC fix, interceptor, HMAC round-trip
metadata:
  type: project
---

Subscription request and subscription management features. Includes both the member-facing endpoints and the master (company admin) endpoints.

**Key design decisions:**
- `IBusinessClock` contract: ALL methods return UTC (Offset == TimeSpan.Zero). `StartOfToday()` returns the UTC equivalent of Colombo midnight (not the Colombo-offset value itself). This means the value is safe as a Npgsql query parameter.
- `ColomboBusinessClock.StartOfToday()` and `FakeBusinessClock.StartOfToday()`/`Now()` both call `.ToUniversalTime()` before returning. The same instant, offset 0.
- `SubscriptionExpiry.ComputeExpiryDate` returns a Colombo-offset (+05:30) DateTimeOffset. At the assignment point in `ApproveRequestAsync`, it is converted with `.ToUniversalTime()` before assigning to `ExpiryDate`.
- Three-layer UTC defense: (1) source fix in clock; (2) `UtcDateTimeOffsetInterceptor` (SaveChangesInterceptor) normalizes entity property values before SaveChanges; (3) `UtcDateTimeOffsetCommandInterceptor` (DbCommandInterceptor) normalizes query parameters before SQL execution. Neither uses an EF value converter (would break Npgsql query translation).
- HMAC key (`MemberSubscriptionKeyService`): both `Generate` and `Verify` call `ToColomboDayString` which converts to Asia/Colombo before formatting, so signing with Colombo-offset and verifying with the UTC value read from DB both yield the same date string — HMAC round-trip is correct.
- `ApproveRequestAsync` uses `await using` on the transaction; no explicit `RollbackAsync` in a catch block. Disposal handles rollback on early return and on exceptions, so the original exception is never masked.
- Request numbers: SR-{id:D6}. Subscription numbers: MS-{id:D6}.

**Why:** `SubscriptionExpiry.ComputeExpiryDate` correctly uses Colombo for business-date arithmetic (rule: Colombo for decisions, UTC for persistence). The UTC fix stops the production 500 from Npgsql. The interceptor is the safety net for any future code that forgets to call `.ToUniversalTime()`.

**How to apply:** Always call `.ToUniversalTime()` when assigning a ComputeExpiryDate result or any other Colombo-derived DateTimeOffset to an entity property. The interceptor will catch anything that slips through, but explicit conversion is preferred.
