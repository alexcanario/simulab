---
feature: F-48
epic: Foundation and identity
status: refining
board: 773
version: 1
---
# Drive the search debounce from TimeProvider

## Summary
The 300 ms debounce of the shared UI kit (`AppDataTable.razor:28`, `AppLookupField.razor:22,75`) waits on the real
clock in every bUnit test that types into a search box or a lookup field, and under the load of the whole solution
that wait is what makes those tests flaky. Found while refining B-19 (decision 2026-09-26): B-19 bought the tests a
bigger timeout (`KitTestContext.cs:32`); this item removes the wall clock.

The first version of this file said MudBlazor runs the debounce on a real `System.Timers.Timer` and that the kit has
to be changed to use `TimeProvider`. That premise is false (see Decisions, 2026-10-03): MudBlazor 9.9.0 already reads
an injected `TimeProvider` in both controls, and the kit's test context never replaces it, so the tests get
`TimeProvider.System`. The fix is in the tests only.

## Start
- Depends on: B-19 (done) — it raised the bUnit wait timeout, which stays.
- Waits on: nothing.
- Needed to validate: nothing.
- Suggested path: `/agile:build F-48`.
- Parallel with: F-50 (table search announces results) touches the same search box and its tests: do them in
  sequence, F-50 first if it is ready, or expect conflicts in `AppDataTable.razor` and `AppDataTableTests.cs`.

## Goal
A test that types into a search box or a lookup field moves the clock instead of waiting for it, so it does not depend
on how busy the machine is.

## Users and use cases
- UC1 A developer writes a test that types into a kit search box or lookup field, advances a fake clock by the
  debounce interval, and asserts on the result without waiting in real time.

## Business rules
- BR1 Production code does not change. The kit keeps its 300 ms; MudBlazor keeps reading `TimeProvider` from the
  container, where the app host registers `TimeProvider.System`.
- BR2 `KitTestContext` registers one `FakeTimeProvider` and exposes it; every kit-based test context inherits it.
  A test that registers its own `TimeProvider` after the base constructor (as `ExamEditionFormTests` does) still wins.
- BR3 The test-side debounce is advanced by one helper that uses the kit's own constants
  (`AppLookupField.DebounceMilliseconds`; the table's literal 300 becomes a public constant of the kit only if the
  helper needs it, otherwise the helper takes the interval as a parameter), never a copied number.
- BR4 Every existing test that types into `.app-table-search input` or a lookup field and then waits for the result
  advances the fake clock instead of waiting; none of them keeps a real-time wait on the debounce.
- BR5 The 5 s bUnit wait timeout from B-19 stays: it still covers async loads that are not debounced.

## Screens and API
No screen and no API change.

## Acceptance criteria
- AC1 Given a kit table with search, when a test types a term and does not advance the clock, then the source is not
  called with that term; when it advances 300 ms, then it is called once with the term on page 0.
- AC2 Given a lookup field, when a test types a term and does not advance the clock, then the search is not called;
  when it advances `AppLookupField.DebounceMilliseconds`, then it is called once.
- AC3 Given every test that types into `.app-table-search input` or a lookup field (the Exams, Issuing authorities,
  Organizers, Users and Catalog search pages, the Exam filter, and the Exam and Exam edition forms), when they run,
  then each advances the fake clock to trigger the search and none relies on a real 300 ms wait.
- AC4 Given the whole Web test project run 20 times in a row, each preceded by a clean build, when it finishes, then
  all 20 runs are green (real counts recorded in `## Delivery`).
- AC5 No production file under `src/` changes, and no new package is added (`Microsoft.Extensions.TimeProvider.Testing`
  is already referenced by the test project).

## Decisions
- 2026-10-03 — The production premise is false: MudBlazor 9.9.0 injects `TimeProvider` in `MudAutocomplete`
  (`MudAutocomplete.razor.cs:44,655`, `TimeProvider.CreateTimer`) and in `MudDebouncedInput`, the base of
  `MudTextField` (`DebounceDispatcher` with `Task.Delay(interval, timeProvider)`); the services register
  `TryAddSingleton(TimeProvider.System)`. Verified in the library source at tag v9.9.0, not from memory. — The kit
  needs no change; the tests only never replaced the clock.
- 2026-10-03 — Scope is tests only (owner's answer: "Só testes"). The duplicated 300 in `AppDataTable` and
  `AppLookupField` is left alone.
- 2026-10-03 — The 5 s timeout of B-19 stays (owner's answer) — it is a ceiling, a green run does not wait on it, and
  other waits still need it under load.
- 2026-10-03 — No new package, no question on packages: `Microsoft.Extensions.TimeProvider.Testing` 10.9.0 is already
  in `Directory.Packages.props` and referenced by `Simulab.Web.Tests`.

## Out of scope
- Changing the 300 ms or merging the two constants (an idea if it matters later).
- Replacing `Countdown`'s `System.Threading.Timer` (F-7), which is a different timer.
- Other flaky tests of B-19 that are not debounce waits.

## Open questions
- (none)

## Change notes
<!--
### v2 — YYYY-MM-DD
- What: <change>
- Why: <reason>
- Affected: <BR/AC ids>; other criteria unchanged.
- Re-approved: <YYYY-MM-DD>
-->

## Validation script
<!-- Written at the end of build. At most 8 steps the product owner follows on screen. -->

## Delivery
<!-- Filled by /agile:ship. -->
- Branch: feature/F-48
- Merge:
- Tests:
- Manual pages:
