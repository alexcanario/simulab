---
feature: F-59
epic: Foundation and identity
status: refining
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
- Depends on: nothing (F-13 job queue and F-10 erasure are done).
- Waits on (to start): nothing.
- Needed to validate: the app host running from this worktree and two browser sessions of one test account
  (Claude prepares them; the owner follows the script).
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: anything outside `Simulab.Identity`.

## Goal
An erasure that committed always finishes: the account's tokens stop and `UserErased` is published, even when the
process dies right after the commit.

## Users and use cases
- UC1 A user erases their own account (F-10 UC2); every device of that account stops within seconds and the rest of
  the system is told, even if the server crashes right after the erasure is saved.

## Business rules
- BR1 The erasure transaction stages one job, `account.erased`, with the user id and the erasure time. A
  rollback (wrong state, last manager, F-10 BR11) takes the job with it: no job outlives an erasure that did not
  happen.
- BR2 The handler no longer revokes sessions or publishes the event itself after the commit. Both happen only in the
  job: first `RevokeAllAsync(userId)` (F-10 BR10, the caller's sessions included), then `PublishAsync(UserErased)`
  (F-10 BR13, so a consumer only hears after the data changed and the tokens stopped).
- BR3 The job is safe to run more than once: revoking an account with no sessions left is a no-op, and a
  `UserErased` consumer must accept the same event twice (written in the contract's doc comment).
- BR4 A job attempt that fails is retried by the F-13 policy (5 attempts, backoff 1-16 min); one that gives up stays
  as a `Failed` row with its log, like an email job.

## Screens and API
- No screen change. `POST /api/v1/identity/account-erasures` (F-10) keeps its contract and error codes; the answer still comes
  after the commit. The tokens stop when the worker runs the job (poll every 5 s) instead of before the answer.
- Job type: `account.erased`, payload `{ "userId": "<guid>", "erasedAt": "<ISO 8601>" }`.

## Acceptance criteria
- AC1 (BR1) Given a valid erasure, when it commits, then the jobs table holds one pending `account.erased`
  job with the user id and the erasure time, written in the same transaction.
- AC2 (BR1) Given an erasure that rolls back on the last-manager rule, when the handler returns the failure, then no
  `account.erased` job exists.
- AC3 (BR2, UC1) Given an erased account with two active sessions, when the job runs, then both access tokens are
  refused by the API and `UserErased` is published once, after the revocation.
- AC4 (BR2) Given the process stops right after the commit (the handler's after-commit code never runs), when the
  runner later processes pending jobs, then the sessions are revoked and `UserErased` is published.
- AC5 (BR3) Given the job already ran, when it runs again, then it succeeds, revokes nothing new and publishes the
  event again.
- AC6 (BR4) Given the session store throws on the first attempt, when the runner processes the job, then the attempt
  fails, the job is scheduled again by the F-13 backoff, and the next attempt revokes and publishes.
- AC7 No new UI text; the missing-key test stays green in pt-BR, pt-PT and en.

## Decisions
- 2026-10-04 — Both after-commit steps move into one job staged in the erasure transaction (owner, option "job in
  the transaction") — reuses the F-13 queue, gives at-least-once delivery; the cost is a delay of about 5 s before
  the access tokens stop.
- 2026-10-04 — No generic outbox; the in-process publisher stays (owner) — `UserErased` is the only integration
  event published today; the next event reuses this job pattern.
- 2026-10-04 — One job for both steps, revoke first then publish (Claude) — keeps F-10 BR13's order and a single
  retry unit; both steps are idempotent, so a repeat after a partial attempt is harmless.
- 2026-10-04 — Consumers of `UserErased` must be idempotent, stated in its doc comment (Claude) — at-least-once
  delivery can repeat the event.
- 2026-10-04 — A job that gives up is handled like a failed email job: `Failed` row and error log, no new alert
  (Claude) — same policy as the rest of the queue; alerting on failed jobs is not this item.
- 2026-10-04 — The job handler lives in the Identity module (Application for the handler, Infrastructure for the
  registration) and uses `IRefreshSessionStore` and `IIntegrationEventPublisher` as today (Claude) — no new contract
  between modules.
- 2026-10-04 — No new packages (Claude) — the job queue, Redis store and test containers are already referenced.
- 2026-10-04 — The app manual does not change (Claude) — no visible behavior changes beyond a delay of seconds.

## Out of scope
- A generic outbox replacing `InProcessIntegrationEventPublisher` (owner, 2026-10-04; no idea captured).
- Consumers of `UserErased` in other modules.
- Alerting on failed jobs.

## Open questions
- (none)

## Change notes

## Validation script
<!-- Written at the end of build. -->

## Delivery
<!-- Filled by /agile:ship. -->
