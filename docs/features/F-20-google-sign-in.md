---
feature: F-20
epic: Foundation and identity
status: done
board: 733
version: 3
---
# Google sign-in

## Summary
Sign up and sign in with a Google account, behind a configuration switch (ADR-0001 #13). Split out of F-11 on 2026-09-20, which became TOTP only: the two are different jobs, and this one cannot start before the owner registers an OAuth client in Google Cloud and gets a client id, a client secret and the redirect URIs for development and for the future public address.

Simulae has the shape to follow (read-only, 2026-09-20): `Identity:GoogleSsoEnabled` plus a `GoogleSso` options section, an `ExternalLogin` entity with its store, four endpoints (`start`, `complete`, `confirm`, `cancel`) and a button on the sign-in page, with four test files. Simulab already maps the `user_logins` table (`IdentityUserLogin<Guid>`), which may replace Simulae's own entity. Refinement has to settle: which host owns the Google scheme and the redirect (Api or Web, with two hosts and a Blazor Server circuit that cannot redirect by itself); what happens when the Google address already has a password account; whether the 18+ declaration and the consent records are collected before or after the Google round trip; and how a first sign-in creates the account.

## Goal
A visitor signs up or signs in with Google in two clicks, without a second password to remember, and without opening a way into someone else's account.

## What exists today
Checked in the code on 2026-09-23 (items that touched it since F-5: F-7, F-8, F-10, F-11, F-16, B-3).
- Only the Web host is reachable from the browser (`WithExternalHttpEndpoints`, `src/Hosts/Simulab.AppHost/AppHost.cs:35`); the Api is called server to server. Google's redirect can only land on the Web host.
- Sign-in: the interactive `/sign-in` page calls the Api token endpoint (`/connect/token`, `src/Modules/Identity/Simulab.Identity.Api/TokenEndpoints.cs`) with the password grant, then hands the tokens to `GET /account/sign-in-complete` through a single-use 30 s ticket (`src/Hosts/Simulab.Web/Services/Auth/SignInTicketStore.cs`), which writes the cookie and keeps the tokens in Redis (B-3). Grants: password, refresh, and the custom `totp` grant (F-11) when TOTP is on.
- Password grant order: unknown user → `invalid_credentials`; lockout checked before the password; not `Active` → `email_not_verified`; two-factor on → a TOTP challenge instead of tokens.
- Sign-up (`RegisterRequest`): email, password, 18+ declaration, terms and privacy acceptance with the versions the page showed, optional full name. The account starts `Pending` and becomes `Active` only through `User.VerifyEmail`.
- `user_logins` (`IdentityUserLogin<Guid>`) is mapped and empty; account erasure already deletes a user's rows (`AccountErasureStore.cs:49`).
- Four actions ask for the current password: erase account (`EraseAccountRequest`), download data (`DataExportRequest`), turn two-factor off (`DisableTotpRequest`), change password. An account created with Google has no password, so none of these works for it as they are.
- "Forgot password" can give a first password to an account without one: `ResetPasswordHandler.cs:68` calls `UserManager.ResetPasswordAsync`, which checks only the token and then writes the hash, with no check that a password existed (ASP.NET Core v10.0.12, `src/Identity/Extensions.Core/src/UserManager.cs:1035-1057`).
- The switch pattern exists: `Identity:TotpEnabled` maps the routes and the grant only when on (F-11 BR12).
- No Google package is referenced today.

## Users and use cases
- UC1 A visitor with no account chooses "Continue with Google" on sign-in or sign-up, confirms terms, privacy and the 18+ declaration on a confirmation page, and is signed in to a new active account.
- UC2 A user whose account was created with Google signs in again with Google and lands signed in.
- UC3 A user with an active password account signs in with Google for the first time: the Google account is linked and they are signed in; the password keeps working.
- UC4 Someone registered the visitor's address with a password but never confirmed it; the visitor signs in with Google and confirms terms, privacy and the 18+ declaration: the account becomes active without that password, and they are signed in.
- UC5 A user with two-factor on signs in with Google and is asked for the code before being signed in.
- UC6 A user whose account has no password tries to erase the account, download their data, turn two-factor off or change the password, and is told to create a password through "forgot password" first.

## Business rules
- BR1 Everything in this item exists only while `Identity:GoogleSignInEnabled` is true, read on both hosts: the button, the Web endpoints, the `google` grant and the registration route. Off (the v1 default, ADR-0001 #13), the screens look as today and the routes answer 404 / `unsupported_grant_type`.
- BR2 The Api accepts a Google identity only as a Google ID token it validates itself: issuer `https://accounts.google.com`, audience the configured client id, signature from Google's published keys, not expired, and `email_verified` true. It trusts no claim relayed by the Web host. A token that fails any check is refused with `google_sign_in.invalid_token`; one without a verified email with `google_sign_in.email_not_verified`.
- BR3 The account is found by the Google subject (`sub`) in `user_logins` (provider `Google`) first, then by email. The Google email is never used to find an account whose link to Google holds a different subject.
- BR4 A linked account continues. An active account found by email is linked only when Google is authoritative for the address (it ends in `@gmail.com`, or the token carries `hd`, a Workspace account); otherwise the grant answers `google_sign_in.account_exists` and nothing changes (the visitor signs in with the password; linking from the account page is F-29). With two-factor off (or TOTP switched off), Google is the last step: clear the failed-attempt count and any lockout and issue tokens (a sign-in with Google is allowed while the password is locked out). With two-factor on, continue to BR6 without touching the count or the lockout: the code step keeps its own lockout rule (F-11 BR10), and only a right code clears the count. The password, if any, is kept.
- BR5 A pending account found by email, with Google authoritative for the address (as in BR4), is not activated at the token endpoint: the grant answers `google_sign_in.sign_up_required` and the visitor goes to the confirmation page. The confirmation then takes that pending account over instead of creating one: the Google account is linked first, then the account becomes active, loses its password and gets a new security stamp; its full name and 18+ declaration become what the page sent, and a new consent record is written. Whoever set that password, name and consent never proved the address. A pending account at an address Google is not authoritative for answers `google_sign_in.account_exists`.
- BR6 An account with two-factor on (and TOTP switched on) gets the existing TOTP challenge instead of tokens, whichever way it signed in.
- BR7 No account found: no account is created at the token endpoint. The visitor is sent to the confirmation page, which shows the Google email and name and asks for the 18+ declaration and the acceptance of terms and privacy with the versions it showed, under the same rules and error codes as the password sign-up (F-4). The new account starts active, with no password, the Google account linked, the full name from Google (editable on the page), the preferred language of the current UI culture, and a consent record as F-4 writes one. No verification email is sent.
- BR8 The Google ID token waiting for confirmation is kept on the Web host in a single-use ticket that expires after 10 minutes; the token never travels in a URL, a log or the Blazor circuit (the page gets only the email and name). An expired or spent ticket sends the visitor back to sign-in with `google_sign_in.expired`.
- BR9 If an active account with that email, or an account linked to that Google subject, appears between the Google step and the confirmation (two tabs), the confirmation does not create a second account: it answers `google_sign_in.account_exists`, and the visitor signs in with Google again (which then follows BR4). A pending account is taken over (BR5), never duplicated.
- BR10 The confirmation counts on the same per-client-address limit as the password sign-up (`registration.rate_limited`, 10 per hour per client IP, `IdentityEndpoints.cs:180`).
- BR11 An account with no password gets `identity.password_not_set` from the four actions that ask for the current password (erase, data download, turning two-factor off, password change), and those screens show that message with a link to "forgot password". The reset flow sets a first password the same way it replaces one.
- BR12 Account erasure removes the Google link with the other `user_logins` rows (already true, F-10), so an erased account can never be found again by its Google subject.

## Screens and API
- `/sign-in` and `/sign-up`: a "Continue with Google" button (Google's branding guidelines for the mark and the label) under the form, separated by an "or" divider. Hidden when the switch is off.
- `GET /account/google/start` (Web, plain endpoint) — challenges the Google scheme; the Blazor page navigates here with a full load.
- `GET /account/google/complete` (Web, plain endpoint, after the handler's `/signin-google` callback) — calls the Api `google` grant with the ID token and routes the outcome: tokens → the existing `/account/sign-in-complete` ticket; TOTP challenge → the existing code step of `/sign-in`; no account → `/sign-up/google`; any error → `/sign-in?error=<code>`.
- `/sign-up/google` — the confirmation page (UC1): Google email (read only), full name (prefilled, editable), 18+ checkbox, terms and privacy checkboxes with links to the documents, "Create account" and "Cancel" (back to sign-in, ticket spent).
- `POST /connect/token` with `grant_type=google` and `id_token` — OpenIddict's own path, OAuth2 error shape, like the `totp` grant (F-5 decision 1). Errors: `google_sign_in.invalid_token`, `google_sign_in.email_not_verified`, `google_sign_in.sign_up_required` (with the email and name as response parameters), `mfa_required` (existing, F-11).
- `POST /api/v1/identity/google-registrations` — `GoogleRegistrationRequest(IdToken, DeclaresAdult, AcceptsTerms, AcceptsPrivacy, TermsVersion, PrivacyVersion, FullName?)` → 201. Errors: `google_sign_in.invalid_token`, `google_sign_in.email_not_verified`, `google_sign_in.account_exists`, and the F-4 codes `registration.age_declaration_required`, `registration.consent_required`, `registration.terms_version_outdated`, `registration.full_name_too_long`, `registration.rate_limited`.
- New error code for BR11: `identity.password_not_set` on the erase, data-download, disable-two-factor and change-password endpoints.
- Configuration: `Identity:GoogleSignInEnabled` (both hosts, default false); `Authentication:Google:ClientId` (both hosts) and `Authentication:Google:ClientSecret` (Web only), from user secrets in development.

## Acceptance criteria
- AC1 Given the switch off, then the sign-in and sign-up pages show no Google button, `/account/google/start` and `/api/v1/identity/google-registrations` answer 404, and `grant_type=google` answers `unsupported_grant_type`. (BR1)
- AC2 Given an ID token with a wrong audience, a wrong issuer, a bad signature or an expired lifetime, when it is sent to the grant or the registration, then it is refused with `google_sign_in.invalid_token`; given `email_verified` false, with `google_sign_in.email_not_verified`. (BR2)
- AC3 Given no account for the token's subject or email, when the grant runs, then no account is created and it answers `google_sign_in.sign_up_required` with the email and name. (BR7)
- AC4 Given a valid token and the consents, when the registration runs, then the account is active, has no password, has the Google link, the full name and the UI culture's language, a consent record with the versions sent, no verification email, and the grant then issues tokens. (UC1, BR7)
- AC5 Given the registration without the 18+ declaration, without a consent, or with an outdated version, then it answers the F-4 code and creates nothing. (BR7)
- AC6 Given an account linked to the token's subject, when the grant runs, then it issues tokens, even if the account's email has since changed. (UC2, BR3)
- AC7 Given an active password account with the token's `@gmail.com` email and no link, when the grant runs, then the link is added, tokens are issued, and the password still signs in. (UC3, BR4)
- AC8 Given an active account with two-factor off locked out by failed passwords, when the grant runs, then tokens are issued and the failed count and lockout are cleared. (BR4)
- AC9 Given a pending account with the token's `@gmail.com` email, when the grant runs, then it answers `google_sign_in.sign_up_required` and the account is unchanged; when the confirmation runs, then the same account is active, has no password, a new security stamp, the full name and 18+ declaration sent, a new consent record and the link, no second account exists, and the old password no longer signs in. (UC4, BR5)
- AC10 Given an account with two-factor on and TOTP switched on, when the grant runs, then it answers `mfa_required` with a challenge, and the existing code step completes the sign-in; the Google step leaves the failed count and the lockout as they were, and a locked account's code step answers `identity.account_locked`. (UC5, BR4, BR6)
- AC11 Given an account linked to a different Google subject, when a token with another subject and the same email is sent, then the grant does not sign in to that account. (BR3)
- AC12 Given an active account with the token's email created between the Google step and the confirmation, when the registration runs, then it answers `google_sign_in.account_exists` and no second account exists. (BR9)
- AC13 Given 10 registrations (password or Google) from one client address in an hour, when an 11th Google registration arrives, then it answers `registration.rate_limited`. (BR10)
- AC14 Given the Web host with the switch on, when `/account/google/start` is requested, then it redirects to Google's authorization endpoint with the configured client id and the `/signin-google` redirect; and `/account/google/complete` routes each grant outcome to the page listed in Screens and API. (Screens)
- AC15 Given a waiting-confirmation ticket older than 10 minutes, or already used, when `/sign-up/google` opens with it, then the visitor is sent to `/sign-in?error=google_sign_in.expired`. (BR8)
- AC16 Given an account with no password, when it asks to erase the account, download its data, turn two-factor off or change the password, then each answers `identity.password_not_set`, and the screen shows the message with a link to "forgot password"; after a reset the same action works with the new password. (UC6, BR11)
- AC17 Given an account linked to Google, when it is erased, then its `user_logins` rows are gone and a token with its old subject leads to `google_sign_in.sign_up_required`. (BR12)
- AC19 Given an active or pending account at an address that is not `@gmail.com` and a token without `hd`, when the grant runs, then it answers `google_sign_in.account_exists` and nothing is linked or changed; with `hd` in the token, the active account is linked and signed in. (BR4, BR5)
- AC18 All new texts (button, divider, confirmation page, the new error messages) exist in pt-BR, pt-PT and en, and the missing-key test is green.
- On screen only (validation script): the real round trip with the owner's Google account and OAuth client.

## Decisions
- 2026-09-23 — Owner: the Google button is on both sign-in and sign-up — whoever comes to sign up gains the most from it.
- 2026-09-23 — Owner: an active password account with the same verified email is linked and signed in; the password stays — both proved the same address.
- 2026-09-23 — Owner: a pending account with the same email becomes active and loses its password — keeping a password nobody proved would let whoever set it in later (pre-hijack).
- 2026-09-23 — Owner: terms, privacy and 18+ are asked after the Google round trip, on a confirmation page — a Google sign-in from the sign-in page can then create the account too.
- 2026-09-23 — Owner: two-factor is asked after Google as after a password — two-factor protects the account, not the password.
- 2026-09-23 — Owner: Google signs in during a password lockout and clears it — the lockout is against password guessing, and otherwise an attacker could lock the owner out.
- 2026-09-23 — Owner: an account with no password creates one through "forgot password" before the four actions that ask for it — least work, and no proof is weakened.
- 2026-09-23 — Owner: linking and unlinking from the account page is out of scope, captured as F-29 (AB#748).
- 2026-09-23 — Owner: packages approved — `Microsoft.AspNetCore.Authentication.OpenIdConnect` 10.0.12 (MIT, Web) and `Microsoft.IdentityModel.Protocols.OpenIdConnect` 8.23.0 (MIT, Api), latest stable on nuget.org on 2026-09-23. Tests need no new package.
- 2026-09-23 — Owner: the Google Cloud OAuth client does not exist yet — the build uses test keys; the on-screen validation waits for the owner to create it, with the steps and the exact redirect URI written in `docs/infra.md` during build.
- 2026-09-23 — The Web host owns the Google scheme (OpenID Connect against `accounts.google.com`, with its own short-lived external cookie) — it is the only host the browser reaches.
- 2026-09-23 — The Api validates the ID token itself (BR2) instead of trusting claims from the Web host — the Api stays the only place that decides who someone is, as with passwords.
- 2026-09-23 — The Google identity reaches the Api as a custom OpenIddict grant (`google`) next to `totp` — the Web host already speaks the token endpoint and the ticket hand-over (F-5, B-3), so the rest of sign-in is reused.
- 2026-09-23 — The link lives in the existing `user_logins` table (provider `Google`, key = the Google `sub`); no new entity and no migration — the table is mapped and erasure already clears it.
- 2026-09-23 — The Api reads Google's keys through the discovery document with the library's cached configuration manager; the tests replace the authority with keys they generate — no call to Google in any test.
- 2026-09-23 — Build: `RegistrationTerms` holds the name, 18+, consent and version checks for both sign-ups; `RegisterUserHandler` now calls it — one place, so the two sign-ups cannot drift apart (profile: structure on the second use).
- 2026-09-23 — Build: `SignInHandOff` ends every sign-in (password, code step, Google step, Google confirmation) and `LegalConsentLabel` renders the consent labels of both sign-up pages — second uses of code that lived in one page.
- 2026-09-23 — Build: the app host turns the feature on only when `Google:ClientId` and `Google:ClientSecret` are in its user secrets, and passes the secret to the Web as a masked parameter (`docs/infra.md`, "Google sign-in locally").
- 2026-09-23 — Build: the pages read the code-step and confirmation tickets without spending them, because a page initializes twice (prerender, then the circuit); the Api spends the challenge itself, and the confirmation spends its ticket once the account exists.
- 2026-09-23 — Build: the Web's OpenID Connect handler uses the authorization code flow with PKCE and `form_post`, its defaults; checked on screen through the app host with a placeholder client (Google answered `invalid_client`, as expected).
- 2026-09-23 — Review 1 (major, confirmed, fixed): the confirmation ticket was a bearer in the URL for 10 minutes; it now opens only in the browser that holds a secret in an HttpOnly cookie scoped to `/sign-up/google`, carried to the circuit like the visitor's address.
- 2026-09-23 — Review 2 (minor, accepted): the code-step ticket is also in the URL; the challenge is single-use at the Api and still needs the code and the TOTP lockout.
- 2026-09-23 — Review 3 (minor, fixed by v3): the pending path activated before linking; the take-over now links first.
- 2026-09-23 — Review 4 (minor, new item): both sign-ups save in several steps without a transaction, as F-4 always has — F-30 (AB#749).
- 2026-09-23 — Review 5 (minor, accepted, not verified): the retry after a key rotation may still see the old keys; Google publishes new keys before using them and the library refreshes them on its own.
- 2026-09-23 — Review 6 and 7 (minor, owner's choice): change note v3.
- 2026-09-23 — Review 8 (minor, fixed): `SignInTicketStore` now uses `SingleUseTickets<T>`; the one-instance assumption is in `docs/infra.md`.
- 2026-09-23 — Review 9 (minor, fixed): a test proves `/account/google/complete` clears the external cookie.

## Out of scope
- Linking or unlinking Google from the account page — F-29 (AB#748).
- Re-authentication with Google as proof for erase, data download, two-factor or password change (owner chose "forgot password", 2026-09-23).
- Other providers (Microsoft, Apple).
- The public address's redirect URI and the production OAuth client (release work).

## Open questions
- (none)

## Change notes

### v2 — 2026-09-23
- What: Google clears the failed-attempt count and the lockout only when it is the last step (two-factor off). With two-factor on, the Google step issues the TOTP challenge and leaves the count and the lockout as they were; the code step keeps its own lockout rule.
- Why: the failed count is shared by wrong passwords and wrong codes (`SecondFactor.cs`), so clearing it on every Google step would give unlimited code tries to whoever holds the victim's Google session; `profile.md`: a multi-step sign-in clears the count only in its last step. Found at the start of the build.
- Affected: BR4, AC8, AC10; other rules and criteria unchanged.
- Re-approved: 2026-09-23

### v3 — 2026-09-23
- What: (a) an existing account is reached by email only when Google is authoritative for the address (`@gmail.com`, or `hd` in the token); otherwise `google_sign_in.account_exists`. (b) A pending account is no longer activated at the token endpoint: the visitor goes through the confirmation page, which takes the pending account over (link first, then active, no password, new stamp, name, 18+ and a new consent record from the page).
- Why: the independent review (findings 6 and 7). Google documents `email_verified` as authoritative only for Gmail and Workspace addresses, so linking other addresses by email leaves a takeover path through a domain that changed hands; and the pending account's consent, 18+ declaration and name were given by someone who never proved the address. Owner's choices, 2026-09-23.
- Affected: UC4, BR4, BR5, BR9, AC7, AC9, AC12, new AC19; other rules and criteria unchanged.
- Re-approved: 2026-09-23

## Validation script
Needs a Google account listed as a test user of your OAuth client, and no Simulab account for its address yet (a `@gmail.com` address).

1. Create the OAuth client and put it in the app host's user secrets: `docs/infra.md`, "Google sign-in locally", steps 1-3. The two `dotnet user-secrets set` commands are the same in Git Bash and PowerShell 7, run from `D:\dev\_icontrol\wt\simulab\F-20`; each prints `Successfully saved Google:ClientId = … to the secret store.` Check with `dotnet user-secrets list --project src/Hosts/Simulab.AppHost` → both `Google:` keys listed.
2. Start the app host from the F-20 worktree (Git Bash: `cd /d/dev/_icontrol/wt/simulab/F-20 && dotnet run --project src/Hosts/Simulab.AppHost`; PowerShell 7: `cd D:\dev\_icontrol\wt\simulab\F-20; dotnet run --project src/Hosts/Simulab.AppHost`) → in the dashboard, `api` and `web` show `Identity__GoogleSignInEnabled = true`.
3. Open `/sign-up` → under the form, "ou" and "Continuar com o Google". Click it, choose the test Google account → the confirmation page "Crie sua conta" shows that Gmail address and your Google name.
4. Switch the language to English on that page → the texts change ("Create your account", "Create account"). Tick the 18+, terms and privacy boxes and press "Create account" → you land signed in on the home page, and no verification email arrives in Mailpit.
5. Sign out, open `/sign-in`, and using only the keyboard press Tab until "Continue with Google" has the focus ring, then Enter → Google, then signed in again straight away (no confirmation page this time).
6. Open "My account" → "Download my data", type anything as the password and confirm → the message says the account was created with Google and has no password, with "Create a password"; that button opens `/forgot-password`.
7. Stop the app host (Ctrl+C). To turn Google sign-in off again: `dotnet user-secrets remove "Google:ClientId" --project src/Hosts/Simulab.AppHost`, then start again → `/sign-in` shows no Google button.

Validated by the owner on 2026-09-23 ("aprovado"): the script passed.

## Delivery
- Branch: `feature/F-20`, built in the worktree `D:\dev\_icontrol\wt\simulab\F-20`
- Merge: `644f24b` on `main` (2026-09-23, authorized by the owner)
- Tests: 866 passed, 0 failed, suite 49 s, full build 22 s, 0 warnings (`gate.js ship`, 2026-09-23)
- Manual pages: `docs/manual/{en,pt-BR,pt-PT}/google-sign-in.md`, linked from each `index.md`. `docs/infra.md`: "Google sign-in locally", the new secrets and the one-instance note. `docs/architecture` unchanged (no new context or table; `DocGen --check` up to date; the project has no hand-written overview page)
- Packages added: `Microsoft.AspNetCore.Authentication.OpenIdConnect` 10.0.12, `Microsoft.IdentityModel.Protocols.OpenIdConnect` 8.23.0
- Items captured: F-29 (AB#748) link and unlink Google on the account page; F-30 (AB#749) sign-up in one transaction
