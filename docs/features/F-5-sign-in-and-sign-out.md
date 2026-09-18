---
feature: F-5
epic: Foundation and identity
status: validating
board: 709
version: 1
---
# Sign-in and sign-out

## Summary
OpenIddict local server with the password and refresh token flows for the first-party Web only. Sign-in page, sign-out, refresh tokens in Redis and session revocation check. Needs /agile:screen.

## Goal
Let a verified student (and, later, curator or admin) authenticate into Simulab, keep a working session across page loads, and end that session on sign-out — the foundation every screen behind a permission depends on (F-6 onward).

## What exists
- Simulab: `User`/`AccountStatus` (F-4) — only `Active` accounts may sign in; `Pending` cannot. ASP.NET Identity core is registered (`AddIdentityCore<User>`) with the password policy and lockout (5 attempts / 15 min) already configured, but with no cookies, no sign-in manager, no OpenIddict, no Redis (`IdentityModule.cs`). `ClientRateLimiter` exists for per-client fixed windows (F-4) but is in-memory/per-process.
- Simulae (read-only reference): a full OpenIddict server (password, refresh token, authorization code + PKCE, plus custom MFA flows), token/authorize/userinfo/revoke/introspect at `/auth/...`, access token 15 min / refresh token 30 days, dev signing/encryption certificates, `Redis` refresh-token store (`identity:session:{jti}`, `identity:user-sessions:{userId}`, `identity:slot:{slotJti}`) with single-use rotation (old entry removed, not marked revoked), a per-request `RevocationCheckMiddleware` backed by `identity:revoked:{jti}` in Redis, sign-out that revokes only the current session, an admin "terminate all sessions" endpoint, and plan-dependent concurrent-session limits (Academica contract slots; B2C revokes the oldest session on new login). No rate limiting beyond Identity lockout anywhere. No "remember me". Sign-in page is a Blazor page posting to `/auth/token` directly, with a generic invalid-credentials message and specific banners for locked/blocked/email-not-verified.

## Users and use cases
- UC1 A visitor with an `Active` account signs in with email and password and reaches the app already authenticated.
- UC2 A visitor with a `Pending` account tries to sign in and is told to verify the email first, with a resend action (reusing F-4's resend).
- UC3 A signed-in user signs out; the session stops working immediately, before the access token would otherwise expire.
- UC4 A visitor who fails the password 5 times is locked out for 15 minutes and sees the remaining time; a correct password during lockout is still refused.
- UC5 A signed-in user's access token refreshes silently in the background while the refresh token is valid (30 days), without asking for the password again.
- UC6 A developer runs the app host and signs in with an account created through F-4.

## Business rules
- BR1 Sign-in is the OpenIddict password grant on a single first-party confidential client (`simulab-web`); its secret lives only on the `Api` host, never reaches the browser (Blazor Interactive Server keeps every token server-side).
- BR2 Only `Active` accounts (F-4) sign in. A `Pending` account returns `identity.email_not_verified` with a resend action. An unknown email or a wrong password for an existing one returns the same generic `identity.invalid_credentials`, so the answer never reveals whether the address exists (mirrors F-4 BR4).
- BR3 Lockout reuses the Identity configuration already set in F-4: 5 failed attempts locks the account for 15 minutes. A locked account returns `identity.account_locked` with the remaining seconds; a correct password submitted during lockout is still refused.
- BR4 Access token: 15 minutes, carries `sub` (user id), `email` and `tenant_id` (always null in v1 — no roles or permissions yet, those arrive with F-6). Refresh token: 30 days, opaque, one row per session in Redis (`identity:session:{jti}`), rotated on every use.
- BR5 Using a refresh token invalidates it immediately (the Redis entry is removed). Reusing an already-rotated or unknown refresh token returns `identity.refresh_token_invalid` — same code either way; distinguishing "expired" from "reused" is deferred (Simulae does not distinguish them either).
- BR6 Sign-out revokes only the caller's own session: its refresh token entry is removed from Redis and its access token's `jti` is added to a revocation set (`identity:revoked:{jti}`, TTL = the access token's remaining lifetime).
- BR7 Every authenticated API request checks the access token's `jti` against `identity:revoked:{jti}` in Redis; a hit returns 401 `identity.token_revoked`. This is what makes sign-out (BR6) effective before the access token's own 15-minute expiry.
- BR8 No "remember me", no concurrent-session limit, no per-device session list, and no admin "terminate all sessions" in v1 — all depend on plans or institutions, neither of which exists in Simulab yet.
- BR9 Sign-in is anonymous. Sign-out requires a signed-in caller: its own access token identifies which session to revoke.
- BR10 The app bar's user menu slot (`.app-user-menu`, empty since F-2 BR1) shows a "Sign in" link to `/sign-in` for an anonymous visitor, and a menu with the user's display name (`FullName` if set, otherwise the email) and a "Sign out" action for a signed-in one.

## Screens and API
Designed with `/agile:screen` on 2026-09-18; mockup: `docs/features/mockups/F-5-sign-in-and-sign-out.html`.

### Route `/sign-in`
Layout: `AuthLayout` (F-4), same centered card as `/sign-up`.
Fields, in tab order:

| Field | Component | Required | Notes |
|---|---|---|---|
| Email | `AppTextField` (`type=email`, `autocomplete=username`) | yes | 254 characters |
| Password | `AppPasswordField` (`autocomplete=current-password`) | yes | — |

Actions: **Sign in** (primary, full width). Footer link: **Create an account** → `/sign-up`. No "forgot password" link yet (F-7).
Navigation: success goes to `/`. A visitor who is already signed in and opens `/sign-in` directly is redirected straight to `/` (no double sign-in screen).
Permissions: anonymous only.

States:
- **ready** — normal.
- **validation error** — per field, on blur and on submit; focus moves to the first field in error.
- **submitting** — submit disabled with a progress indicator; fields read-only.
- **invalid credentials** (`identity.invalid_credentials`) — `AppAlert` error at the top, generic text, focus moved to it; the password field is cleared, the email kept.
- **email not verified** (`identity.email_not_verified`) — `AppAlert` warning with an inline **Resend the verification email** action that posts to the existing F-4 resend endpoint with the typed address, then shows the generic "a new link is on its way" text in the same alert (no navigation away from `/sign-in`, the address is already at hand).
- **account locked** (`identity.account_locked`) — `AppAlert` error with the remaining time, counting down (`aria-live="polite"`, same throttling pattern as F-4's resend cooldown); the submit button stays disabled until it reaches zero, then the alert clears on the next attempt.
- **server error** — `AppAlert` error with **Try again**.

### App bar user menu (`.app-user-menu`, F-2 BR1)
- **Anonymous**: a text link, **Sign in**, to `/sign-in`.
- **Signed in**: an icon button (initial of the display name) opens a menu showing the display name and email, with one action, **Sign out**. Choosing it calls sign-out and returns to `/`.

### Accessibility
- Same field, alert and tab-order rules as F-4's `/sign-up` (labelled inputs, `aria-describedby`, `aria-invalid`, `role="alert"` on the form alert taking focus).
- The lockout countdown and the resend confirmation are `aria-live="polite"`, updated at most once every few seconds, never on every tick.
- The user menu button has an accessible name that includes the display name; the menu is reachable and operable by keyboard, closes on Esc and on selecting an item.

### UI texts
Extends `Simulab.Identity/Resources/Identity.resx` (F-4) plus the shared layout resources.

| Key | en | pt-BR | pt-PT |
|---|---|---|---|
| `SignIn.Title` | Sign in | Entrar | Iniciar sessão |
| `SignIn.Subtitle` | Welcome back. Keep practising. | Bem-vindo de volta. Continue praticando. | Bem-vindo de volta. Continue a praticar. |
| `SignIn.Email.Label` | Email | E-mail | E-mail |
| `SignIn.Password.Label` | Password | Senha | Palavra-passe |
| `SignIn.Submit` | Sign in | Entrar | Iniciar sessão |
| `SignIn.Submitting` | Signing in... | Entrando... | A iniciar sessão... |
| `SignIn.NoAccount` | New here? {0} | Novo por aqui? {0} | Novo por aqui? {0} |
| `SignIn.CreateAccount` | Create an account | Criar uma conta | Criar uma conta |
| `identity.invalid_credentials` | Incorrect email or password. | E-mail ou senha incorretos. | E-mail ou palavra-passe incorretos. |
| `identity.email_not_verified` | Confirm your email before signing in. | Confirme seu e-mail antes de entrar. | Confirme o seu e-mail antes de iniciar sessão. |
| `identity.email_not_verified.resend` | Resend the verification email | Reenviar o e-mail de verificação | Reenviar o e-mail de verificação |
| `identity.account_locked` | Too many attempts. Try again in {0}. | Muitas tentativas. Tente de novo em {0}. | Demasiadas tentativas. Tente novamente dentro de {0}. |
| `Layout.UserMenu.SignIn` | Sign in | Entrar | Iniciar sessão |
| `Layout.UserMenu.SignOut` | Sign out | Sair | Terminar sessão |
| `Layout.UserMenu.Label` | Account menu for {0} | Menu da conta de {0} | Menu da conta de {0} |

### API
- `POST /connect/token` — OpenIddict token endpoint; `grant_type=password` for sign-in, `grant_type=refresh_token` for silent refresh (decision, question 1). 200 with the token pair; `invalid_grant` for wrong credentials (mapped to `identity.invalid_credentials`); custom errors for `identity.email_not_verified` and `identity.account_locked`.
- `POST /api/v1/identity/sign-out` — revokes the caller's current session (BR6). 204; requires a valid access token.
- `GET /api/v1/identity/session` — the caller's own `sub`, `email` and `session_jti`, read from its access token server-side (build decision: the token is encrypted, so the Web asks for these instead of decoding it). 200; requires a valid access token.
- Error codes: `identity.invalid_credentials`, `identity.email_not_verified`, `identity.account_locked`, `identity.refresh_token_invalid`, `identity.token_revoked`.

## Acceptance criteria
- AC1 Given an `Active` account with the right password, when the visitor signs in, then a token pair is issued with `sub`, `email`, `tenant_id` claims and the app shows the signed-in state. (BR1, BR4)
- AC2 Given a `Pending` account, when sign-in is attempted with the right password, then the answer is `identity.email_not_verified` and a resend action is offered; no token is issued. (BR2)
- AC3 Given a wrong password or an unknown email, when sign-in is attempted, then the answer is the same generic `identity.invalid_credentials` in both cases. (BR2)
- AC4 Given 5 consecutive wrong passwords for one account, when a 6th attempt is made — even with the right password — then the answer is `identity.account_locked` with the remaining seconds; after 15 minutes a correct password succeeds. (BR3)
- AC5 Given a valid refresh token, when it is exchanged, then a new access/refresh pair is issued, the old refresh token is removed from Redis, and reusing the old one returns `identity.refresh_token_invalid`. (BR4, BR5)
- AC6 Given a signed-in session, when the user signs out, then its Redis session entry is removed, its access token's `jti` is added to the revocation set, and a request with that same access token afterwards returns `identity.token_revoked`. (BR6, BR7)
- AC7 Given a session that was never signed out, when its access token is used inside its 15-minute lifetime, then requests succeed normally (the revocation check does not block valid sessions). (BR7)
- AC8 Architecture: `Simulab.Identity.Domain` and `Simulab.Identity.Application` reference neither EF Core, ASP.NET nor OpenIddict; the OpenIddict client and Redis session code sit in `Simulab.Identity.Infrastructure`/`Simulab.Identity.Api`. (profile)
- AC9 All new texts — the sign-in screen, validation messages and error codes — exist in pt-BR, pt-PT and en, and the missing-key test is green.
- AC10 Given an anonymous visitor, then the app bar's user menu slot shows "Sign in" linking to `/sign-in`; given a signed-in user, then it shows their display name and a "Sign out" action that revokes the session and returns to `/`. (BR10)

## Decisions
- 2026-09-18 — OpenIddict protocol endpoints (`/connect/token`, `/connect/revoke`, ...) stay at OpenIddict's own convention path, outside `/api/v1` — declared exception to the `api-contracts` rule, because they are a standard OAuth2/OIDC protocol surface, not a REST resource — owner, question 1.
- 2026-09-18 — One confidential OpenIddict client, `simulab-web`, registered through `OpenIddict.EntityFrameworkCore` in the `identity` schema; its secret is configuration on the `Api` host only — owner, question 2.
- 2026-09-18 — No concurrent-session limit in v1: multiple devices may hold a session at once. Simulae's oldest-session eviction depends on a plan the `Plans` module does not implement yet — owner, question 3.
- 2026-09-18 — No "remember me" — owner, question 4; the 30-day refresh token already covers a long session.
- 2026-09-18 — Sign-out effectiveness requires the per-request revocation check (`identity.token_revoked` via Redis), imported from Simulae's `RevocationCheckMiddleware` — owner, question 5.
- 2026-09-18 — Refresh rotation keeps Simulae's behavior: the used token is deleted, not marked revoked; reuse of a rotated-out or unknown token is `identity.refresh_token_invalid` in both cases. Finer reuse detection is deferred — owner, question 6.
- 2026-09-18 — Token lifetimes: access 15 minutes, refresh 30 days, same as Simulae — owner, question 7.
- 2026-09-18 — No lockout warning email in v1 — owner, question 8; captured as an idea for later.
- 2026-09-18 — Sign-in error messages: generic for invalid credentials, specific for locked and for email-not-verified (with resend) — owner, question 9.
- 2026-09-18 — OpenIddict signing/encryption certificates: development certificates in dev; staging/production stay `planned`, same as the rest of `docs/infra.md` — no real decision needed yet, only path today.
- 2026-09-18 — Token claims in F-5 are `sub`, `email`, `tenant_id` only; no role or permission claim until F-6 exists — owner, question 11.
- 2026-09-18 — Admin "terminate all sessions" and a per-device session list are out of scope, deferred to the Institutions epic — owner, question 12.
- 2026-09-18 — New packages approved: `OpenIddict.AspNetCore` 7.7.1 and `OpenIddict.EntityFrameworkCore` 7.7.1 (Apache-2.0), `Aspire.Hosting.Redis` 13.5.4 (MIT, `AppHost`), `StackExchange.Redis` 3.3.0 (MIT, `Infrastructure`), `Testcontainers.Redis` 4.15.0 (MIT, tests only) — owner, question 13.
- 2026-09-18 — Build: dropped `Microsoft.Extensions.Caching.StackExchangeRedis` from the approved list — the session store needs `IConnectionMultiplexer` (sets, per-key TTL), not the `IDistributedCache` abstraction that package provides.
- 2026-09-18 — Build: the Web host writes its own cookie session after a successful `/connect/token` call (claims: `sub`, `email`, `name`, `session_jti`, plus the access/refresh tokens so later calls and sign-out can use them); Blazor Interactive Server cannot call `HttpContext.SignInAsync` mid-circuit (no open HTTP response), so the interactive `/sign-in` page hands the tokens to a single-use, 30-second server-side ticket and does a full page navigation (`forceLoad: true`) to a small Web-host endpoint that finishes the cookie sign-in and redirects to `/`. Sign-out follows the same shape in reverse. No product-visible difference from the mockup.
- 2026-09-18 — Build (found on screen): `AddRedis` secures the local container with TLS and a password by default; a plain `ConnectionMultiplexer.Connect(...)` cannot validate its dev certificate and the first sign-in hung until the socket timed out. Fixed by adding `Aspire.StackExchange.Redis` back and registering the multiplexer with `builder.AddRedisClient("redis")` on the `Api` host instead — that client integration is what wires the certificate trust; the module only asks for `IConnectionMultiplexer` from `services`.
- 2026-09-18 — Build (found on screen): `RevocationCheckMiddleware` declared `IRefreshSessionStore` as an `InvokeAsync` parameter, so ASP.NET Core resolved it (and opened a Redis connection) on every request, authenticated or not — every anonymous page paid for a Redis round trip it never needed, and broke host-level tests that run without Redis at all. Fixed to resolve it from `HttpContext.RequestServices` only inside the branch that already found a `session_jti` claim.
- 2026-09-18 — Build (found on screen): the encryption certificate makes access tokens an encrypted JWE, not a plain signed JWT, so the Web's first attempt at decoding `sub`/`email` itself out of the token failed with an unhandled exception (visible as a frozen "Signing in..." button, since Blazor Server's error boundary stops further re-renders). Added `GET /api/v1/identity/session` (authenticated) so the Api — which already validates and decrypts its own tokens — hands those claims back as JSON; the Web never inspects the token's contents itself.

## Out of scope
- MFA (TOTP) and Google sign-in (F-11): switched off, not imported yet.
- Role and permission claims in the token, and endpoint/page/menu permission checks (F-6).
- Password recovery and change (F-7); `/sign-in` links to `/sign-up` only, no "forgot password" link yet.
- "Remember me", concurrent-session limits, per-device session list.
- Admin "terminate all sessions" (Institutions epic).
- A lockout warning email.
- Authorization code + PKCE flow (reserved for a future mobile client, ADR-0001 #12).

## Open questions
- (none)

## Change notes
<!-- Added by /agile:change during build. Increase `version` in the header. -->
<!--
### v2 — YYYY-MM-DD
- What: <change>
- Why: <reason>
- Affected: <BR/AC ids>; other criteria unchanged.
- Re-approved: <YYYY-MM-DD>
-->

## Validation script
A container runtime (Docker Desktop or Podman) must be running. Close any app host or IDE running from this checkout first.
1. Start `dotnet run --project src/Hosts/Simulab.AppHost` and open the dashboard URL printed on start. Wait for `postgres`, `mailpit`, `redis`, `api` and `web` to reach Running.
2. Open the `web` URL and go to `/sign-in` with an account already created and verified through F-4 (or sign one up first). Submit the empty form: both fields show their own message.
3. Sign in with a wrong password: the generic "Incorrect email or password" alert appears (never a hint about which field is wrong). Sign in with a correct password: the page returns to `/`, and the app bar's account icon now shows your email in its tooltip.
4. Fail the password 5 times in a row for the same account (open `/sign-in` again each time), then try once more with the correct password: "Too many attempts" with a countdown. Wait 15 minutes (or use a fresh account to move on) and it succeeds again.
5. Click the account icon in the app bar: a menu opens with "Sign out". Choose it: the page returns to `/` and the app bar shows "Sign in" again.
6. Sign in again, then open the same address directly in a new tab: instead of the form, it goes straight to `/`.
7. Switch the language to Português (Brasil) and to English on `/sign-in`: every text changes, including the "New here?" line and the error alerts.
8. Sign up a fresh account and, without verifying the email, try to sign in: the "Confirm your email before signing in" alert appears with a "Resend the verification email" action; use it and check Mailpit for a second message.

## Delivery
<!-- Filled by /agile:ship. -->
- Branch: <feature/F-<number>>
- Merge: <commit>
- Tests: <count, duration>
- Manual pages: <paths>
