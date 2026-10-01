---
feature: F-47
epic: Foundation and identity
status: refining
board: 22
version: 1
---
# Every Identity handler that writes twice commits once

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
  handlers have no transaction and what an event records is already committed. Inside a Postgres transaction a
  failed statement aborts the whole transaction, so a swallowed failure there would silently undo the rest.
- `PasswordNotice.EnqueueAsync` stages the "password changed" job and saves it with
  `IIdentityUnitOfWork.SaveChangesAsync`.

### Premises in the summary that were wrong
- "13 handlers": `SaveRoleHandler` (and `SetUserRolesHandler`, `DeleteRoleHandler`) and `EraseAccountHandler`
  already run inside `RunExclusiveAsync`. `ProfileHandler` writes once; its second `UpdateAsync` is a retry of the
  same write after a concurrency failure, not a second write.
- "the verify-email path" is `VerifyEmailHandler`; "TOTP" is three paths of `TotpAccountHandler`, only one of them
  harmful; "GoogleSignInHandler" is `ClearFailuresAsync` only, called by the Google grant in `TokenEndpoints`.

### Every multi-write path, and what a crash between two writes leaves
| Path | Writes today, in order | State after a crash in the middle | Verdict |
|---|---|---|---|
| `ResetPasswordHandler.HandleAsync` | consume link; reset password; reset failure count; clear lockout; (pending) activate + consume verification links; revoke sessions (Redis); stage+save notice; event | link spent, password set, account still locked or pending, no notice | wrap (BR1) |
| `ChangePasswordHandler.HandleAsync` (success path) | change password; reset failure count; revoke other sessions + restamp caller (Redis); stage+save notice; event | password changed with no "password changed" email to the owner | wrap (BR1) |
| `RequestPasswordResetHandler.HandleAsync` | consume older links; new link + mail job; event | every older link dead and no new one, while the throttle still counts the dead ones | wrap (BR1) |
| `ResendVerificationHandler.HandleAsync` | consume older links; new link + mail job | same as above, for verification | wrap (BR1) |
| `TotpAccountHandler.ConfirmAsync` | code check (failure count, last step); enable two-factor; store recovery codes; event | two-factor on with no recovery codes: a lost phone is a lost account | wrap (BR1) |
| `GoogleSignInHandler.ClearFailuresAsync` | reset failure count; clear lockout | count reset, lockout still set: the owner who just proved the Google identity waits out the lockout | wrap (BR1) |
| `VerifyEmailHandler.HandleAsync` | activate account; consume link | account active, link not consumed — and F-4 BR10 already answers "verified" for any link of an active account | safe (BR7) |
| `TotpAccountHandler.DisableAsync` | password and code checks; disable two-factor; remove recovery codes; event | two-factor off with codes left; codes are only read by the code step, which requires two-factor on, and are replaced when it is turned on again | safe (BR7) |
| `TotpAccountHandler.StartAsync`, `RegenerateRecoveryCodesAsync` | code check; one write | — | safe (BR7) |
| `ExportDataHandler.HandleAsync` | password check; reset failure count; best-effort notice | count reset, no notice; the export is a read and F-16 made the notice best-effort | safe (BR7) |
| `GoogleLinkHandler.LinkAsync`, `UnlinkAsync` | password check (unlink); one write; event | — | safe (BR7) |
| `ProfileHandler.SaveAsync` | one write (retried once on a concurrency failure) | — | safe (BR7) |
| `TotpSignInHandler.CompleteAsync`, password grant in `TokenEndpoints` | spend challenge (Redis) or one write; events; session (Redis) | — | safe (BR7) |
| `RegisterUserHandler`, `RegisterGoogleUserHandler` | — | wrapped by F-30 | done |
| `SaveRoleHandler`, `SetUserRolesHandler`, `DeleteRoleHandler`, `EraseAccountHandler` | — | inside `RunExclusiveAsync` | done |

## Goal
A password reset, a password change, a new reset or verification link, turning two-factor on and clearing a lockout
after a Google sign-in either happen whole or not at all, so no account is left in a state no rule describes.

## Users and use cases
- UC1 A visitor resets the password with the emailed link; if the server fails mid-request, the link still works,
  the old password still works, and nothing else about the account changed.
- UC2 A signed-in user changes the password; if the server fails mid-request, the old password still works and no
  "password changed" email leaves.
- UC3 A visitor asks for a new reset or verification link; if the server fails mid-request, the links they already
  had still work.
- UC4 A user confirms two-factor; if the server fails mid-request, two-factor stays off and they can start again.
- UC5 A user signs in with Google; if the server fails while clearing the failure count and the lockout, both stay
  as they were.

## Business rules
- BR1 These six paths commit every Postgres write of their success path in one transaction, opened with
  `IIdentityUnitOfWork.BeginAsync`: `ResetPasswordHandler.HandleAsync`, `ChangePasswordHandler.HandleAsync`,
  `RequestPasswordResetHandler.HandleAsync`, `ResendVerificationHandler.HandleAsync`,
  `TotpAccountHandler.ConfirmAsync`, `GoogleSignInHandler.ClearFailuresAsync`. A failure before the commit leaves
  every Postgres row of the request as it was before the request began.
- BR2 The transaction opens only after every check: input validation, lookups, the current password, the two-factor
  code and the new-password rules. What a check itself writes — a wrong password or code counting towards the
  lockout (`AccessFailedAsync`), the TOTP time step a right code spends — is committed on its own, before the
  transaction, so a later rollback never gives an attacker a free attempt. The F-7 BR2 link consumption is not a
  check: it is the first write inside the transaction.
- BR3 Session writes in Redis (revoke every other session, restamp the caller's) happen inside the transaction's
  scope, after the last Postgres write and before the commit. A commit that then fails leaves the account's owner
  signed out with the old password still valid — never a session the change was meant to end still alive.
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
- BR8 The class comment of `AccountEventLog` and of `IIdentityUnitOfWork.BeginAsync` say which handlers now hold a
  transaction (they say "sign-up handlers" and "no transaction of their own" today).

## Screens and API
- No screen, route, endpoint, error code or migration changes. No new UI text.

## Acceptance criteria
- AC1 Given a valid reset link for a locked-out pending account, when the request fails after the password is reset
  and before the commit, then the link is not consumed, the stored password hash, the
  failure count, lockout end and pending status are unchanged, no notice job exists, and the same link then resets
  the password. (BR1, BR6)
- AC2 Given a signed-in user with two sessions, when a password change fails after the password is changed and
  before the commit, then the old password still works, the failure count is unchanged and no "password changed"
  job exists. (BR1)
- AC3 Given a user with two sessions, when a password change or a reset fails at the commit after the sessions were
  revoked, then the other session is revoked and the password is the old one. (BR3)
- AC4 Given a pending account with one valid reset link, when a new reset request fails after the older links are
  consumed and before the new link is saved, then the older link still resets the password and no new link or job
  exists. (BR1)
- AC5 Given a pending account with one valid verification link, when a resend fails after the older links are
  consumed and before the new link is saved, then the older link still verifies the account and no new link or job
  exists. (BR1)
- AC6 Given a user enrolling two-factor with a right code, when the confirmation fails after two-factor is enabled
  and before the recovery codes are stored, then two-factor is off and no recovery codes are stored. (BR1)
- AC7 Given a user enrolling two-factor, when a wrong code is sent and the request then fails before the commit,
  then the failure count went up by one; the same holds for a wrong current password on the password change.
  (BR2)
- AC8 Given a locked-out account with failures counted, when a Google sign-in clears them and the request fails
  between the two writes, then both the failure count and the lockout end are unchanged. (BR1)
- AC9 Given any of the six paths, when it succeeds, then its existing tests stay green unchanged, and two concurrent
  resets with the same link still produce exactly one success. (BR6)
- AC10 Given an account event logged on a wrapped path, when the request succeeds, then the event is written after
  the commit; when the request fails before the commit, then no event of that path is written. (BR4)
- AC11 Given the request fails inside a wrapped path, when the client reads the answer, then it is the host's
  existing 500 problem details. (BR5)
- AC12 No UI text is added or changed, so the missing-key test stays green in pt-BR, pt-PT and en.

## Decisions
- 2026-10-01 — only the six harmful paths are wrapped; the others are declared safe with their reason (owner) —
  less code and fewer failure tests, and each exception is written down (BR7).
- 2026-10-01 — the lockout counter survives a rollback: the transaction opens after every check (owner) — a counter
  that rolled back with the rest would give an attacker a free attempt per failed request. Same rule as F-30 BR9.
- 2026-10-01 — Redis session writes go before the commit (owner) — a failed commit then signs the owner out, which
  is harmless; after the commit, a crash would keep alive the session a password change exists to end.
- 2026-10-01 — the erasure's after-commit steps (revoke sessions, publish `UserErased`, no outbox) stay out and go
  to their own item (owner) — it is cross-module messaging, not a local transaction. Captured as F-59
  (`docs/features/F-59-erasure-after-commit-steps.md`, issue #98).
- 2026-10-01 — account events stay after the commit (Claude) — `AccountEventLog` swallows a failed insert; inside a
  Postgres transaction that failure aborts the transaction, and the commit would then silently roll back the
  password change it was recording.
- 2026-10-01 — the handlers keep the `BeginAsync` / `CommitAsync` / dispose-to-roll-back shape of F-30, no new
  abstraction (Claude) — the same shape is already reviewed and tested; `ClearFailuresAsync` gets an
  `IIdentityUnitOfWork` parameter in its constructor like the others.
- 2026-10-01 — failures are injected as in F-30: a double of a store or mailer the handler already takes, made to
  throw (reset/verification token stores, `IPasswordMailer`, `IRefreshSessionStore` for AC3), and, where only
  `UserManager` writes (AC6, AC8), a test `IUserStore` decorator registered in the test host that throws on the
  chosen call (Claude) — never by breaking the database; everything runs on the Identity tests' Postgres container.
- 2026-10-01 — no new package (Claude).

## Out of scope
- The erasure's after-commit steps — F-59.
- The paths declared safe in BR7, beyond their one comment line.
- An outbox or retry for the Redis writes.

## Open questions
- (none)

## Change notes

## Validation script
<!-- Written at the end of build. At most 8 steps the product owner follows on screen. -->

## Delivery
<!-- Filled by /agile:ship. -->
