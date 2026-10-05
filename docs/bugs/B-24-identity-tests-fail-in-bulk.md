---
bug: B-24
feature: -
epic: Foundation and identity
status: refining
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
- Cause not verified: measure it at `/agile:refine`. The failure messages of the 24 tests were not read (the gate output kept only the test names), so it is not known whether they share a cause (for example PostgreSQL `max_connections`, container start-up under load, or a timeout). The first step is to reproduce under parallel load and read the messages.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: none.

## Expected
A red full run means a real defect: the same tree gives the same result on a rerun, or the output says why a test failed.

## Cause
Not verified.

## Fix

## Regression test

## Open questions
- (none)

## Validation script

## Delivery
