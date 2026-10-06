---
feature: F-59
epic: Foundation and identity
status: validating
board: 98
version: 1
---
# The account erasure's after-commit steps survive a crash

Technical terms: [glossary](../glossary.md)

## Summary
`EraseAccountHandler` (`src/Modules/Identity/Simulab.Identity.Application/Account/EraseAccountHandler.cs`) commits
the erasure inside `RunExclusiveAsync`, then revokes every refresh session in Redis and publishes the `UserErased`
integration event, both after the commit and with no outbox. A crash between the commit and those two steps leaves
an erased account whose access tokens still work for up to 15 minutes, and an event that is never published.
Raised while refining F-47 (2026-10-01); the owner chose to keep it out of F-47.

## What exists (verified 2026-10-04)
- `EraseAccountHandler.cs:66-77`: the erasure commits inside `RunExclusiveAsync`; then `RevokeAllAsync` and
  `PublishAsync(UserErased)` run with nothing that retries them. The handler is unchanged since F-21 (`1d30161`);
  F-47 did not touch it.
- Refresh tokens already die with the commit: `AccountErasureStore.RemoveAccountDataAsync` deletes the account's
  OpenIddict tokens (F-10 change note), so a refresh fails with `invalid_grant` first; behind that, `User.Erase()`
  renews the security stamp (`User.cs:142`) and the row is soft deleted (query filter,
  `IdentityModuleDbContext.cs:92`), so the stamp check of the refresh grant (`TokenEndpoints.cs:297-300`) would
  refuse it too. Premise "sessions still alive" is only half true.
- What a crash really leaves alive: the access tokens already issued (15 min, `TokenLifetimes.cs:6`).
  `RevokeAllAsync` is what puts them on the deny list that `RevocationCheckMiddleware` reads.
- `UserErased` has no consumer today (no `IIntegrationEventConsumer<UserErased>` in `src/`), and it is the only
  integration event published anywhere. Premise "other modules never hear" is latent: nothing is lost yet.
- A durable place to put after-commit work already exists: the F-13 job queue (`Simulab.Jobs`). A job staged
  through `IJobQueue` is written in the same transaction as the account (the farewell email already does this),
  the worker polls every 5 s and makes at most 5 attempts (waits of 1, 2, 4, 8 min between them). It claims one
  job at a time, oldest first, across every job type, so a job waits behind the farewell email staged before it.
  A job that gives up has its payload cleared (`JobRunner.cs:113-119`); what it was about stays only in the log.
  `InProcessIntegrationEventPublisher` itself says to move to the job table "when delivery must survive a crash".
- F-10 BR10/AC7 and `AccountErasureTests.cs:246-251` require every access token refused right after the 204;
  F-10 AC11 and `AccountErasureTests.cs:305` require `UserErased` published by then. The Web relies on the first
  (`AccountEndpoints.cs:142`: no sign-out call after an erasure).

## Start
- Depends on: nothing (F-13 job queue and F-10 erasure are done).
- Waits on (to start): nothing.
- Needed to validate: the app host running from this worktree and two browser sessions of one test account
  (Claude prepares them; the owner follows the script).
- Suggested path: `/agile:refine` -> `/agile:build`.
- Parallel with: anything outside `Simulab.Identity`.

## Goal
An erasure that committed always finishes: the account's tokens stop and `UserErased` is published, even when the
process dies right after the commit.

## Users and use cases
- UC1 A user erases their own account (F-10 UC2); every device of that account stops at its next call, as today,
  and the rest of the system is told. If the server crashes right after the erasure is saved, the job finishes both
  steps once the worker runs again.

## Business rules
- BR1 The erasure transaction stages one job, `account.erased`, with the user id and the erasure time. A
  rollback (wrong state, last manager, F-10 BR11) takes the job with it: no job outlives an erasure that did not
  happen.
- BR2 Right after the commit the handler still revokes every session (`RevokeAllAsync`, F-10 BR10 unchanged: the
  tokens are refused before the 204). It no longer publishes `UserErased` itself.
- BR3 The `account.erased` job runs `RevokeAllAsync(userId)` again (a no-op when the handler already did it; the
  backstop when the process died before it), then publishes `UserErased` (F-10 BR13: a consumer only hears after
  the data changed and the tokens stopped). The event is therefore published when the worker runs the job, not
  before the 204.
- BR4 The job is safe to run more than once: revoking an account with no sessions left is a no-op, and a
  `UserErased` consumer must accept the same event twice (written in the contract's doc comment).
- BR5 A job attempt that fails is retried by the F-13 policy (at most 5 attempts, waits of 1, 2, 4, 8 min); one
  that gives up stays as a `Failed` row with an empty payload, and the user id is in the error log, like an email
  job.
- BR6 F-10 BR13 and AC11 change with this item: "published after the transaction commits" becomes "published by the
  `account.erased` job, after the commit". F-10 BR10 and AC7 do not change.

## Screens and API
- No screen change. `POST /api/v1/identity/account-erasures` (F-10) keeps its contract, error codes and the
  immediate revocation; the Web's reasoning at `AccountEndpoints.cs:142` stays true.
- Job type: `account.erased`, payload `{ "userId": "<guid>", "erasedAt": "<ISO 8601>" }`.

## Acceptance criteria
- AC1 (BR1) Given a valid erasure, when it commits, then the jobs table holds one pending `account.erased`
  job with the user id and the erasure time, written in the same transaction.
- AC2 (BR1) Given an erasure that rolls back on the last-manager rule, when the handler returns the failure, then no
  `account.erased` job exists.
- AC3 (BR2, UC1) Given a user signed in on two devices, when the erasure answers 204, then both access tokens are
  already refused and no `UserErased` has been published yet; when the runner then processes pending jobs,
  `UserErased` is published exactly once.
- AC4 (BR3, the crash) Given an erasure committed whose after-commit revocation never ran (the session store fails
  right after the commit, so the handler stops there), when the runner later processes pending jobs, then every
  session of the account is revoked and `UserErased` is published, after the revocation.
- AC5 (BR4) Given the job already ran, when it runs again, then it succeeds, revokes nothing new and publishes the
  event again.
- AC6 (BR5) Given the session store throws on the job's first attempt, when the runner processes the job, then the
  attempt fails, the job is due again after `JobPolicy.BackoffAfter(1)`, and the next attempt revokes and publishes.
- AC7 (BR6) The F-10 tests for AC7 still pass unchanged; the F-10 AC11 test runs the jobs before checking the event,
  and F-10's file records the change in a change note.
- AC8 No new UI text; the missing-key test stays green in pt-BR, pt-PT and en.

## Decisions
- 2026-10-04 - First answer: both after-commit steps only in a job (owner). Replaced the same day after the
  independent review of this file found it breaks F-10 BR10/AC7 and its tests, and that the revocation would wait
  behind the farewell email job (and up to 15 min if the job fails).
- 2026-10-04 - Revoke right after the commit as today, and stage an `account.erased` job in the erasure transaction
  that revokes again and publishes `UserErased` (owner, option "revoke now + job") - keeps the immediate revocation
  of F-10 and gives both steps an at-least-once backstop; only the event's timing changes.
- 2026-10-04 - No generic outbox; the in-process publisher stays (owner) - `UserErased` is the only integration
  event published today; the next event reuses this job pattern.
- 2026-10-04 - One job for both steps, revoke first then publish (Claude) - keeps F-10 BR13's order and a single
  retry unit; both steps are idempotent, so a repeat after a partial attempt is harmless.
- 2026-10-04 - The handler does not publish inline as well (Claude) - publishing in both places would send every
  event twice in the normal case; with no consumer today, the delay of one job poll costs nothing.
- 2026-10-04 - Consumers of `UserErased` must be idempotent, stated in its doc comment (Claude) - at-least-once
  delivery can repeat the event.
- 2026-10-04 - A job that gives up is handled like a failed email job: `Failed` row and error log, no new alert
  (Claude) - same policy as the rest of the queue; alerting on failed jobs is not this item.
- 2026-10-04 - The job handler lives in Identity Infrastructure; the handler stages the job through an Application
  port, the way `IErasureMailer` -> `ErasureMailer` does (Claude) - `Simulab.Jobs` depends on EF Core, which
  Application never references (`docs/agile/profile.md`); no new contract between modules.
- 2026-10-04 - A given-up job keeps the F-13 behavior (payload cleared, user id in the log) (Claude) - the runner's
  rule is shared by every job type; changing it is not this item.
- 2026-10-04 - No new packages (Claude) - the job queue, Redis store and test containers are already referenced.
- 2026-10-04 - The app manual does not change (Claude) - nothing visible changes for the user.

## Out of scope
- A generic outbox replacing `InProcessIntegrationEventPublisher` (owner, 2026-10-04; no idea captured).
- Consumers of `UserErased` in other modules.
- Alerting on failed jobs.

## Open questions
- (none)

## Change notes

## Validation script
Needed to validate: the Redis and PostgreSQL containers of the app host (Claude has not started them; the owner's `dotnet run` does).

1. Start the app host: `dotnet run --project src/Hosts/Simulab.AppHost` (same command in Git Bash and PowerShell 7), from `D:\wt\simulab\f-59-erasure-after-commit`.
2. Sign up and confirm a test account, then sign in on two browsers (two devices).
3. In one browser, erase the account (Account > Erase, current password). Expect: signed out at once.
4. In the other browser, reload or click any page: expect the sign-in page (the access token is refused).
5. In the app host dashboard, open the `api` logs: within about 5 seconds the job `account.erased` runs and no job stays failed.
6. Check the jobs table is empty of `account.erased` rows afterwards (schema `jobs`, table `jobs`).
7. Language switch and keyboard pass: no text changed; nothing to check.
The crash case (steps killed between commit and revoke) is proven by tests AC4 and AC6, not by hand.

## Delivery
<!-- Filled by /agile:ship. -->
