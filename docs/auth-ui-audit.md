# Auth UI Audit — feature/auth-ui-redesign

**Date:** 2026-11-16  
**Branch:** feature/auth-ui-redesign  
**Auditor:** Claude Code (read-only, no source files modified)

---

## 1. Summary

| Section | Result |
|---|---|
| Build | **PASS** — clean production build, one chunk-size warning (pre-existing) |
| Lint | **WARNINGS** — 4 auth-specific unused-var warnings in `SignupPage.jsx`; `react/static-components`; all pre-existing `set-state-in-effect` warnings are outside auth |
| Sign in — empty field validation | **FAIL** — silent no-op, no inline error |
| Sign in — login flow & overlay | Partial — overlay/redirect work; network errors misrouted to "wrong credentials" banner |
| Sign in — eye toggle, disabled button | PASS |
| Sign in — master activation wiring | PASS |
| Forgot / Reset password | Partial — flow works; minor double-error display for empty username |
| Signup step 1: TIN | **FAIL** — warning banners unreachable (API contract mismatch) |
| Signup `accountType` gap | **GAP** — dropdown shown and enabled; value is never sent to backend |
| Signup step 2: details | PASS for validation and payload; email-dupe error not highlighted on field |
| Signup step 3: verify email | Partial — OTP validates; resend is spammable |
| Signup step 4: set password | **FAIL** — SUCCESS screen likely unreachable due to `RedirectIfLoggedIn` redirecting away before it renders |
| Step indicator sync | PASS for TIN→PASSWORD; FAIL on Done (stepper hidden, spec says show all done) |
| Design match / tokens | Mostly correct; 5 SVG assets unused; double smoke; `cd-bob` keyframe conflict |
| Accessibility | Mostly PASS; `cd-shake` not stopped in reduced-motion; spinner not stopped; minor `aria-*` gaps |
| Responsive | Partial — 768–960 px range may appear cramped |
| **CSS class collision (AuthLayout / AuthShell)** | **FAIL** — same class names in both CSS files; MasterActivation page likely broken |

---

## 2. Issues Table

| # | Severity | Area | What's wrong | File:line | Suggested fix |
|---|---|---|---|---|---|
| 1 | **Blocker** | Sign in | Empty username or password silently returns — no inline error shown, no field highlight, no banner | `LoginPage.jsx:159–162` | Add `setLoginError('empty-username')` / `'empty-password'` guards and render corresponding `<p role="alert">` under each input |
| 2 | **Blocker** | Signup TIN | `VerifyTinResponse` has no `Code` property — backend returns `{ statusCode, message }` but frontend reads `err.code` which is always `undefined`. All TIN warning banners are unreachable; every TIN failure shows the generic "valid 9-digit" field error | `VerifyTinResponse.cs:5–7`, `SignupPage.jsx:244–253` | Add `public string? Code { get; set; }` to `VerifyTinResponse`; set it from `TinStatusToError()` in `SignupService.cs` |
| 3 | **Blocker** | Sign in | A network error (offline) or 500 response sets `loginError = 'invalid'` → shows "Incorrect username or password" banner. Users are told their credentials are wrong when the problem is a server fault | `LoginPage.jsx:179–182` | Add a third error variant (e.g. `'network'`), detect `err.status >= 500 || !err.status`, show neutral "Something went wrong. Please try again." banner |
| 4 | **Major** | CSS | `AuthLayout.css` and `AuthShell.css` both declare `.auth-shell`, `.auth-left`, `.auth-right` with different values. Both files are bundled (MasterActivation still uses `AuthLayout`). Whichever loads last wins for all pages — MasterActivation almost certainly renders with the wrong layout | `AuthLayout.css:4,15,38`, `AuthShell.css:5,13,100` | Rename all selectors in one file, or migrate MasterActivationPage to `AuthShell`, or scope `AuthLayout.css` selectors under a wrapper class (e.g., `.auth-layout-legacy`) |
| 5 | **Major** | Signup done | The SUCCESS screen is likely never shown. `handleSetPassword` calls `await refreshUser()` which sets the user in AuthContext, then immediately calls `setStep('SUCCESS')`. Because the signup route is wrapped in `<RedirectIfLoggedIn>`, the moment the auth context holds a user, React renders `<Navigate to="dashboard">` instead of `<SignupPage>` — skipping the success screen | `SignupPage.jsx:349–350`, `App.jsx:87–89` | Move `refreshUser()` out of `handleSetPassword` (call it only when "Continue to sign in" is clicked), OR remove `RedirectIfLoggedIn` from the `/signup` route guard |
| 6 | **Major** | CSS animation | `@keyframes cd-bob` is defined twice with different values: HarbourScene.css (with ±0.6° rotation) and LoginPage.css (plain vertical only). Whichever is later in the Vite bundle wins globally. The overlay ship may rotate unintentionally or the harbour scene ship may lose its rotation | `HarbourScene.css:11–14`, `LoginPage.css:84–87` | Rename one: use `cd-bob-load` for the overlay ship and update the class in LoginPage.jsx/css |
| 7 | **Major** | Signup DETAILS | The Account Type `<select>` is fully enabled and shows two options, but `accountType` is never sent to `POST /api/signup/register` and `RegisterRequest.cs` has no such field. The user's selection is silently dropped. See §3 for the backend account-type question | `SignupPage.jsx:607–617`, `RegisterRequest.cs:1–13` | Hide the select (or disable it with a tooltip) until a backend field is added; see §3 |
| 8 | **Major** | HarbourScene | Double smoke: `ship.svg` contains a static `<g id="ship-smoke">` with three circles visible in the `<image>` render. `HarbourScene.jsx` also adds two animated `.cd-smoke` circles at scene coords (~162, 225) — which overlap the static smoke at the same location. Both show simultaneously | `HarbourScene.jsx:66–67`, `ship.svg` (ship-smoke group) | Use only the animated circles (remove static smoke from ship.svg), or suppress the JSX circles and rely on the ship's built-in smoke |
| 9 | **Minor** | Assets | `sun.svg`, `cloud.svg`, `gulls.svg`, `shimmer.svg`, `wave.svg` are unimported. `HarbourScene.jsx` renders all five scene parts as inline SVG. The reference files add ~3 KB to the repo for no runtime benefit | `HarbourScene.jsx:1–97` | Delete the five unused files, or import and use them (see §6 follow-ups) |
| 10 | **Minor** | Forgot password | When `forgotUsername` is empty, `forgotError = 'Enter your username.'` triggers both the top Banner *and* a `<p role="alert">` below the input. Screen readers announce the error twice | `LoginPage.jsx:388–414` | Remove the `{forgotError === 'Enter your username.' && ...}` inline error; the Banner alone is sufficient |
| 11 | **Minor** | Signup step indicator | On the SUCCESS screen, the spec says show all 4 stepper items as done. The code hides the stepper entirely via `showStepper = step !== 'SUCCESS'` | `SignupPage.jsx:362`, `Signup.css` | Render `<SignupStepper step="DONE" />` on the SUCCESS screen and add an `active = DONE` mapping that marks all 4 as done |
| 12 | **Minor** | Signup lint | `RightHeader` function component is defined inside the render body of `Signup`. React re-creates it every render and resets its subtree state. Lint: `react/static-components` | `SignupPage.jsx:376–401` | Lift `RightHeader` above the `Signup` function; pass `step` as a prop |
| 13 | **Minor** | Signup lint | 4 unused declarations: `ArrowRight` (import), `navigate` (useNavigate), `signupPortal` (useState), `field` (variable in register error handler) | `SignupPage.jsx:4, 158, 189, 290` | Remove all four |
| 14 | **Minor** | Accessibility | `@keyframes cd-shake` / `.auth-banner.cd-shake` is not suppressed in the `prefers-reduced-motion` block of `AuthShell.css`. Banners shake for users who prefer reduced motion | `AuthShell.css:91–98` (no `.cd-shake` there), `AuthShell.css:367–369` | Add `.auth-banner.cd-shake { animation: none; }` inside the existing `@media (prefers-reduced-motion: reduce)` block |
| 15 | **Minor** | Accessibility | `.auth-btn-spinner` animation (`auth-spin`) has no `prefers-reduced-motion` override. The submit-button spinner keeps spinning for reduced-motion users | `AuthShell.css:310–318` | Add `@media (prefers-reduced-motion: reduce) { .auth-btn-spinner { animation: none; border: 2px solid rgba(255,255,255,0.7); } }` |
| 16 | **Minor** | Signup password | Confirm-password `auth-pw-wrap` has no eye-toggle button, but the class adds 52 px right padding to the input, leaving an empty gap. The password-field toggle simultaneously reveals/hides both inputs, which is non-standard | `SignupPage.jsx:759–770` | Add a separate eye toggle for the confirm field (using `showConfirmPassword` already tracked), or drop `auth-pw-wrap` on the confirm field and rely on a plain input |
| 17 | **Minor** | Accessibility | Signup OTP input (step 3) has no `aria-invalid` attribute | `SignupPage.jsx:671–682` | Add `aria-invalid={!!serverErr}` |
| 18 | **Minor** | Accessibility | `StepProgressBar` uses `aria-valuemin={1}`. ARIA spec recommends `0` as the minimum for progressbars | `SignupPage.jsx:65` | Change to `aria-valuemin={0}` |
| 19 | **Minor** | Accessibility | Login username and password inputs have no `aria-invalid` attribute. When `loginError` is set, the fields are highlighted only visually | `LoginPage.jsx:308–316`, `327–336` | Add `aria-invalid={!!loginError}` to both inputs |
| 20 | **Minor** | Signup OTP | Resend OTP has no client-side cooldown. The button has no `disabled` state and clicking it rapidly fires `POST /api/signup/register` on each click | `SignupPage.jsx:317–327` | Disable the resend button for the full 4-second period (use `resendLabel === 'sent'` as the guard) |
| 21 | **Minor** | Responsive | Flex-wrap collapses the two panels once the viewport is narrower than ~960 px (520 + 440 flex-basis). The responsive CSS adjustments only apply at `max-width: 768px`. The 769–960 px range gets neither a full side-by-side layout nor the mobile adjustments | `AuthShell.css:450–468` | Add a `@media (max-width: 960px)` block with intermediate adjustments, or lower the flex-basis values |
| 22 | **Polish** | CSS | Class `cd-load-bar` on the progress-bar `<div>` has no matching CSS selector — the animation is applied via `.login-overlay-bar`. Dead class | `LoginPage.jsx:89`, `LoginPage.css:62–68` | Remove `cd-load-bar` from the JSX className |
| 23 | **Polish** | Sign in overlay | The loading overlay appears only after both `POST /api/auth/login` *and* `GET /api/auth/me` have resolved. The first stage text "Checking your credentials…" is displayed post-login, which is semantically wrong | `LoginPage.jsx:174–177` | Show the overlay immediately after the login API succeeds; call `refreshUser()` asynchronously during the stage cycle |
| 24 | **Polish** | HarbourScene | `<image href={shipSvg}>` and `<image href={dockSvg}>` have no `preserveAspectRatio` attribute. SVG `<image>` defaults to `xMidYMid meet`; minor pillarboxing/letterboxing possible since aspect ratios differ slightly (ship 2.529:1 vs box 2.520:1; dock 1.941:1 vs box 1.908:1) | `HarbourScene.jsx:69, 74` | Add `preserveAspectRatio="xMidYMid meet"` to make intent explicit |
| 25 | **Polish** | Sign in overlay | Ship `<img>` in overlay uses `objectFit: 'contain'` within a 64×40 box. Ship.svg aspect ratio is 860:340 (2.529:1) vs 64:40 (1.6:1). With `contain`, the rendered ship is ~25 px tall, not 40 px — it doesn't fill the lane height | `LoginPage.jsx:59–63` | Use `objectFit: 'fill'` (allow minor stretch) or size the container to match the ship aspect (64×25) and adjust lane layout |
| 26 | **Polish** | Signup register error | Duplicate-email errors (`AccountExists`, `EmailBelongsToMaster`) show in the top banner but do not highlight the email input field (`getFieldError('email')` returns null because `serverErr.field` is null — the backend sends no `field` property) | `SignupPage.jsx:287–295`, `SignupController.cs:40–43` | Either add `"field": "email"` to the backend conflict responses or handle these codes explicitly in `getFieldError` |
| 27 | **Polish** | Sign in overlay | 5 `setTimeout` calls in the success path are not stored and not cancelled. Navigating away (e.g., browser back) before the timers expire could fire `setOverlay` on an unmounted component (React 18 suppresses the error but wastes cycles) | `LoginPage.jsx:175–179` | Store timer IDs and clear them in a `useEffect` cleanup |

---

## 3. Gaps

### G1 — `accountType` shown but not sent (design ↔ API mismatch)

The DETAILS step shows an **enabled** `<select>` with two options:
- "CHA Agent (Declarant)"
- "Importer (Consignee)"

`RegisterRequest.cs` has no `AccountType` field. The user's selection is silently ignored.

**How account type is decided today:** not by the frontend or the signup flow. It is presumably derived by the backend from eReg data or the company TIN, or a default is assigned at the time of approval. The frontend dropdown has no effect.

**Recommendation:** Mark the select as `disabled` with a tooltip like "Account type is determined by your company registration." and wire a backend `AccountType` field when ready. Do not remove it entirely — it communicates the concept to the user. Do not leave it enabled since the selection misleads users into believing their choice matters.

### G2 — Resend OTP endpoint reuses `/api/signup/register`

The "Resend code" button calls `POST /api/signup/register` again with the same payload (re-registering the user) rather than a dedicated `/api/signup/resend-otp` endpoint. This relies on the backend being idempotent for already-registered-but-unverified users. If the backend is not idempotent, it could create duplicate records or return errors that are silently swallowed (`catch { /* ignore */ }`).

Confirm backend behaviour, then either add a dedicated resend endpoint or document the idempotency guarantee.

### G3 — No `code` field in `VerifyTinResponse` (backend gap, see Issue #2)

This blocks all TIN-specific error messaging in the UI. The `VerifyTinResponse` model needs a `Code` property mirroring the `(string Code, string Message)` tuple in `TinStatusToError()`.

---

## 4. Manual Browser Checklist

Perform these checks in a Chromium-based browser with the network tab open and the dev tools console visible. Run the dev server with `npm run dev` in the `frontend/` directory.

---

### A — Wrong password

1. Navigate to `/login`.
2. Enter a valid username and type a deliberately wrong password.
3. Click **Sign in**.
4. **Expected:** red "Incorrect username or password" banner with shake animation; password field cleared; button re-enabled.
5. Enter correct password and click **Sign in** again — banner should clear on edit and re-appear if wrong again.

---

### B — Locked account (5 failures)

1. Navigate to `/login`.
2. Enter a valid username and incorrect password **5 times** in a row.
3. **Expected on 5th attempt:** amber "Account locked. Too many failed attempts. Try again in 20 minutes, or reset your password." banner — **not** the red wrong-credentials banner, **not** raw "HTTP 401".
4. Inspect the network tab: the 5th response should be `401` with `{ "code": "AccountLocked" }`.

---

### C — Successful Member login

1. Navigate to `/login`.
2. Enter valid Member credentials.
3. Click **Sign in**.
4. **Expected:** button shows spinner; loading overlay appears (ship sailing lane, progress bar); text cycles: "Checking your credentials…" → "Clearing Customs…" → "Opening your portal…"; then arrival circle + "Welcome back" → navigates to `/member/dashboard` without a full-page reload.
5. Confirm URL ends with `/member/dashboard`.

---

### D — Successful Master login

1. Log out (or use a fresh incognito window).
2. Navigate to `/login`; enter valid Master credentials.
3. **Expected:** same overlay sequence; navigates to `/master/dashboard` (or `/company/dashboard` per portal name).

---

### E — Forgot → Reset → Sign in with new password

1. On the login page, click **Forgot password?**.
2. **Expected:** "Reset your password" view; username field pre-filled with what was entered on login.
3. Submit with the username empty — **expected:** banner "Could not send code — Enter your username." (note Issue #10: also shows inline field error).
4. Enter a valid username and click **Send reset code**.
5. **Expected:** transitions to "Check your email" view.
6. Enter the code from the email (digits only, max 6).
7. Enter a new password meeting the rules; click **Reset password**.
8. **Expected:** "Password updated" view.
9. Click **Back to sign in** — **expected:** returns to login view, all fields cleared.
10. Sign in with the new password — **expected:** successful login overlay.

---

### F — Full sign-up flow

1. Navigate to `/signup`.
2. **Step 1 TIN:** Enter a 9-digit TIN for a registered and Master-activated company. Click **Verify TIN**.
   - **Expected:** advances to step 2.
   - **Regression (Issue #2):** Try a TIN for an unregistered company — currently shows generic "Please enter a valid 9-digit company TIN." instead of the amber "Organisation not registered yet" banner.
3. **Step 2 Details:** Fill all fields. For NIC, test `123456789V` (9+V) and `123456789012` (12 digits). Check the terms checkbox. Note the Account Type select (Issue #7: selection is ignored).
4. Click **Send verification code** — **expected:** advances to step 3.
5. **Step 3 OTP:** Enter the 6-digit code from the email. Click **Verify email**.
   - Test wrong code — **expected:** error banner "Invalid or expired code."
   - Test "Resend code" — **expected:** changes to "Code sent again" for ~4 s.
6. **Step 4 Password:** Watch the live password-rules card update as you type. Enter a matching confirm password.
   - Test mismatch confirm — **expected:** inline "The two passwords do not match." error.
7. Click **Create account**.
   - **Expected (spec):** "Your account is ready" success screen with first name, then "Continue to sign in" navigates to dashboard.
   - **Likely actual (Issue #5):** screen is bypassed; browser navigates directly to `/{portal}/dashboard`.

---

### G — Back on each step

1. Start the signup flow and reach Step 2 (Details).
2. Click **Back** — **expected:** returns to Step 1 (TIN); TIN value preserved; progress bar resets to 1 segment; stepper moves active ring back to step 1; "Step 1 of 4" label shown; Back button hidden.
3. Re-verify TIN, fill Details, reach Step 3 (OTP).
4. Click **Back** — **expected:** returns to Step 2; all previously-entered Detail field values are preserved.
5. Proceed to Step 4. Click **Back** — **expected:** returns to Step 3; OTP input still has whatever was entered.
6. Confirm: Back button is never shown on Step 1 or the Done screen.

---

### H — Browser refresh mid-signup

1. Start signup and complete Step 1 (TIN verified).
2. Press **F5** (refresh).
3. **Expected:** returns to Step 1 (TIN) with an empty form — no session persistence; user must restart.

---

### I — TIN error cases

| TIN value | Expected response | Expected UI today (before fix) |
|---|---|---|
| Non-9-digit (e.g. "123") | Frontend field error before API call | "Enter the 9-digit company TIN." |
| 9-digit but company not in CDNS | Backend `OrganizationNotRegistered` | Generic field error (Issue #2) |
| 9-digit, company not Master-activated | Backend `CompanyNotActivated` | Generic field error (Issue #2) |
| 9-digit, pending eReg verification | Backend `CompanyPendingEregVerification` | Generic field error (Issue #2) |
| 9-digit, setup incomplete | Backend `CompanySetupIncomplete` | Generic field error (Issue #2) |

---

### J — Narrow window (responsive)

1. Open DevTools → set viewport to **700 px wide**.
2. Navigate to `/login`.
3. **Expected:** single-column layout; left panel stacks above right panel; no horizontal overflow; harbour scene visible above the form.
4. Set viewport to **860 px wide** (between 768 and 960 px).
5. **Expected (possible issue #21):** panels may appear side by side but cramped, or may have already wrapped; check for overflow or clipped text.

---

### K — Reduced motion

1. In macOS: System Settings → Accessibility → Display → **Reduce Motion ON**.
2. Navigate to `/login`.
3. **Expected:** harbour scene is completely static (no sailing, no bobbing, no waves, no cloud drift, no shimmer); entrance animations still play (they are `motion/react` transitions, not CSS animations); banners do **not** shake (Issue #14: currently they still shake).
4. Navigate to `/signup`.
5. **Expected:** stepper pulse ring stopped; progress bar fill animation stopped; password-rule check-draw animation stopped.
6. Trigger a login error — **expected:** banner should not shake (currently it does, Issue #14).

---

## 5. Compliance Statement

- No source files, CSS files, migration files, or seed data files were created, modified, or deleted.
- No packages were installed.
- No database commands were run.
- No browser automation tools (Playwright, Puppeteer, Chrome for Testing) were installed or invoked.
- No commits were made and no branch was switched.
- Commands run (read-only): `ls`, `find`, `grep`, `cat` (bash), `npm run build`, `npm run lint` in `frontend/`.
- The only file written is this report at `docs/auth-ui-audit.md`.
