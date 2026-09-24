---
bug: B-14
feature: F-33
status: refining
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
