---
bug: B-<number>
feature: F-<number or ->
status: idea
board: <work item id or ->
severity: low | medium | high | critical
---
<!--
One short file per bug. Save as: docs/bugs/B-<number>-<slug>.md
Same status flow as a feature. Approval is needed only when the expected behavior is a product decision.
-->
# <Bug title>

## What happens
<!-- Observed behavior, with the steps to reproduce. -->
1. <Step>

## Expected
<!-- Correct behavior. If this is a product decision, it goes to the owner. -->

## Cause
<!-- Confirmed in code: file and line. Never a guess. -->

## Fix
<!-- What changes. Keep it minimal. -->

## Regression test
<!-- The test that fails before the fix and passes after it. -->
- <Test name> — <project>

## Open questions
- (none)

## Validation script
1. <Step> → <expected result>

## Delivery
- Branch: <bug/B-<number>>
- Merge: <commit>
