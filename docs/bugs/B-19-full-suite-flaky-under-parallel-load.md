---
bug: B-19
feature: F-27
status: refining
board: 772
severity: high
---
# The full suite is not reliably green: two tests fail under parallel load

## What happens
`dotnet test Simulab.slnx` runs the test projects in parallel, and two tests lose races there that they never lose
alone. It stopped the merge of F-28 twice, on a change that touches neither of them.

Measured on 2026-09-26, all runs of the whole solution unless said otherwise:

| Where | Runs | Result |
|---|---|---|
| `feature/F-28` (`0e6d15a`), `gate.js ship` | 2 | 2 red, both `Simulab.Jobs.Tests.JobWorkerTests.Start_WorkerDisabled_RunsNeitherTheJobsNorTheCleanup` |
| `main` (`f598227`), clean worktree | 3 | 1 red — `Simulab.Web.Tests.Layout.MainLayoutTests.MenuButton_Desktop_TogglesCollapsedAndWritesCookie` — and 2 green |
| `Simulab.Jobs.Tests` alone, the failing test only | 3 | 3 green |
| `Simulab.Jobs.Tests` alone, the whole project | 3 | 3 green |

So neither failure belongs to F-28, and `main` is not reliably green either. The Jobs one reproduced on both
attempts of one branch and on none of three of another, which is what a load-sensitive race looks like.

1. In a clean worktree of `main`, run `dotnet test Simulab.slnx` a few times in a row.
2. One run in three fails, on one of the two tests above, while each passes alone.

## Start
- Depends on: nothing.
- Waits on: nothing.
- Suggested path: `/agile:refine` first — the cause of each test has to be reproduced before any fix, and the two
  may have nothing in common. The loop the rules ask for (clean build before each run, N green in a row) is the
  only proof a fix works.
- Parallel with: anything. It touches test code.

## Evidence for the Jobs one
- `JobWorkerTests.Start_WorkerDisabled_RunsNeitherTheJobsNorTheCleanup:61` asserts a log entry
  (`"switched off"`) written by `JobWorker.ExecuteAsync` (`src/BuildingBlocks/Simulab.Jobs/JobWorker.cs:23`).
  The two assertions before it pass, so the worker really did nothing — only the log entry is missing.
- The failure message dumps a `Logs.Entries` full of EF migration SQL (`COMMENT ON COLUMN jobs.jobs.last_error ...`),
  so that host did run its migrations in that run. `RecordingLoggerProvider` is an unbounded `ConcurrentQueue`
  (`tests/Simulab.Testing/RecordingLoggerProvider.cs:9`), so nothing is being dropped for capacity.
- `_host` is per test (`IAsyncLifetime`), and the worker's logger is `_host.Logs.CreateLogger<JobWorker>()`
  (`JobWorkerTests.cs:16,25`), so the entry and the assertion are on the same queue. Why it is missing is **not
  explained**; that is the first thing the refinement has to reproduce.
- Both the test and the code under it came from F-27 (`69dbd9e`), merged on 2026-09-26.

## Evidence for the Web one
- `MainLayoutTests.MenuButton_Desktop_TogglesCollapsedAndWritesCookie` failed once on `main` and was not
  investigated. Not reproduced yet.

## Why it matters now
The definition of done requires the full suite green before a merge, so any item can be blocked by a test it has
nothing to do with — and worse, a real failure is easy to wave away as "the usual flake" once the suite has one.
F-28 is sitting at `validating` because of this.
