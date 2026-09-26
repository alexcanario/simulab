---
feature: F-28
epic: Foundation and identity
status: refining
board: 747
version: 1
---
# Partial index filter in the data dictionary

## Summary
The data dictionary that `tools/Simulab.DocGen` writes lists a partial index without its filter: since F-18, `docs/architecture/Jobs/data-dictionary.md` shows `ix_jobs_active_created_at` on `created_at`, but not `WHERE status IN (0, 1)`, so a reader takes it for a full index. Show the filter of every partial index (and the schema diagram too, if the format allows it). Captured at the F-18 retro (2026-09-23).

## Start
- Depends on: nothing. F-15 (DocGen), F-18 (the partial index itself), F-24 (descriptions), F-26 (the DBML
  schemas) and F-45 (the derived model list) are all `done` and on `main`.
- Waits on: nothing. It touches `tools/Simulab.DocGen/` and the generated documents only — no module, no schema.
- Suggested path: `/agile:autopilot F-28` — one renderer change, its tests, and the regenerated documents.
- Parallel with: anything. F-29 (`building`) and F-30 (`approved`) are both inside `Simulab.Identity`; the only
  overlap is the regenerated `docs/architecture/Identity/data-dictionary.md`, which neither of them rewrites.

## What exists (verified 2026-09-26, on `main` at `f598227`)
- The premise holds. `docs/architecture/Jobs/data-dictionary.md:22` and
  `docs/architecture/Identity/data-dictionary.md:99` both read `- \`ix_jobs_active_created_at\` on created_at`,
  with no sign of the filter. The index appears in **two** dictionaries because the `Job` entity is mapped into
  both module contexts; only those two have a `## jobs` section.
- The line comes from `EntityModels.RenderDictionary` (`tools/Simulab.DocGen/EntityModels.cs:284-292`): it orders
  `table.Table.Indexes` by name and writes `` - `name` on col, col `` plus ` (unique)` or
  ` (unique, NULLS NOT DISTINCT)`. Nothing reads the filter.
- **There is exactly one partial index in the solution today**: `ix_jobs_active_created_at`, declared with
  `.HasFilter(ActiveFilter)` in `src/BuildingBlocks/Simulab.Jobs/Persistence/Configurations/JobConfiguration.cs:45`
  and recorded as `status IN (0, 1)` in the migration snapshots of both contexts.
- **The filter never reaches the DBML schema, for a different reason.** `ix_jobs_active_created_at` covers
  `created_at`, a standard column F-26 leaves out of the diagram, so the whole index is dropped from
  `schema.dbml` — the filter is not missing there, the index is. No partial index is rendered in any `.dbml`
  today.
- DBML does support a note on an index (`created_at [name: 'created_at_index', note: 'Date']`), per the DBML
  documentation read on 2026-09-26, so a filter can be shown there when a partial index over visible columns
  appears.
- The unique flag and `NULLS NOT DISTINCT` are already shown, so the filter is the only thing missing from the
  line; the index method and the sort order are not shown either, and no index in the solution sets them.
- `DocGen --check` is part of the ship gate, so the regenerated documents are committed with the change or the
  gate goes red.

## Goal
A reader of the data dictionary can tell a partial index from a full one without opening the migrations: the
line says which rows the index covers.

## Users and use cases
- UC1 <Actor> <does something> <and gets a result>.

## Business rules
- BR1 <Rule>.

## Screens and API
<!-- Routes, main components, endpoints (always /api/v1/...), error codes. -->
- <Route> — <purpose>
- <METHOD> /api/v1/<resource> — <purpose>
- Error codes: `<area>.<error>`

## Acceptance criteria
<!-- Given / When / Then. Each one is covered by a test. Always include the localization criterion. -->
- AC1 Given <context>, when <action>, then <result>.
- AC<n> All new texts appear in pt-BR, pt-PT and en.

## Decisions
<!-- date — decision — reason. Technical decisions made by Claude are recorded here too. -->
- <YYYY-MM-DD> — <decision> — <reason>

## Out of scope
- <Item>

## Open questions
<!-- Approval is blocked while any line here is not answered or marked "deferred (owner, YYYY-MM-DD)". -->
- (none)

## Change notes
<!-- Added by /agile:change during build. Increase `version` in the header. -->
<!--
### v2 — YYYY-MM-DD
- What: <change>
- Why: <reason>
- Affected: <BR/AC ids>; other criteria unchanged.
- Re-approved: <YYYY-MM-DD>
-->

## Validation script
<!-- Written at the end of build. At most 8 steps the product owner follows on screen. -->
1. <Step> → <expected result>

## Delivery
<!-- Filled by /agile:ship. -->
- Branch: <feature/F-<number>>
- Merge: <commit>
- Tests: <count, duration>
- Manual pages: <paths>
