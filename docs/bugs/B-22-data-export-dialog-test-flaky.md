---
bug: B-22
feature: F-16
status: idea
board: 109
severity: low
---
# The data export dialog test fails now and then in the full suite

Technical terms: [glossary](../glossary.md)

## What happens
`Simulab.Web.Tests.Identity.DataExportPageTests.Dialog_AccountWithoutPassword_ShowsHowToCreateOne` failed once in the full suite and passes when run alone.

1. In the worktree of F-50 (`feature/F-50`), on 2026-10-03, run `gate.js ship` (the whole solution). Its output saved to a file ended `agile gate RED` with `Failed: 1, Passed: 832, Total: 833` for `Simulab.Web.Tests.dll` and the line `[xUnit.net 00:00:02.84] Simulab.Web.Tests.Identity.DataExportPageTests.Dialog_AccountWithoutPassword_ShowsHowToCreateOne [FAIL]`.
2. Run `dotnet test tests/Hosts/Simulab.Web.Tests --filter "FullyQualifiedName~DataExportPageTests"` on the same tree: `Passed: 7, Failed: 0`.
3. The next full run of `gate.js ship` on the same code (after two commits that touch neither the test nor `DataExportPage`) was green: `Simulab.Web.Tests.dll` `Passed: 833`.

It is the kind of failure B-19 measured (a test that loses a race under parallel load). F-50 changes neither the dialog nor its page.

## Start
- Depends on: nothing.
- Waits on (to start): nothing.
- Needed to validate: nothing.
- Suggested path: `/agile:refine` → `/agile:build`. Cause not verified: measure it at `/agile:refine` (run the whole solution a few times in a row and read the failure message).
- Parallel with: none.

## Expected
The test passes whenever the solution is tested, alone or in parallel.

## Cause
Cause not verified. Seen: one failure in the full run, none alone (7 of 7), none in the next full run.

## Fix
Unknown until the cause is measured.

## Regression test
- Unknown until the cause is measured.

## Open questions
- (none)

## Validation script
1. Run `dotnet test Simulab.slnx` several times in a row. → `DataExportPageTests` never fails.

## Delivery
- Branch: bug/B-22
- Merge: <commit>
