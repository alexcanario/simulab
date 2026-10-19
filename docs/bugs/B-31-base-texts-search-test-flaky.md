---
bug: B-31
feature: F-102
status: idea
board: 198
severity: low
epic: Question bank
---
# Base texts search test still fails under heavy parallel load

Technical terms: [glossary](../glossary.md)

## What happens
`Simulab.Web.Tests.QuestionBank.BaseTextsPageTests.Search_SendsTheWordToTheServerAsSearch_AndNarrowsTheRows` still failed in 3 of 72 runs of the stress loop of B-30 (2026-10-19: 24 `dotnet test --filter "FullyQualifiedName~QuestionBank"` processes at once, 3 rounds, on copies of the build output kept inside the worktree), all of them after the B-30 fix (`WaitForAfterDebounce`):
1. Round 1, twice (9 s and 10 s tests, `Check count: 0`, 228 and 268 renders): the bUnit wait ended before its check ran once; the cold start of 24 hosts starved the machine.
2. Round 3, once (6 s, `Check count: 32`): the fake API held only the first list call (`?page=0&pageSize=25`); no `search=tarifas` request came out although the helper moved the fake clock 32 times. The keystroke or its debounce was lost, not late: a different failure from the one B-30 fixed.

Already fixed by earlier bugs, so a later refinement does not read them as open: B-19 (thread-pool minimum), B-22 (one dialog test), B-23 (URL read after a click) and B-30 (a debounced wait whose clock advance came before the delay existed). What is left: the failure above, seen only in this test and only at 24 processes at once.

## Start
- Depends on: B-30 (it adds `WaitForAfterDebounce`; the work starts from its branch or after its merge).
- Waits on (to start): nothing.
- Needed to validate: nothing; no screen. The proof is a stress loop like the one in `docs/bugs/B-30-question-page-tests-flaky.md` (`## Delivery`) and a regression test.
- Model: unknown — settled at `/agile:refine`
- Suggested path: `/agile:refine B-31`, then `/agile:build B-31`. Cause not verified: measure it at `/agile:refine` (first question: does the third failure reproduce with a diagnostic that records the input event, the debounce callback and the list calls with timestamps?).
- Parallel with: unknown — settled at `/agile:refine`

## Expected
unknown — settled at `/agile:refine`

## Cause
Not verified: measure it at `/agile:refine`. Evidence so far: the output files of the three runs named in `## What happens` (`Check count`, render counts, the fake API's `Received` list), summarized in `docs/bugs/B-30-question-page-tests-flaky.md` (`## Delivery`).

## Fix
unknown — settled at `/agile:refine`

## Docs
- none — test-only, to be confirmed at `/agile:refine`

## Regression test
- unknown — settled at `/agile:refine`

## Decisions
- 2026-10-19 — Captured as a separate bug and not as part of B-30 — B-30 was accepted with 69 of 72 runs and fixes the late-registered delay; this failure has another shape — asked

## Open questions
- (none)

## Validation script

## Events

## Delivery
- Branch: bug/B-31
