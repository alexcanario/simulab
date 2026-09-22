---
feature: F-18
epic: Foundation and identity
status: validating
board: 731
version: 2
---
# Job claim index for stale rows

## Summary
The claim query of the job queue (F-13, `JobRunner.ClaimSql`) filters on `(status = Pending AND run_after <= now) OR (status = Running AND started_at <= stale)`. Only the first branch is covered by `ix_jobs_status_run_after_created_at`, so PostgreSQL cannot use the index for the OR and every poll — one every 5 seconds — scans `jobs.jobs` whole. It costs nothing while the table is small, but BR7 lets `Failed` rows accumulate with no cleanup, so the scan grows for good. Either add an index over `(status, started_at)` and let the planner use both, or split the claim into two statements. Found by `/agile:review` on F-13 (2026-09-20) and deferred by the owner: ADR-0001 #20 says the volume is low.

## Goal
The cost of each poll of the job queue depends only on the jobs still to run, never on how many `Failed` rows have been kept as evidence.

## What already exists (verified 2026-09-23)
- `JobRunner.ClaimSql` (`src/BuildingBlocks/Simulab.Jobs/JobRunner.cs:22`) claims one row: `WHERE (status = 0 AND run_after <= now) OR (status = 1 AND started_at <= now - 5 min) ORDER BY created_at FOR UPDATE SKIP LOCKED LIMIT 1`. Statuses: `Pending = 0`, `Running = 1`, `Failed = 2` (`JobStatus.cs`).
- `JobWorker` calls `JobRunner.RunPendingAsync` every 5 s (`JobPolicy.PollInterval`), up to 100 jobs per poll; every successful job is deleted (BR6), so only `Pending`, `Running` and `Failed` rows stay.
- One index, `ix_jobs_status_run_after_created_at` over `(status, run_after, created_at)` (`JobConfiguration.cs:33`, migration `20260920152540_InitialJobs`).
- `Failed` rows are kept with no cleanup (F-13 BR7).
- Tests: `Simulab.Jobs.Tests` runs the queue on a real PostgreSQL container (`JobTestHost`).
- No item touched this code after F-13 (`ed24feb`, `572f4c5`).
- **Premise corrected:** "PostgreSQL cannot use the index for the OR" is not true in principle — the index starts with `status`, so both branches can use it and the planner may combine them (BitmapOr). Whether it does, or scans the table, depends on the statistics; not measured. The risk the item names stands either way: the plan is up to the planner, and `Failed` rows sit in the index and the table the claim reads.
- **Measured during build (2026-09-23)**, 10,000 `Failed` rows and one due job, after `ANALYZE`: with the old index the planner already avoided a sequential scan — two bitmap index scans combined by `BitmapOr`, then a `Sort` on `created_at`, estimated cost 16.23. With the partial index: one `Index Scan using ix_jobs_active_created_at`, already in `created_at` order, no sort, estimated cost 8.46. The owner kept F-18 for that simpler plan and an index that does not grow with `Failed` rows (change note v2).

## Users and use cases
- UC1 The Api host's job worker claims the next due job every 5 seconds; with thousands of `Failed` rows kept, the claim reads only the active rows.

## Business rules
- BR1 The claim query reads through an index that holds only the active rows (`Pending` and `Running`): a `Failed` row is never in it.
- BR2 The claim's behaviour does not change: same rows due, same order (oldest `created_at` first), same `SKIP LOCKED`, same stale-row retake after 5 minutes (F-13 BR8-BR10).
- BR3 `Failed` rows are still kept (F-13 BR7 unchanged).

## Screens and API
- No screen, route, endpoint or error code. One migration in the `jobs` schema.

## Acceptance criteria
- AC1 Given `jobs.jobs` with 10,000 `Failed` rows and a few `Pending` and `Running` rows, analyzed, when the claim query is explained, then the plan reads `jobs.jobs` through `ix_jobs_active_created_at` alone, in `created_at` order: no sequential scan, no bitmap scan and no sort. (BR1)
- AC2 Given the model and the migrations, when the schema is built, then the index is partial on `status IN (Pending, Running)` and `ix_jobs_status_run_after_created_at` is gone. (BR1)
- AC3 Given the existing `JobRunnerTests` (due, not yet due, oldest first, stale retake, `SKIP LOCKED`, `Failed` never taken), when they run after the migration, then all pass unchanged. (BR2, BR3)
- AC4 No UI text; nothing to localize.

## Decisions
- 2026-09-23 — Approved by the owner ("aprovo F-18").
- 2026-09-23 — The fix is a partial index over the active rows, replacing the current index, not the `(status, started_at)` index nor a split claim — Claude: the partial index keeps `Failed` rows out whatever the planner does with the OR, needs no change to the runner and adds no round trip; the other two options still read an index that grows with every `Failed` row.
- 2026-09-23 — The index is proven by an `EXPLAIN` test on the test container with 10,000 `Failed` rows after `ANALYZE` — Claude: the planner picks by statistics, so only a plan over a realistic table proves the claim; asserting the index exists is not enough.
- 2026-09-23 — Retention of `Failed` rows stays out of F-18 and is captured as F-27 (board 746) — owner: F-18 only fixes the index, and with it the kept rows no longer weigh on the claim; how long to keep the evidence is its own decision.
- 2026-09-23 — No new package — Claude: `Simulab.Jobs.Tests` already runs a PostgreSQL container.
- 2026-09-23 — The runner changes after all: `ClaimSql` states `Pending` and `Running` as constants instead of parameters, and is `internal` (with `InternalsVisibleTo Simulab.Jobs.Tests`) so the plan test explains the runner's own statement — Claude: a parameter can hide the status from a cached generic plan, and then the planner cannot prove the partial index's filter; corrects "no change to the runner" above.
- 2026-09-23 — An empty Identity migration, `JobsActiveIndexSnapshot`, moves the Identity snapshot — Claude: `IdentityModuleDbContext` maps the job table excluded from its migrations (F-13), so its snapshot names the index; without it EF reports pending model changes at startup.

## Out of scope
- Cleanup or retention of `Failed` rows: F-27.
- Any change to the poll interval, the batch size or the stale timeout.

## Open questions
- (none)

## Change notes
### v2 — 2026-09-23
- What: AC1 requires the claim plan to read `jobs.jobs` through `ix_jobs_active_created_at` alone, in order, with no sequential scan, no bitmap scan and no sort; "What already exists" records the measured plans.
- Why: the premise was measured false during build — the old index already avoided a sequential scan (BitmapOr + Sort, cost 16.23). The partial index wins by a single ordered index scan (cost 8.46); the owner kept the item (option a).
- Affected: AC1; other criteria unchanged.
- Re-approved: 2026-09-23 ("aprovo").

## Validation script
No screen, no UI text and no permission: the item changes an index. Claude did not start the app host: its first start applies `ActiveJobsIndex` to the local development database, which is the owner's data (rule: never change a database outside the test containers). Steps 2-4 are that check.
1. In `D:\dev\_icontrol\wt\simulab\feature-18`, run the plan tests (run by Claude in both shells on 2026-09-23):
   - Git Bash: `dotnet test tests/BuildingBlocks/Simulab.Jobs.Tests --filter "FullyQualifiedName~JobClaimPlanTests"`
   - PowerShell 7: `dotnet test tests/BuildingBlocks/Simulab.Jobs.Tests --filter "FullyQualifiedName~JobClaimPlanTests"`
   → `Passed!  - Failed:     0, Passed:     3, Skipped:     0, Total:     3`. Repeat the same command as often as you like; each run makes its own database.
2. Stop any app host running from `D:\dev\_icontrol\simulab`. In the worktree folder run `dotnet run --project src/Hosts/Simulab.AppHost --launch-profile https` and open the dashboard → the `api` resource reaches `Running` (the migration applied without error).
3. Open https://localhost:7125/forgot-password, type the email of your own test account and send → the page says the link is on its way.
4. Open Mailpit from the dashboard → the password reset email arrives within about 5 seconds: the worker claimed the job through the new index. Switch the page to English once before sending, to see the usual language switch still works.

## Coverage
| Criterion | Test |
|---|---|
| AC1 | `JobClaimPlanTests.Claim_ManyFailedRowsKept_ReadsOnlyTheActiveRowsIndex` (explains the runner's own `ClaimSql`) |
| AC2 | `JobClaimPlanTests.Migrations_JobTable_HasThePartialActiveIndexOnly` (migrated by `MigrateJobsAsync`, as the Api host does) |
| AC3 | `JobRunnerTests` (12, unchanged) and `JobClaimPlanTests.RunPending_ManyFailedRowsKept_RunsTheDueAndTheStaleJobs`; `Simulab.Api.Tests` and `Simulab.Identity.Tests` green with the new migrations |
| AC4 | No resource change |

## Delivery
<!-- Filled by /agile:ship. -->
- Branch: <feature/F-<number>>
- Merge: <commit>
- Tests: <count, duration>
- Manual pages: <paths>
