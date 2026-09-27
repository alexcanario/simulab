---
feature: F-48
epic: Foundation and identity
status: idea
board: 773
version: 1
---
<!--
One file per feature. Save as: docs/features/F-<number>-<slug>.md
Status flow: idea -> refining -> approved -> building -> validating -> done
- idea: title, summary and start only (/agile:idea, /agile:epic).
- refining: sections below filled during /agile:refine.
- approved: set only after the product owner says "approve F-<number>" and Open questions is empty or deferred.
- building / validating / done: set by /agile:build and /agile:ship.
Remove these comments when the file leaves `idea`.
-->
# Drive the search debounce from TimeProvider

## Summary
The 300 ms debounce of the shared UI kit is MudBlazor's own `DebounceInterval` (`AppDataTable.razor:25`,
`AppLookupField.razor:22,75`), which runs on a real `System.Timers.Timer`. Every bUnit test that types into a
search box or a lookup field therefore waits 300 ms of wall clock, and under the load of the whole solution that
wait is what makes them flaky. Drive the debounce from `TimeProvider` instead, so a test advances the clock rather
than waiting for it. Found while refining B-19 (decision 2026-09-26): B-19 buys the tests a bigger timeout; this
item removes the wall clock.

## Start
- Depends on: B-19 — it raises the bUnit wait timeout, which is what keeps the suite green until this lands.
- Waits on: nothing.
- Suggested path: `/agile:refine` — it changes shared UI kit components used by every list and lookup, so the
  blast radius and the test strategy are settled before any code.
- Parallel with: unknown — settled at `/agile:refine`.

## Goal
<!-- Why this feature exists, in one or two sentences. -->

## Users and use cases
- UC1 <Actor> <does something> <and gets a result>.

## Business rules
- BR1 <Rule>.

## Screens and API
<!-- Routes, main components, endpoints (always /api/v1/...), error codes. -->
- <Route> — <purpose>
- <METHOD> /api/v1/<resource> — <purpose>
- Error codes: `<area>.<error>`

## Acceptance criteria
<!-- Given / When / Then. Each one is covered by a test. Always include the localization criterion. -->
- AC1 Given <context>, when <action>, then <result>.
- AC<n> All new texts appear in pt-BR, pt-PT and en.

## Decisions
<!-- date — decision — reason. Technical decisions made by Claude are recorded here too. -->
- <YYYY-MM-DD> — <decision> — <reason>

## Out of scope
- <Item>

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

## Validation script
<!-- Written at the end of build. At most 8 steps the product owner follows on screen. -->
1. <Step> → <expected result>

## Delivery
<!-- Filled by /agile:ship. -->
- Branch: <feature/F-<number>>
- Merge: <commit>
- Tests: <count, duration>
- Manual pages: <paths>
