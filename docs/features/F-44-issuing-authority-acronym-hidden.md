---
feature: F-44
epic: Assessment catalog
status: idea
board: 767
version: 1
---
<!--
One file per feature. Save as: docs/features/F-<number>-<slug>.md
Status flow: idea -> refining -> approved -> building -> validating -> done
- idea: title, summary and start only (/agile:idea, /agile:epic).
- refining: sections below filled during /agile:refine.
- approved: set only after the product owner says "approve F-<number>" and Open questions is empty or deferred.
- building / validating / done: set by /agile:build and /agile:ship.
Remove these comments when the file leaves `idea`.
-->
# The issuing authority's acronym off the screen

## Summary
The issuing authority's **acronym leaves the screen**: out of the dialog and the list columns, and the picker
shows the name alone instead of `Name (ACRONYM)`. The column stays in the table, optional and without its
unique index, so nothing already typed is lost and showing it again later costs nothing. Asked by the owner
after reading F-43's mockup (2026-09-25).

This item also carried a second change, **dropped on 2026-09-26**: the exam board as an extra field and filter
on the exam. See `## Decisions`.

## Start
- Depends on: F-34 (`done`, merged as `3386b40`) — the issuing authority, its dialog, its list and the picker
  that shows `Name (ACRONYM)` all come from there. Not blocked.
- Waits on: nothing.
- Suggested path: `/agile:autopilot F-44` — one field off three screens, plus a migration that only drops a
  unique index. Small and decided.
- Parallel with: unknown — settled at `/agile:refine`. It touches Catalog screens F-43 restyles, so the likely
  order is F-43 first, then this one.

## Decisions
- 2026-09-26 — The exam board is **not** added to the exam, and this item keeps only the acronym change — the
  Manaus municipal guard edital the owner supplied settles it. Its cover reads "A PREFEITURA DE MANAUS, por
  meio da SEMAD, torna pública a realização de Concurso Público" and its item 1.1 reads "sendo sua execução de
  responsabilidade do Instituto Consulplan": the contracting body publishes, the board executes, and the board
  is declared **inside the edital**, which is the edition. The same document even splits responsibility per
  stage (the Investigação Social is "SEMSEG / Instituto Consulplan"), finer than one board per edition.
  The owner's own use case decides it too: *"select 20 Mathematics questions from board Consulplan, from the
  editais of 2021 to 2026"* only answers correctly if each edition carries its board, because an exam changes
  board between years. A board on the exam would return the wrong questions for every year it changed. The
  requirement moved to F-35, which is where the edition is built (owner, 2026-09-26).
- 2026-09-26 — `SEMSEG`, the secretariat the job belongs to, as distinct from the `SEMAD` that runs the
  process, is not modelled: it does not serve what the app is for (owner, 2026-09-26).
- 2026-09-25 — The acronym is hidden, not dropped — removing the column would erase data already typed, and
  nothing needs it gone (owner, 2026-09-25).

## Goal
<!-- Why this feature exists, in one or two sentences. -->

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
