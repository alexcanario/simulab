---
feature: F-47
epic: Foundation and identity
status: done
board: 22
version: 1
---
# Every Identity handler that writes twice commits once

Technical terms: [glossary](../glossary.md)

## Summary
F-30 puts the two sign-ups in one transaction. They are not alone: other paths of `Simulab.Identity.Application`
write more than once in a request, each write its own committed transaction, so a failure between two of them
leaves the account in a state no rule describes. `ResetPasswordHandler` was read and confirmed while refining
F-30 — it consumes the token, resets the password, resets the failure count, clears the lockout and updates the
user, in separate commits, so a crash after the third leaves a consumed reset token and a locked-out account with
the new password already set. Go through every multi-write path with the transaction F-30 introduced, and decide
each one: wrap it, or write down why its order is already safe. Raised while refining F-30 (2026-09-26).

## Start
- Depends on: F-30 — done; it introduced `IIdentityUnitOfWork.BeginAsync` and the failure-injection test pattern
  (`SignUpTransactionTests`).
- Waits on: nothing.
- Needed to validate: nothing beyond the app host; the change has no screen of its own.
- Suggested path: `/agile:refine` → `/agile:build`. It touches authentication, so the independent review runs on
  the file (before approval) and on the diff.
- Parallel with: anything outside `Simulab.Identity`. F-42, F-44 and F-46 change nothing under
  `src/Modules/Identity` or `tests/Modules/Identity` (checked 2026-10-01 with `git diff --stat main...<branch>`).

## What exists (verified 2026-10-01, on `main` at `8d9bfc9`)
Since F-30 captured this item, the code it lists was touched by F-30 itself (sign-ups wrapped) and F-38 (the
per-address limit in the token endpoint); neither wrapped any of the paths below.

- `IIdentityUnitOfWork.BeginAsync` opens a transaction on `IdentityModuleDbContext`; disposal without
  `CommitAsync` rolls back and clears the change tracker (`IdentityTransaction`). `UserManager<User>` writes through
  `UserStore<..., IdentityModuleDbContext, ...>`, the same scoped context, so its saves join that transaction. Used
  today by `RegisterUserHandler` and `RegisterGoogleUserHandler` only.
- `RoleAdministrationStore.RunExclusiveAsync` opens its own transaction plus an advisory lock. Used by
  `SaveRoleHandler`, `SetUserRolesHandler`, `DeleteRoleHandler` and `EraseAccountHandler`.
- Refresh sessions (`RedisRefreshSessionStore`) and TOTP challenges (`RedisTotpChallengeStore`) live in Redis, outside
  any Postgres transaction.
- `AccountEventLog` saves each event on its own and swallows a failure (F-21 BR7); its comment says the account
  handlers have no transaction and what an event records is already committed.
- `PasswordNotice.EnqueueAsync` stages the "password changed" job and saves it with
  `IIdentityUnitOfWork.SaveChangesAsync`. In both password handlers it runs today after the Redis session calls.
- ASP.NET Core Identity (`UserManager.AccessFailedAsync`, release/10.0 source, read 2026-10-01): the failure that
  reaches the limit sets the lockout end **and resets the count to 0** in the same save, and
  `ResetAccessFailedCountAsync` does not save when the count is already 0. Every path here checks
  `IsLockedOutAsync` before it counts a failure, so while a lockout is active the count is 0.
- `SecondFactor.VerifyAsync` writes on success: a recovery code is redeemed (`SetAuthenticationTokenAsync`, one
  save), the count is reset (a save only when above 0) and the user is updated (the spent TOTP step, one save); on
  failure, `AccessFailedAsync`. All of it happens before any of today's handlers write anything else.
- F-30 BR9 and the `BeginAsync` comment say no outbound call happens while the transaction is open.

### Premises in the summary that were wrong
- "13 handlers": `SaveRoleHandler` (and `SetUserRolesHandler`, `DeleteRoleHandler`) and `EraseAccountHandler`
  already run inside `RunExclusiveAsync`. `ProfileHandler` writes once; its second `UpdateAsync` is a retry of the
  same write after a concurrency failure, not a second write.
- "the verify-email path" is `VerifyEmailHandler`; "TOTP" is three paths of `TotpAccountHandler` plus the
  `SecondFactor` helper, only one of them harmful; "GoogleSignInHandler" is `ClearFailuresAsync` only, called by the
  Google grant in `TokenEndpoints`, and it is not harmful either (see its row).

### Every multi-write path, and what a crash between two writes leaves
| Path | Writes today, in order | State after a crash in the middle | Verdict |
|---|---|---|---|
| `ResetPasswordHandler.HandleAsync` | consume link; reset password; reset failure count; clear lockout; (pending) activate + consume verification links; revoke sessions (Redis); stage+save notice; event | link spent, password set, account still locked or pending, no notice | wrap (BR1) |
| `ChangePasswordHandler.HandleAsync` (success path) | change password; reset failure count; revoke other sessions + restamp caller (Redis); stage+save notice; event | password changed with no "password changed" email to the owner | wrap (BR1) |
| `RequestPasswordResetHandler.HandleAsync` | consume older links; new link + mail job; event | every older link dead and no new one, while the throttle still counts the dead ones | wrap (BR1) |
| `ResendVerificationHandler.HandleAsync` | consume older links; new link + mail job | same as above, for verification | wrap (BR1) |
| `TotpAccountHandler.ConfirmAsync` | code check (failure count, last step); enable two-factor; store recovery codes; event | two-factor on with no recovery codes: a lost phone is a lost account | wrap (BR1) |
| `GoogleSignInHandler.ClearFailuresAsync` | reset failure count (only when above 0); clear lockout | while a lockout is active the count is 0, so only the lockout write happens; with a count above 0 any lockout end is already past, so a crash leaves a harmless past date | safe (BR7) |
| `SecondFactor.VerifyAsync` (inside every TOTP path) | recovery code: redeem; reset count; save spent step | one of ten recovery codes spent with nothing else done; the other nine still work. TOTP code: the step stays spent and the next code works | safe (BR7) |
| `VerifyEmailHandler.HandleAsync` | activate account; consume link | account active, link not consumed — and F-4 BR10 already answers "verified" for any link of an active account | safe (BR7) |
| `TotpAccountHandler.DisableAsync` | password and code checks; disable two-factor; remove recovery codes; event | two-factor off with codes left; codes are only read by the code step, which requires two-factor on, and are replaced when it is turned on again | safe (BR7) |
| `TotpAccountHandler.StartAsync` | one write | — | safe (BR7) |
| `TotpAccountHandler.RegenerateRecoveryCodesAsync` | code check (`SecondFactor`); replace codes; event | a recovery code used to prove it is spent and the old set stays: the other old codes still work | safe (BR7) |
| `ExportDataHandler.HandleAsync` | password check; reset failure count; best-effort notice | count reset, no notice; the export is a read and F-16 made the notice best-effort | safe (BR7) |
| `GoogleLinkHandler.LinkAsync`, `UnlinkAsync` | password check (unlink); one write; event | — | safe (BR7) |
| `ProfileHandler.SaveAsync` | one write (retried once on a concurrency failure) | — | safe (BR7) |
| `TotpSignInHandler.CompleteAsync` | spend challenge (Redis); code check (`SecondFactor`); event | as `SecondFactor`: a spent code and no tokens; the visitor signs in again | safe (BR7) |
| password grant in `TokenEndpoints` | one write (failure count); events; session (Redis) | — | safe (BR7) |
| `RegisterUserHandler`, `RegisterGoogleUserHandler` | — | wrapped by F-30 | done |
| `SaveRoleHandler`, `SetUserRolesHandler`, `DeleteRoleHandler`, `EraseAccountHandler` | — | inside `RunExclusiveAsync` | done |

## Goal
A password reset, a password change, a new reset or verification link and turning two-factor on either happen whole
or not at all, so no account is left in a state no rule describes.

## Users and use cases
- UC1 A visitor resets the password with the emailed link; if the server fails mid-request, the link still works,
  the old password still works, and nothing else about the account changed.
- UC2 A signed-in user changes the password; if the server fails mid-request, the old password still works and no
  "password changed" email leaves.
- UC3 A visitor asks for a new reset or verification link; if the server fails mid-request, the links they already
  had still work.
- UC4 A user confirms two-factor; if the server fails mid-request, two-factor stays off and they can confirm again
  with the next code.

## Business rules
- BR1 These five paths commit every Postgres write of their success path after the checks in one transaction,
  opened with `IIdentityUnitOfWork.BeginAsync`: `ResetPasswordHandler.HandleAsync`,
  `ChangePasswordHandler.HandleAsync`, `RequestPasswordResetHandler.HandleAsync`,
  `ResendVerificationHandler.HandleAsync`, `TotpAccountHandler.ConfirmAsync`. A failure before the commit leaves
  every Postgres row those writes touch as it was before them.
- BR1b Inside the transaction, an `IdentityResult` that reports a failure is not ignored: the handler throws, so the
  transaction rolls back (as F-30 BR4 did for `AddToRoleAsync`). Today `TotpAccountHandler.ConfirmAsync` ignores
  the result of `UpdateAsync` and of `RecoveryCodes.ReplaceAsync`, so a concurrency failure answers success with
  codes that were never stored.
- BR2 The transaction opens only after every check: input validation, lookups, the current password, the two-factor
  code and the new-password rules. What a check itself writes — a wrong password or code counting towards the
  lockout (`AccessFailedAsync`), the TOTP time step a right code spends — is committed on its own, before the
  transaction, so a later rollback never gives an attacker a free attempt. The F-7 BR2 link consumption is not a
  check: it is the first write inside the transaction.
- BR3 Session writes in Redis (revoke every other session, restamp the caller's) happen inside the transaction's
  scope, after the last Postgres write and before the commit; the notice save (`PasswordNotice.EnqueueAsync`) moves
  before them. A commit that then fails leaves the account's owner signed out with the old password still valid —
  never a session the change was meant to end still alive. This is the one exception to F-30 BR9 ("no outbound call
  with the transaction open"): Redis session writes, on these two password paths only.
- BR4 Account events (`IAccountEventLog`) are recorded after the commit, never inside the transaction, as today
  (F-21 BR7: the trail never breaks what it records). An event that records a check's own write (the lockout an
  attempt caused, F-21 BR5) stays where it is today, next to that write.
- BR5 A failure inside the transaction answers as an unhandled error does today: the host's existing problem-details
  500. No new error code, no new text, no screen.
- BR6 What each path does on success, and every existing answer and error code, is unchanged; two concurrent resets
  with the same link still let exactly one succeed (F-7 BR2).
- BR7 The other multi-write paths stay as they are, each for the reason written in "Every multi-write path" above;
  the reasons are copied as one comment line on each of those handlers, so the next reader does not reopen the
  question.
- BR8 The class comment of `AccountEventLog` and the comment of `IIdentityUnitOfWork.BeginAsync` say which handlers
  now hold a transaction (they say "no transaction of their own" and "sign-up handlers" today), and the `BeginAsync`
  comment names the BR3 exception for Redis session writes.

## Screens and API
- No screen, route, endpoint, error code or migration changes. No new UI text.

## Acceptance criteria
- AC1 Given a valid reset link for a locked-out pending account, when the request fails after the password is reset
  and before the commit, then the link is not consumed, the stored password hash, the
  failure count, lockout end and pending status are unchanged, no notice job exists, and the same link then resets
  the password. (BR1, BR6)
- AC2 Given a signed-in user with two sessions, when a password change fails after the password is changed and
  before the sessions are touched, then the old password still works, the failure count is unchanged, no "password
  changed" job exists and the other session still refreshes. (BR1, BR3)
- AC3 Given a user with two sessions, when a password change or a reset fails after the sessions were revoked and
  before the commit (a session-store double that calls the real store, then throws), then the other session is
  revoked and the password is the old one. (BR3)
- AC4 Given a pending account with one valid reset link, when a new reset request fails after the older links are
  consumed and before the new link is saved, then the older link still resets the password and no new link or job
  exists. (BR1)
- AC5 Given a pending account with one valid verification link, when a resend fails after the older links are
  consumed and before the new link is saved, then the older link still verifies the account and no new link or job
  exists. (BR1)
- AC6 Given a user enrolling two-factor with a right code, when the confirmation fails after two-factor is enabled
  and before the recovery codes are stored, then two-factor is off and no recovery codes are stored. (BR1)
- AC7 Given a user enrolling two-factor, when a right code is accepted and the confirmation then fails after
  two-factor is enabled, then sending the same code again is refused (its step stays spent) and the next code
  confirms. (BR2)
- AC8 Given a user enrolling two-factor, when the store reports a failed `IdentityResult` for the recovery codes, then
  the request does not answer success and two-factor is off. (BR1b)
- AC9 Given any of the five paths, when it succeeds, then its existing tests stay green unchanged — among them a
  wrong code or wrong current password still adds one to the failure count — and two concurrent resets with the same
  link still produce exactly one success. (BR2, BR6)
- AC10 Given an account event logged on a wrapped path, when the request succeeds, then the event is written after
  the commit; when the request fails before the commit, then no event of that path is written. (BR4)
- AC11 Given the request fails inside a wrapped path, when the client reads the answer, then it is the host's
  existing 500 problem details. (BR5)
- AC12 No UI text is added or changed, so the missing-key test stays green in pt-BR, pt-PT and en.

## Decisions
- 2026-10-01 — only the harmful paths are wrapped; the others are declared safe with their reason (owner) — less
  code and fewer failure tests, and each exception is written down (BR7). The owner chose this with six paths on the
  list; the review below moved `GoogleSignInHandler.ClearFailuresAsync` to safe, so the same rule now gives five.
- 2026-10-01 — the lockout counter survives a rollback: the transaction opens after every check (owner) — a counter
  that rolled back with the rest would give an attacker a free attempt per failed request. Same rule as F-30 BR9.
- 2026-10-01 — Redis session writes go before the commit (owner) — a failed commit then signs the owner out, which
  is harmless; after the commit, a crash would keep alive the session a password change exists to end.
- 2026-10-01 — the erasure's after-commit steps (revoke sessions, publish `UserErased`, no outbox) stay out and go
  to their own item (owner) — it is cross-module messaging, not a local transaction. Captured as F-59
  (`docs/features/F-59-erasure-after-commit-steps.md`, issue #98).
- 2026-10-01 — account events stay after the commit (Claude) — an event records what is already committed (F-21
  BR7); recorded inside, a rolled-back request would leave an event for something that never happened.
- 2026-10-01 — the handlers keep the `BeginAsync` / `CommitAsync` / dispose-to-roll-back shape of F-30, no new
  abstraction (Claude) — the same shape is already reviewed and tested. `RequestPasswordResetHandler`,
  `ResendVerificationHandler` and `TotpAccountHandler` gain an `IIdentityUnitOfWork` constructor parameter;
  `ResetPasswordHandler` and `ChangePasswordHandler` already have one.
- 2026-10-01 — failures are injected as in F-30: a double of a store or mailer the handler already takes, made to
  throw (reset/verification token stores, `IPasswordMailer`; for AC3 an `IRefreshSessionStore` double that calls the
  real store and then throws), and, where only `UserManager` writes (AC6, AC8), a test subclass of
  `UserStore<User, Role, IdentityModuleDbContext, Guid>` that overrides `SetTokenAsync` to throw or to return a
  failed result — a subclass, so every store interface `UserManager` probes stays implemented (Claude) — never by
  breaking the database; everything runs on the Identity tests' Postgres container.
- 2026-10-01 — no new package (Claude).

### Independent review of this file (2026-10-01, before approval)
The `reviewer` agent read the file against the code; every major was checked here before being accepted.
- major, confirmed — `ClearFailuresAsync` cannot leave the harmful state: `AccessFailedAsync` resets the count when
  it locks, and `ResetAccessFailedCountAsync` skips the save at 0 (verified in the Identity release/10.0 source).
  Moved to safe; its use case and criterion removed.
- major, confirmed — AC7 described a case that cannot happen (a wrong code returns before any transaction) and
  missed the real one: a right code whose spent step must survive a rollback. Rewritten; the wrong-code count moved
  into AC9.
- major, confirmed — BR3 contradicted F-30 BR9 and the `BeginAsync` comment without saying so. BR3 now names the
  exception and BR8 updates the comment.
- major, confirmed — `SecondFactor.VerifyAsync` is a multi-write helper missing from the table, and three "one
  write" cells were wrong. Row added, cells fixed; the verdicts stay safe.
- minor, accepted — the "a swallowed insert aborts the transaction" premise is likely false (EF Core takes a
  savepoint inside a user transaction). The decision now rests on F-21 BR7 alone.
- minor, accepted — BR3 needs the notice save moved before the Redis calls; said in BR3, and AC2 now checks the
  other session still refreshes.
- minor, accepted — AC3 could not be built with a double that only throws; reworded with a delegating double.
- minor, accepted — the constructor premise was wrong and a plain `IUserStore` decorator would lose features; fixed
  above. The ignored `IdentityResult` in `ConfirmAsync` is now BR1b and AC8.
- Not checked by the reviewer: savepoint and rollback-after-failed-commit behaviour under Npgsql at runtime; whether
  a 500 from the `/connect/token` passthrough is problem details (AC11 covers the account endpoints, which do not go
  through it); the architecture tests.

## Out of scope
- The erasure's after-commit steps — F-59.
- The paths declared safe in BR7, beyond their one comment line.
- An outbox or retry for the Redis writes.

## Open questions
- (none)

## Change notes

### Build decisions and independent review of the diff (2026-10-02)
- `RecoveryCodes.ReplaceAsync` now throws on a failed `IdentityResult` (BR1b). `RegenerateRecoveryCodesAsync`
  shares it, so a failed store there answers 500 instead of success with codes that were never stored; accepted as
  the same defect, said here because BR7 otherwise leaves that path as is.
- `IdentityResultExtensions.ThrowIfFailed` (internal, `Security/`) is the one way the five paths throw on a failed
  result; `ResetPasswordHandler` uses it for its existing check too.
- Review, minor, fixed — the password grant in `TokenEndpoints` lacked its BR7 comment line; added.
- Review, minor, fixed — the exception text said "inside a transaction" on a path that has none; now neutral.
- Review, minor, fixed — `ResetPasswordHandler` threw by hand beside `ThrowIfFailed`; one way now.
- Review, no blocker or major. Concurrent resets with the same link: the conditional update inside the transaction
  blocks the second request on the row lock and updates 0 rows after the first commits, so exactly one succeeds.
- No app-host wiring changed (no connection string, resource or URL), so there is no app-host check beyond the
  validation script below.

## Validation script
Needed to validate: nothing beyond the app host. The change has no screen; the failure cases are proven by tests,
so the script checks that the normal flows still work through the real app host.
1. Start: `dotnet run --project src/Hosts/Simulab.AppHost`; open the Web at https://localhost:7125 and Mailpit from
   the dashboard.
2. Sign up at `/sign-up`, then verify with the email link and sign in at `/sign-in`.
3. Sign out, open `/forgot-password`, ask for a link twice (the second after a minute): only the newest link works.
4. Use it at the reset page with a new password: you can sign in with it, not with the old one, and a "password
   changed" email arrives in Mailpit. Opening the same link again says it is invalid.
5. Signed in on two browsers, change the password on the Security page: the other browser is signed out, this one
   stays, and the notice email arrives.
6. On the Security page turn two-factor on (secret from the page, code from an authenticator): ten recovery codes
   appear; sign out and in again with a code.
7. A sign-in with a wrong password still counts: five wrong attempts lock the account.
8. Evidence the failure cases hold (terminal, Git Bash and PowerShell 7, same command):
   `dotnet test tests/Modules/Identity/Simulab.Identity.Tests --filter "FullyQualifiedName~IdentityTransactionTests"`
   expects `Passed!  - Failed: 0, Passed: 9`.

## Delivery
- Branch: `feature/F-47`; merge commit `589f0d2` (board #22). App version 0.2.0 -> 0.3.0.
- Tests: full suite green on the branch after merging `main` into it (Identity 408, Catalog 334, Web 818,
  architecture 164, Api 12, Jobs 25, Persistence 25, Ai 10, AppHost 8, ApiResults 13, SharedKernel 12, Email 2; 0
  failed). Full build 23 s, full test run 93 s. The 9 new tests of `IdentityTransactionTests` fail without the
  change and pass with it.
- Manual: no page changed; no visible behavior changed (the failure cases only roll back). `docs/infra.md` unchanged.
- Validated by the owner on screen, in the conversation, 2026-10-02.
