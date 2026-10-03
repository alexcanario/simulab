---
feature: F-27
epic: Foundation and identity
status: done
board: 746
version: 1
Autopilot: shipped
---
# Retention of failed jobs

Technical terms: [glossary](../glossary.md)

## Summary
Jobs given up on stay in `jobs.jobs` as `Failed` rows forever: they are the evidence of what was lost (F-13 BR7, no automatic cleanup). Decide how long that evidence is kept (for example 90 days) and add a cleanup that removes older `Failed` rows. Captured during the F-18 refinement (2026-09-23); the owner kept it out of F-18, whose index already stops those rows from slowing the claim query.

## Start
- Depends on: nothing. F-13 (`done`) built the queue, the worker and `JobPolicy`; F-18 (`done`) added the
  partial index the claim query uses. Both are on `main`.
- Waits on: nothing. The three answers were given by the owner on 2026-09-26.
- Suggested path: `/agile:autopilot F-27` — one constant, one `DELETE`, one guard in the worker's loop.
- Parallel with: anything outside `Simulab.Jobs`.

## What exists
- `Job` (`src/BuildingBlocks/Simulab.Jobs/Job.cs`) is infrastructure: no tenant, never soft deleted. A row that
  is given up on keeps `Type`, `Attempts`, `CreatedAt` and `LastError`; its `Payload` is already cleared at that
  moment (F-13 BR7), so the evidence holds no live link and no address.
- `JobStatus` has three values only — `Pending`, `Running`, `Failed`. A job that succeeds leaves no row (BR6).
- `JobWorker` (Api host) calls `JobRunner.RunPendingAsync` every `JobPolicy.PollInterval` (5 s). It is switched
  off by `JobOptions.WorkerEnabled`, which is how the test host drives the runner itself without waiting.
- **No periodic-task mechanism exists** beyond that loop: the cleanup has to live somewhere, which is what the
  owner's second answer decided.
- The numbers of the module live in `JobPolicy`; `JobOptions` holds only `WorkerEnabled`.
- No screen shows a job. `docs/infra.md` documents watching the queue by hand with a `select`.

## Goal
A failed job is evidence, and evidence has a shelf life. Keep it for a quarter, then let the table forget it,
without anyone remembering to run anything.

## Users and use cases
- UC1 An operator investigating an e-mail that never arrived finds its `Failed` row, with the error and the
  attempts, for as long as the retention lasts.
- UC2 Nobody has to prune `jobs.jobs` by hand: the app forgets rows older than the retention on its own.

## Business rules
- BR1 A `Failed` row is deleted once its `CreatedAt` is older than `JobPolicy.FailedRetention`, which is
  **90 days**. The constant sits in `JobPolicy` beside `MaxAttempts` and `StaleAfter`, not in configuration.
- BR2 Age is counted from `CreatedAt`, never from `StartedAt`: `CreatedAt` is always set, it is what the claim
  query orders by, and the distance between the two is at most the ~31 minutes of the retry backoff.
- BR3 Only `Failed` rows are deleted. A `Pending` or `Running` row is never touched however old it is — an old
  `Running` row is a stuck job the worker still takes again (F-13 BR10), not rubbish.
- BR4 The deletion is a real delete, not a soft delete: `Job` has none, and the row is the only thing to remove.
- BR5 The cleanup runs inside the worker's poll, at most once an hour, guarded by the last run instant held in
  memory. It never blocks the poll that runs the jobs: a cleanup that throws is logged and the poll goes on.
- BR6 The first poll after the process starts runs the cleanup: a process that never lives an hour would
  otherwise never clean.
- BR7 One `LogInformation` with the number of rows removed, and only when it removed some. A cleanup that finds
  nothing says nothing, so the log does not grow by one line an hour forever.
- BR8 The cleanup obeys `JobOptions.WorkerEnabled` like the rest of the worker: a test host drives it directly
  and no test waits for a timer.
- BR9 No new UI text. This feature has no screen, so the localization criterion that every item carries does
  not apply here; `docs/infra.md` gains the retention instead, where the queue is already documented.

## Screens and API
None. No route, no endpoint, no error code: the cleanup is a `DELETE` inside the worker's loop. The schema does
not change either — no column, no index, no migration.

## Acceptance criteria
- AC1 Given a `Failed` row created 91 days ago and another created 89 days ago, when the cleanup runs, then the
  first is gone from `jobs.jobs` and the second is still there.
- AC2 Given `Pending` and `Running` rows created a year ago, when the cleanup runs, then both are still there.
- AC3 Given the cleanup already ran less than an hour ago, when the worker polls again, then it does not run
  again and no row is touched.
- AC4 Given the cleanup last ran more than an hour ago, when the worker polls, then it runs again.
- AC5 Given a process that has just started, when the worker polls for the first time, then the cleanup runs.
- AC6 Given rows were removed, when the cleanup finishes, then one log line carries the count; given none were,
  then nothing is logged.
- AC7 Given the cleanup throws, when the worker polls, then the failure is logged and the jobs of that poll
  still run.
- AC8 Given `WorkerEnabled` is false, when the host starts, then neither the jobs nor the cleanup run.

## Decisions
- 2026-09-26 — The retention is 90 days, a constant in `JobPolicy` (BR1) — the module's other numbers live
  there, and a configuration knob is one more thing to test and document for a value nobody will turn
  (owner, question 1).
- 2026-09-26 — The cleanup runs inside the worker's poll, at most once an hour (BR5) — the worker already
  exists, is already switched off in tests by `WorkerEnabled`, and the runner is already driven directly, so
  no new mechanism and no new way to hang a test. A self-re-enqueuing job was offered and left out: if its row
  were ever lost the cleanup would stop in silence, and it is the cleanup that would have deleted it
  (owner, question 2).
- 2026-09-26 — One log line when something was removed, nothing when not (BR7) — a metric was offered and left
  out: nothing reads it today (owner, question 3).
- 2026-09-26 — Age counts from `CreatedAt` (BR2) — decided here, not asked: it is always set, the index orders
  by it, and the gap to `StartedAt` is at most the backoff.
- 2026-09-26 — No new package: the work is one constant, one `DELETE` and its tests, with the libraries the
  Jobs test project already has (rule `build-config`).

## Out of scope
- A screen that lists failed jobs, or any way to read them outside the database.
- Re-enqueuing a failed job, by hand or otherwise.
- Retention for anything other than `jobs.jobs`.
- Making the retention configurable per environment (offered at refinement and left out).

## Open questions
<!-- Approval is blocked while any line here is not answered or marked "deferred (owner, YYYY-MM-DD)". -->
- (none)

## Change notes
<!-- Added by /agile:change during build. Increase `version` in the header. -->
<!--
### v2 — YYYY-MM-DD
- What: <change>
- Why: <reason>
- Affected: <BR/AC ids>; other criteria unchanged.
- Re-approved: <YYYY-MM-DD>
-->

## Coverage

| Criterion | Test |
|---|---|
| AC1 a row past the retention goes, a younger one stays | `JobCleanupTests.Run_AFailedRowPastTheRetention_IsRemovedAndAYoungerOneStays` (91 and 89 days) |
| AC2 `Pending` and `Running` are never deleted | `JobCleanupTests.Run_PendingAndRunningRows_AreNeverTouchedHoweverOldTheyAre` (both a year old) |
| AC3 not due inside the hour | `JobCleanupTests.RunIfDue_LessThanAnHourAfterTheLastRun_DoesNothing` (one second short) |
| AC4 due again after the hour | `JobCleanupTests.RunIfDue_AnHourAfterTheLastRun_RunsAgain` |
| AC5 the first poll of a process cleans | `JobCleanupTests.RunIfDue_TheFirstCall_Runs` |
| AC6 one line with the count, silence when nothing went | `JobCleanupTests.Run_RowsRemoved_LogsOneLineWithTheCount`, `.Run_NothingToRemove_LogsNothing` |
| AC7 a cleanup that throws is logged and the jobs still run | `JobWorkerTests.Poll_TheCleanupThrows_ItIsLoggedAndTheJobsStillRun` — the real runner and queue, with a scope factory that cannot open a scope |
| AC8 switched off runs neither | `JobWorkerTests.Start_WorkerDisabled_RunsNeitherTheJobsNorTheCleanup` |
| BR2 age counts from `created_at` | `JobCleanupTests.Run_AgeIsCountedFromCreatedAt` |
| BR9 no new UI text | Nothing to check: the feature has no screen and adds no resource key. `docs/infra.md` carries the retention instead |

All ten run against a real PostgreSQL container, like the rest of the queue's tests: the deletion is a statement,
not a loop in memory.

## Validation script
This feature has no screen. There is nothing to look at, so the check is the tests and, if you want to see it
happen for real, one row in the local database. Steps 1 and 2 were both run before this script was handed over.

1. Run the cleanup's tests. Git Bash:

   ```bash
   cd D:/dev/_icontrol/wt/simulab/feature-27 && dotnet test tests/BuildingBlocks/Simulab.Jobs.Tests/Simulab.Jobs.Tests.csproj --nologo --filter "FullyQualifiedName~JobCleanupTests|FullyQualifiedName~JobWorkerTests"
   ```

   PowerShell 7:

   ```powershell
   Set-Location D:/dev/_icontrol/wt/simulab/feature-27; dotnet test tests/BuildingBlocks/Simulab.Jobs.Tests/Simulab.Jobs.Tests.csproj --nologo --filter "FullyQualifiedName~JobCleanupTests|FullyQualifiedName~JobWorkerTests"
   ```

   → `Passed! - Failed: 0, Passed: 10, Skipped: 0, Total: 10`.

2. Read the ten test names in that output against the table above → each criterion is named by a test that says
   what it checks, including the two halves of AC6 and the broken cleanup of AC7.
3. Open `src/BuildingBlocks/Simulab.Jobs/JobPolicy.cs` → `FailedRetention` is 90 days and `CleanupInterval` is
   one hour, beside `MaxAttempts` and `StaleAfter`. Changing the retention is a code change, not a setting.
4. Open `docs/infra.md` → the retention, where it runs and what it never deletes are written under the
   background-work line.

Optional, to watch it happen on the dev database (it changes one row of your local `jobs.jobs`):

5. With the app host stopped, connect to the local jobs database and insert one expired row:

   ```sql
   insert into jobs.jobs (id, type, payload, status, attempts, created_at, run_after, last_error)
   values (gen_random_uuid(), 'test.retention', '', 2, 5, now() - interval '91 days', now(), 'seeded by hand');
   ```

6. Start the app host and read the `Simulab.Api` log in the Aspire dashboard → within five seconds,
   `Removed 1 failed job(s) older than 90 days.`
7. Run `select count(*) from jobs.jobs where type = 'test.retention';` → `0`. Any other `Failed` row younger
   than 90 days is still there.
8. Sign up with an address you do not use elsewhere → the verification e-mail still arrives in Mailpit: the
   cleanup did not disturb the queue it shares a poll with.

## Delivery
- Branch: feature/F-27
- Merge: 9a4e8e6 (`Merge feature/F-27: retention of failed jobs (AB#746)`), 10 files, +525/-23
- Tests: 1327 passed, 0 failed, 92 s (full suite, 2026-09-26); full build 24 s, 0 new warnings
- Manual pages: none. The feature has no screen and changes nothing an end user sees, so `docs/manual/` is
  untouched; the retention is documented in `docs/infra.md`, where the queue already was.
- Ran under `/agile:autopilot`: stop 1 took the three recommended answers, stop 2 the validation and the merge.
- Fixed during the build, not hidden: a new `CA1873` warning on the log call (two boxed ints) became a
  source-generated `[LoggerMessage]`, the pattern `ExportDataHandler` already uses. Confirmed with a
  `--no-incremental` build at `0 Warning(s)`.
