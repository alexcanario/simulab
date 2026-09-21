---
feature: F-11
epic: Foundation and identity
status: building
board: 715
version: 2
---
# Two-factor sign-in with an authenticator app (TOTP)

## Summary
A user can protect the account with a six-digit code from an authenticator app (RFC 6238). Enrolment lives on a new security page; sign-in gains a second step; ten single-use recovery codes cover a lost phone. The whole feature is behind `Identity:TotpEnabled`, false by default and true in development (ADR-0001 #13).

## Goal
Give a user who wants it a second barrier on their account, without turning it into a way to lose the account: the code protects sign-in, the recovery codes protect the user, and a switch keeps the feature out of v1 until the owner turns it on.

## What already exists (verified 2026-09-20)
- **False premise in the original summary.** "Imports ... switched off" reads as if the code were already here and only needed a flag. Nothing of either feature exists in Simulab: no Google handler, endpoint, option or button; no TOTP secret, service, enrolment screen or second step at sign-in; no configuration key; no package. A grep for `Google`, `Totp`, `Mfa` or `TwoFactor` over `src/` finds only `TwoFactorEnabled = false` in `User.Erase()` (F-10) and the column ASP.NET Identity creates. F-11 is new code written against Simulae as the source, not a flag flip.
- **Second false premise, smaller.** "No UI is shown while they are off" cannot be validated on screen: with the switch off there is nothing for the owner to look at. Hence the switch is true in development (decision below).
- **What the account row already carries**: `TwoFactorEnabled` (from `IdentityUser`, always false today), the `user_tokens` table (`IdentityUserToken<Guid>`, where ASP.NET Identity keeps recovery codes) and the `user_logins` table. No TOTP secret column.
- **Sign-in today** (F-5): the Web's `/sign-in` page posts to the Api's OpenIddict token endpoint `POST /connect/token` with the password grant, gets access and refresh tokens, and finishes through the non-Blazor Web endpoint `/account/sign-in-complete`, which writes the auth cookie. The Api allows the password and refresh grants only; the password grant is first-party only (ADR-0001 #12).
- **A second step at sign-in does not exist.** `TokenEndpoints.HandlePasswordGrantAsync` either issues both tokens or refuses. The endpoint is a declared exception to the api-contracts rule (F-5, decision 1): it answers with the OAuth2 error shape, not problem details.
- **Sessions and single-use tokens already live in Redis** (`IRefreshSessionStore`, F-5): the sign-in challenge has somewhere to go that survives an Api restart.
- **Password confirmation is a solved pattern**: `ChangePasswordHandler` (F-7) and `EraseAccountHandler` (F-10) check the current password through `UserManager` with the same lockout as sign-in.
- **In Simulae** (read 2026-09-20, read-only): TOTP is ~11 files with `TotpService` (Otp.NET), an AES-GCM protector for the secret, `MfaEnabled` and `TotpSecretEncrypted` on the user, setup and verify endpoints, a code screen at sign-in and a security panel; ~728 lines of tests. It has **no recovery codes**, **no switch**, and no replay protection.
- **Packages Simulab does not have yet**: `Otp.NET`, `QRCoder`.

## Users and use cases
- UC1 A signed-in user opens Security from My account, scans a QR code with an authenticator app, types the first code and turns two-factor on.
- UC2 At enrolment the user is shown ten recovery codes once, and confirms having saved them.
- UC3 A user with two-factor on signs in: after the password, a second screen asks for the six-digit code, and only then is the session created.
- UC4 A user who lost the phone signs in with a recovery code instead of the six-digit code; that code stops working afterwards.
- UC5 A user regenerates the recovery codes from the Security page, which invalidates the previous ten.
- UC6 A user turns two-factor off with the current password and a valid code (or a recovery code).
- UC7 With `Identity:TotpEnabled` false, nobody sees the feature: the Security page is not reachable, the endpoints are not mapped, and an account that already has two-factor signs in with the password alone.

## Business rules
- BR1 A user enrols, regenerates and turns off two-factor only on their own account. The account is the token subject; no id is taken from the route or the body.
- BR2 Enrolment is two steps: the app asks for a secret and shows it as a QR code and as text; two-factor turns on only when the user sends a code that the secret validates. An unconfirmed secret grants nothing and is replaced by the next enrolment attempt.
- BR3 The secret is stored encrypted (AES-256-GCM) with the key in `Identity:TotpEncryptionKey`. The key is required when the feature is on and is validated at startup; without it the Api refuses to start rather than storing secrets in the clear.
- BR4 A code is valid for its own 30-second step and one step either side (clock skew). A code already accepted for an account cannot be accepted again: the last accepted step is stored and a code at or below it is refused.
- BR5 Turning two-factor on generates ten single-use recovery codes, shown once and never again. They are stored the way ASP.NET Identity stores them (hashed, in `user_tokens`).
- BR6 A recovery code is accepted wherever a six-digit code is, at sign-in and to turn two-factor off, and is consumed on use.
- BR7 Regenerating the recovery codes needs a valid code or recovery code; the previous ten stop working at once.
- BR8 Turning two-factor off needs the current password **and** a valid code or recovery code. It clears the secret, the flag, the recovery codes and the last accepted step.
- BR9 Sign-in with two-factor on: the password grant does not issue tokens. It answers `mfa_required` with a single-use challenge valid for 5 minutes, kept in Redis. The second call exchanges the challenge and the code for the tokens; the challenge is consumed whether the code was right or wrong.
- BR10 A wrong code counts as an access failure on the same lockout as a wrong password (5 attempts, 15 minutes, F-5 BR3): while locked, even a right code is refused.
- BR11 Turning two-factor on or off does not end the other sessions: the second factor applies from the next sign-in.
- BR12 The whole feature is behind `Identity:TotpEnabled`. False: the endpoints are not mapped, the Security page is not reachable and the password grant ignores `TwoFactorEnabled`, so nobody is locked out by a switch. True: everything above applies. The default is false; development sets it true.
- BR13 Recovery codes and the secret are erased with the account (F-10 BR8 already deletes `user_tokens`); the erasure adds the secret columns to what it clears.

## Screens and API
- `/account/security` — new page, signed-in only (a redirect to `/sign-in`, as `/account` and `/account/password` do), reached from a link on `/account` next to Change password. Only mapped when the feature is on.
  - **Off state** (two-factor not enabled): what two-factor is, in two sentences, and a primary **Turn on two-factor** button.
  - **Enrolment**: the QR code (rendered on the server), the secret in text for manual entry, a six-digit field, **Confirm** and **Cancel**.
  - **Recovery codes**: after a successful confirmation, the ten codes, a **Copy** and a **Download** action, and a checkbox "I saved my recovery codes" that enables **Done**. Shown once.
  - **On state**: when two-factor was turned on, how many recovery codes are left, **Regenerate recovery codes** and **Turn off two-factor** (destructive, in a danger zone like F-10).
  - States: loading (skeleton), load error (`AppAlert` with Try again), submitting (button disabled with progress), wrong code (error under the field), locked out (`AppAlert` with the wait), unexpected failure (`AppAlert` with Try again).
- `/sign-in` — when the Api answers `mfa_required`, the page shows the code step instead of the password form: a six-digit field, a link "Use a recovery code" that swaps the field, **Sign in**, and **Cancel** back to the password form. The challenge lives in the page's state, never in the URL.
- `POST /connect/token` — the password grant gains a third answer: `error=mfa_required` with `challenge` and `expires_in`. A new grant type `totp` takes `challenge` and `code` and issues the tokens (a declared exception to api-contracts, like the rest of this endpoint).
- `POST /api/v1/identity/totp/enrolments` — starts enrolment; returns the secret, the `otpauth://` URI and the QR code as a data URI. Requires authentication.
- `POST /api/v1/identity/totp/enrolments/confirmations` — `ConfirmTotpRequest(Code)`; turns two-factor on and returns the ten recovery codes. Requires authentication.
- `POST /api/v1/identity/totp/recovery-codes` — `RegenerateRecoveryCodesRequest(Code)`; returns ten new codes. Requires authentication.
- `DELETE /api/v1/identity/totp` — `DisableTotpRequest(CurrentPassword, Code)`; 204. Requires authentication.
- `GET /api/v1/identity/totp` — `TotpStatusResponse(Enabled, EnabledAt, RecoveryCodesLeft)`, what the Security page reads. Requires authentication.
- Error codes: `totp.code_invalid`, `totp.challenge_invalid`, `totp.already_enabled`, `totp.not_enabled`, `totp.current_password_invalid`. Reused: `identity.account_locked`.

## Acceptance criteria
- AC1 Given a signed-in user with the feature on, when they start enrolment, then they get a secret, an `otpauth://` URI carrying the account's email and a QR image, and two-factor is still off. (UC1, BR2)
- AC2 Given an enrolment in progress, when the user confirms with a code generated from that secret, then two-factor is on, the stored secret is encrypted (the column does not contain the plain secret) and ten recovery codes come back. (UC1, UC2, BR2, BR3, BR5)
- AC3 Given an enrolment in progress, when the user confirms with a wrong code, then the answer is `totp.code_invalid`, two-factor stays off and the access-failure count went up. (UC1, BR10)
- AC4 Given a user with two-factor on, when they sign in with the right password, then no tokens are issued: the answer is `mfa_required` with a challenge; and when the challenge and a valid code are sent, then the tokens are issued. (UC3, BR9)
- AC5 Given a challenge, when it is used a second time, or after 5 minutes, or with another account's code, then it is refused with `totp.challenge_invalid` and no tokens are issued. (BR9)
- AC6 Given a code already accepted once, when it is sent again inside its window, then it is refused. (BR4)
- AC7 Given a code from the previous or the next 30-second step, when it is sent, then it is accepted. (BR4)
- AC8 Given a user with two-factor on, when they sign in with a recovery code instead of the six-digit code, then the tokens are issued, that code no longer works and nine are left. (UC4, BR6)
- AC9 Given five wrong codes at the second step, then the account is locked for 15 minutes and a right code is refused with `identity.account_locked` and the seconds left. (BR10)
- AC10 Given a user with two-factor on, when they regenerate the recovery codes with a valid code, then ten new codes come back and the previous ten no longer work. (UC5, BR7)
- AC11 Given a user with two-factor on, when they turn it off with the current password and a valid code, then the flag, the secret, the recovery codes and the last accepted step are cleared, and the next sign-in needs only the password; with a wrong password or a wrong code, nothing changes. (UC6, BR8)
- AC12 Given two sessions of the same user, when two-factor is turned on or off, then both sessions still work. (BR11)
- AC13 Given `Identity:TotpEnabled` false, then the `totp` routes answer 404, the `totp` grant is refused, `/account/security` is not reachable, `/account` shows no link to it, and a user whose account has two-factor on signs in with the password alone. (UC7, BR12)
- AC14 Given the feature is on and `Identity:TotpEncryptionKey` is missing or not a 256-bit key, then the Api fails to start with a message naming the setting. (BR3)
- AC15 Given a user with two-factor on, when the account is erased (F-10), then the secret, the flag and the recovery codes are gone. (BR13)
- AC16 Given `/account/security`, then the QR code, the manual secret, the recovery codes with Copy and Download, and the danger zone behave as described, and Done is disabled until the checkbox is ticked. (Screens)
- AC17 All new texts (the Security page, the sign-in code step, the recovery codes, the errors `totp.*`) appear in pt-BR, pt-PT and en, and the missing-key test is green.

## Decisions
- 2026-09-20 — F-11 is TOTP only; Google sign-in becomes its own item — owner's choice; they are two different jobs and Google also needs credentials registered in Google Cloud, an action outside this repository.
- 2026-09-20 — Ten single-use recovery codes, shown once at enrolment — owner's choice; without them a lost phone locks the account for good, and neither the password reset nor the erasure helps (erasure needs the password *and*, from now on, a code).
- 2026-09-20 — Any signed-in user may turn two-factor on, for their own account; turning it off needs the current password **and** a code — owner's choice; otherwise whoever stole the password disarms the protection.
- 2026-09-20 — A wrong code counts toward the same lockout as a wrong password — owner's choice; a six-digit code is brute-forceable without a limit.
- 2026-09-20 — Turning two-factor on or off leaves the other sessions alone — owner's choice; it applies from the next sign-in, like a profile change.
- 2026-09-20 — `Identity:TotpEnabled`, false by default and true in development — owner's choice; it honours ADR-0001 #13 and still lets the owner validate on screen through the app host.
- 2026-09-20 — The secret is encrypted at rest with a configuration key — owner's choice; a database dump must not hand over anybody's second factor. Operational consequence, to record in `docs/infra.md`: losing the key invalidates every enrolment.
- 2026-09-20 — Enrolment lives on its own page `/account/security` — owner's choice; a QR code, a manual secret, a code field and ten recovery codes do not fit a dialog (rule `ui-project`).
- 2026-09-20 — The challenge between the password and the code lasts 5 minutes and is single-use, kept in Redis — owner's choice; long enough to pick up the phone, short enough not to leave a half-open door.
- 2026-09-20 — New packages, approved by the owner (rule `build-config`): `Otp.NET` 1.4.0 (MIT) and `QRCoder` 1.6.0 (MIT). No new test package: the encryption comes from .NET and the recovery codes from ASP.NET Identity.
- 2026-09-20 — The account reuses `TwoFactorEnabled` from `IdentityUser` instead of Simulae's `MfaEnabled`; the new columns are the encrypted secret, the instant it was turned on and the last accepted step — one flag, not two.
- 2026-09-20 — The second step is a custom OpenIddict grant `totp` on `/connect/token`, not a new REST route: it issues tokens, which is what that endpoint is for, and the page already talks to it.
- 2026-09-20 — The replay rule (BR4) is ours, not Simulae's: without it a code works for up to 90 seconds after being used.
- 2026-09-20 — Approved by the owner.
- 2026-09-21 — Package versions raised to the latest stable releases, `Otp.NET` 1.4.1 and `QRCoder` 1.8.0 (both MIT, the versions Simulae runs) — owner's choice at build start; the approved 1.4.0 and 1.6.0 were behind, and rule `build-config` asks for the latest stable release when a package is added.

## Out of scope
- Google sign-in — F-20 (AB#733), refined when the Google Cloud credentials exist.
- An admin turning off two-factor for somebody else — the recovery codes cover the common case; unlocking another person's account is a separate feature, with an audit trail.
- A second factor by SMS or email.
- Requiring two-factor by role, by permission or by plan.
- Trusting a device ("do not ask on this browser again").
- Showing the user which devices or sessions are signed in (already out of scope in F-8).

## Open questions
- (none)

## Change notes
### v2 — 2026-09-20
- What: the item drops Google sign-in and becomes TOTP only; the file is renamed `F-11-two-factor-totp.md`.
- Why: refinement found that neither feature exists in Simulab (the summary implied a flag flip), that Google needs credentials the owner has to create outside the repository, and that a fully switched-off feature cannot pass the validation gate.
- Affected: the whole file, which was still a template; no criterion was approved before this.
- Re-approved: 2026-09-20 (the whole item, since nothing had been approved before).

## Validation script
<!-- Written at the end of build. At most 8 steps the product owner follows on screen. -->
1. <Step> → <expected result>

## Delivery
<!-- Filled by /agile:ship. -->
- Branch: <feature/F-11>
- Merge: <commit>
- Tests: <count, duration>
- Manual pages: <paths>
