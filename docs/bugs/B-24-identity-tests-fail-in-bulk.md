---
bug: B-24
feature: -
epic: Foundation and identity
status: approved
board: 136
severity: medium
---
# Identity tests fail in bulk in one full gate run

Technical terms: [glossary](../glossary.md)

## What happens
On 2026-10-05, at the ship of F-51, the first `node gate.js ship` run (worktree `D:\wt\simulab\f-51-import-guard`, 19 worktrees open on the machine) ended `agile gate RED: tests failed (Simulab.slnx)` with 24 of 408 tests failing in `Simulab.Identity.Tests`, for example `FixEnumColumnDescriptionsMigrationTests`, `SeedAdminTests`, `TotpTests`, `RegistrationEndpointTests`. The failures were spread over unrelated classes, not one test.
1. Run `dotnet test tests/Modules/Identity/Simulab.Identity.Tests` alone: 408 passed, 0 failed (1 m 34 s).
2. Run `node gate.js ship` again, same tree: `agile gate GREEN`, 2198 tests, 141 s.
3. The change under test (F-51) only added a data migration to the Catalog module and its tests.

## Start
- Depends on: nothing. Related: B-19 (done), which documented that a red full run is a real failure, not "the usual flake" (`docs/infra.md`, "Measured times"); this occurrence puts that premise in doubt.
- Waits on (to start): nothing.
- Needed to validate: nothing.
- Cause: reproduced at the refinement, see `## Cause` (thread-pool starvation in the test process).
- Suggested path: `/agile:build`.
- Parallel with: anything that does not touch `tests/Directory.Build.props`. The proof loop (about 40 minutes of full load) should not run while another session runs its own suite: it would add load the baseline did not have.

## Expected
A red full run means a real defect: the same tree gives the same result on a rerun, or the output says why a test failed.

- AC1 Given every test project under `tests/`, when its test process starts, then the thread pool's minimum worker
  thread count is at least 256, set once in `tests/Directory.Build.props` (`RuntimeHostConfigurationOption`
  `System.Threading.ThreadPool.MinThreads`), not per project and not in code.
- AC2 Given each test project that starts the Api or Web host through `WebApplicationFactory`
  (`Simulab.Identity.Tests`, `Simulab.Catalog.Tests`, `Simulab.Api.Tests`, `Simulab.Web.Tests`), when its guard test
  runs, then it asserts `ThreadPool.GetMinThreads` reports at least 256 worker threads; the guard is seen failing
  (24 on this machine) before the setting exists.
- AC3 Given the load of `## Cause` (full suite plus 3 extra `Simulab.Identity.Tests` processes at once, copies under
  the worktree's `bin/stress/`), when it runs 10 rounds in a row after the fix, then all 40 Identity process runs and
  all 10 full-suite runs are green.
- AC4 Given the raised minimum, when the full suite runs alone, then it is no slower: its duration stays within the
  baseline range of `## Cause` (88-92 s) plus 10%.
- AC5 No product code changes: the diff touches only `tests/`, docs, and `docs/agile/retro-log.md`.
- AC6 Given `docs/infra.md` "Measured times", when the item ships, then the line "a red full run is now a real
  failure" says what B-24 found (it held only once the test process stopped starving its pool) and the new numbers.
- AC7 No new UI text: the localization criterion does not apply and the missing-key test stays green.

## Cause
Reproduced on 2026-10-05 in this worktree (`bug/B-24`, `ee9c89e`, same code as `main` `d49fa08`). The cause is in
the tests' process, not in the product code and not in PostgreSQL: the test process runs out of thread-pool
threads while 24 Api hosts start at once, and Npgsql's 15-second open timeout expires while the connection's
continuation waits for a thread.

**Measurements** (logs kept in the session scratchpad; machine: 24 cores, Docker 29.8.2):

| Setup | Runs | Result |
|---|---|---|
| `dotnet test Simulab.slnx`, nothing else running | 3 | 3 green (2215 tests, 88-92 s; Identity 1 m 27 s) |
| full suite + 3 extra `Simulab.Identity.Tests` processes at once (copies under `bin/stress/`) | 3 rounds, 12 Identity processes | round 1 green; round 2: one process **24 failed**, another 1 failed; round 3: one process **24 failed** |
| full suite + 6 extra Identity processes (3 with `System.Threading.ThreadPool.MinThreads=256`) | 3 rounds, 21 Identity processes | all green, in both halves: the A/B did not separate them |

Under load Identity takes about 3 m instead of 1 m 27 s, so the load is real; the failure showed in 3 of the 33
Identity process runs under load (about 9%): 3 of 12 in the first setup, 0 of 21 in the second.

**What the failures have in common** (both 24-failure runs here, and the F-51 gate run of 2026-10-05):
- Always exactly 24 tests: the number of test classes xUnit runs at once on this 24-core machine.
- All 24 end inside 80 ms of each other: `00:00:36.23`-`36.31` here and `00:00:36.00`-`36.02` in F-51 (the first
  wave of classes); `01:57.26`-`01:57.29` in the other run.
- All 24 messages are the same: `InvalidOperationException: An exception has been raised that is likely due to a
  transient failure` → `NpgsqlException: The operation has timed out` → `TimeoutException`, thrown from
  `NpgsqlTimeout.Check()` inside `NpgsqlConnector.RawOpen`, while the Api host starts
  (`src/Hosts/Simulab.Api/Program.cs:118` → `ModuleMigrationExtensions.MigrateModuleAsync`
  (`src/BuildingBlocks/Simulab.Persistence/ModuleMigrationExtensions.cs:23`) → `IHistoryRepository.CreateIfNotExistsAsync`),
  reached from the test's synchronous first use of the factory (`WebApplicationFactory.StartServer()` →
  `IHost.Start()`, e.g. `tests/Modules/Identity/Simulab.Identity.Tests/IdentityApiTests.cs:38`).
- The tests that failed are unrelated to each other, as in F-51: what they share is the moment their host started.

**Why it is the thread pool and not the database** (Npgsql 10.0.3 source, `src/Npgsql/Internal/NpgsqlConnector.cs`):
- `RawOpen` (line 947) first awaits `ConnectAsync`, whose socket connect is bounded by a
  `CancellationTokenSource.CancelAfter(timeLeft)` (lines 1428-1432). A database or Docker port that does not answer
  makes that connect fail with `Failed to connect to <endpoint>` (line 1456).
- Our exception is not that one. It is thrown by `timeout.CheckAndApply(this)` at line 975, **after** the socket
  connected. So the connect completed, but by the time its continuation ran the 15 seconds were gone, and the
  `CancelAfter` timer, whose callback also needs a pool thread, had not fired either. Nothing ran for 15 seconds:
  that is a starved thread pool.
- The only non-Npgsql failure seen under load says it in its own words: `RedisTimeoutException: Timeout performing
  SCAN (5000ms) ... WORKER: (Busy=25,Free=32742,Min=24,Max=32767), POOL: (Threads=25,QueuedItems=64,...)`
  (`ClientRateLimiterTests.Keys_AreUnderThePrefixWithATimeToLiveWithinTheWindow`): 25 busy worker threads against a
  minimum of 24, and 64 work items waiting.

**Who holds the threads.** Every Api test class (`ApiHostTests`, `tests/Simulab.Testing.ApiHost/ApiHostTests.cs`)
gets one host per test, and the first `Factory.Services` / `Factory.CreateClient()` starts it synchronously:
`WebApplicationFactory.StartServer()` blocks the calling thread until the host, migrations included, has started.
With 24 classes starting at once, 24 threads block while the work that would release them (the socket
continuations, the timers) waits in the pool's queue. The pool adds threads slowly when it sees starvation; on an
idle machine that is fast enough, on a loaded one it is not, which is why the same tree is green alone and red
under other sessions' runs (F-51 ran with 19 worktrees on the machine).

**Same mechanism elsewhere.** Every test project that starts a `WebApplicationFactory` synchronously is exposed
the same way: `Simulab.Catalog.Tests` (`CatalogApiTests`, on `ApiHostTests`), `Simulab.Api.Tests` (`ApiFactory`,
its own `ApiHostTests`), and the Web host tests (`Simulab.Web.Tests`, 9 files under `Auth/`, `Layout/` and the
root). None of them failed in these runs (Identity starts by far the most hosts: one per test, 425 tests). No
product code takes part: the product's own start is asynchronous.

**Not verified.** The A/B (raised minimum vs default) produced no failure in either half, so the fix is not yet
proven by comparison; the 3 failing process runs all had the default minimum. Proving the fix is the build's job
(AC3).

**On the B-19 premise** (`docs/infra.md`, "Measured times": "a red full run is now a real failure"): it is still
false under load. This failure needs the tests to fix, not a rerun.

## Fix
- `tests/Directory.Build.props`: one `RuntimeHostConfigurationOption` raising `System.Threading.ThreadPool.MinThreads`
  to 256 for every test project. With 24 threads blocked in `WebApplicationFactory.StartServer()`, the pool still
  has threads for the socket continuations and the timers at once, instead of adding them one by one under load.
  Test-only; the product's start is not touched.
- `docs/infra.md` "Measured times": the B-24 finding and the new numbers (AC6).
- `docs/agile/retro-log.md`: a plugin note, not a plugin change: the ship gate kept only the failing test names
  (`tail(r.out, 25)` in `gate.js`), so the 24 messages of F-51 were lost; keeping the first failure message would
  have saved a refinement round.

## Regression test
- The guard of AC2, one per exposed project: seen failing first (minimum 24, the core count) with the setting
  absent, green after it. It fails deterministically; the race itself does not.
- The 10 load rounds of AC3 against the baseline of `## Cause`: 3 failing Identity process runs out of 33 under load
  (about 9%); 0 in 40 by luck would have about a 2.3% chance.

## Decisions
- 2026-10-05 — Fix by raising the thread-pool minimum for every test project, not by starting the hosts off the
  pool (owner) — one line covers the four exposed projects; starting hosts on dedicated threads would touch four
  families of test hosts for the same effect.
- 2026-10-05 — Proof is 10 load rounds, all green (owner) — the baseline fails about 1 process run in 11, so 5
  rounds (20 runs, about 15% chance of luck) could still be luck.
- 2026-10-05 — Regression test is a guard reading `ThreadPool.GetMinThreads` in each exposed project (owner) — the
  race cannot be forced on demand, and a guard keeps the line from being removed in silence.
- 2026-10-05 — The gate keeping only test names becomes a plugin note in the retro-log, not a change here (owner) —
  the gate belongs to the agile plugin, which this repository never edits.
- 2026-10-05 — 256 as the minimum (Claude) — above the 24 blocked threads per process with a wide margin, and a
  minimum costs nothing until the threads are needed.
- 2026-10-05 — No new package (Claude) — the fix is an MSBuild item and the guard uses xUnit and AwesomeAssertions.
- 2026-10-05 — No similar item (Claude): B-19 (done) is related but had other causes (a `BackgroundService` race and a
  bUnit wait budget); B-22 is one Web dialog test.

## Out of scope
- Starting the test hosts asynchronously or on dedicated threads (the root of the blocking): not needed once the
  pool has threads; an idea only if the guard and the proof loop ever fail.
- Changing `gate.js` (plugin code).

## Open questions
- (none)

## Validation script

## Delivery
