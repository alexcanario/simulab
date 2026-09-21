---
bug: B-12
feature: F-15
status: idea
board: 742
severity: low
---
<!--
One short file per bug. Save as: docs/bugs/B-<number>-<slug>.md
Same status flow as a feature. Approval is needed only when the expected behavior is a product decision.
-->
# DocGen lists JSON-owned types as extra tables

## What happens
The generated docs from F-15 show `role_changes` three times. The first block is `RoleChange`; the other two are the
owned `RoleChangeItem` (`Added` and `Removed`), which are stored in JSON columns with `ToJson`. Those two blocks list the
JSON properties `Key`, `Name` and `__synthesizedOrdinal` as if they were table columns. `docs/architecture/Identity/entities.md`
also draws three `role_changes` boxes. Found while refining F-16 (2026-09-21).
1. Run `dotnet run --project tools/Simulab.DocGen`.
2. Open `docs/architecture/Identity/data-dictionary.md` → three `## role_changes` sections. Open `entities.md` → three `role_changes {` blocks.

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

## Open questions
- (none)

## Validation script
1. <Step> → <expected result>

## Delivery
- Branch: <bug/B-<number>>
- Merge: <commit>
