---
feature: F-25
epic: Foundation and identity
status: done
board: 743
version: 1
---
# DocGen types sharing a table

Technical terms: [glossary](../glossary.md)

## Summary
An owned type mapped without `ToJson` (table splitting, `OwnsOne` stored in the owner's table) would still be rendered by
DocGen as a second section with the owner's table name, like B-12 did for JSON. B-12 fixes only the JSON case, the one
real case today (`RoleChangeItem`). Refinement (2026-09-22) widened it to every type stored in another type's table:
owned types without `ToJson`, EF complex types (`ComplexProperty`) and two entity types mapped to the same table.
DocGen renders one section and one box per database table, with every column of that table.

## Goal
`docs/architecture/` matches the database table by table, whatever EF mapping a module picks, so the docs do not break
the first time a module uses table splitting or a complex type.

## What exists today (verified 2026-09-22)
- `tools/Simulab.DocGen/EntityModels.cs:191-194` — `Tables(model)` yields one section per entity type with a table name,
  skipping only JSON-mapped types (B-12, `f0777ec`). A table-split owned type or a second entity type on the same table
  would repeat the section and the box.
- `EntityModels.cs:196-197` — `Columns(entity)` reads `GetProperties()` of one entity type only. The columns of an owned
  type in the owner's table would be in a separate section; the columns of a complex type (`GetComplexProperties()`) would
  be missing from the docs altogether.
- `EntityModels.cs:120-125` — relations come from every foreign key between two tables. The link between a table-split
  type and its owner is a key-to-key foreign key inside one table and would be drawn as `users ||--|| users`.
- No real case in the code: the built models and the snapshots (`IdentityModuleDbContextModelSnapshot.cs`, Jobs) have no
  `OwnsOne`/`OwnsMany` without `ToJson`, no `ComplexProperty`, and no two entity types on one table. The only owned type is
  `RoleChangeItem`, mapped to JSON (`RoleChangeConfiguration.cs:22-23`). Only B-12 touched `EntityModels.cs` since F-15.

## Users and use cases
- UC1 A developer opens a module's `data-dictionary.md` and finds each database table once, with every column the
  migration creates, including the ones that come from an owned type, a complex type or another entity type.
- UC2 A developer opens a module's `entities.md` and sees one box per table, with no relation from a table to itself that
  exists only because two types share it.

## Business rules
- BR1 One section in `data-dictionary.md` and one box in `entities.md` per database table of the module, whatever number of
  entity types, owned types or complex types are stored in it.
- BR2 A table lists every column of the table in the relational model (the model the migrations are built from). Name,
  store type and nullability come from that column. A column of an optional owned type is nullable even when its property
  is required, as in the migration.
- BR3 Column order: primary key columns first; then the principal entity type's own columns by name; then the columns that
  come from other types, grouped by origin (origin in ordinal order), each group by column name; then the JSON columns (B-12).
- BR4 The Notes cell of a column that comes from another type starts with its origin: `Owned: <Type>` for an owned type
  without `ToJson`, `Complex: <Type>` for a complex type, `Entity: <Type>` for another entity type on the same table.
  `<Type>` is the CLR name of the type that declares the property. Other notes (`max 256`, comment) follow, joined by `; `.
- BR5 A column shared by several types (the primary key under table splitting) is listed once, as a column of the
  principal type, with no origin note.
- BR6 The section header names the entity types of the table: `Entity: <Type>` when there is one, `Entities: <Principal>,
  <Other>, ...` when there are several (principal first, the others in ordinal order). Owned and complex types are not
  listed there; their notes already name them.
- BR7 A complex type mapped with `ToJson` is a JSON column like B-12: its note is `JSON: <Type> (<json property names>)`.
- BR8 The diagram draws no relation between a table and itself when the foreign key only links types that share the table
  (its columns are the primary key on both sides). A real self-reference (for example a `parent_id` column pointing at the
  same table) is still drawn.
- BR9 The Indexes list of a table includes the indexes declared on any type stored in it, each index once.
- BR10 The output for today's models (Identity, Jobs) does not change: `docs/architecture/` stays byte-identical.

## Screens and API
- No screen, no endpoint, no error code. Output only: `docs/architecture/<Module>/data-dictionary.md` and `entities.md`.

## Acceptance criteria
- AC1 (BR1, BR2) Given a test model where `Customer` owns `Address` with `OwnsOne` and no `ToJson`, when the dictionary is
  rendered, then `## customers` appears exactly once and lists the address columns with the type and nullability of the
  relational model (nullable when the navigation is optional).
- AC2 (BR3, BR4) Given the same model, when the dictionary is rendered, then the address columns come after the customer's
  own columns and their Notes start with `Owned: Address`.
- AC3 (BR2, BR4) Given a test model with a complex property `Price` of type `Money`, when the dictionary is rendered, then
  the `Money` columns are listed in the owner's section with the note `Complex: Money`, and the diagram box has them.
- AC4 (BR1, BR5, BR6) Given a test model where `Order` and `OrderSummary` are mapped to the table `orders`, when the
  dictionary is rendered, then `## orders` appears once with `Entities: Order, OrderSummary`, the key column is listed once
  with no origin note, and the other `OrderSummary` columns have the note `Entity: OrderSummary`.
- AC5 (BR1, BR8) Given the test models, when the diagram is rendered, then each table has exactly one box and there is no
  relation line from `customers` or `orders` to itself.
- AC6 (BR8) Given a test model with a real self-reference (`categories.parent_id` → `categories`), when the diagram is
  rendered, then the `categories ||--}o categories` line is drawn.
- AC7 (BR7) Given a test model with a complex type mapped with `ToJson`, when the dictionary is rendered, then its container
  column is listed once with the note `JSON: <Type> (<json property names>)`, and no row for its inner properties.
- AC8 (BR9) Given a test model with an index declared on the owned `Address`, when the dictionary is rendered, then the
  index appears once in the `customers` Indexes list.
- AC9 (BR10) Given the committed `docs/architecture/`, when `dotnet run --project tools/Simulab.DocGen -- --check` runs, then
  it prints `docs/architecture is up to date`; the B-12 and F-15 tests over the real models stay green.
- No UI text changes: the localization criterion does not apply.

## Decisions
- 2026-09-22 — Build F-25 now with a test-only model, without waiting for a real case (owner). Reason: the tool is right
  before a module needs it, and the cost is small. This replaces, for F-25 only, the B-12 choice of testing over the real
  model; the real-model tests stay as the regression guard (AC9).
- 2026-09-22 — Cover the three shared-table cases: owned without `ToJson`, complex types, and entity types on one table
  (owner). Reason: one cause — sections come from entity types instead of database tables.
- 2026-09-22 — Columns from another type carry an origin prefix in Notes (`Owned:`, `Complex:`, `Entity:`) (owner).
  Reason: same pattern as `JSON:` from B-12; the reader knows where the column comes from.
- 2026-09-22 — A table with several entity types lists them all in the header, principal first (owner). Reason: the header
  says which entities live in the table; owned and complex types are already in the notes.
- 2026-09-22 — Sections and columns are built from the relational model (`model.GetRelationalModel()`, tables of the module,
  their columns and the property mappings of each column), not from entity type properties (Claude). Reason: it is the
  model the migrations use, so every case above comes out right with no special rule per mapping, as B-12 did for the
  JSON container column.
- 2026-09-22 — The test models are small `DbContext` classes in `tests/Simulab.ArchitectureTests/DocGen/`, built without a
  connection, and are not in the assemblies next to the tool (Claude). Reason: `EntityModels.Load()` scans only the tool
  folder, so they never reach `docs/architecture/`.
- 2026-09-22 — No new packages (Claude). Reason: the tool and the tests already reference EF Core relational and Npgsql.
- 2026-09-22 (build) — The header keeps the backticks of the existing `Entity:` line: `Entities: `Order`, `OrderSummary``
  (Claude). Reason: one format for one and several entities; AC4 is checked with the backticks.
- 2026-09-22 (build) — Relations come from the table's foreign key constraints (`ITable.ForeignKeyConstraints`), not from
  every foreign key of the entity types (Claude). Reason: the key-to-key link between types sharing a table is not a
  constraint in the relational model, so BR8 needs no special rule and a real self-reference stays.
- 2026-09-22 (build) — In the test model, `OrderSummary.total` is `not null`: the relational model makes a table-split entity
  with a required property a required dependent, and the migration would do the same (Claude). Reason: BR2 takes nullability
  from the column; the nullable case is covered by the optional owned `Address` (`address_street`, required property,
  nullable column).

## Coverage
All in `tests/Simulab.ArchitectureTests/DocGen/EntityModelsTests.cs`, over the test model in `DocGen/SharedTables/`
(AC1-AC8) or the real models (AC9). Six new tests were seen failing before the change (`Failed: 6, Passed: 12`); AC6 and
AC8 passed by accident before it (the index happened to fall in the duplicated `Address` section, which no longer exists).

| Criterion | Test |
|---|---|
| AC1 | `RenderDictionary_ListsOwnedColumnsInTheOwnerTableOnce` |
| AC2 | `RenderDictionary_ListsOwnedColumnsAfterTheOwnerColumns` |
| AC3 | `Render_ListsComplexColumnsInTheOwnerTable` |
| AC4 | `RenderDictionary_ListsEntitiesSharingATableInOneSection` |
| AC5 | `RenderEntities_DrawsOneBoxPerTableWithoutSplittingSelfRelations` |
| AC6 | `RenderEntities_KeepsARealSelfReference` |
| AC7 | `RenderDictionary_ListsComplexJsonAsOneColumn` |
| AC8 | `RenderDictionary_ListsAnOwnedTypeIndexInTheOwnerTableOnce` |
| AC9 | `DocGen --check` (`docs/architecture is up to date`, no diff against `main`); B-12/F-15 tests over the real models: `RenderDictionary_ListsJsonColumnsInTheOwnerTableOnce`, `RenderEntities_DrawsJsonColumnsInTheOwnerBoxWithoutSelfRelation`, `Render_ListsEachTableOnce`, `RenderDictionary_ListsEveryIdentityTableInSnakeCase` |

## Out of scope
- `OwnsMany` without `ToJson`: it has its own table and is already rendered as its own section.
- Table and column descriptions (F-24), orthogonal diagram layout (F-26).
- Any change to a real module's mapping.

## Open questions
- (none)

## Change notes

## Validation script
No app host needed: this is a docs tool. Folder: `D:\dev\_icontrol\wt\simulab\feature-25` (branch `feature/F-25`). The
commands are the same in Git Bash and PowerShell 7; each one only reads and can be repeated.
1. Check the committed docs: `dotnet run --project tools/Simulab.DocGen -- --check` → `docs/architecture is up to date`,
   exit code 0.
2. Confirm today's docs did not change: `git diff --stat main -- docs/architecture` → no output.
3. Run the DocGen entity tests: `dotnet test tests/Simulab.ArchitectureTests --filter "FullyQualifiedName~EntityModelsTests"`
   → `Passed!  - Failed:     0, Passed:    18`.
4. Open `tests/Simulab.ArchitectureTests/DocGen/SharedTables/SharedTableDbContext.cs` → the four mappings (owned `Address`,
   complex `Money` and JSON `Dimensions`, `Order` + `OrderSummary` on `orders`, `Category.Parent`).
5. Open `tests/Simulab.ArchitectureTests/DocGen/EntityModelsTests.cs`, from `SharedTableModel()` down → the expected rows
   match BR2-BR9 (for example `| address_street | character varying(200) | yes |  |  | Owned: Address; max 200 |` and
   `Entities: `Order`, `OrderSummary``).

## Delivery
- Branch: feature/F-25 (worktree `wt/simulab/feature-25`)
- Merge: d08ea5b
- Validated by the owner (2026-09-22).
- Full check (`gate.js ship`), after bringing the branch up to date with B-13: build 22 s, 0 warnings, baseline stays
  empty; suite 703 tests, 0 failed, 51 s (ArchitectureTests 59, including the 8 new F-25 tests; 6 seen failing first:
  `Failed: 6, Passed: 12`).
- `DocGen --check`: `docs/architecture is up to date`, also with the foreign keys B-13 added (no change to the generated docs).
- Incident: the first merge (`dd1c678`) landed on `bug/B-13`, because the shared checkout had been switched to that branch
  and the branch was not checked right before the merge. The B-13 session rebuilt its branch without it (the old one is
  kept as `bug/B-13-with-f25`, never pushed); `main` was never touched.
- App manual: unchanged (no user-visible behavior). `docs/infra.md`: measured times.
