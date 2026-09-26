---
feature: F-47
epic: Foundation and identity
status: idea
board: 771
version: 1
---
<!--
One file per feature. Save as: docs/features/F-<number>-<slug>.md
Status flow: idea -> refining -> approved -> building -> validating -> done
- idea: title, summary and start only (/agile:idea, /agile:epic).
- refining: sections below filled during /agile:refine.
- approved: set only after the product owner says "approve F-<number>" and Open questions is empty or deferred.
- building / validating / done: set by /agile:build and /agile:ship.
Remove these comments when the file leaves `idea`.
-->
# Every Identity handler that writes twice commits once

## Summary
F-30 puts the two sign-ups in one transaction. They are not alone: 13 handlers of `Simulab.Identity.Application`
write more than once in a request, each write its own committed transaction, so a failure between two of them
leaves the account in a state no rule describes. `ResetPasswordHandler` was read and confirmed while refining
F-30 — it consumes the token, resets the password, resets the failure count, clears the lockout and updates the
user, in five separate commits, so a crash after the third leaves a consumed reset token and a locked-out account
with the new password already set. The other ten (`TotpAccountHandler`, `GoogleSignInHandler`, `ExportDataHandler`,
`EraseAccountHandler`, `ChangePasswordHandler`, `RequestPasswordResetHandler`, `ResendVerificationHandler`,
`ProfileHandler`, `SaveRoleHandler` and the verify-email path) are candidates found by `grep`, not verified. Go
through them with the transaction F-30 introduces, and decide each one: wrap it, or write down why its order is
already safe. Raised while refining F-30 (2026-09-26).

## Start
- Depends on: F-30 — it introduces `IIdentityUnitOfWork.BeginAsync` and the test pattern for injecting a failure
  mid-request. Nothing to design here until that lands.
- Waits on: nothing.
- Suggested path: `/agile:refine` first. Two real choices: which handlers are wrapped and which are declared safe
  with a reason, and whether the password-confirmed actions (erasure, export, TOTP) need the lockout counter to
  survive a rollback — a failure counter that rolls back with the rest would let an attacker retry for free.
- Parallel with: anything outside `Simulab.Identity`. It touches authentication, so the independent review runs on
  the file and on the diff.
