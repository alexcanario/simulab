---
feature: F-59
epic: Foundation and identity
status: refining
board: 98
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
# The account erasure's after-commit steps survive a crash

Technical terms: [glossary](../glossary.md)

## Summary
`EraseAccountHandler` (`src/Modules/Identity/Simulab.Identity.Application/Account/EraseAccountHandler.cs`) commits
the erasure inside `RunExclusiveAsync`, then revokes every refresh session in Redis and publishes the `UserErased`
integration event, both after the commit and with no outbox. A crash between the commit and those two steps leaves
an erased account whose sessions are still alive, and other modules that never hear about the erasure. Raised while
refining F-47 (2026-10-01); the owner chose to keep it out of F-47.

## What exists (verified 2026-10-04)
- `EraseAccountHandler.cs:66-77`: the erasure commits inside `RunExclusiveAsync`; then `RevokeAllAsync` and
  `PublishAsync(UserErased)` run with nothing that retries them. The handler is unchanged since F-21 (`1d30161`);
  F-47 did not touch it.
- Refresh tokens already die with the commit: `User.Erase()` (`User.cs:142`) renews the security stamp and the row
  is soft deleted (query filter, `IdentityModuleDbContext.cs:92`), so the refresh grant (`TokenEndpoints.cs:297-300`)
  refuses every old session. Premise "sessions still alive" is only half true.
- What a crash really leaves alive: the access tokens already issued (15 min, `TokenLifetimes.cs:6`).
  `RevokeAllAsync` is what puts them on the deny list that `RevocationCheckMiddleware` reads.
- `UserErased` has no consumer today (no `IIntegrationEventConsumer<UserErased>` in `src/`), and it is the only
  integration event published anywhere. Premise "other modules never hear" is latent: nothing is lost yet.
- A durable place to put after-commit work already exists: the F-13 job queue (`Simulab.Jobs`). A job staged
  through `IJobQueue` is written in the same transaction as the account (the farewell email already does this),
  the worker polls every 5 s and retries up to 5 times (backoff 1, 2, 4, 8, 16 min).
  `InProcessIntegrationEventPublisher` itself says to move to the job table "when delivery must survive a crash".

## Start
- Depends on: nothing.
- Waits on: nothing.
- Suggested path: `/agile:discuss` first (an outbox, or revoking the sessions before the commit).
- Parallel with: anything outside `Simulab.Identity`.
- Cause not verified: measure it at /agile:refine. Symptom seen in the code: both steps run after
  `RunExclusiveAsync` returns, with nothing that retries them.
