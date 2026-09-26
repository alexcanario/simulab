---
bug: B-19
feature: F-27
status: validating
board: 772
severity: high
---
# The full suite is not reliably green: tests fail under parallel load

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

Measured again at the refinement, in the worktree `b-19-full-suite-flaky` (branch `bug/B-19`, `0f344a1`),
three consecutive `dotnet test Simulab.slnx`, nothing else changed — this is the **baseline the fix is compared
against**:

| Run | Result | Failing test |
|---|---|---|
| 1 | red | `JobWorkerTests.Start_WorkerDisabled_RunsNeitherTheJobsNorTheCleanup` **and** `ExamsPageTests.Search_SendsTheTermToTheServer` |
| 2 | red | `ExamsPageTests.Search_SendsTheTermToTheServer` |
| 3 | green | — |

2 of 3 runs red. The title says "two tests"; it is at least three, and the most frequent one
(`Search_SendsTheTermToTheServer`, 2 of 3) was not in the original report. `MainLayoutTests` did not fail once in
these three.

## Start
- Depends on: nothing.
- Waits on: nothing.
- Suggested path: `/agile:refine` first — the cause of each test has to be reproduced before any fix, and the two
  may have nothing in common. The loop the rules ask for (clean build before each run, N green in a row) is the
  only proof a fix works.
- Parallel with: anything. It touches test code.

## Cause
Reproduced on 2026-09-26 in the worktree `b-19-full-suite-flaky`, first run of `dotnet test Simulab.slnx`:
two failures in one run, and one of them is a test B-19 did not list. There is no single cause; there are two,
and both are in the tests, not in the product code.

**C1 — `JobWorkerTests.Start_WorkerDisabled_RunsNeitherTheJobsNorTheCleanup`
(`tests/BuildingBlocks/Simulab.Jobs.Tests/JobWorkerTests.cs:55-61`): the worker's body may never run at all.**
`BackgroundService.StartAsync` does not call `ExecuteAsync` on the calling thread. In .NET 10
(`dotnet/runtime`, `release/10.0`, `Microsoft.Extensions.Hosting.Abstractions/src/BackgroundService.cs`) it is:

```csharp
_stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
_executeTask = Task.Run(() => ExecuteAsync(_stoppingCts.Token), _stoppingCts.Token);
return Task.CompletedTask;
```

`Task.Run(delegate, token)` with a token that is cancelled **before the thread pool dequeues the work item never
runs the delegate at all** — the task goes straight to `Canceled`. The test calls `StartAsync` and then
`StopAsync` (`JobWorkerTests.cs:55,57`), and `StopAsync` cancels that very token. Alone, the pool is idle and the
delegate runs in microseconds, so the log line is always there: 3/3 green. Under the whole solution in parallel
the pool is saturated, the work item waits, `StopAsync` cancels first, `JobWorker.ExecuteAsync` never executes and
never writes `"The job worker is switched off by configuration."` (`src/BuildingBlocks/Simulab.Jobs/JobWorker.cs:23`).
That is exactly the observed failure: the first two assertions pass **because nothing ran**, and only the log
assertion (`JobWorkerTests.cs:61`) fails. The product code is correct; the test proves "the worker did nothing" with
evidence that also disappears when the worker was never started.

The sibling test `Poll_TheCleanupThrows_ItIsLoggedAndTheJobsStillRun` (`JobWorkerTests.cs:36`) does not have this
hole: it waits with `WaitUntil` until the worker has visibly done its work before cancelling.

**C2 — bUnit tests behind the 300 ms debounce run out of the default 1-second wait.**
`ExamsPageTests.Search_SendsTheTermToTheServer` (`tests/Hosts/Simulab.Web.Tests/Catalog/ExamsPageTests.cs:51`)
failed in the same run with `WaitForFailedException`, `Check count: 62`. The search box is
`AppDataTable.razor:25`, `DebounceInterval="300"` — MudBlazor's debounce is a real `System.Timers.Timer`, not the
`TimeProvider` the rest of the code uses, so the test really has to wait 300 ms of wall clock, then a render, then
the fake API call. bUnit's default `WaitForAssertion` timeout is 1 second, so the budget under load is 700 ms for
work the loaded machine does not deliver in time. `AppLookupField.razor:22,75` has the same 300 ms debounce, so
every test that types into a lookup field is in the same family.

**C3 — `MainLayoutTests.MenuButton_Desktop_TogglesCollapsedAndWritesCookie`: not reproduced, and reading does not
explain it.** Unlike its siblings (`MainLayoutTests.cs:58,99`) it clicks without first waiting for the viewport
subscription, which arrives from `OnAfterRenderAsync` (`MainLayout.razor:93-101`). But `MenuButtonLabel`
(`MainLayout.razor:78`) reads `_isDesktop == false`, so a still-null `_isDesktop` gives the same labels as desktop,
and every assertion in that test survives it on paper. It stays open: see `## Open questions`.

No duplicate of either cause was found in product code — both are test-side. C1 is the only place in the solution
that starts a `BackgroundService` and stops it without waiting for it (`grep` for `StartAsync(` over `tests/`).

## Evidence for the Jobs one
- `JobWorkerTests.Start_WorkerDisabled_RunsNeitherTheJobsNorTheCleanup:61` asserts a log entry
  (`"switched off"`) written by `JobWorker.ExecuteAsync` (`src/BuildingBlocks/Simulab.Jobs/JobWorker.cs:23`).
  The two assertions before it pass, so the worker really did nothing — only the log entry is missing.
- The failure message dumps a `Logs.Entries` full of EF migration SQL (`COMMENT ON COLUMN jobs.jobs.last_error ...`),
  so that host did run its migrations in that run. The provider these tests use is
  `tests/BuildingBlocks/Simulab.Jobs.Tests/RecordingLoggerProvider.cs` — a `List` behind a `lock`, unbounded — so
  nothing is dropped for capacity or lost to a data race. (The report first pointed at
  `tests/Simulab.Testing/RecordingLoggerProvider.cs`; that one is a different class with the same name, not the one
  wired in `JobTestHost.BuildServices`.)
- `_host` is per test (`IAsyncLifetime`), and the worker's logger is `_host.Logs.CreateLogger<JobWorker>()`
  (`JobWorkerTests.cs:16,25`), so the entry and the assertion are on the same list. **Answered at the refinement:**
  the entry is missing because `ExecuteAsync` never ran — see C1 in `## Cause`.
- Both the test and the code under it came from F-27 (`69dbd9e`), merged on 2026-09-26.

## Evidence for the Web one
- `ExamsPageTests.Search_SendsTheTermToTheServer` failed in 2 of the 3 refinement runs with
  `Bunit.Extensions.WaitForHelpers.WaitForFailedException`, `Check count: 62`, `Component render count: 198`: the
  page rendered, the typed term simply had not reached the fake API inside bUnit's default 1-second wait. See C2.
- `MainLayoutTests.MenuButton_Desktop_TogglesCollapsedAndWritesCookie` failed once on `main` and did not fail in
  any of the 3 refinement runs. Not reproduced; see C3 and `## Open questions`.

## Why it matters now
The definition of done requires the full suite green before a merge, so any item can be blocked by a test it has
nothing to do with — and worse, a real failure is easy to wave away as "the usual flake" once the suite has one.
F-28 is sitting at `validating` because of this.

## Expected
- AC1 Given the whole solution run in parallel (`dotnet test Simulab.slnx`), when it is run 5 times in a row with
  a clean build before each, then all 5 are green.
- AC2 Given `JobWorkerTests.Start_WorkerDisabled_RunsNeitherTheJobsNorTheCleanup`, when the worker is started and
  then stopped, then the test waits for the worker to have reached its decision before stopping it, so the test
  never asserts on a worker that the thread pool has not run yet.
- AC3 Given the worker is switched off, when it is stopped without ever having been scheduled, then the test
  fails rather than passing by accident: the assertion that nothing ran does not stand alone as proof.
- AC4 Given any bUnit test in `Simulab.Web.Tests` that waits behind the 300 ms debounce, when the machine is under
  the load of the whole solution, then the wait budget is large enough that the debounce is not the reason it
  fails — the bUnit default wait timeout is raised once, in the shared test context, not test by test.
- AC5 Given the raised timeout, when the suite is green, then it is no slower: the timeout is a maximum, and the
  measured duration of `Simulab.Web.Tests` stays within its current range.
- AC6 No product code changes: the diff touches only files under `tests/`.
- AC7 No new UI text: the localization criterion does not apply and the missing-key test stays green.

## Fix
- **C1** (`tests/BuildingBlocks/Simulab.Jobs.Tests/JobWorkerTests.cs`): before `StopAsync`, wait until the worker
  has reached its decision, the way the sibling test already does with `WaitUntil` (`JobWorkerTests.cs:36,65`).
  The test then proves what it claims — the worker ran and chose to do nothing — instead of passing whenever the
  worker never started. Test-only.
- **C2** (`tests/Hosts/Simulab.Web.Tests`): raise bUnit's default wait timeout once, in the shared test context,
  from its 1-second default to 5 seconds. One knob covers every test behind the 300 ms debounce
  (`AppDataTable.razor:25`, `AppLookupField.razor:22`) instead of a timeout argument per call. A timeout is a
  maximum, so a green run does not get slower.
- **C3**: nothing. Deferred, see `## Open questions`.

## Regression test
- C1: the changed `Start_WorkerDisabled_RunsNeitherTheJobsNorTheCleanup` is itself the regression test. It is seen
  failing first the way the race makes it fail — with the worker never scheduled — and green after the fix.
  Reproducing that state deterministically (rather than waiting for a loaded machine) is the build's job; if it
  cannot be forced, the build says so and falls back to the 5-run loop as the only evidence.
- C2: `ExamsPageTests.Search_SendsTheTermToTheServer` is the regression test; it failed in 2 of 3 baseline runs.
- Both: the 5 consecutive green full-suite runs of AC1, against the recorded baseline of 2 red in 3.

**Seen failing first, deterministically (C1).** The race was forced instead of waited for, with a temporary
diagnostic (`ThreadPoolDelayDiagnosticTests`, deleted before the fix commit, the way B-11 did): every pool thread
busy for 1.5 s, released from a dedicated thread, so the work item `StartAsync` queues is not dequeued before
`StopAsync` cancels it. Two cases, one run of
`dotnet test tests/BuildingBlocks/Simulab.Jobs.Tests/Simulab.Jobs.Tests.csproj`:

```
Simulab.Jobs.Tests.ThreadPoolDelayDiagnosticTests.Old_TheWorkerIsStoppedBeforeThePoolRunsIt [FAIL]
  ... to have an item matching entry.Item2.Contains("switched off", OrdinalIgnoreCase).
Failed!  - Failed: 1, Passed: 1, Skipped: 0, Total: 2, Duration: 9 s - Simulab.Jobs.Tests.dll (net10.0)
```

`Old` is today's body and went red with exactly the message the loaded machine produces; `Fixed` is the body with
the wait and went green in the same run. That is the cause proven, not inferred.

**Proof loop (AC1).** `dotnet build Simulab.slnx --no-incremental` then `dotnet test Simulab.slnx` (`--no-build`),
5 times in a row in the worktree, at `07b00ac`:

| Run | Build | Tests | Warnings |
|---|---|---|---|
| 1 | ok | 1343 passed, 0 failed | 0 |
| 2 | ok | 1343 passed, 0 failed | 0 |
| 3 | ok | 1343 passed, 0 failed | 0 |
| 4 | ok | 1343 passed, 0 failed | 0 |
| 5 | ok | 1343 passed, 0 failed | 0 |

5 of 5 green, against the baseline of 2 red in 3 on the same machine and the same worktree.

**AC5.** `Simulab.Web.Tests` in those five runs: 13 s, 12 s, 12 s, 11 s, 11 s — the baseline runs were 12 s. The
raised timeout costs nothing on a green run, as expected of a maximum.

## Decisions
- 2026-09-26 — C2 is fixed for the whole family with one knob (the shared bUnit default wait timeout), not per
  test (owner) — the family is every test behind a 300 ms debounce, and a per-test timeout leaves the rest at the
  same 700 ms of slack.
- 2026-09-26 — the debounce keeps using MudBlazor's real timer; moving it to `TimeProvider` is not done here
  (owner) — that is production UI code shared by every list and lookup, an item of its own, not a bug fix.
  Captured as an idea.
- 2026-09-26 — C1 is fixed in the test, not in `JobWorker` (owner) — the product follows the `BackgroundService`
  contract correctly; changing production code to make a test easier would be the wrong direction.
- 2026-09-26 — C3 is deferred rather than blocking this bug (owner) — the green suite is what unblocks F-28, F-29
  and F-30, and one failure seen once in six runs does not justify holding all three.
- 2026-09-26 — proof is 5 consecutive green full-suite runs with a clean build before each (owner) — the baseline
  is 2 red in 3, so 3 green could still be luck.
- 2026-09-26 — no new package (Claude) — both fixes use xUnit, bUnit and the test helpers already referenced.

## Open questions
- `MainLayoutTests.MenuButton_Desktop_TogglesCollapsedAndWritesCookie` (C3): failed once on `main`, not reproduced
  in the 3 refinement runs, and reading the component does not explain it — `MenuButtonLabel`
  (`MainLayout.razor:78`) treats a null `_isDesktop` as desktop, so a late viewport subscription changes none of
  the test's assertions. `deferred (owner, 2026-09-26)`: if it reappears in the 5-run proof, it comes back into
  this bug; if it appears later on its own, it becomes its own bug with a fresh repro.

## Validation script
No screen: the item changes test code only. Everything below runs in the worktree
`D:\dev\_icontrol\wt\simulab\b-19-full-suite-flaky`. Every command was run here before the script was handed over,
in both shells, and gave the output quoted.

1. The whole change is two test files and nothing else (AC6).
   Git Bash: `cd /d/dev/_icontrol/wt/simulab/b-19-full-suite-flaky && git diff main --stat`
   PowerShell 7: `cd D:\dev\_icontrol\wt\simulab\b-19-full-suite-flaky; git diff main --stat`
   → three files: this bug file, `tests/BuildingBlocks/Simulab.Jobs.Tests/JobWorkerTests.cs | 9 +` and
   `tests/Hosts/Simulab.Web.Tests/Ui/KitTestContext.cs | 6 +`. The only code is those two test files, 15 lines
   added and none removed; nothing under `src/`.
   (The branch was brought up to date with `main` first. `main` had gained one commit since the branch started,
   `32d4e0a`, the F-48 idea file — docs only, no code and no test, so the proof loop below still stands.)

2. Read the two changes and see that they say why (AC2, AC4):
   `JobWorkerTests.cs` now waits for the worker's own log line before `StopAsync`; `KitTestContext.cs` sets
   `DefaultWaitTimeout = TimeSpan.FromSeconds(5)`. Nothing under `src/`.

3. The C1 regression test, green (AC2).
   Git Bash: `dotnet test tests/BuildingBlocks/Simulab.Jobs.Tests/Simulab.Jobs.Tests.csproj --nologo`
   PowerShell 7: `dotnet test tests\BuildingBlocks\Simulab.Jobs.Tests\Simulab.Jobs.Tests.csproj --nologo`
   → `Passed! - Failed: 0, Passed: 25, Skipped: 0, Total: 25`.

4. The C2 regression test and its family, green (AC4, AC7 — the missing-key test is in this project).
   Git Bash: `dotnet test tests/Hosts/Simulab.Web.Tests/Simulab.Web.Tests.csproj --nologo`
   PowerShell 7: `dotnet test tests\Hosts\Simulab.Web.Tests\Simulab.Web.Tests.csproj --nologo`
   → `Passed! - Failed: 0, Passed: 616, Skipped: 0, Total: 616`, in about 12 s (AC5: the baseline was also 12 s).

5. The thing the bug is actually about (AC1). One run is not the proof — the baseline was green 1 time in 3, so a
   single green run proves nothing. The 5-run loop is recorded above in `## Regression test`; to repeat it:
   Git Bash: `for i in 1 2 3 4 5; do dotnet build Simulab.slnx --no-incremental && dotnet test Simulab.slnx --nologo --no-build; done`
   PowerShell 7: `1..5 | ForEach-Object { dotnet build Simulab.slnx --no-incremental; dotnet test Simulab.slnx --nologo --no-build }`
   → 5 runs, each `1343 passed, 0 failed`, each build `0 Warning(s)`. Takes about 25 minutes.

6. What is deliberately still open: `MainLayoutTests.MenuButton_Desktop_TogglesCollapsedAndWritesCookie` (C3) was
   never reproduced and is deferred — see `## Open questions`. It did not fail in any of the 8 full-suite runs of
   this item (3 baseline + 5 proof).

## Precedent
B-11 (`docs/bugs/B-11-flaky-overflow-menu-test.md`, done) was the same shape: a Web test that asserted on the line
after `Click()` instead of waiting. It produced the project rule at `.claude/rules/agile/project.md:71` — "after a
click whose handler awaits, assert what follows with `WaitForAssertion`, never on the line after `Click()` (F-8)".
B-19 is the two cases that rule does not reach: a hosted service that the thread pool has not scheduled yet (C1),
and a wait that is written correctly but is given a budget too small for a real 300 ms timer under load (C2).
Whether either deserves a rule of its own is for the retro, not for this file.

## Delivery
<!-- Filled by /agile:ship. -->
- Branch: bug/B-19
- Merge: <commit>
- Tests: <count, duration>
- Manual pages: none (no visible behavior changed)
