---
feature: F-44
epic: Assessment catalog
status: refining
board: 19
version: 1
---
# The issuing authority's acronym off the screen

## Summary
The issuing authority's **acronym leaves every screen**, admin and student: out of the dialog, the lists, the
pickers, the delete confirmation and the student catalog, and the searches stop matching it. Every screen
names the authority by its name alone. The column stays in the table, optional and without its unique index,
so nothing already typed is lost and showing it again later costs nothing. Asked by the owner after reading
F-43's mockup (2026-09-25).

This item also carried a second change, **dropped on 2026-09-26**: the exam board as an extra field and filter
on the exam. See `## Decisions`.

## Start
- Depends on: F-34 (`done`, merged as `3386b40`) — the issuing authority, its dialog, its list and the pickers
  come from there. F-43 (`done`) restyled those screens. Not blocked.
- Waits on: nothing.
- Suggested path: `/agile:build F-44`.
- Parallel with: not F-42 (`refining`, worktree `D:\dev\_icontrol\wt\simulab\f-42-state-picker`): both change
  `ExamForm.razor` and add a Catalog migration, so they go in sequence. Any item outside the Catalog module
  and its screens can run beside it.

## What exists today (checked 2026-10-01, `main` at `dce6010`)
The item's first summary named three places; the code shows the acronym in eight, plus three searches:
- `IssuingAuthorities.razor` — the `Acronym` column, sortable (`IssuingAuthoritySort.Acronym`).
- `IssuingAuthorityDialog.razor` — the required `Acronym` field, sharing a row with the name (F-43 BR1).
- `Exams.razor` — the acronym as the secondary line under the authority's name in the list, `Name (ACRONYM)`
  in the authority filter and in the delete confirmation message.
- `ExamForm.razor` — `Name (ACRONYM)` in the authority picker, on load and in the options.
- `CatalogSearch.razor` (student) — the acronym as the secondary line under the authority's name.
- `CatalogExam.razor` (student) — `Name (ACRONYM)` in the aside row "Issuing authority".
- Searches that match the acronym: `IssuingAuthorityQueries` (admin list and pickers) and
  `PublishedExamQueries` (student catalog). Placeholders say so: `Exams.Field.IssuingAuthority.Placeholder`
  ("Name or acronym"), `IssuingAuthorities.Search.Placeholder` ("Search by name or acronym").
- Domain and table: `IssuingAuthority.Acronym` is required (2 to 20 characters, uppercase), with
  `NormalizedAcronym` and the unique index `ux_issuing_authorities_tenant_normalized_acronym`;
  `SaveIssuingAuthorityHandler` and `IssuingAuthorityUniqueViolations` answer `issuing_authority.acronym_taken`.
- Contracts carrying it: `SaveIssuingAuthorityRequest`, `IssuingAuthorityResponse`, `ExamResponse`,
  `PublishedExamResponse`, `PublishedExamDetailResponse`.
- Manual: `docs/manual/<locale>/issuing-authorities.md`, `exams.md`, `catalog.md` mention it.

## Goal
Name the issuing authority the way the notice names it, and stop asking the curator for a short name nothing
needs, without losing the acronyms already typed.

## Users and use cases
- UC1 A catalog curator adds or edits an issuing authority by its name, description and website, with no
  acronym field.
- UC2 A catalog curator sees issuing authorities and exams listed, filtered, picked and confirmed for
  deletion by the authority's name alone.
- UC3 A student sees the exams in the catalog search and on the exam page with the authority's name alone.
- UC4 Anyone searching for an authority (admin list, pickers, student catalog) finds it by its name.

## Business rules
- BR1 No screen, admin or student, shows the issuing authority's acronym. Where the screen showed
  `Name (ACRONYM)`, it shows `Name`; where the acronym was a column or a secondary line, it is gone.
- BR2 The issuing authority has no acronym field: creating one asks only for name, description and website.
- BR3 Editing an issuing authority keeps the acronym already stored for it, unchanged (the hidden value is
  never erased by a save).
- BR4 The acronym is optional in the table and no longer unique: two authorities may hold the same stored
  acronym, and a new authority stores none. The name stays required and unique (F-34 BR18, unchanged).
- BR5 The searches match the authority's name only: the admin list, the authority pickers of the exam
  screens and the student catalog search no longer match a stored acronym.
- BR6 The list of issuing authorities sorts by name only; there is no acronym sort.
- BR7 The organizer (exam board) keeps its acronym everywhere: this item touches only the issuing authority.

## Screens and API
- `/admin/issuing-authorities` — list without the `Acronym` column; search placeholder "Search by name".
- Issuing authority dialog — name (full row), description, website.
- `/admin/exams` — list shows the authority's name only; filter options and delete confirmation use the name.
- `/admin/exams/new`, `/admin/exams/{id}` — authority picker options are names; placeholder "Name".
- Student catalog search and exam page — authority by name only.
- `POST /api/v1/issuing-authorities`, `PUT /api/v1/issuing-authorities/{id}` — the request no longer has
  `acronym`.
- `GET /api/v1/issuing-authorities`, exam and published-exam endpoints — responses no longer carry the
  authority's acronym; `sortBy=acronym` is no longer a value (an unknown value sorts by name, as today).
- Error codes removed: `issuing_authority.acronym_required`, `issuing_authority.acronym_too_long`,
  `issuing_authority.acronym_taken`, with their resource keys in the three languages.
- Migration: `acronym` and `normalized_acronym` become nullable; `ux_issuing_authorities_tenant_normalized_acronym`
  is dropped. No row is changed.

## Acceptance criteria
- AC1 Given an issuing authority stored with an acronym, when the admin opens the issuing authorities list,
  then there is no acronym column and no acronym text in any row. (BR1, BR6)
- AC2 Given the add dialog, when it opens, then it has name, description and website and no acronym field;
  and saving a valid name creates the authority with no acronym stored. (BR2, BR4)
- AC3 Given an authority stored with acronym `INSS`, when the admin edits its name and saves, then the stored
  acronym is still `INSS`. (BR3)
- AC4 Given two authorities with different names, when both are saved with no acronym, then both are
  accepted (no acronym conflict); and the migration leaves every existing row's acronym unchanged. (BR4)
- AC5 Given exams of an authority stored with an acronym, when the admin opens the exams list, the authority
  filter, the exam form's picker and the delete confirmation, then each shows the authority's name with no
  acronym. (BR1)
- AC6 Given a published exam of an authority stored with an acronym, when a student sees it in the catalog
  search and on its exam page, then the authority appears by its name with no acronym. (BR1)
- AC7 Given an authority named `Instituto Nacional do Seguro Social` stored with acronym `INSS`, when someone
  searches `INSS` in the admin list, the authority picker or the student catalog, then it is not found by
  the acronym; searching `Seguro` finds it. (BR5)
- AC8 Given the organizer screens and the edition texts, when they render, then the organizer's acronym is
  shown as before. (BR7)
- AC9 All changed texts (placeholders) appear in pt-BR, pt-PT and en, the removed keys are gone from the
  three files, and the missing-key test is green.

## Decisions
- 2026-10-01 — The acronym leaves every screen, admin and student, not only the three first named — one rule
  everywhere, and a value no longer typed would show empty on new authorities (owner, 2026-10-01).
- 2026-10-01 — The searches stop matching the acronym — a new authority has none, so a search by acronym
  would find some and not others with nothing on screen to explain why (owner, 2026-10-01).
- 2026-10-01 — The organizer keeps its acronym; out of this item — boards are known by their acronym and the
  request was about the issuing authority only (owner, 2026-10-01).
- 2026-10-01 — The acronym is removed from the API contracts (save request, issuing authority, exam and
  published-exam responses) instead of kept as an optional field — the Web is the only caller and a field no
  screen reads is a field nobody tests (Claude, technical).
- 2026-10-01 — The domain keeps `Acronym` and `NormalizedAcronym` as nullable properties that no save sets:
  `Create` leaves them null and `Update` does not touch them, so an edit never erases a stored acronym (BR3)
  (Claude, technical).
- 2026-10-01 — The migration makes both columns nullable and drops the acronym's unique index; it changes no
  row and drops no column, so showing the acronym again later is a code change only (Claude, technical).
- 2026-10-01 — `IssuingAuthoritySort.Acronym`, the three `issuing_authority.acronym_*` error codes and their
  resource keys are removed, not left unused (Claude, technical).
- 2026-10-01 — No new packages (Claude, technical).
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

## Out of scope
- The organizer's (exam board's) acronym (BR7).
- Dropping the `acronym` columns or clearing stored acronyms.
- The exam board on the exam (moved to F-35, 2026-09-26).

## Open questions
- (none)

## Change notes
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
- Branch: <feature/F-44>
- Merge: <commit>
- Tests: <count, duration>
- Manual pages: <paths>
