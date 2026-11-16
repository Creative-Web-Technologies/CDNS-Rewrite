# Auth UI Fixes + Shared Harbour Scene — Implementation Report

**Date:** 2026-11-16  
**Branch:** feature/auth-ui-redesign  
**Build:** ✓ clean (`npm run build`)  
**Lint:** ✓ no new warnings in touched files (one pre-existing `set-state-in-effect` in Banner component, unchanged)

---

## 1. Files changed, created and deleted

### Backend
| Action | File |
|---|---|
| Modified | `backend/src/Cdns.Application/Feature/SignUp/VerifyTinResponse.cs` |
| Modified | `backend/src/Cdns.Infrastructure/Services/SignupService.cs` |

### Frontend — Phase 1 (auth fixes)
| Action | File |
|---|---|
| Modified | `frontend/vite.config.js` |
| Modified | `frontend/package.json` (vite-plugin-svgr added to devDependencies) |
| Deleted | `frontend/src/pages/auth/AuthLayout.jsx` |
| Deleted | `frontend/src/pages/auth/AuthLayout.css` |
| Modified | `frontend/src/pages/auth/AuthShell.css` |
| Modified | `frontend/src/pages/auth/HarbourScene.jsx` |
| Modified | `frontend/src/pages/auth/HarbourScene.css` |
| Modified | `frontend/src/pages/auth/Login/LoginPage.jsx` |
| Modified | `frontend/src/pages/auth/Login/LoginPage.css` |
| Modified | `frontend/src/pages/auth/Signup/SignupPage.jsx` |
| Modified | `frontend/src/pages/auth/Signup/Signup.css` |
| Modified | `frontend/src/pages/auth/MasterActivation/MasterActivationPage.jsx` |
| Modified | `frontend/src/pages/auth/OtpVerification/OtpVerificationPage.jsx` |

### Frontend — Phase 2 (shared harbour scene)
| Action | File |
|---|---|
| Created | `frontend/src/components/scene/scene.css` |
| Created | `frontend/src/components/scene/Sun.jsx` |
| Created | `frontend/src/components/scene/Cloud.jsx` |
| Created | `frontend/src/components/scene/Gulls.jsx` |
| Created | `frontend/src/components/scene/Shimmer.jsx` |
| Created | `frontend/src/components/scene/SeaWaves.jsx` |
| Created | `frontend/src/components/scene/Ship.jsx` |
| Created | `frontend/src/components/scene/Dock.jsx` |
| Modified | `frontend/src/components/dashboard/BannerScene.jsx` |
| Modified | `frontend/src/assets/svg/wave.svg` (fill → currentColor) |
| Modified | `frontend/src/assets/svg/sun.svg` (added cd-glare/cd-glare-b/cd-rays class attrs) |

---

## 2. Step 2.0 Inventory — Before and After

### Before
| SVG file | Auth HarbourScene | Dashboard BannerScene |
|---|---|---|
| `ship.svg` | `<image href={shipSvg}>` ✓ | ✗ Inline `ShipBody` function |
| `dock.svg` | `<image href={dockSvg}>` ✓ | ✗ Inline `DockArt` function |
| `sun.svg` | ✗ Unused reference file | ✗ Not used |
| `cloud.svg` | ✗ Unused reference file | ✗ Not used |
| `gulls.svg` | ✗ Unused reference file | ✗ Not used |
| `shimmer.svg` | ✗ Unused reference file | ✗ Not used |
| `wave.svg` | ✗ Unused reference file | ✗ Not used |

### After — every file imported from `src/assets/svg/`
| SVG file | Auth HarbourScene | Dashboard BannerScene |
|---|---|---|
| `ship.svg` | `Ship.jsx` via `?react` ✓ | `Ship.jsx` via `?react` ✓ |
| `dock.svg` | `Dock.jsx` via `?react` ✓ | `Dock.jsx` via `?react` ✓ |
| `sun.svg` | `Sun.jsx` via `?react` ✓ | (auth-only sky element) |
| `cloud.svg` | `Cloud.jsx` via `?react` ✓ | (auth-only sky element) |
| `gulls.svg` | `Gulls.jsx` via `?react` ✓ | (auth-only sky element) |
| `shimmer.svg` | `Shimmer.jsx` via `?react` ✓ | (auth-only water element) |
| `wave.svg` | `SeaWaves` via `?react` ✓ | `SeaWaves` via `?react` ✓ |

**Zero hard-coded SVG artwork remains in either component.** The only inline SVG remaining in BannerScene is the wake-trail lines (3 `<path>` strokes), which are a layout effect, not artwork.

---

## 3. How each audit item was fixed

| # | Severity | Fixed in | Notes |
|---|---|---|---|
| B1 | Blocker | `LoginPage.jsx:159–165` | Empty username/password now shows inline `<p role="alert">` and sets `aria-invalid`; form returns without API call |
| B2 | Blocker | `VerifyTinResponse.cs:5`, `SignupService.cs:58`, `SignupPage.jsx:244–253` | Backend adds `Code` property; service sets it from `TinStatusToError()`; frontend reads `err.code` and maps to amber banners |
| B3 | Blocker | `LoginPage.jsx:179–185`, `SignupPage.jsx` (each step catch) | `!err.status \|\| err.status >= 500` branch shows neutral "Something went wrong" banner; password NOT cleared on network errors |
| M1 | Major | `MasterActivationPage.jsx`, `OtpVerificationPage.jsx` migrated to `AuthShell`; `AuthLayout.jsx` and `AuthLayout.css` deleted | CSS collision eliminated; both pages now use `auth-*` CSS classes |
| M2 | Major | `SignupPage.jsx:329–358`, `handleContinue()` | `refreshUser()` removed from `handleSetPassword`; SUCCESS screen now shows; button "Continue to your dashboard" calls `refreshUser()` then navigates |
| M3 | Major | `LoginPage.css`: `cd-bob-load` keyframe; `scene.css`: shared `cd-bob` | Overlay ship now uses `cd-bob-load` (plain vertical, no rotation); scene ship uses `cd-bob` (with rotation); no keyframe name conflict |
| M4 | Major | `SignupPage.jsx:280–281` | Account type `<select>` removed; `// TODO(accountType)` comment points to audit doc |
| M5 | Major | `HarbourScene.jsx` | Extra animated smoke circles removed; ship.svg's own `#ship-smoke` group is animated via CSS in scene.css |
| #10 | Minor | `LoginPage.jsx:388–414` | `forgotError` banner only shown for API-level errors; empty-field case uses inline field error only (no duplicate) |
| #11 | Minor | `SignupPage.jsx:35–55` (SignupStepper), `Signup.css` | On SUCCESS, `active=5` makes all 4 steps show as done; stepper visible on SUCCESS screen |
| #12 | Minor | `SignupPage.jsx` | `StepHeader` lifted outside render body |
| #13 | Minor | `SignupPage.jsx` | Removed `ArrowRight` import, `navigate`, `signupPortal`, `field` |
| #14 | Minor | `AuthShell.css` (reduced-motion block) | `.auth-banner.cd-shake { animation: none; }` added |
| #15 | Minor | `AuthShell.css` (reduced-motion block) | `.auth-btn-spinner { animation: none; opacity: 0.7; }` added |
| #16 | Minor | `SignupPage.jsx:759–770` | Confirm password field has its own independent eye toggle |
| #17 | Minor | `SignupPage.jsx:671` | OTP input has `aria-invalid={!!serverErr}` |
| #18 | Minor | `SignupPage.jsx:65` | `aria-valuemin={0}` |
| #19 | Minor | `LoginPage.jsx:308–316, 327–336` | Username/password inputs have `aria-invalid` wired to error state |
| #20 | Minor | `SignupPage.jsx` (handleResend + resendCooldown state) | 30-second countdown with visible "Resend code in 0:NN" label; button disabled during cooldown |
| #21 | Minor | `AuthShell.css` | Added `@media (max-width: 960px)` block: left panel takes full width, right panel stacks below |
| #22 | Minor | `LoginPage.jsx:89`, `LoginPage.css` | Dead `cd-load-bar` class removed from JSX; `cd-load-bar` renamed to match the bar element's own CSS selector |
| #23 | Minor | `LoginPage.jsx:174–193` | Overlay shown immediately after login API succeeds; `refreshUser()` runs in parallel via `Promise.all` with a 2600ms minimum; navigate only after both resolve |
| #24 | Minor | `HarbourScene.jsx` | Ship and dock use `<g transform="... scale(...)"><Ship/></g>` (transform approach inherently correct); `preserveAspectRatio` is now a non-issue for the `<g>` approach |
| #25 | Minor | `LoginPage.css` `.login-overlay-ship` | Container sized 64×25 (ship aspect 860:340 ≈ 2.53:1); `objectFit: fill` |
| #26 | Minor | `SignupPage.jsx:290` | `AccountExists` / `EmailBelongsToMaster` codes set `field: 'email'`; email input turns red |
| #27 | Minor | `LoginPage.jsx:134–142` | Timer IDs stored in `timerRefs`; cleanup on unmount via `useEffect` return |
| G2 resend | — | `SignupPage.jsx:handleResend` | Resend now updates `tempToken` from response (critical bug fix — see §4) |

---

## 4. G2 — Resend OTP idempotency finding

**Is re-calling `POST /api/signup/register` idempotent for an unverified user?**

**Yes, with one significant caveat.**

The backend `RegisterAsync` (SignupService.cs:130–140) explicitly handles the resend case:

> "Soft-delete stale TemporaryMember rows so only the newest OTP/GlobalIdentity is live"

For a user who is at step 3 (OTP) and clicks "Resend code":
1. The backend soft-deletes the existing unverified `TemporaryMember` row for that email
2. A new `TemporaryMember` is created with a fresh OTP and a new `GlobalIdentity` (the `tempSignupToken`)
3. The response returns the new `tempSignupToken`

**The critical bug (now fixed):** the original code ignored the response from resend:
```js
// Before — tempToken never updated:
try {
  await apiPost('/api/signup/register', { ... });
} catch { /* ignore */ }
```

After resend, the new `TemporaryMember` has a **different** `tempSignupToken`. The user then submits the new OTP with the **old** token. The old `TemporaryMember` is soft-deleted (marked `IsDeleted=true`), so `VerifyOtpAsync` can't find it → "Invalid or expired code." every time.

**The fix** (SignupPage.jsx `handleResend`): capture the new `tempSignupToken` and update `tempToken` state:
```js
const data = await apiPost('/api/signup/register', { ... });
if (data?.tempSignupToken) setTempToken(data.tempSignupToken);
```

**Edge cases noted:**
- **Re-calling after OTP verified:** If the user somehow calls resend after `VerifyOtpAsync` has set `OneTimePassword=0`, step 3c (email not in `member_users`) still passes, so the verified session is soft-deleted and a new OTP is issued. This is acceptable since the user is still on the OTP step.
- **Concurrent resends:** Each call soft-deletes all existing rows and creates a new one. Only the last response's token is valid. The client only uses the most recently received token (after the fix). ✓
- **No duplicate rows:** The soft-delete before insert prevents accumulation. ✓

**Conclusion:** idempotent for the UI's purpose. The only caveat was the missing token update in the frontend (now fixed).

---

## 5. Visual fixes confirmed in code

| Issue | Fix |
|---|---|
| **Left panel dark navy** | Eliminated by M1: AuthLayout.css (which had `.auth-left { background-color: #0b1e3d }`) is deleted. AuthShell.css is now the only `.auth-left` rule, using `var(--auth-panel) = #E3F2FD`. |
| **Scene boxed rectangle** | Removed the sky `<rect fill="#E3F2FD">` from HarbourScene; SVG is transparent. The panel's background shows through. `HarbourScene.css` changes the SVG to `flex: 1 1 0` so it fills remaining panel height. |
| **Dock squashed and underwater** | Repositioned: `translate(380 126) scale(0.515)` places dock quay top (dock y=296) at scene y=278 (waterline), not y=398. Building height in scene ≈ 103px, crane extends to y=147. |
| **Sign in arrow right** | Button renders label first, then `<ArrowRight>`: "Sign in →". |

---

## 6. wave.svg hard-coded fill (reported as required)

`wave.svg` had `fill="#5E97A8"` hard-coded on its path. This was changed to `fill="currentColor"` so the CSS colour classes (`.cd-wave-a path { fill: #5E97A8 }` etc. in scene.css) can recolour each of the three layers independently. The file was edited; this note serves as the report.

---

## 7. Compliance statement

- No migrations, seed data, or database files touched.
- No browser automation tools installed or invoked.
- No commits made; branch unchanged (`feature/auth-ui-redesign`).
- Backend change: exactly `VerifyTinResponse.cs` (added `Code` property) and `SignupService.cs` (set `Code` in `VerifyTinAsync`) — nothing else.
- New package: `vite-plugin-svgr@5.2.0` added to `devDependencies` only. No other packages added.
- Build passes clean. Lint produces zero new warnings in any touched file.
