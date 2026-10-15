---
bug: B-<number>
feature: F-<number or ->
status: idea
board: <work item id or ->
severity: low | medium | high | critical
---
<!--
One short file per bug. Save as: docs/bugs/B-<number>-<slug>.md
With an external ticket (`/agile:idea bug --ticket PRJ-123`): the id is the ticket's, `bug: PRJ-123`, the file docs/bugs/PRJ-123-<slug>.md, and an optional header line `ticket-url: <URL>` right after `board:`.
Same status flow as a feature, including `cancelled` (a duplicate, from idea or refining only) and `blocked` (see `templates/feature.md`). Approval is needed only when the expected behavior is a product decision.
-->
# <Bug title>

Technical terms: [glossary](../glossary.md)

## What happens
<!-- Observed behavior, with the steps to reproduce. -->
1. <Step>

## Start
<!-- What this bug needs before it can be fixed, and how to run it. Filled when the bug is created, confirmed at refinement. -->
- Depends on: <items that must be done first: B-/F- ids or `<owner>/<repo>#<n>`; while one is not delivered, the approved item is blocked by itself and released when it ships — or "nothing">
- Waits on (to start): <what stops the fix from starting: an owner decision, a person or team, access to data or an environment to reproduce it — who provides it — or "nothing">
- Needed to validate: <what only the validation needs (data, an environment, a person or device) — who provides it — or "nothing"; never blocks a start or an approval>
- Suggested path: <`/agile:refine` → `/agile:build` | `/agile:autopilot <id>` (cause clear, fix small)>
- Parallel with: <ids that can run at the same time in a worktree — or "none">

## Expected
<!-- Correct behavior. If this is a product decision, it goes to the owner. -->

## Cause
<!-- Confirmed in code: file and line. Never a guess. -->
<!-- If this is a business rule, list every duplicate occurrence found elsewhere in the solution (file:line each), or write "no duplicate found". -->
<!-- If the cause is in a function used from more than one place, add the `Callers:` line: each caller (file:line) with `same defect`, `relies on old behavior` or `unaffected`; one caller or none: `Callers: none beyond the reported path (...)` or `Callers: not applicable (...)`. -->

## Fix
<!-- What changes. Keep it minimal. -->
<!-- If duplicates were listed above: which are fixed here, which are deferred and why. -->

## Docs
<!-- Only when the fix changes a flow, a contract or a setting a technical page already describes (references/tech-pages.md): `- <kind>: update docs/<folder>/<slug>.md#<section> — <what changes>`. A bug never lists `new`. No page is touched: `- none — <reason>`. -->
- none — <reason>

## Regression test
<!-- The test that fails before the fix and passes after it. One per occurrence fixed. -->
- <Test name> — <project>

<!-- test-lock.js lock (/agile:build, once the test is committed and failing) writes `Tests locked: <sha> (<n> files); unlocks: <n>` itself, right under the heading of this section. -->

## Decisions
<!-- date — decision — reason — source. Technical decisions made by Claude are recorded here too. The source: `asked`, `confirmed: <path or item id>` or `changed: <old rule> (<where>)` (references/decisions.md in the plugin). -->
- <YYYY-MM-DD> — <decision> — <reason> — asked

## Open questions
- (none)

## Validation script
<!-- Each step names the way to the screen (entry point, list, row, control) from docs/agile/ui-map.md. -->
### English
1. <Step> → <expected result>

### pt-BR
1. <Passo> → <resultado esperado>

## Events
<!-- Written only by events.js (`add`; the gate and the test lock write theirs): one line per event, `- <date> <kind> — <note>`. Kinds: override, question, assumed, rework, test-fail, blocked, change, correction, stop. -->

## Delivery
- Branch: <bug/B-<number>>
- Merge: <commit>
