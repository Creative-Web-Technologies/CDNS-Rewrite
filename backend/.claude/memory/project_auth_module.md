---
name: project-auth-module
description: Auth module (Login/MasterActivation/PasswordReset) implementation status and key design decisions
metadata:
  type: project
---

Auth module (Tenuka's slice) is on branch `feature/login-activation-reset`.
Chirath's signup/OTP slice is on `feature/signup-otp-activation` — do not touch signup-named files.

**Why:** Separating team slices onto separate branches; both build off the same MemberUser foundation from `feature/authenticationScaffold`.

**How to apply:** Before any auth changes, confirm which slice they belong to. Never touch `MemberSignupTables/TempororyMember.cs`, `TemperoryMemberConfiguration.cs`, or any Signup-prefixed service.

## Files created (2026-10-29)

### Application layer
- `Cdns.Application/Interfaces/IEregService.cs` — `ValidateMasterActivation(username, password)`
- `Cdns.Application/Interfaces/IAuthResultIssuer.cs` — creates MemberLoginSession, returns SessionId string
- `Cdns.Application/Interfaces/IEmailService.cs` — stub for OTP delivery
- `Cdns.Application/Auth/ILoginService.cs` + `LoginResult` sealed record hierarchy
- `Cdns.Application/Auth/IMasterActivationService.cs` + request/result models
- `Cdns.Application/Auth/IPasswordResetService.cs` + result models

### Infrastructure layer
- `ExternalServices/FakeEregService.cs` — always returns true; real HMAC-HTTP impl deferred
- `Notifications/LoggingEmailService.cs` — IEmailService stub via ILogger
- `Services/Auth/MemberLoginSessionIssuer.cs` — IAuthResultIssuer writes MemberLoginSession row
- `Services/Auth/LoginService.cs` — main login logic
- `Services/Auth/MasterActivationService.cs` — eReg + profile fill + set password + CusDec claim
- `Services/Auth/PasswordResetService.cs` — OTP generate/verify/reset

### API layer
- `Endpoints/Auth/AuthRequestModels.cs` — request record types
- `Endpoints/Auth/AuthEndpoints.cs` — minimal API mapped at `/api/auth/...`

## Key design decisions

**IsWrongAttempt is a bool, not a counter.** Single-strike lockout. To support N-attempt threshold, add a `WrongAttemptCount int` column (requires migration). Brief acknowledged this.

**ActivationToken = MemberUser.GlobalIdentity.** Returned on successful eReg validation; client carries it through profile-fill and set-password steps. No separate DB row needed.

**SetupDefaultSubscription is stubbed.** `MemberSubscription.SubscriptionId` FK requires a real Subscriptions row; no subscription seed data exists. Implement when subscription records are seeded.

**CusDec auto-claim is implemented** via EF `ExecuteUpdateAsync` bulk update — mirrors `UpdateMasterMemberDefaultAcknowledgement`. Claims both Declarant and Consignee roles for unclaimed+undisclaimed rows matching the member's company.

## Endpoints
| Method | Path | Notes |
|--------|------|-------|
| POST | /api/auth/login | Returns `{ type, sessionToken }` or `{ type, activationToken }` |
| POST | /api/auth/master-activation/profile | Fill FirstName/LastName/Mobile/Nic/Designation |
| POST | /api/auth/master-activation/password | Set password, issues session, triggers CusDec claim |
| POST | /api/auth/password-reset/forgot-password | Always 200 (no enumeration) |
| POST | /api/auth/password-reset/verify-otp | Unlocks account on valid OTP |
| POST | /api/auth/password-reset/reset-password | Verifies OTP + sets new password |

## Seed accounts
- `master-a@test.cdns` / `Test@12345` → normal login (master, RequiredEregVerification=false)
- `existingmember-a@test.cdns` / `Test@12345` → normal login (regular member)
- `master-b@test.cdns` → master activation branch (RequiredEregVerification=true, NotActive)
