---
feature: F-16
epic: Foundation and identity
status: done
board: 729
version: 2
---
# Download my data

Technical terms: [glossary](../glossary.md)

## Summary
A signed-in user downloads everything the app holds about them, in a machine-readable file (portability, LGPD article 18 / GDPR article 20). Left out of F-10 (account erasure) on 2026-09-19 because Identity held little personal data. Built now with the account data only (owner, 2026-09-21); each future module (attempts, answers, scores, coach conversations) adds its own section to the same file.

## Goal
Answer a portability request from the app itself, without manual work, and set the file format that later modules extend.

## What already exists
Checked in the code on 2026-09-21, on `main` at `c919a94`.
- Personal data about a user, all in the Identity module (schema `identity`, see `docs/architecture/Identity/data-dictionary.md`):
  - `users`: email, full name, phone number, preferred language, adult self-declaration, `email_verified_at`, `created_at`, `status`;
  - `consent_records` (`Simulab.Identity.Domain/Entities/ConsentRecord.cs:11`): terms and privacy versions, adult declaration, locale, `AcceptedAt` and `IpAddress`; `IConsentRecordStore` can only add (`Application/Abstractions/IConsentRecordStore.cs:8`);
  - `role_changes` (`Domain/Entities/RoleChange.cs:11`, F-14): rows whose `TargetUserId` is the user, with the actor in the audit field `CreatedBy` (`SharedKernel/Entities/TenantEntity.cs:12`);
  - `user_roles`: the roles the user holds now;
  - refresh sessions in Redis (`Infrastructure/Sessions/RedisRefreshSessionStore.cs:14-16`): a set of session ids per user. The sessions have **no creation time**, and no method lists them; only `RevokeAllAsync` reads the set (`:62`).
- No study data exists yet: there are no attempts, answers, scores or coach conversations.
- The account erasure (F-10) confirms the current password in `EraseAccountHandler` (`Application/Account/EraseAccountHandler.cs:36-47`). It uses the lockout check, `CheckPasswordAsync` and `AccessFailedAsync`, and answers `identity.account_locked` with 423 and `retryAfterSeconds` (`Api/IdentityEndpoints.cs:367-373`). The Web side has `EraseAccountDialog.razor` and `IdentityApiClient.EraseAccountAsync` (`Services/IdentityApiClient.cs:112-145`).
- Notice emails go through the job queue: `PasswordMailer.SendPasswordChangedAsync` calls `queue.EnqueueEmail` (`Infrastructure/Email/PasswordMailer.cs:52-80`). The texts live in `IdentityEmails{,.pt-BR,.pt-PT}.resx` and are rendered in `user.PreferredLanguage` through `EmailCulture.Use`.
- `/account` (`Components/Pages/Identity/Account.razor`) has a profile card (`:30-74`) and a danger zone (`:77-83`). No file download exists in the Web app today.
- Items that touched this code since F-10: F-13 (emails through the job table), F-14 (role changes), B-10 (icon contrast on the page).

## Users and use cases
- UC1 A signed-in user opens My account, reads what the file contains, and asks to download their data.
- UC2 The user confirms with the current password; the browser saves one JSON file with their account data, and an email tells them their data was downloaded.
- UC3 The user types the wrong current password; nothing is downloaded, and repeated attempts lock the account the same way a sign-in does.

## Business rules
- BR1 A user exports only their own data. The account is the token subject; no id is taken from the route or the body.
- BR2 The export is confirmed with the current password. A wrong or empty password fails with `data_export.current_password_invalid` and counts as an access failure. While the account is locked out, the request is refused with `identity.account_locked` and the remaining seconds (423), exactly as F-10 BR2. A correct password clears the failure count, as a password change does.
- BR3 The file is JSON, serialized with the shared `AppJson.Options`: enums as strings and instants in ISO 8601 UTC. Its top level is `{ "format": "simulab.data-export", "version": 1, "exportedAt": ..., "identity": { ... } }`. Each module owns one top-level section named after it, so later modules add sections without changing the existing ones.
- BR4 The `identity` section holds:
  - `account`: id, email, full name, phone number, preferred language, created at, email verified at, adult self-declaration, status;
  - `roles`: the names of the roles held now;
  - `consents`: every consent record, with terms version, privacy version, adult declaration, locale, accepted at and IP address;
  - `roleChanges`: every role change whose target is the user, with when, action, role name, the roles added and removed by name, and `byYou` (true when the actor is the user). The actor's id and email are never included: they are another person's data;
  - `sessions`: `{ "active": <n> }`, the number of refresh sessions that still exist for the user.
- BR5 The file never holds secrets or security material: no password hash, security stamp, TOTP secret, tokens, token hashes, session ids, lockout counters or concurrency stamps.
- BR6 The file is built on each request and sent as the response body. It is never stored, and there is no limit on how often it can be downloaded: the password and the lockout protect it.
- BR7 After a successful export, one email goes to the account's address in its `PreferredLanguage`, through the job queue. It says the data was downloaded, when (UTC), and what to do if it was not the user (change the password). A failure to enqueue it does not block the download, and the failure is logged.
- BR8 The file name is `simulab-my-data-<yyyy-MM-dd>.json`, using the UTC date of the export.

## Screens and API
- `/account` gains a **Your data** card between the profile card and the danger zone. It has a heading, one paragraph listing what the file contains (account, roles, consents with IP address, role changes, number of active sessions), and a button "Download my data".
  - The button opens a dialog with the same shape as the erasure dialog (v2): the same list, an `AppPasswordField` for the current password, "Cancel" and "Download". The confirm button is disabled while the password field is empty and while the call runs.
  - States: submitting (button disabled with progress); wrong password (error text under the field, from the error code); locked out (`AppAlert` with the wait); unexpected failure (`AppAlert` with Try again). The dialog stays open on every failure.
  - Success: the dialog closes, the browser saves the file, and a snackbar says the download started and that a confirmation email is on its way.
- `POST /api/v1/identity/data-exports`: `DataExportRequest(CurrentPassword)`. It answers 200 with `application/json` and `Content-Disposition: attachment; filename=simulab-my-data-<date>.json`, 422 with `data_export.current_password_invalid`, or 423 with `identity.account_locked`. It requires authentication and no permission (own account only, as F-8 and F-10).
- Error codes: `data_export.current_password_invalid` (new); `identity.account_locked` (reused).

## Acceptance criteria
- AC1 (UC2, BR1, BR3, BR4) Given a signed-in user with the right current password, a consent record and a role change as target, when they `POST /api/v1/identity/data-exports`, then the answer is 200 JSON with `format` `simulab.data-export`, `version` 1, and an `identity` section holding their account, roles, consents with IP address, the role change with `byYou` false, and `sessions.active`.
- AC2 (BR1) Given two users, when user A exports, then nothing of user B appears in the file: not B's consents, B's role changes, or B's id and email as actor.
- AC3 (BR5) Given a user with a password, TOTP turned on and an active session, when they export, then the file contains none of: password hash, security stamp, TOTP secret, token, session id, `access_failed_count`, concurrency stamp.
- AC4 (UC3, BR2) Given a wrong current password, when the user exports, then the answer is 422 `data_export.current_password_invalid`, the access failure count goes up by one, and no email is enqueued.
- AC5 (BR2) Given an account locked out by failed attempts, when the user exports with the right password, then the answer is 423 `identity.account_locked` with `retryAfterSeconds`.
- AC6 (BR2) Given one failed attempt, when the user then exports with the right password, then the export succeeds and the failure count is back to zero.
- AC7 (BR7) Given a successful export, when the request ends, then one `email.send` job is enqueued for the account's address in its preferred language, with the export time in UTC.
- AC8 (BR8, BR6) Given a successful export, when the response is read, then `Content-Disposition` names `simulab-my-data-<UTC date>.json`, and nothing about the export is stored besides the email job.
- AC9 (UC1, UC2, screen) Given the `/account` page, when the user opens the Your data card, types the password and confirms, then the Api is called with that password, the file is handed to the browser, and the snackbar appears. On a wrong password the dialog stays open with the error text; on a lockout it shows the alert with the wait.
- AC10 (unauthenticated) Given no access token, when `POST /api/v1/identity/data-exports` is called, then the answer is 401.
- Localization: the card, the dialog, the snackbar, the error code and the email exist in pt-BR, pt-PT and en, and the missing-key test is green.

## Decisions
- 2026-09-21 - Build now with the account data only - owner; LGPD article 18 and GDPR article 20 already apply to what the app holds, and the format is set before other modules exist.
- 2026-09-21 - One JSON file - owner; GDPR asks for a structured, commonly used, machine-readable format, and one format is one thing to maintain.
- 2026-09-21 - Immediate download from the Api - owner; today the file is a few KB. When study history makes it large, moving to a job plus an emailed link is a change note.
- 2026-09-21 - Confirm with the current password, reusing F-10's lockout behavior - owner; the file gathers everything about the person, so an open session left behind must not be enough.
- 2026-09-21 - Contents: account, consents with IP address, role changes as target, number of active sessions - owner. Security state (2FA on, email verified, lockout) is not a separate section - owner (`email_verified_at` is part of the account).
- 2026-09-21 - Sessions as a count only - owner; the Redis sessions have no creation time, and adding one would change the F-5 session store.
- 2026-09-21 - No download limit - owner; the password and the lockout already protect it, and generating the file is cheap.
- 2026-09-21 - Email notice after each export, through the job queue - owner; like the password-changed notice (F-7), it warns the person if it was not them.
- 2026-09-21 - Current role names are included in `roles` - technical; `user_roles` is data about the user, and it makes the role history readable.
- 2026-09-21 - The role change actor appears only as `byYou` - technical; the actor's id or email is another person's personal data.
- 2026-09-21 - No cross-module abstraction yet (for example an `IPersonalDataSource` per module) - technical; there is one module. The profile says to add structure on the second use; the top-level section per module (BR3) keeps that step small.
- 2026-09-21 - The browser saves the file through Blazor's `DotNetStreamReference` and a small function in the Web's JavaScript - technical; this is built into ASP.NET Core, needs no package, and a Blazor Server circuit cannot hand a response to the browser any other way.
- 2026-09-21 - `IRefreshSessionStore` gains `CountAsync(userId)`, counting the set members whose session key still exists - technical; the set keeps ids of expired sessions until `RevokeAllAsync` runs.
- 2026-09-21 - No new package - technical; the tests use the existing Identity test host with containers and bUnit.
- 2026-09-21 - The app manual page `my-account.md` gains a "Download my data" section in pt-BR, pt-PT and en at ship - technical (definition of done).

## Out of scope
- Study data (attempts, answers, scores, coach conversations): each module adds its section when it exists.
- A download through a job and an emailed link, and file storage.
- A download limit.
- Security state as its own section, and session creation times.
- Exporting another user's data (for example by an administrator on a request).
- ZIP or CSV formats.

## Coverage
| Criterion | Test |
|---|---|
| AC1, AC2, AC8 | `DataExportTests.Export_WithTheRightPassword_ReturnsTheAccountsDataAsAJsonAttachment` |
| AC3 | `DataExportTests.Export_NeverContainsSecretsOrSecurityMaterial` (TOTP on) |
| AC4 | `DataExportTests.Export_WrongPassword_FailsCountsTheFailureAndSendsNothing` |
| AC5 | `DataExportTests.Export_WhileLockedOut_IsRefusedEvenWithTheRightPassword` |
| AC6 | `DataExportTests.Export_AfterAFailedAttempt_SucceedsAndClearsTheFailureCount` |
| AC7, AC8 | `DataExportTests.Export_QueuesOneNoticeInTheAccountsLanguageAndStoresNothingElse` |
| AC9 | `DataExportPageTests` (6 tests) and the validation script |
| AC10 | `DataExportTests.Export_WithoutAToken_IsUnauthorized`; also 401 through the app host |
| Localization | Web missing-key test green (334/334); email texts in the three `IdentityEmails` files |

App-host check (2026-09-22, app host started and stopped by Claude): the route is in the OpenAPI document, an unauthenticated POST answers 401, `/account` redirects to sign-in, and `shell.js` serves `downloadFile`. The signed-in flow was not run by Claude: its rules forbid creating accounts and typing passwords to sign in. It is in the validation script.

## Validation script
1. Start the app: `dotnet run --project src/Hosts/Simulab.AppHost` (Git Bash and PowerShell 7). Open the dashboard link it prints and wait until `web` and `api` are Running, then open `https://localhost:7125`.
2. Sign in with a test account of yours. Open **Minha conta**: a **Seus dados** card sits between the profile and **Apagar minha conta**, listing what the file contains.
3. Click **Baixar meus dados**. The **Baixar** button stays disabled until you type a password. Type a wrong password and confirm: "A senha atual não está correta." shows under the field and the dialog stays open.
4. Type the right password and confirm: the dialog closes, the browser saves `simulab-my-data-<today UTC>.json`, and a green snackbar says the download started.
5. Open the file: `format` is `simulab.data-export`, and `identity` has your email, roles, consents with an `ipAddress`, `roleChanges` and `sessions.active`. Search it for `password`, `stamp`, `totp` and `token`: none of them appears.
6. Open Mailpit (the `mailpit` link on the dashboard): one email "Seus dados foram baixados — Simulab" in your account's language, with the time in UTC.
7. Switch the language to **English** in the app bar, reopen the dialog: the card, the dialog and the button read "Your data", "Download my data", "Download".
8. Keyboard only: Tab to **Download my data**, Enter, type the password, Enter. The file downloads as in step 4.

Validated by the owner, 2026-09-22.

## Delivery
- Branch: `feature/F-16`, merged into `main` with `--no-ff` in `7f0335d` (AB#729).
- Tests: full suite 689 passed, 0 failed, 30 s; build 11 s, 0 warnings (`gate.js ship`, 2026-09-22).
  New: `DataExportTests` (7) and `DataExportPageTests` (6).
- Api: `POST /api/v1/identity/data-exports`, error code `data_export.current_password_invalid`; contracts `DataExport*`, `IdentityDataResponse` and friends.
- Web: the Your data card and `DownloadDataDialog` on `/account`, `simulabShell.downloadFile` in `shell.js`, texts in pt-BR, pt-PT and en.
- Technical docs: `docs/api/Simulab.Api.json` and `docs/architecture/Identity/routes.md` regenerated; `DocGen --check` up to date.
- App manual: `my-account.md` and `index.md` in the three languages.

## Change notes

### v2 — 2026-09-22
- What: a wrong current password answers **422** (not 400) with `data_export.current_password_invalid`; the dialog has the same shape as the erasure dialog instead of being built on `AppConfirmDialog`.
- Why: rule `api-contracts` maps a business rule to 422, and the erasure (F-10) already answers 422 for the same check; the 400 in v1 was a refinement mistake. `AppConfirmDialog` has no password field, so the erasure dialog's shape was reused.
- Affected: AC4, Screens and API. No code change: the build already did this.
- Re-approved by the owner on 2026-09-22.

## Open questions
