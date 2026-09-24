---
bug: B-14
feature: F-33
status: validating
board: 758
severity: low
---
<!--
One short file per bug. Save as: docs/bugs/B-<number>-<slug>.md
Same status flow as a feature. Approval is needed only when the expected behavior is a product decision.
-->
# A concurrent duplicate organizer answers 500 instead of 409

## What happens
Two requests creating organizers with the same normalized name (or acronym) at the same time both pass
`SaveOrganizerHandler.TakenAsync`, because each reads the table before the other writes. The unique index does its
job and refuses the second insert, but the handler does not translate the violation, so `SaveChangesAsync` throws
`DbUpdateException` and the caller gets an uncoded 500. F-33 BR9 promises 409 `organizer.name_taken` /
`organizer.acronym_taken`. Found by the independent review of F-33 (2026-09-23); accepted there because the back
office has one Admin at a time, so the race is unlikely in v1.

## Expected
- A duplicate refused by the database answers with the same 409 and the same error code as a duplicate caught by
  the handler's own check, whichever of the two wins the race.

## Cause
Verified on `feature/F-33` (c3e5d5d), the only place the code exists: F-33 is `validating` and not merged, so `main`
has no organizer code yet.

- `SaveOrganizerHandler.HandleAsync` (`src/Modules/Catalog/Simulab.Catalog.Application/Organizers/SaveOrganizerHandler.cs:38`)
  checks `TakenAsync`, then adds or updates, then calls `store.SaveChangesAsync` (line 55) with no transaction and
  no lock between the check and the write. Two requests both read "free" before either commits.
- `OrganizerStore.SaveChangesAsync` (`src/Modules/Catalog/Simulab.Catalog.Infrastructure/Persistence/OrganizerStore.cs:29`)
  passes straight to the context, so PostgreSQL's unique violation (SQLSTATE 23505, constraint
  `ux_organizers_tenant_normalized_name` or `ux_organizers_tenant_normalized_acronym`) surfaces as
  `DbUpdateException`. Nothing in `src/` catches it, so the endpoint answers 500.
- The existing `OrganizerUniqueIndexTests` proves the index throws `DbUpdateException` wrapping a
  `PostgresException` whose `ConstraintName` is the index name; that is the hook for the fix.

Duplicates (the same check-then-write rule implemented again):
- `SaveRoleHandler.CreateAsync` / `UpdateAsync` (`src/Modules/Identity/Simulab.Identity.Application/Roles/SaveRoleHandler.cs:33`
  and `:89`) is the same rule for `role.name_taken`, but it is not exposed to the bug: both run inside
  `store.RunExclusiveAsync` (`RoleAdministrationStore.cs:21`), a transaction-level advisory lock that serializes
  role writes.
- No other duplicate found.

## Fix
`OrganizerStore` translates the database's refusal. When saving raises `DbUpdateException` whose inner
`PostgresException` has SQLSTATE `23505` and `ConstraintName` `ux_organizers_tenant_normalized_name` or
`ux_organizers_tenant_normalized_acronym`, the store answers with the same `Error` the handler's own check
returns (`organizer.name_taken` / `organizer.acronym_taken`, `ErrorKind.Conflict`), and the handler returns it as a
failure. Any other exception, or a 23505 on another constraint, is not swallowed and still propagates.

Built as: `OrganizerUniqueViolations.Translate` (Infrastructure) does the mapping; `IOrganizerStore.TrySaveChangesAsync`
returns the `Error?`; `OrganizerStore` also clears the change tracker on a refusal so the rejected row is not
retried by a later save on the same context; `SaveOrganizerHandler` uses `TrySaveChangesAsync`. `SaveChangesAsync`
stays for `DeleteOrganizerHandler`, which cannot violate a unique index.

## Regression test
- `ConcurrentOrganizerDuplicateTests` (`Simulab.Catalog.Tests`): the "is taken" checks are forced to answer "free"
  (a store wrapper), so the database is the only arbiter. Seen failing before the fix: 5 failed, 0 passed, all
  `DbUpdateException` with 23505 on the organizer index. After the fix: green.
- `OrganizerEndpointTests.Create_TwoIdenticalRequestsAtOnce_AnswerOneCreatedAndOneConflict`: two real parallel
  `POST`s end as one 201 and one 409.

## Acceptance criteria
- AC1. Given the handler's check passed for a name that another request has just committed, when the organizer is
  created, then the result is 409 `organizer.name_taken`.
- AC2. Same for the acronym: 409 `organizer.acronym_taken`.
- AC3. Given an update that renames an organizer to a name committed by another request after the check, then the
  answer is 409 `organizer.name_taken` (acronym likewise).
- AC4. A database error that is not one of the two organizer unique indexes is not translated and still propagates.
- AC5. The race changes no text: both codes already have texts in the three languages (F-22), and the missing-key
  test stays green.

## Decisions
- 2026-09-24 (owner): wait for the F-33 merge before building. The organizer code exists only on `feature/F-33`;
  this item is approved on its own and its branch is brought up to date with `main` after that merge, before any
  code. Reason: no dependency between open branches.
- 2026-09-24 (owner): scope is the organizers only. Reason: `SaveRoleHandler` is serialized by
  `RunExclusiveAsync` and does not have the defect; no other duplicate was found.
- 2026-09-24 (Claude): fix by translating the unique violation in the store, not by an advisory lock. Reason: the
  index is already the real guarantee (F-33 BR5); translating it keeps one source of truth, adds no lock, and
  gives the same answer for every writer, whereas a lock only serializes the writers that take it.
- 2026-09-24 (Claude): when both the name and the acronym collide in the same race, whichever constraint
  PostgreSQL reports first decides the code; both are the correct 409.

## Out of scope
- `SaveRoleHandler` (already serialized). Any other module's check-then-write.
- Changing the F-33 rules (BR9) or the texts of the two codes.

## Open questions
None.

## Validation script
The race cannot be triggered by hand on screen, so the proof is the tests. From the worktree
`D:\dev\_icontrol\wt\simulab\b-14`, with Docker running (the tests start their own PostgreSQL container), in Git Bash
or in PowerShell 7 (same command):
1. `dotnet test tests/Modules/Catalog/Simulab.Catalog.Tests --filter "FullyQualifiedName~ConcurrentOrganizerDuplicate|FullyQualifiedName~TwoIdenticalRequestsAtOnce"`
   → expected: `Passed!  - Failed: 0, Passed: 6, Skipped: 0, Total: 6`. Repeat it a few times: the answer never changes.
2. Duplicate name on the organizers screen (the path that did not change) → the same error text as before, and the
   list keeps one organizer with that name.

## Delivery
- Branch: `bug/B-14` (worktree `D:\dev\_icontrol\wt\simulab\b-14`), from `main` after the F-33 merge.
- Commits: `fix(B-14)` on top of the approval; no schema change, no new package, no new text.
- Tests: Catalog 67 passed (57 before, 10 new), architecture 90 passed, Web localization/resources 31 passed;
  `--no-incremental` build of the Catalog infrastructure: 0 warnings, 0 errors. The Stop gate: `agile gate GREEN`.
