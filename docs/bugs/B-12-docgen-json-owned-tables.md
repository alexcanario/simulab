---
bug: B-12
feature: F-15
status: building
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
- Each table has exactly one section in `data-dictionary.md` and one box in `entities.md`.
- An owned type mapped with `ToJson` is not a table. Its container column is listed in the owner's table with the database
  type and nullability from the migration (`added`, `removed`: `jsonb`, null yes). The Notes cell describes the content:
  `JSON: RoleChangeItem (Key, Name)`, using the JSON property names EF writes and leaving out the synthesized key and
  ordinal.
- The diagram draws the container columns in the owner's box (`jsonb added`, `jsonb removed`) and draws no relation for
  JSON ownership: `role_changes ||--}o role_changes` is gone.

## Cause
- `tools/Simulab.DocGen/EntityModels.cs:179-180` — `Tables(model)` keeps every entity type whose `GetTableName()` is not
  null. An owned type mapped with `ToJson` reports its owner's table name (`role_changes`), so `RoleChangeItem` (owned twice:
  `Added`, `Removed`, `RoleChangeConfiguration.cs:22-23`) is rendered as two more `role_changes` sections/boxes, with its
  JSON properties and the shadow key (`RoleChangeId`, `__synthesizedOrdinal`) shown as columns.
- `EntityModels.cs:115-118` — the same owned types add the relation `role_changes ||--}o role_changes` (a self-relation
  that does not exist in the database).
- `EntityModels.cs:182-183` — `Columns(entity)` reads only the owner's scalar properties, so the real JSON container
  columns `added` and `removed` (`jsonb`, nullable, `20260921161225_AddRoleChanges.cs:36-37`) are missing from the
  `RoleChange` section and box.
- Not caught by `EntityModelsTests.RenderDictionary_ListsEveryIdentityTableInSnakeCase` (`tests/Simulab.ArchitectureTests/DocGen/EntityModelsTests.cs:40`):
  it checks each table appears, not that it appears once.
- Not a business rule. Other `GetEntityTypes()` loops: `src/BuildingBlocks/Simulab.Persistence/ModuleDbContext.cs:35`
  filters by `TenantEntity`, which owned items are not — no duplicate found.

## Fix
- `EntityModels.Tables`: skip entity types that are mapped to JSON (`IsMappedToJson()`).
- `EntityModels` column rendering: after the owner's scalar properties, add one row per JSON navigation of the entity
  (`GetContainerColumnName()`, `GetContainerColumnType()`, nullable unless the navigation is required), with the note
  built from the target type's non-key properties and their `GetJsonPropertyName()`. Nested JSON owned types, if any
  appear later, are listed by name only in the note.
- Relations: skip foreign keys whose dependent is mapped to JSON.
- Regenerate `docs/architecture/` with `dotnet run --project tools/Simulab.DocGen` in the same commit (`--check` stays green).
- Owned types without `ToJson` (table splitting) are out of scope: captured as F-25.

## Acceptance criteria
- AC1 — Given the Identity model, when the data dictionary is rendered, then `## role_changes` appears exactly once and
  lists `| added | jsonb | yes |` and `| removed | jsonb | yes |` with the note `JSON: RoleChangeItem (Key, Name)`, and no
  `__synthesizedOrdinal` row.
- AC2 — Given the Identity model, when the entity diagram is rendered, then `role_changes {` appears exactly once, the box
  contains `jsonb added` and `jsonb removed`, and there is no `role_changes ||--}o role_changes` line.
- AC3 — Given every real model, when rendered, then no table name appears in more than one section or box.
- AC4 — Given the committed `docs/architecture/`, when `DocGen --check` runs, then it passes.
- No UI text changes: the localization criterion does not apply.

## Decisions
- 2026-09-22 — The JSON columns are documented in the owner's table with a note that names the stored type and its JSON
  properties (owner). Reason: the dictionary must match the database and still say what is inside the JSON.
- 2026-09-22 — Only JSON-owned types are fixed; table-split owned types become F-25 (owner). Reason: `RoleChangeItem` is the
  only owned type today; a generic merge would have no real case to test.
- 2026-09-22 — The regression tests go in `EntityModelsTests` over the real Identity model, not a fake model. Reason: the
  bug came from the real mapping; the F-15 tests already load the real models.

## Regression test
- `RenderDictionary_ListsJsonColumnsInTheOwnerTableOnce` (AC1) — `Simulab.ArchitectureTests`
- `RenderEntities_DrawsJsonColumnsInTheOwnerBoxWithoutSelfRelation` (AC2) — `Simulab.ArchitectureTests`
- `Render_ListsEachTableOnce` (AC3, over Identity and Jobs) — `Simulab.ArchitectureTests`
- AC4: `dotnet run --project tools/Simulab.DocGen -- --check`, run in the build and at ship (no test compares the
  committed files; `GeneratedDocsTests` covers the check logic).
All three new tests are run and seen failing before the fix.

## Out of scope
- Owned types without `ToJson` (F-25).
- Business descriptions of tables and columns (F-24).

## Open questions
- (none)

## Validation script
1. `dotnet run --project tools/Simulab.DocGen` → finishes without error; `git status` shows no change under `docs/architecture/`.
2. Open `docs/architecture/Identity/data-dictionary.md` → one `## role_changes` section, with the `added` and `removed`
   rows (`jsonb`, null yes, note `JSON: RoleChangeItem (Key, Name)`).
3. Open `docs/architecture/Identity/entities.md` in a Markdown preview → one `role_changes` box with `added` and `removed`,
   and no line from `role_changes` to itself.

## Delivery
- Branch: bug/B-12
- Merge: <commit>
