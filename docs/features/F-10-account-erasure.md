---
feature: F-10
epic: Foundation and identity
status: done
board: 714
version: 1
---
# Account erasure

Technical terms: [glossary](../glossary.md)

## Summary
A user can ask to erase the account (LGPD/GDPR). Identity anonymizes the personal data and publishes an integration event so other modules can anonymize theirs later; attempts stay for statistics without identity (ADR-0001 #9).

## Goal
A signed-in user can end their relationship with Simulab on their own: the personal data in Identity is overwritten at once, the address becomes usable again, and the account id survives as a pseudonym so future question statistics keep working without naming anybody (ADR-0001 #9).

## What already exists (verified 2026-09-19)
- **Identity is the only module.** `src/Modules/` holds `Identity` alone; Catalog, ExamEngine and Plans are not imported yet. No attempt, no answer, no exam data exists, so "other modules anonymize theirs later" has no consumer today: the event is published for the future, with no subscriber to test against.
- **Personal data in the database**: `users` (email, normalized email, user name, normalized user name, `FullName`, `PreferredLanguage`, password hash, security stamp, phone, lockout, `IsAdultDeclared`), `consent_records` (`IpAddress`, locale, versions, `AcceptedAt`), `email_verification_tokens`, `password_reset_tokens`, the OpenIddict tables (`openiddict_tokens`, `openiddict_authorizations`) and `user_roles`/`user_claims`/`user_logins`. Sessions and revocations live in Redis (`IRefreshSessionStore`).
- **Soft delete already works on `User`**: `IsDeleted`, `DeletedAt`, `DeletedBy` set by `AuditAndSoftDeleteInterceptor`, plus a global `soft_delete` query filter on `User` in `IdentityModuleDbContext`. A soft-deleted user is invisible to `UserManager`, so sign-in, password reset and the admin list stop finding it with no extra code.
- **False premise in the code, to fix here**: `UserDirectory` says "the soft-delete filter is not [ignored], so an erased account frees its address". It does not. `ux_users_tenant_normalized_email` is unique regardless of `IsDeleted`, so the row keeps the address reserved until the email column itself is replaced. Anonymization has to overwrite `Email`, `NormalizedEmail`, `UserName` and `NormalizedUserName`, not only flip `IsDeleted`.
- **Integration events**: `IIntegrationEvent` / `IIntegrationEventPublisher` / `InProcessIntegrationEventPublisher` exist in `SharedKernel.Messaging`, registered by `AddIntegrationEvents()`. Zero events and zero consumers exist so far: `UserErased` would be the first one, and publishing is synchronous in the caller's scope (a consumer failure reaches the caller).
- **No job queue and no scheduler.** No `BackgroundService`, no job table (ADR-0001 #20 plans it; F-13 waits for it). A grace period with automatic execution has nothing to run it today.
- **Re-authentication pattern exists**: `ChangePasswordHandler` (F-7) checks the current password through `UserManager.CheckPasswordAsync`, applies the same lockout as sign-in and ends the other sessions (`RevokeAllAsync`). The same shape fits "confirm your password to erase".
- **`AccountStatus`** has only `Pending` and `Active`; F-4 left blocking and suspension to the feature that needs them.
- **Last-manager guard**: `SetUserRolesHandler` refuses a change that leaves `CountActiveManagersAsync() == 0` (`role_assignment.last_manager`). Erasing the last user who can manage roles is the same hole, and nothing checks it today.
- **Consent records are declared write-once**: `ConsentRecord` says "written once at sign-up and never updated or deleted: it is the evidence LGPD and GDPR ask for" — it carries `IpAddress`, which is personal data.
- **Screens**: `/account` (F-8) is the user's own page, `MudPaper` form card, kit components only, with a link to `/account/password`. `/admin/users` (F-9) lists every account behind `identity.roles.manage`; it has no deactivate or delete action. `AppConfirmDialog` exists in the kit.
- **Simulae has no erasure code**: a grep for anonymize/erasure/LGPD finds only the consent records and the privacy text. Nothing to import; this is new code.
- **Emails** go out inside the request through `IEmailSender` (Mailpit in dev), with the templates in `IdentityEmails` in the user's `PreferredLanguage`.

## Users and use cases
- UC1 A signed-in user opens My account, reads what erasing the account does, and asks to erase it.
- UC2 The user confirms with the current password; the account is erased at once and every session of that user ends, including this browser's, which lands on the sign-in page with a farewell message.
- UC3 The user types the wrong current password and the account is untouched; repeated attempts lock the account the same way a sign-in does.
- UC4 The user who just erased the account signs up again with the same email address and gets a new, empty account.
- UC5 An erased account cannot sign in, cannot reset the password, is not found by the back-office user list and holds no roles.
- UC6 The last user able to manage roles asks to erase the account and is refused, with a message saying why.
- UC7 The erased address receives one last email, in the account's language, saying what was erased and what was kept.

## Business rules
- BR1 A user erases only their own account. The account is the token subject; no id is taken from the route or the body.
- BR2 The erasure is confirmed with the current password. A wrong password fails with `account_erasure.current_password_invalid` and counts as an access failure; while the account is locked out the request is refused with `identity.account_locked` and the remaining seconds, exactly as a password change (F-7 BR8).
- BR3 Erasure is immediate and cannot be undone. There is no grace period and no cancellation (no job table exists yet, ADR-0001 #20).
- BR4 Erasing overwrites in the `users` row, in one transaction: `Email`, `NormalizedEmail`, `UserName` and `NormalizedUserName` with the tombstone address, `FullName`, `PhoneNumber`, `PasswordHash` and `SecurityStamp` (renewed, not blank) to null or a fresh value, `LockoutEnd` to null, `EmailConfirmed` to false, `IsAdultDeclared` to false. `Id`, `CreatedAt` and `PreferredLanguage` stay: the id is the pseudonym, the language is not personal data.
- BR5 The tombstone address is `erased-<user id, 32 hex digits>@erased.invalid` (`.invalid` is reserved by RFC 2606, so it can never be a real address). It is unique per account, so it never collides with another erased row under `ux_users_tenant_normalized_email`.
- BR6 After the overwrite the row is soft deleted (`IsDeleted`, `DeletedAt`, `DeletedBy` by the interceptor) and `Status` becomes `Erased`. Every existing query stops finding it through the `soft_delete` filter: sign-in, password reset, resend verification, the profile and the back-office user list.
- BR7 The real address becomes free: a new sign-up with it creates a new account with a new id, unconnected to the erased one.
- BR8 Erasing also removes, in the same transaction, the rows that only exist to serve that account: `email_verification_tokens`, `password_reset_tokens`, `user_roles`, `user_claims`, `user_logins`, `user_tokens`, and the OpenIddict tokens and authorizations whose subject is that account.
- BR9 `consent_records` keep their rows — they are the evidence LGPD and GDPR ask for — with `IpAddress` set to null. Nothing else in them changes.
- BR10 Every session of the account ends: each refresh session is removed and each access token is revoked (`RevokeAllAsync` with no exception), so other devices stop at their next call.
- BR11 The erasure is refused with `account_erasure.last_manager` when the account is the last one able to manage roles, judged inside the same transaction after the change, exactly as F-9 BR8b does for role assignment. Nothing is erased.
- BR12 One email goes to the real address before it is overwritten, in the account's `PreferredLanguage`, saying the account was erased, what was kept (unidentified records and the consent evidence) and that a new account can be created with the same address. A mail failure is logged and does not undo the erasure (same rule as the password-changed notice, F-7).
- BR13 The integration event `UserErased(UserId, ErasedAt)` is published after the transaction commits, so a future module anonymizes its own data. No consumer exists today; publication itself is the tested behavior.
- BR14 A second erasure of the same account is impossible: the account is gone from every lookup after the first one.

## Screens and API
- `/account` (F-8) gains a **danger zone** at the bottom of the page, below the profile card: its own `MudPaper` card with an error-toned heading, one paragraph saying what erasure does and that it cannot be undone, and a single destructive button "Erase my account".
  - The button opens a dialog built on `AppConfirmDialog`: the same explanation, an `AppPasswordField` for the current password, "Cancel" and "Erase my account" (destructive). The confirm button is disabled while the password field is empty and while the call runs.
  - States: submitting (button disabled with progress), wrong password (error text under the field, translated from the error code), locked out (`AppAlert` with the wait), last manager (`AppAlert` with `account_erasure.last_manager`), unexpected failure (`AppAlert` with Try again). The dialog stays open on every failure.
  - Success: the page does a full navigation to `/account/sign-out?reason=account-erased`, which clears the Web session and the auth cookie without calling the Api (the session is already revoked) and lands on `/sign-in` with a farewell alert.
- `POST /api/v1/identity/account-erasures` — `EraseAccountRequest(CurrentPassword)`; 204 on success, 400 with an error code, 429-style lockout answer as F-7. Requires authentication; no permission (own account only, as F-8).
- `GET /account/sign-out?reason=account-erased` — existing Web endpoint, new reason: it skips the Api sign-out call (like `session-ended`) and redirects to `/sign-in` with the farewell alert.
- Error codes: `account_erasure.current_password_invalid`, `account_erasure.last_manager`. Reused: `identity.account_locked`.
- Event: `UserErased(Guid UserId, DateTimeOffset ErasedAt)` in `Simulab.Identity.Contracts`.

## Acceptance criteria
- AC1 Given a signed-in user with the right current password, when they `POST /api/v1/identity/account-erasures`, then the answer is 204 and their `users` row has the tombstone email, no full name, no password hash, `EmailConfirmed` false and `Status` `Erased`, and `IsDeleted` is true. (UC2, BR1, BR4, BR6)
- AC2 Given an erased account, when someone tries to sign in with the old address and password, then it fails with `identity.invalid_credentials`. (UC5, BR4, BR6)
- AC3 Given an erased account, when a password reset is requested for the old address, then no reset email is sent and the answer is the same 202 as for an unknown address. (UC5, BR6)
- AC4 Given an erased account, when an admin lists users in the back office, then the account is not in the list and its roles are gone. (UC5, BR6, BR8)
- AC5 Given an erased account, when someone signs up with the same email address, then a new account is created with a different id and the sign-up succeeds. (UC4, BR5, BR7)
- AC6 Given a signed-in user, when they post an erasure with the wrong current password, then the answer is 400 `account_erasure.current_password_invalid`, the account is untouched and the access-failure count went up; after the configured failures the answer is `identity.account_locked` with the remaining seconds. (UC3, BR2)
- AC7 Given a user signed in on two devices, when the account is erased, then both sessions are gone from the session store and an access token of either is refused. (UC2, BR10)
- AC8 Given an erased account, then its `email_verification_tokens`, `password_reset_tokens`, `user_roles` and OpenIddict tokens and authorizations are gone, while its `consent_records` rows are still there with the same versions and `AcceptedAt` and with `IpAddress` null. (BR8, BR9)
- AC9 Given the only user able to manage roles, when they post an erasure with the right password, then the answer is 400 `account_erasure.last_manager` and the account, its roles and its sessions are untouched. (UC6, BR11)
- AC10 Given a user whose preferred language is pt-PT, when the account is erased, then one email is sent to the old real address written in pt-PT; and given a mail failure, the erasure still succeeds with 204. (UC7, BR12)
- AC11 Given a successful erasure, then a `UserErased` event carrying the user id and the erasure instant is published exactly once, after the data changed. (BR13)
- AC12 Given `/account`, then the danger zone is shown, the button opens the dialog, the dialog refuses to confirm while the password is empty, a wrong password shows the translated message with the dialog still open, and a success navigates to `/account/sign-out?reason=account-erased`. (Screens)
- AC13 Given the erasure farewell, when the browser lands on `/sign-in`, then it is signed out (the auth cookie is gone) and the farewell alert is shown. (UC2, Screens)
- AC14 All new texts (danger zone, dialog, farewell alert, email template, errors `account_erasure.*`, the `Erased` status label) appear in pt-BR, pt-PT and en, and the missing-key test is green.

## Decisions
- 2026-09-19 — Self-service only, immediate, no grace period — owner's choice; there is no job table or scheduler to run a delayed erasure (ADR-0001 #20, F-13), and an admin erasing someone else's account is a different feature with different evidence needs.
- 2026-09-19 — Anonymize in place and free the address, instead of deleting the row — owner's choice; ADR-0001 #9 keeps the id as the pseudonym that future unidentified attempts hang from, and a person who leaves must be able to come back with the same email.
- 2026-09-19 — `consent_records` keep the row with `IpAddress` cleared — owner's choice; the proof that the terms were accepted is what the law asks to keep, the IP is the personal part of it.
- 2026-09-19 — The last user able to manage roles cannot erase the account (`account_erasure.last_manager`) — owner's choice; the same hole F-9 BR8b already closes for role assignment.
- 2026-09-19 — Confirmation with the current password in a dialog on `/account`, in a danger zone — owner's choice; the F-7 check already exists with its lockout, and a destructive action needs more than a click.
- 2026-09-19 — `AccountStatus` gains `Erased` — owner's choice; a query that ignores the soft-delete filter sees at once what the row is.
- 2026-09-19 — One farewell email before the address is overwritten — owner's choice; without it an unwanted erasure passes unnoticed.
- 2026-09-19 — `UserErased` is published now, with no consumer — owner's choice; it fulfills ADR-0001 #9 and the next module only subscribes.
- 2026-09-19 — Data export (portability) is out of scope and becomes an idea — owner's choice; today the user's data is email, name and language.
- 2026-09-19 — No new packages, for code or for tests (rule `build-config`: the owner's yes given here).
- 2026-09-19 — The rules that belong to the account live in `User` (`Erase(...)`, returning `Result`), and the ones that need other rows (tokens, roles, consent, sessions, event) live in `EraseAccountHandler` in `Simulab.Identity.Application/Account/` — the profile's rule for where a rule lives.
- 2026-09-19 — The tombstone uses the reserved TLD `.invalid` (RFC 2606) with the account id, so it is unique and can never reach a real mailbox.
- 2026-09-19 — The farewell email goes through a new `IErasureMailer` next to `IPasswordMailer` and `IVerificationMailer`, with its template in `IdentityEmails` in the three languages — same shape as the mailers that already exist.
- 2026-09-19 — The Web reuses `/account/sign-out` with a new `reason`, instead of a new endpoint — the cookie and the stored Web session are already cleared there, and a Blazor circuit cannot clear a cookie (F-8).
- 2026-09-19 — Found and to fix here: the comment in `UserDirectory` claiming that soft delete frees the address is wrong; it is corrected by BR4 and by the comment itself.
- 2026-09-20 — Approved by the owner.
- 2026-09-20 (build) — BR11 refined: the erasure is refused only when there was an active manager **before** it and none is left after. As first written it also refused an ordinary account in a system with no manager at all (a fresh database), which blocked every erasure. AC9 is unchanged.
- 2026-09-20 (build) — The erasure runs inside `IRoleAdministrationStore.RunExclusiveAsync`, reusing F-9's advisory lock instead of taking a second one — the last-manager count has to see role changes and erasures in one order, and both now take the same lock.
- 2026-09-20 (build) — The consent records lose their IP through one `ExecuteUpdateAsync` in the store, not through a domain method: the record is written once everywhere else and has no other reason to be loaded. `IpAddress` stays `init`-only.
- 2026-09-20 (build) — A refresh token of an erased account is refused by OpenIddict with `invalid_grant`, not by our own `identity.refresh_token_invalid`: BR8 deletes the stored protocol token, so the grant fails before the module's session check. Asserted as such in AC7's test.
- 2026-09-20 (build) — The Web gets its own `AccountErasureResult` instead of reusing `PasswordChangeResult`: the two answers have the same shape but one name each, so neither lies about its call.
- 2026-09-20 (build) — Found on screen and fixed here: the dark palette had kept the light error red, so every error text on a dark card read at 2.93:1, and the light `ErrorContrastText` read at 1.92:1 on a filled destructive button — both below WCAG AA (ADR-0001 #29). Dark `Error` is now `#E06C6C` with `#19243E` ink (5.05:1 and 4.79:1), light keeps its red with white ink (5.54:1), and `ThemeContrastTests` holds the numbers. Captured, not fixed: the success and warning contrast texts have the same flaw, invisible today because no filled success or warning button exists.
- 2026-09-20 — Validated on screen by the owner, who followed the validation script and said it passed.
- 2026-09-20 (build) — App host check: the whole path was exercised on the running app (wrong password reaching the Api and raising the failure count, erasure anonymizing the row to the tombstone, consent kept with a null IP, farewell email in Mailpit carrying the app host's sign-up URL, and the same address signing up again).

## Out of scope
- An admin erasing someone else's account.
- A grace period, a scheduled erasure and cancelling an erasure request (needs the job table, F-13 / ADR-0001 #20).
- Anonymizing data in other modules: none exist; each one subscribes to `UserErased` when it arrives.
- A back-office screen listing erased accounts.
- Exporting the user's data before erasure (portability) — captured as F-16 (AB#729).
- Deleting the audit fields (`CreatedBy`, `UpdatedBy`, `DeletedBy`) that point to the erased id elsewhere: the id is already a pseudonym.

## Open questions
<!-- Approval is blocked while any line here is not answered or marked "deferred (owner, YYYY-MM-DD)". -->
- (none)

## Change notes
<!-- Added by /agile:change during build. Increase `version` in the header. -->
### F-59 - 2026-10-06
- What: BR13 and AC11 - `UserErased` is published by the `account.erased` job, staged in the erasure transaction, after the commit; it is no longer published inline by the handler.
- Why: a crash between the commit and the inline publish lost the event; the job is retried by the F-13 policy (F-59).
- Affected: BR13, AC11 (the event arrives when the worker runs the job, at least once). BR10 and AC7 unchanged: the handler still revokes every session before the 204.
- Re-approved: 2026-10-04 (owner, with F-59)
<!--
### v2 — YYYY-MM-DD
- What: <change>
- Why: <reason>
- Affected: <BR/AC ids>; other criteria unchanged.
- Re-approved: <YYYY-MM-DD>
-->

## Validation script
1. Close any IDE build and start the app host. Git Bash and PowerShell 7 take the same command; it prints the dashboard address and keeps running.
   ```
   dotnet run --project src/Hosts/Simulab.AppHost
   ```
   → The Aspire dashboard answers and the Web is at https://localhost:7125.
2. Create an account at `/sign-up` with an address you do not use elsewhere, open Mailpit from the dashboard, click the verification link and sign in. → You are signed in.
3. Open the user menu and click "My account", then look at the bottom of the page. → Below the profile card there is a red-bordered "Erase my account" section saying what is erased, what is kept and that the address becomes free.
4. Click "Erase my account". → A dialog asks for your current password; its "Erase my account" button is disabled while the field is empty.
5. Type a wrong password and confirm. → The dialog stays open with "The current password is not correct." under the field. Cancel the dialog. → Nothing was erased; you are still signed in.
6. Switch to the dark theme (the header switch) and reopen the dialog; then switch the language to Português (Brasil) and reopen it again. → The section and the dialog are readable in both themes, and every text is in the chosen language.
7. Open the dialog, type the right password and confirm. → You land on the sign-in page, signed out, with "Sua conta foi apagada..." (in the language you chose). Trying to sign in with that address and password fails.
8. Keyboard only, from `/account`: Tab to "Erase my account" and press Enter, Tab to the password field, type the password, then Tab to "Erase my account" in the dialog and press Enter (or press Enter in the password field). → Every element gets a visible focus ring and the erasure works from the keyboard. Then in Mailpit, open the last message to the erased address, and finally sign up again with that same address. → The farewell email is in your language and points at sign-up; the new sign-up is accepted.

## Delivery
- Branch: feature/F-10 (removed after the merge)
- Merge: 903911d (--no-ff, AB#714)
- Validation: passed by the owner on screen, 2026-09-20
- Tests: full suite 514 passed, 0 failed, 28 s; full build 14 s, 0 warnings, baseline empty
- Manual pages: docs/manual/{pt-BR,pt-PT,en}/erase-account.md (new); my-account.md and index.md (updated); docs/infra.md (Identity:SignUpUrl, measured times)
- Captured during refinement: F-16 Download my data (AB#729). Captured during build: the success and warning contrast texts of the theme, same flaw as the error one, invisible today
