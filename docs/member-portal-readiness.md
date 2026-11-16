# Member Portal Readiness — Repository Audit

_Read-only audit of CDNS-Rewrite. No files were modified. Generated 2026-11-15._

This documents what currently exists in the repo relevant to building a **Member dashboard page that shows real data**, and ends with a gaps list.

---

## 1. Frontend routing

Router: **react-router-dom v7** (`BrowserRouter`), wired in `frontend/src/App.jsx`. `AuthProvider` wraps all routes.

### Routes

| Path | Component file | Guard wrapper |
|------|----------------|---------------|
| `/login` | `pages/auth/Login/LoginPage.jsx` | `RedirectIfLoggedIn` |
| `/signup` | `pages/auth/Signup/SignupPage.jsx` | `RedirectIfLoggedIn` |
| `/master-activation` | `pages/auth/MasterActivation/MasterActivationPage.jsx` | none (transient flow) |
| `/otp-verification` | `pages/auth/OtpVerification/OtpVerificationPage.jsx` | none (transient flow) |
| `/forgot-password` | `pages/auth/ForgotPassword/ForgotPasswordPage.jsx` | none (transient flow) |
| `/member` (layout) | `components/portal/PortalLayout.jsx` | `PortalGuard portal="Member"` |
| `/member` index | → redirects to `dashboard` | (inside guard) |
| `/member/dashboard` | `pages/member/MemberDashboard.jsx` | (inside guard) |
| `/member/cusdecs` | `pages/portal/ComingSoon.jsx` ("My CusDecs") | (inside guard) |
| `/member/subscriptions` | `ComingSoon` ("Subscriptions") | (inside guard) |
| `/member/notifications` | `ComingSoon` ("Notifications") | (inside guard) |
| `/member/profile` | `ComingSoon` ("Profile") | (inside guard) |
| `/company` (layout) | `components/portal/PortalLayout.jsx` | `PortalGuard portal="Company"` |
| `/company` index | → redirects to `dashboard` | (inside guard) |
| `/company/dashboard` | `pages/company/CompanyDashboard.jsx` | (inside guard) |
| `/company/cusdecs` | `ComingSoon` ("Company CusDecs") | (inside guard) |
| `/company/members` | `ComingSoon` ("Members & Subscriptions") | (inside guard) |
| `/company/payments` | `ComingSoon` ("Payments") | (inside guard) |
| `/company/notifications` | `ComingSoon` ("Notifications") | (inside guard) |
| `/company/profile` | `ComingSoon` ("Profile") | (inside guard) |
| `*` | — | `SmartRedirect` (→ own portal dashboard, else `/login`) |

### Guards (all defined inline in `App.jsx`)
- **`RedirectIfLoggedIn`** — while `loading` shows a spinner; if `user` exists redirects to `/${user.portal}/dashboard`; otherwise renders children. Used to keep logged-in users off `/login` and `/signup`.
- **`PortalGuard({ portal })`** — spinner while loading; no user → `/login`; `user.portal !== portal` → redirect to the user's own portal dashboard; else renders `<PortalLayout portal={portal} />` (which renders the matched child route via `<Outlet/>`).
- **`SmartRedirect`** — catch-all.

### Member vs Master decision
Decided **on the backend** and surfaced as `user.portal`:
- The `/api/auth/me` endpoint maps `MemberType.CompanyMasterMember → portal "Company"`, everything else → `"Member"` (`backend/.../Endpoints/Auth/AuthEndpoints.cs`).
- After login, `LoginPage.handleLogin` either routes to `/master-activation` (when the API returns `type: "masterActivationRequired"`) or calls `refreshUser()` and navigates to `/${portal}/dashboard`.
- The frontend never inspects `memberType` directly for routing — it keys entirely off `portal`.

### Where the session/user lives
- **Stored in React context:** `contexts/AuthContext.jsx` (`AuthProvider`) holds `user` + `loading` in `useState`. Context object is in `contexts/AuthContext.js`; hook accessor is `contexts/useAuth.js` (`useAuth()`).
- `user` shape: `{ userName, firstName, lastName, memberType, portal }` (or `null`).
- **`refreshUser()`** — `GET /api/auth/me`, sets `user`, returns the data (or `null` on failure). Called once on mount and after login.
- **`logout()`** — `POST /api/auth/logout` (best-effort) then clears `user`.
- There is **no token in JS/localStorage** — identity rides entirely on an HTTP-only `session_id` cookie (see §5). Reads: `useAuth()` consumed by `PortalLayout`, `MemberDashboard`, `CompanyDashboard`, `LoginPage`.

---

## 2. Frontend data access

- **Shared client:** `frontend/src/lib/api.js` exposes `apiGet(url)` and `apiPost(url, body)`. Both use `fetch` with `credentials: 'include'` (so the session cookie is sent). `apiPost` sets `Content-Type: application/json`.
- **Error handling:** on non-2xx, both throw an `Error` built from the parsed JSON body — attaching `error.status`, `error.code`, and (post) `error.field`. Message falls back to `HTTP <status>`. There is **no centralized 401 handling / auto-redirect / interceptor** — a 401 is just a thrown error each caller handles itself.
  - `AuthContext.refreshUser` swallows the error and sets `user = null` (this is the de-facto 401 handling path → guards then bounce to `/login`).
  - `MemberDashboard` / `CompanyDashboard` catch and set a local `loadErr` flag, then render with zeroed/empty data (`summary ?? {}`); they do **not** redirect on 401.
- **No raw `fetch` in pages** — dashboards and auth pages all go through `lib/api.js`.
- **Vite proxy** (`frontend/vite.config.js`): `/api` → `http://localhost:5030`, `changeOrigin: false`. Dev server default port 5173 (matches backend CORS `AllowVite` origin).

---

## 3. Frontend stack

- **Dependencies (`frontend/package.json`):** only `react@^19`, `react-dom@^19`, `react-router-dom@^7`. Dev: `@vitejs/plugin-react`, `vite@^8`, `oxlint`.
- **No UI / icon / animation libraries** installed — no Tailwind, MUI, Chakra, styled-components, emotion, framer-motion, clsx, or an icon package. All icons are **hand-written inline SVG** (e.g. the icon components inside `PortalLayout.jsx` and the dashboards).
- **Styling:** plain **hand-written CSS files** imported per component (no CSS Modules, no Tailwind, no CSS-in-JS). Files: `index.css`, `App.css`, `AuthShell.css`, `AuthLayout.css`, `LoginPage.css`, `Signup.css`, `PasswordRequirements.css`, `PortalLayout.css`, `Dashboard.css`, `ComingSoon.css`. Dashboards share one stylesheet (`pages/portal/Dashboard.css`).
- **Shared layout components:**
  - `pages/auth/AuthShell.jsx` — the auth-screen chrome (gov header bar + split left/right panel); used by `LoginPage`. (`AuthLayout.jsx`/`.css` also exist but are not referenced by the router.)
  - `components/portal/PortalLayout.jsx` — the authenticated portal shell: topbar (brand, portal badge, user name, logout), collapsible sidebar driven by `MEMBER_NAV` / `COMPANY_NAV` arrays, and `<Outlet/>` for page content. Same component serves both portals, switched by the `portal` prop.

---

## 4. Backend API

Two surfaces: **minimal-API auth endpoints** (`Endpoints/Auth/AuthEndpoints.cs`, mapped via `app.MapAuthEndpoints()`) and **MVC controllers** (`Controllers/`). Health check at `GET /health`.

### Auth endpoints — `/api/auth` (no auth required unless noted)
| Verb | Route | Auth | Request → Response |
|------|-------|------|--------------------|
| POST | `/api/auth/login` | none | `{userName,password}` → `{type:"success",portal}` \| `{type:"masterActivationRequired",activationToken}` \| 401 `{code:"AccountLocked"\|"InvalidCredentials"}`. On success sets `session_id` cookie. |
| GET  | `/api/auth/me` | **RequireAuthorization** | → `{userName,firstName,lastName,memberType,portal}` \| 401 |
| POST | `/api/auth/logout` | none (reads cookie) | `{}` → 200; deletes session row + cookie |
| POST | `/api/auth/master-activation/profile` | none (token in body) | `{activationToken,firstName,lastName,mobile,nic,designation}` → 200 \| 400 `{code,field}` \| 404 |
| POST | `/api/auth/master-activation/password` | none (token in body) | `{activationToken,password}` → `{type:"success",portal}` \| 400 `{code:"WeakPassword"}` \| 404 |
| POST | `/api/auth/password-reset/forgot-password` | none | `{userName}` → `{sent:true}` (always 200, anti-enumeration) |
| POST | `/api/auth/password-reset/verify-otp` | none | `{userName,otp}` → 200 \| 400 `{code:"InvalidOtp"}` |
| POST | `/api/auth/password-reset/reset-password` | none | `{userName,otp,newPassword}` → 200 \| 400 `{code:"WeakPassword"\|"InvalidOtp"}` |

### Signup controller — `/api/Signup` (no auth)
| Verb | Route | Request → Response |
|------|-------|--------------------|
| POST | `/api/Signup/verify-tin` | `{tin}` → `VerifyTinResponse` (statusCode) |
| POST | `/api/Signup/register` | `RegisterRequest` → `RegisterResponse` (success/code); 409 on `AccountExists`/`EmailBelongsToMaster` |
| POST | `/api/Signup/verify-otp` | `VerifyOtpRequest` → `{success,message}` |
| POST | `/api/Signup/set-password` | `SetPasswordRequest` → `{success,message,portal}` \| 400 `{code:"WeakPassword"}` |

### Portal data controllers
| Verb | Route | Auth policy | Response | Notes |
|------|-------|-------------|----------|-------|
| GET | `/api/member/dashboard-summary` | **`MemberOnly`** | `{activeSubscriptions,claimedCusDecs,unreadNotifications,pendingRequests,recentActivity:[]}` | **Stub — all values hard-coded `0`/empty.** Serves the member dashboard. |
| GET | `/api/company/dashboard-summary` | **`MasterOnly`** | `{companyMembers,pendingSubscriptionRequests,activeSubscriptions,paymentsDue,recentActivity:[]}` | **Stub — all hard-coded `0`/empty.** Serves the company dashboard. |

**Auth-only vs portal-data:** `/api/auth/me` and `/api/auth/logout` are the only *authenticated* auth endpoints. The two `dashboard-summary` endpoints are the only **portal-data** endpoints — and both return placeholder zeros (no DB access). Everything else is pre-login auth/signup flow.

---

## 5. Backend auth

- **Scheme:** custom `SessionCookieAuthHandler` (`Api/Authentication/SessionCookieAuthHandler.cs`), registered as the default scheme `"SessionCookie"` in `Program.cs`.
- **Per-request validation (`HandleAuthenticateAsync`):**
  1. Read `session_id` cookie, parse as `Guid` → else `NoResult`.
  2. Look up `MemberLoginSessions` by `SessionId`; missing → `Fail`.
  3. Expiry: `LoginDateTime + 8h < UtcNow` → `Fail` (sliding/absolute 8-hour window; not refreshed).
  4. Load `MemberUser` by `session.MemberId`; if missing or `UserStatusType != Active` → `Fail` (live status re-check every request).
  5. Issue claims: `NameIdentifier = MemberId`, `Name = UserName`, **`MemberType` = enum string**.
- **Resolving current member in a controller:** via `ClaimsPrincipal` claims — `MemberId` from `ClaimTypes.NameIdentifier`, member type from the custom `"MemberType"` claim. The `/api/auth/me` endpoint demonstrates the pattern (`principal.FindFirstValue(ClaimTypes.NameIdentifier)` + EF lookup). **There is no shared helper / `ControllerBase` extension / `ICurrentUser` abstraction** — and the stub `MemberController`/`CompanyController` don't read the claim at all yet.
- **Authorization policies (`Program.cs`):**
  - `"MemberOnly"` → `RequireClaim("MemberType", "Member")`
  - `"MasterOnly"` → `RequireClaim("MemberType", "CompanyMasterMember")`
  - Applied via `[Authorize(Policy = "MemberOnly")]` on `MemberController` and `[Authorize(Policy = "MasterOnly")]` on `CompanyController`. Note the enum has more member types than these two literals cover (see §6/gaps).
- **CORS:** `AllowVite` policy allows `http://localhost:5173` with credentials.

---

## 6. Data — entity / configuration / migration / seed status

All entities below have an EF Core entity class, a Fluent `IEntityTypeConfiguration`, and a table created by migration `20261026075640_InitialSchema`. The second migration (`20261103051539`) only adds a `check_tin_signup_status` SQL function. **Dev seed** comes solely from `AuthSeedData.SeedAsync` (runs in Development), which seeds only auth/company bootstrap rows.

| Entity | Entity class | EF config | Migration (table) | Dev seed |
|--------|:---:|:---:|:---:|:---:|
| Member | ✅ | ✅ | ✅ `members` | ✅ (3 members) |
| MemberUser | ✅ | ✅ | ✅ `member_users` | ✅ (2 login users) |
| Company | ✅ | ✅ | ✅ `companies` | ✅ (2 companies) |
| CompanyMemberMapping | ✅ | ✅ | ✅ `company_member_mappings` | ✅ (2 mappings) |
| MemberSubscription | ✅ | ✅ | ✅ `member_subscriptions` | ❌ |
| Subscription | ✅ | ✅ | ✅ `subscriptions` | ❌ |
| SubscriptionRequest | ✅ | ✅ | ✅ `subscription_requests` | ❌ |
| MemberNotification | ✅ | ✅ | ✅ `member_notifications` | ❌ |
| CusDec | ✅ | ✅ | ✅ `cus_decs` | ❌ |
| CusDecItem | ✅ | ✅ | ✅ `cus_dec_items` | ❌ |
| CusDecContainer | ✅ | ✅ | ✅ `cus_dec_containers` | ❌ |
| CusDecStatus | ✅ | ✅ | ✅ `cus_dec_statuses` | ❌ |
| CusDecStatusMapping | ✅ | ✅ | ✅ `cus_dec_status_mappings` | ❌ |
| CusDecContainerStatus | ✅ | ✅ | ✅ `cus_dec_container_statuses` | ❌ |
| **ContainerStatusMapping** | ❌ | ❌ | ❌ | ❌ |
| CusDecPaymentSummary | ✅ | ✅ | ✅ `cus_dec_payment_summaries` | ❌ |
| CustomerSupportTicket | ✅ | ✅ | ✅ `customer_support_tickets` | ❌ |

**Notes**
- **`ContainerStatusMapping` does not exist** under that name anywhere (no entity, config, or table). The closest existing artifacts are `CusDecContainerStatus` (`cus_dec_container_statuses`) and `CusDecStatusMapping` (`cus_dec_status_mappings`) — if a distinct container-status-mapping table is intended, it must be created.
- Seed detail (`AuthSeedData.cs`): Company A (`TIN 100000001`) + master login (`master-a@test.cdns`), an existing plain member (`existingmember-a@test.cdns`), and Company B (`TIN 100000002`) + inactive master — all with password `Test@12345`. **No subscription, CusDec, notification, payment, or support-ticket rows are seeded.**
- Related entities also present but outside the requested list: `CompanyStatusDetail`, `MemberSubscriptionStatusHistory`, `TemporaryMember`, plus the full CusDec inspection/tax family and admin-portal tables.

---

## 7. Existing service / repository classes

**No repository classes exist** (no `*Repository` types; data access is direct `CdnsDbContext` / EF Core). There is **no subscription, request, CusDec, or notification service.** The only service classes are auth/signup:

| Class | Location | Responsibility |
|-------|----------|----------------|
| `LoginService` | `Infrastructure/Services/Auth/` | Credential check, lockout, session issue |
| `MasterActivationService` | `Infrastructure/Services/Auth/` | Master profile + password activation |
| `PasswordResetService` | `Infrastructure/Services/Auth/` | Forgot/verify/reset OTP flow |
| `MemberLoginSessionIssuer` | `Infrastructure/Services/Auth/` | Creates `MemberLoginSession` + cookie (`IAuthResultIssuer`) |
| `SignupService` | `Infrastructure/Services/` | TIN verify, register, OTP, set password |
| `FakeEregService` | `Infrastructure/ExternalServices/` | Stub eReg verification (dev) |
| `LoggingEmailService` | `Infrastructure/Notifications/` | Logs emails instead of sending |

**Partial, relevant subscription/CusDec logic exists only as stubs inside `MasterActivationService`:**
- `SetupDefaultSubscription(memberId, companyId)` — **stub**; comments state it can't run because no `Subscription` seed rows exist. Intended to create a `SubscriptionRequest` (Approved) + `MemberSubscription` (Active).
- `ClaimCusDecsForMaster(memberId, companyId)` — queries `CusDecs` for declarant/consignee counts and logs them (claim wiring incomplete).

---

## Gaps — what's missing before a Member dashboard shows real data

1. **`/api/member/dashboard-summary` is a hard-coded stub.** It returns `0`/empty and never touches the DB or reads the current member. Needs a real implementation that reads `NameIdentifier` and queries by member id.
2. **No current-user accessor for controllers.** Member id/type are only reachable via raw `ClaimsPrincipal` claims; the stub controllers don't use them. A shared helper/`ICurrentUser` would be needed (and `MemberController` must actually scope queries to the logged-in member).
3. **No domain/service layer for the dashboard's data.** No subscription, subscription-request, CusDec, or notification service or repository exists — all would compute the four dashboard tiles (active subscriptions, claimed CusDecs, unread notifications, pending requests) plus recent activity.
4. **No seed data for any of the dashboard's entities.** `MemberSubscription`, `Subscription`, `SubscriptionRequest`, `MemberNotification`, `CusDec*` tables are all empty in dev — even a correct query returns zeros. In particular `Subscription` rows must be seeded before `MemberSubscription`/`SetupDefaultSubscription` can work (the existing stub says so explicitly).
5. **CusDec → member ownership linkage is unproven.** `ClaimCusDecsForMaster` counts CusDecs but the claim/assignment path is a stub; how a CusDec is tied to a given member ("claimed CusDecs") isn't implemented or seeded.
6. **`ContainerStatusMapping` entity/table does not exist** — if the Member portal's CusDec/container status display depends on it, it must be modeled, configured, and migrated.
7. **Frontend has no 401/session-expiry handling on data calls.** Dashboards swallow errors into a zeroed view rather than redirecting to login; once real data exists, an expired 8-hour session would silently show empty tiles instead of re-authenticating.
8. **Authorization policies cover only two member-type literals** (`Member`, `CompanyMasterMember`). Any other `MemberType` value would be locked out of `/api/member/*` — worth confirming against the real member-type set before go-live.
