---
bug: B-14
feature: F-33
status: approved
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

## Regression test
An integration test in `Simulab.Catalog.Tests` that lets the database be the arbiter: the "is taken" checks are
forced to answer "free" (a store wrapper), a first organizer is saved, then a second one with the same normalized
name (and another test with the same acronym) is saved through `SaveOrganizerHandler`. Before the fix it throws
`DbUpdateException`; after, it returns the 409 codes. It must be seen failing before the fix.

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
1. With the host running, send two `POST /api/v1/catalog/organizers` with the same name at the same time (for
   example two terminals, or a script sending both in parallel). Expected: one 201 and one 409
   `organizer.name_taken`, never a 500.
2. Repeat with the same acronym and different names: 409 `organizer.acronym_taken`.

## Delivery
Pending.
