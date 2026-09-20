---
feature: F-13
epic: Foundation and identity
status: done
board: 723
version: 2
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
- BR2 Enqueuing happens in the same `SaveChanges` / transaction as the data that justifies the email, wherever that data has a save of its own: the verification token, the resend token, the reset token and the erasure. Either both rows exist, or neither does; there is no state where a token is valid and no email was ever enqueued. The two after-the-fact notices are the stated exception: `UserManager` has already committed the new password when the password-changed email is written, so that job is saved on its own, immediately after (v2).
- BR3 Because BR2 makes enqueuing a local database write, the `try/catch`-and-log around the mailers is removed from the five handlers: a failure to enqueue is a database failure and the request fails with it, the way any other failed write does. For the password-changed notice this means a database that fails right after the password changed turns that request into an error, which the old best-effort `catch` used to hide (v2).
- BR4 The payload is the message already rendered — recipient, subject, HTML body and text body — built inside the request in the recipient's language. The worker never localizes and never reads the Identity module.
- BR5 A failed send is retried up to 5 times, with `run_after` set to 1, 2, 4, 8 and 16 minutes after the failed attempt. After the fifth attempt the row becomes `Failed`, keeps `last_error`, and is logged at error level.
- BR6 A job whose send succeeds is deleted in the same transaction that marks it done. A valid reset or verification link and the recipient's address never stay at rest in the database.
- BR7 A `Failed` row is kept: it holds the evidence of what was lost — the job type, how many attempts it took and the type of the exception that stopped it. Its payload is cleared when it is given up on, because the message is not evidence: it is a live link and a recipient address, and BR6's promise would otherwise be defeated by a mail server that stays down (v2). The whole exception, with its message, is in the log. There is no automatic cleanup and no automatic retry after the fifth attempt.
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
- AC6 Given a job that has already failed 4 times, when the fifth attempt throws, then the row becomes `Failed`, keeps its type, its attempt count and the exception type, has its payload cleared, and is not picked again. (BR5, BR7)
- AC7 Given a job whose send succeeds, when the worker finishes it, then no row is left in `jobs.jobs`. (BR6)
- AC8 Given two workers polling the same table, when one claims a job, then the other never claims the same row. (BR8)
- AC9 Given a job left `Running` more than 5 minutes ago, when the worker polls, then it is claimed again. (BR10)
- AC10 Given each of the five identity emails, when its handler runs, then the message reaches `IEmailSender` in the account's language, with the same subject and body the current tests assert. (BR4; regression over F-4, F-7, F-8 and F-10)
- AC11 Given the test host, when it starts, then the worker is not running and a test drains the queue by calling it explicitly. (BR13)
- AC12 No UI resource key is added or removed; the existing email keys stay complete in pt-BR, pt-PT and en and the missing-key test is green.

## Coverage
| Criterion | Test |
|---|---|
| AC1 | `IdentityEmailQueueTests.ResetRequest_KnownAndUnknownAddress_NeitherTouchesTheMailServer` |
| AC2 | `IdentityEmailQueueTests.SignUp_LeavesTheVerificationEmailAsOnePendingJobCarryingTheRenderedMessage` |
| AC3 | `IdentityEmailQueueTests.RunningTheQueue_SendsExactlyTheStoredMessageAndLeavesNoRow`; `JobRunnerTests.RunNext_EmailJob_SendsTheMessageThatWasStored` |
| AC4 | `IdentityEmailQueueTests.SignUp_WhenTheTokenWriteFails_LeavesNoJobBehind` |
| AC5 | `JobRunnerTests.RunNext_HandlerThrows_KeepsTheJobPendingWithABackoff`; `RunNext_BeforeTheBackoffHasPassed_DoesNotTakeTheJobAgain`; `RunNext_RetryAfterTheBackoff_Succeeds`; end to end: `PasswordResetTests.RequestLink_MailServerFails_AnswersTheSameAndKeepsTheEmailForARetry`, `PasswordChangeTests.Change_MailServerFails_TheChangeSucceedsAndTheNoticeIsKeptForARetry`, `AccountErasureTests.Erase_WhenTheFarewellEmailFails_StillErasesTheAccount` |
| AC6 | `JobRunnerTests.RunNext_FifthAttemptFails_MarksTheJobFailedAndStopsTakingIt` |
| AC7 | `JobRunnerTests.RunNext_JobSucceeds_LeavesNoRow`; `IdentityEmailQueueTests.RunningTheQueue_SendsExactlyTheStoredMessageAndLeavesNoRow` |
| AC8 | `JobRunnerTests.RunPending_SeveralWorkersAtOnce_RunsEachJobExactlyOnce` |
| AC9 | `JobRunnerTests.RunNext_JobLeftRunningTooLong_IsClaimedAgain`; `RunNext_RowRemovedWhileTheJobWasRunning_FinishesQuietly` |
| AC10 | `VerificationEmailLanguageTests.Register_WritesTheEmailInTheRequestLanguage` (en, pt-BR, pt-PT); `VerificationEndpointTests.Verify_TokenReplacedByAResend_IsInvalid` (resend); `PasswordEmailLanguageTests.ResetAndChangedEmails_UseTheAccountLanguage` (en, pt-BR, pt-PT); `PasswordChangeTests.Change_Success_ClearsTheFailedCountAndSendsThePasswordChangedEmail`; `AccountErasureTests.Erase_SendsTheFarewellEmailInTheAccountsLanguage` |
| AC11 | `IdentityEmailQueueTests.TestHost_RegistersTheWorkerAndKeepsItOff`; `JobCompositionTests.The_host_wires_the_queue_the_runner_and_the_worker`; and the whole Identity suite, which drains the queue explicitly |
| AC12 | `EmailResourceParityTests.Every_key_exists_in_every_language`; `Every_text_differs_from_the_neutral_one` |

Nothing is checked only on screen: every criterion goes through the endpoint or the runner the worker itself uses. The validation script confirms the wiring through the app host, which no test covers.

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
- 2026-09-20 — Build: `Simulab.Jobs` references `Simulab.Email`, not the other way round, so `Simulab.Email` stays free of EF Core. The `email.send` handler is the one job type the block ships with.
- 2026-09-20 — Build: `Job` does not inherit `TenantEntity`. It is infrastructure, not business data: BR6 deletes the row for real (soft delete would keep the live link), and the worker has to see every row whatever the tenant. `JobsDbContext` still inherits `ModuleDbContext` with its own `jobs` schema, which is what the architecture tests check.
- 2026-09-20 — Build: the job table is mapped a second time into `IdentityModuleDbContext` with `ExcludeFromMigrations()`. That is what makes BR2 possible without sharing a connection between two contexts: staging a job puts it in the module's change tracker, and the module's own `SaveChanges` commits both rows. `JobsDbContext` owns the table and its migration.
- 2026-09-20 — Build: `IIdentityUnitOfWork` is added to the Application layer. `Application_ReferencesDomainAndContractsOnly` forbids a reference to `Simulab.Jobs` there, and the two notice emails (password changed, F-7 BR11) have no later save of their own to ride on, because `UserManager` has already written the password.
- 2026-09-20 — Build: one new package after all, `Microsoft.Extensions.Hosting.Abstractions` 10.0.12 — `BackgroundService` is not in a class library's framework reference. First-party, same version line as the other `Microsoft.Extensions.*` entries; no third-party job library was added.
- 2026-09-20 — Build: CA1711 ("do not end a name in Queue") is suppressed locally on `IJobQueue` and `DbContextJobQueue`, with the reason in the attribute. The name is the one the owner and the item use.
- 2026-09-20 — Build: `RecordingEmailSender` moved from the Identity tests to `tests/Simulab.Testing`; two test projects need it now.
- 2026-09-20 — Build: the three "the mail server fails" tests were rewritten. With the queue the request never reaches SMTP, so the old assertions proved nothing; they now assert that the answer is unchanged **and** that the failed send leaves the job for a retry instead of losing it.

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

### v2 — 2026-09-20
- What: (a) BR2 gains its stated exception — the password-changed notice is saved on its own, right after `UserManager` commits the password, because there is no later save to ride on; BR3 spells out the consequence. (b) BR7: a job given up on has its payload cleared; it keeps its type, its attempt count and the exception type, and the full exception stays in the log.
- Why: raised by `/agile:review` on 2026-09-20. (a) BR2 was written without exception and the code could not honour it for that one email; saying so beats a silent divergence. (b) The decision of question 7 promised that an erased account leaves no job row carrying its old address — true only when the send succeeds. A mail server that stays down was leaving a live reset link and a recipient address at rest indefinitely.
- Affected: BR2, BR3, BR7, AC6; every other criterion unchanged.
- Re-approved: 2026-09-20 (owner, in conversation: BR2, BR3, BR7, AC6)

## Validation script
Use an address you have not used before; the app host keeps its database between runs.

1. Start the app host (`dotnet run --project src/Hosts/Simulab.AppHost`) and open `https://localhost:7125/sign-up`.
2. Sign up as `valida.f13@exemplo.com` with the password `Estudar#2026!x`, accepting the three checkboxes. → "Check your email" appears at once; the page does not wait for a mail server.
3. In the Aspire dashboard open the `mailpit` endpoint. → "Confirm your email — Simulab" is there within a few seconds. Follow its link. → The account is verified and signs in.
4. Switch the language to **Português (Portugal)**, open `/forgot-password` and ask for a link for the same address. → The message in Mailpit is in pt-PT ("Redefina a sua palavra-passe").
5. On `/forgot-password` type `ninguem.f13@exemplo.com`, which does not exist. → The same screen, as fast as step 4, and no new message in Mailpit.
6. In the Aspire dashboard **stop** the `mailpit` resource. Wait a minute (the per-account cooldown) and ask for a reset link for `valida.f13@exemplo.com` again. → The page answers exactly as in step 4; nothing arrives, because nothing is running.
7. In a terminal, look at the queue, then start `mailpit` again in the dashboard, wait a minute (the first retry) and run the same command. → First one row with `attempts` 1 and the connection error; after the retry, no rows, and the email is in Mailpit.

   Git Bash:
   ```bash
   docker exec -e PGPASSWORD=postgres $(docker ps --format "{{.Names}}" | grep -i "^postgres" | head -1) psql -U postgres -d simulab -c "select status, attempts, left(coalesce(last_error,''), 60) as last_error from jobs.jobs;"
   ```
   PowerShell 7:
   ```powershell
   $pg = (docker ps --format "{{.Names}}" | Select-String -Pattern "^postgres").ToString(); docker exec -e PGPASSWORD=postgres $pg psql -U postgres -d simulab -c "select status, attempts, left(coalesce(last_error,''), 60) as last_error from jobs.jobs;"
   ```
8. Keyboard only: reload `/sign-up` and fill the whole form with `Tab`, `Space` (checkboxes) and `Enter`. → It can be completed and submitted without a mouse, and the focus ring is visible on every field.

## Delivery
- Branch: `feature/F-13`
- Merge: `ff28e8a` — merge(F-13): identity emails through the job queue (AB#723)
- Tests: `agile gate GREEN` — build 15 s with 0 warnings, full suite 533 green in 28 s across 9 projects (Identity 178, Web 272, Architecture 29, Persistence 14, SharedKernel 12, Jobs 12, Api 8, AppHost 6, Email 2). Budget < 5 min.
- Manual pages: none — nothing the end user sees changed, and no page promised an instant email. `docs/infra.md` gained the worker, `Jobs:WorkerEnabled` and the measured times.
- Validated on screen by the owner on 2026-09-20, following the script above.
