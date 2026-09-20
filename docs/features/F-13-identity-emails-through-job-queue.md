---
feature: F-13
epic: Foundation and identity
status: building
board: 723
version: 1
---
# Identity emails through the job queue

## Summary
Send the identity emails through a job table run by a hosted worker in the `Api` host, instead of inside the request (ADR-0001 #20). The queue arrives as a building block, `Simulab.Jobs`, with its own schema: the AI exam import will use the same table. The anonymous endpoints stop waiting for SMTP, so an unknown address and a real one answer in the same time, and a slow mail server no longer slows a page. Found by `/agile:review` on F-7 (2026-09-19).

## Goal
Take the mail server off the request path. Today a failing or slow SMTP server is swallowed by a `try/catch` in five handlers, which hides a real delivery failure and still costs the caller the round trip; an email that fails is simply lost. A job row is retried, survives a restart, and makes the response time independent of the recipient.

## What exists today (verified 2026-09-20)
- No job table, no `BackgroundService` and no `IHostedService` anywhere in `src/`. ADR-0001 #20 plans them; nothing was built.
- `IEmailSender` / `SmtpEmailSender` in `Simulab.Email` (F-3). Only the `Api` host registers it (`Program.cs:25`).
- **Five** call sites send email inside the request, not the three the idea listed:
  | Handler | Mailer |
  |---|---|
  | `RegisterUserHandler` | `IVerificationMailer.SendAsync` |
  | `ResendVerificationHandler` | `IVerificationMailer.SendAsync` |
  | `RequestPasswordResetHandler` | `IPasswordMailer.SendResetLinkAsync` |
  | `ChangePasswordHandler` / `ResetPasswordHandler`, through `PasswordNotice` | `IPasswordMailer.SendPasswordChangedAsync` |
  | `EraseAccountHandler` | `IErasureMailer.SendAccountErasedAsync` (F-10, shipped after this idea was written) |
- Four of the five wrap the send in a `try/catch` that logs and continues (`PasswordNotice`, `RequestPasswordResetHandler:52`, `EraseAccountHandler`); `RegisterUserHandler` does not.
- The three mailers render the message in the recipient's language with `EmailCulture.Use(locale)` and `IStringLocalizer<IdentityEmails>`, then call `IEmailSender.SendAsync`.
- Integration tests read the message synchronously right after the HTTP call: `RecordingEmailSender` behind `IdentityApiFactory.Emails`, about 60 uses across six test files.
- Raw verification and reset tokens exist only in memory; the database keeps the hash (`SecureToken.Generate`).

## Users and use cases
- UC1 A visitor asks for a password reset link for an address that does not exist and gets the same answer, in the same time, as a visitor who used a real address.
- UC2 A visitor signs up, changes a password, resets a password or erases an account while the mail server is down; the request succeeds at its own speed and the email leaves when the server is back.
- UC3 The operator sees, in the logs and in the job table, which emails failed permanently and why.
- UC4 A developer adds a new background job (the AI exam import, later) on the same table and the same worker, without building a second queue.

## Business rules
- BR1 A job is one row in table `jobs.jobs`: id, type, payload (JSON), `status`, `attempts`, `run_after`, `created_at`, `last_error`. States: `Pending → Running → Failed` (a job that succeeds leaves no row, BR6).
- BR2 Enqueuing happens in the same `SaveChanges` / transaction as the data that justifies the email. Either the token row and the job row both exist, or neither does. There is no state where a token is valid and no email was ever enqueued.
- BR3 Because BR2 makes enqueuing a local database write, the `try/catch`-and-log around the mailers is removed from the five handlers: a failure to enqueue is a database failure and the request fails with it, the way any other failed write does.
- BR4 The payload is the message already rendered — recipient, subject, HTML body and text body — built inside the request in the recipient's language. The worker never localizes and never reads the Identity module.
- BR5 A failed send is retried up to 5 times, with `run_after` set to 1, 2, 4, 8 and 16 minutes after the failed attempt. After the fifth attempt the row becomes `Failed`, keeps `last_error`, and is logged at error level.
- BR6 A job whose send succeeds is deleted in the same transaction that marks it done. A valid reset or verification link and the recipient's address never stay at rest in the database.
- BR7 A `Failed` row is kept: it holds the evidence of what was lost. There is no automatic cleanup and no automatic retry after the fifth attempt.
- BR8 The worker claims jobs with `SELECT ... FOR UPDATE SKIP LOCKED`, so two `Api` instances never send the same message twice.
- BR9 The worker polls every 5 seconds for jobs with `status = Pending` and `run_after <= now`, oldest first.
- BR10 A worker that is stopped mid-send leaves the row `Running`; a job `Running` for more than 5 minutes is taken again by the next poll. Delivery is at-least-once, not exactly-once.
- BR11 `run_after` exists for BR5 only. Nothing in this feature schedules a job for a future business reason.
- BR12 The queue does not replace the in-process integration event publisher (F-3, decision 4). Integration events stay synchronous.
- BR13 The worker runs in the `Api` host only, and can be switched off by configuration (`Jobs:WorkerEnabled`). The test host switches it off and drains the queue explicitly.

## Screens and API
No screen, no endpoint and no UI text. The five existing endpoints keep their routes, their status codes and their error codes; only the moment the email leaves changes.

New building block, following the profile's `BuildingBlocks` shape:
- `src/BuildingBlocks/Simulab.Jobs/` — `IJobQueue` (`EnqueueAsync`), `Job` entity, `JobsDbContext` (schema `jobs`), `JobWorker : BackgroundService`, `IJobHandler<T>`, `JobsServiceCollectionExtensions.AddJobs(...)`.
- `Simulab.Jobs.Contracts` is not created: the queue has no cross-module DTO, only an interface the host registers.
- Email job type: `email.send`, handled by a `SendEmailJobHandler` that calls `IEmailSender`.
- Migration: `jobs` schema and its one table, in `Simulab.Jobs`.

## Acceptance criteria
- AC1 Given an address that is not registered and an address that is, when each asks for a reset link, then neither request reaches the SMTP server and both return 202. (BR2, BR4)
- AC2 Given a sign-up, when the request returns, then the verification email has not been sent yet and exactly one `Pending` job row exists carrying the rendered message. (BR1, BR2, BR4)
- AC3 Given a pending email job, when the worker runs, then `IEmailSender` receives exactly the recipient, subject, HTML body and text body that the handler rendered. (BR4)
- AC4 Given a sign-up whose token row fails to save, when the transaction rolls back, then no job row exists. (BR2)
- AC5 Given a job whose send throws, when the worker runs it, then `attempts` is 1, `run_after` is one minute later, `last_error` holds the message, and the row is still `Pending`. (BR5)
- AC6 Given a job that has already failed 4 times, when the fifth attempt throws, then the row becomes `Failed` and is not picked again. (BR5, BR7)
- AC7 Given a job whose send succeeds, when the worker finishes it, then no row is left in `jobs.jobs`. (BR6)
- AC8 Given two workers polling the same table, when one claims a job, then the other never claims the same row. (BR8)
- AC9 Given a job left `Running` more than 5 minutes ago, when the worker polls, then it is claimed again. (BR10)
- AC10 Given each of the five identity emails, when its handler runs, then the message reaches `IEmailSender` in the account's language, with the same subject and body the current tests assert. (BR4; regression over F-4, F-7, F-8 and F-10)
- AC11 Given the test host, when it starts, then the worker is not running and a test drains the queue by calling it explicitly. (BR13)
- AC12 No UI resource key is added or removed; the existing email keys stay complete in pt-BR, pt-PT and en and the missing-key test is green.

## Decisions
- 2026-09-20 — All five identity emails go through the queue, including the F-10 farewell email — owner, question 1; leaving one out keeps a second, synchronous way of sending email alive.
- 2026-09-20 — The queue is a building block, `Simulab.Jobs`, with its own `jobs` schema and a generic `IJobQueue` — owner, question 2; ADR-0001 #20 already assigns the AI exam import to the same table, and a second owner later costs a data migration.
- 2026-09-20 — No administration screen in v1: logs and the `Failed` rows are the evidence — owner, question 3; a back-office page is its own item, with permission, three languages and bUnit tests.
- 2026-09-20 — The payload is the message already rendered in the recipient's language — owner, question 4; the worker stays free of `IStringLocalizer` and of the Identity module, which is what lets other modules share it.
- 2026-09-20 — The job is written in the same transaction as the data that justifies it (outbox) — owner, question 5; the alternative leaves a window where a valid token exists and no email was ever enqueued.
- 2026-09-20 — 5 attempts with 1-2-4-8-16 minute backoff — owner, question 6; covers a short mail-server outage without flooding it.
- 2026-09-20 — The row is deleted as soon as the send succeeds — owner, question 7; a valid link and a recipient address do not stay at rest, and an erased account (F-10) leaves no job row carrying its old address.
- 2026-09-20 — Tests drain the queue explicitly (`RunJobsAsync`) with the worker switched off — owner, question 8; deterministic, no waiting, and it still exercises the real worker code. The alternative fights `FakeTimeProvider`, which never advances a polling loop.
- 2026-09-20 — `FOR UPDATE SKIP LOCKED` — owner, question 9; a second `Api` replica must not duplicate an email. It is the one piece of raw SQL in the item.
- 2026-09-20 — Integration events stay in process — owner, question 10; F-3 decision 4 already allows the swap behind the same interface when a case needs guaranteed delivery. None does yet.
- 2026-09-20 — `run_after` exists but only the retry backoff writes it — owner, question 11; the column keeps a grace period (F-10) or a cleanup job from needing a migration, without being exposed or tested as a feature.
- 2026-09-20 — No new package — owner, question 12; `BackgroundService`, `System.Text.Json` and EF Core raw SQL cover it. Hangfire and Quartz bring a dashboard that would need its own authentication.
- 2026-09-20 — Claude: `IEmailSender` keeps its interface and its SMTP implementation. The mailers keep rendering; only their last line changes from `sender.SendAsync(...)` to `queue.EnqueueAsync(...)`.

## Out of scope
- Any screen or endpoint for jobs (listing, retrying, cancelling).
- Jobs scheduled for a business reason: the F-10 grace period, cleanups, reminders.
- Moving integration events to the table (F-3 decision 4).
- The AI exam import jobs: this item builds the queue, not its first heavy user.
- Multi-tenancy of the job table, priorities, and per-type concurrency limits.
- Changing `IEmailSender`, the SMTP sender, the email templates or the three resource files.
- A metric or dashboard for queue depth.

## Open questions
- (none)

## Change notes

## Validation script
<!-- Written at the end of build. -->

## Delivery
- Branch: `feature/F-13`
- Merge: -
- Tests: -
- Manual pages: - (no visible behaviour changes)
