---
bug: B-8
feature: F-1
status: idea
board: 734
severity: medium
---
<!--
One short file per bug. Save as: docs/bugs/B-<number>-<slug>.md
Same status flow as a feature. Approval is needed only when the expected behavior is a product decision.
-->
# Outlined primary buttons in the kit may fail AA contrast

## What happens
Found during F-11 (AB#715), not yet verified on the kit components themselves.
1. `AppErrorState` ("Try again") and `AppEmptyState` (its action) use `MudButton Variant.Outlined Color.Primary`.
2. On `/account/security` (F-11) the same outlined primary button measured 3.53:1 on the dark card (4.60:1 light), below the AA 4.5:1 for text; F-11 switched its own button to the default colour.
3. The two kit components sit on the page background, not on a card, so their ratio there is not measured.

## Expected
Every kit button meets AA (4.5:1) in both themes, on every surface it sits on, and `ThemeContrastTests` asserts it.

## Cause
<!-- Confirmed in code: file and line. Never a guess. -->

## Fix
<!-- What changes. Keep it minimal. -->

## Regression test
<!-- The test that fails before the fix and passes after it. One per occurrence fixed. -->
