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
- Depends on: <items that must be done first — or "nothing">
- Waits on (to start): <what stops the fix from starting: an owner decision, a person or team, access to data or an environment to reproduce it — who provides it — or "nothing">
- Needed to validate: <what only the validation needs (data, an environment, a person or device) — who provides it — or "nothing"; never blocks a start or an approval>
- Suggested path: <`/agile:refine` → `/agile:build` | `/agile:autopilot <id>` (cause clear, fix small)>
- Parallel with: <ids that can run at the same time in a worktree — or "none">

## Expected
<!-- Correct behavior. If this is a product decision, it goes to the owner. -->

## Cause
<!-- Confirmed in code: file and line. Never a guess. -->
<!-- If this is a business rule, list every duplicate occurrence found elsewhere in the solution (file:line each), or write "no duplicate found". -->

## Fix
<!-- What changes. Keep it minimal. -->
<!-- If duplicates were listed above: which are fixed here, which are deferred and why. -->

## Regression test
<!-- The test that fails before the fix and passes after it. One per occurrence fixed. -->
- <Test name> — <project>

<!-- test-lock.js lock (/agile:build, once the test is committed and failing) writes `Tests locked: <sha> (<n> files); unlocks: <n>` itself, right under the heading of this section. -->

## Open questions
- (none)

## Validation script
1. <Step> → <expected result>

## Delivery
- Branch: <bug/B-<number>>
- Merge: <commit>
