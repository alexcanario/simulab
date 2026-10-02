---
feature: F-42
epic: Assessment catalog
status: validating
board: 81
version: 1
---
# State picker for the exam scope

## Summary
F-34 stores the exam's scope detail as free text, so `SP`, `Sao Paulo` and `São Paulo` can sit in the same
column. Offer the 27 Brazilian states as a fixed list in that field when the scope is `State`, keeping free
text for `Municipal` (5570 municipalities are a feature of their own). Raised while refining F-34 (owner,
screen question 2, 2026-09-24). Epic 704 (Portugal exams) has to revisit the list: Portugal has districts,
not states.

## Start
- Depends on: F-34 (done) — the field, the scope enum and the form page it sits on come from there; F-36 (done) added the normalized scope detail the student search reads.
- Waits on (to start): nothing.
- Needed to validate: the app host running from this worktree, with at least one `State` exam whose detail is free text (to see BR6).
- Suggested path: `/agile:build F-42`.
- Parallel with: any item outside the Catalog module and the exam form. Not with F-57, which builds on this one.

## Goal
One exam, one way to name its state: the back office picks it from a list, the API refuses anything else, and
the student finds and reads it the same way on every screen.

## What already exists (verified 2026-10-01)
- `ExamScope` is `National` / `State` / `Municipal` (`src/Modules/Catalog/Simulab.Catalog.Contracts/ExamScope.cs`).
- `Exam.ScopeDetail` is `string?`, at most 120 characters, `scope_detail varchar(120) null`; `NormalizedScopeDetail` (F-36) is the accent-free upper-case copy the published search matches (`Exam.cs:137`, `PublishedExamQueries.cs`).
- BR8 of F-34 lives in `Exam.cs:105-118`: national forces null; state and municipal require the detail (`exam.scope_detail_required`), max length (`exam.scope_detail_too_long`).
- `SaveExamRequest.ScopeDetail` goes through `POST /api/v1/catalog/exams` and `PUT /api/v1/catalog/exams/{id}`; `ExamResponse`, `PublishedExamResponse` and `PublishedExamDetailResponse` return it as stored.
- The form (`ExamForm.razor`) shows one `AppTextField` for both scopes, with the label `Exams.Field.State` or `Exams.Field.Municipality`.
- The detail is shown in `Exams.razor` (back-office list), `CatalogSearch.razor` (student search) and `CatalogExam.razor` (student exam page).
- No list of Brazilian states exists in `src/` or in Simulae. The seeded exams (`SeedMunicipalGuardCatalog`) are all `Municipal`; no seeded `State` exam.
- The UI kit has `AppLookupField` (type-to-filter, `MinChars` 2 by default) and `AppSelectField`.

## Users and use cases
- UC1 A catalog editor sets an exam's scope to `State` and picks its state from the 27, typing part of the name or the acronym to filter, and saves.
- UC2 A catalog editor opens an exam saved before this item with a state the list does not recognize, sees what was stored, picks the right state and saves.
- UC3 A student searches the catalog for `SP` or `sao paulo` and finds the exams of São Paulo, each shown as `São Paulo (SP)`.
- UC4 An API client saves a `State` exam with an acronym off the list and gets a validation error.

## Business rules
- BR1 When the scope is `State`, the scope detail is one of the 27 Brazilian states (26 states and the Federal District), identified by its acronym. See `## Approved list`.
- BR2 The API refuses a `State` exam whose detail is not an acronym of the list with `exam.scope_detail_unknown_state`. The acronym is matched ignoring case and surrounding spaces and stored in upper case.
- BR3 `Municipal` keeps free text exactly as in F-34 (BR8 of F-34, unchanged); `National` keeps forcing null.
- BR4 Wherever the detail of a `State` exam is shown, it reads `<name> (<acronym>)`, e.g. `São Paulo (SP)`. A stored value the list does not recognize (BR6) is shown as stored.
- BR5 The student search finds a `State` exam by its state's name or acronym, with or without accents.
- BR6 Existing `State` exams: a migration replaces a detail whose normalized text equals a state's normalized name, its acronym, or `<name> (<acronym>)` / `<name>/<acronym>` / `<name> - <acronym>` by that state's acronym. Anything else stays as it was; editing that exam opens the picker empty, with a hint that quotes the old text, and saving requires a state (BR2).
- BR7 The list is the same whatever the exam's content language. Portugal's districts are epic 704's job.

## Screens and API
- `ExamForm.razor` (create and edit) — scope `State` shows an `AppLookupField` with the 27 states instead of the text field; the whole list opens on focus, typing filters by name or acronym (accent-insensitive); `Municipal` keeps the `AppTextField`. Switching scope clears the detail, as today.
- `Exams.razor`, `CatalogSearch.razor`, `CatalogExam.razor` — the detail of a `State` exam reads `São Paulo (SP)` (BR4).
- `POST /api/v1/catalog/exams`, `PUT /api/v1/catalog/exams/{id}` — unchanged shape; for `State`, `scopeDetail` carries the acronym.
- `GET` responses — unchanged shape; `scopeDetail` of a `State` exam carries the acronym.
- Error codes: `exam.scope_detail_unknown_state` (new), `exam.scope_detail_required` and `exam.scope_detail_too_long` (unchanged).

## Acceptance criteria
- AC1 Given the exam form with scope `State`, when the editor focuses the state field, then the 27 states of `## Approved list` are offered, in that order, each as `<name> (<acronym>)`. (UC1, BR1)
- AC2 Given the state field, when the editor types `sp`, `paulo` or `sao`, then the list narrows to the states whose name or acronym contains it, ignoring accents and case. (UC1)
- AC3 Given scope `State` and São Paulo picked, when the editor saves, then the exam is stored with detail `SP` and reopening it shows `São Paulo (SP)` selected. (UC1, BR1)
- AC4 Given scope `Municipal`, when the editor fills the detail, then it is a free text field as in F-34 and saves whatever text is typed. (BR3)
- AC5 Given a `State` save request with `scopeDetail` `sp`, then it is stored as `SP`; with `Sampa`, then it is refused with `exam.scope_detail_unknown_state` and nothing is stored. (UC4, BR2)
- AC6 Given a `State` exam stored as `SP`, then the back-office list, the student search and the student exam page show `São Paulo (SP)`. (UC3, BR4)
- AC7 Given a published `State` exam stored as `SP`, when a student searches `sp`, `sao paulo` or `São Paulo`, then the exam is found. (UC3, BR5)
- AC8 Given `State` exams stored as `Sao Paulo`, `são paulo`, `SP`, `São Paulo (SP)`, `Ceará/CE` and `Sampa` before the migration, when it runs, then the first five hold `SP`, `SP`, `SP`, `SP`, `CE` and `Sampa` stays `Sampa`; `Municipal` and `National` rows are untouched. (BR6)
- AC9 Given a `State` exam stored as `Sampa`, when the editor opens it, then the state field is empty with a hint quoting `Sampa`, and saving without picking a state shows the required error. (UC2, BR6)
- AC10 Given an exam whose content language is pt-PT with scope `State`, then the same 27 states are offered. (BR7)
- AC11 All new texts appear in pt-BR, pt-PT and en, and the missing-key test is green.

## Approved list
Brazilian states, in this order (name as shown, acronym stored):
1. Acre (AC)
2. Alagoas (AL)
3. Amapá (AP)
4. Amazonas (AM)
5. Bahia (BA)
6. Ceará (CE)
7. Distrito Federal (DF)
8. Espírito Santo (ES)
9. Goiás (GO)
10. Maranhão (MA)
11. Mato Grosso (MT)
12. Mato Grosso do Sul (MS)
13. Minas Gerais (MG)
14. Pará (PA)
15. Paraíba (PB)
16. Paraná (PR)
17. Pernambuco (PE)
18. Piauí (PI)
19. Rio de Janeiro (RJ)
20. Rio Grande do Norte (RN)
21. Rio Grande do Sul (RS)
22. Rondônia (RO)
23. Roraima (RR)
24. Santa Catarina (SC)
25. São Paulo (SP)
26. Sergipe (SE)
27. Tocantins (TO)

## Decisions
- 2026-10-01 — The state field is a type-to-filter list (`AppLookupField`) that opens the whole list on focus — typing `SP` or `paulo` is faster than scrolling 27 lines (owner, question 1).
- 2026-10-01 — A state is shown as `São Paulo (SP)` everywhere — the student recognizes either form, and the search finds both (owner, question 2).
- 2026-10-01 — Existing free-text states are migrated when they match a state unambiguously; the rest stay and are fixed at the next edit (BR6) — no data is guessed or lost (owner, question 3).
- 2026-10-01 — The API refuses a state off the list too, not only the screen — otherwise free text comes back through the other door (owner, question 4).
- 2026-10-01 — Filtering exams by state is out of scope, captured as F-57 — keeps this item to one field on one screen; the text search already finds `SP` (owner, question 5).
- 2026-10-01 — The 27 states are offered whatever the content language; epic 704 replaces them for Portugal — there are no Portugal exams yet (owner, question 6).
- 2026-10-01 — The acronym is what is stored in `scope_detail`, and the name comes from a static list in `Simulab.Catalog.Contracts` (`BrazilianStates`) that the domain and the Web share — the stored value stays short and clean, the display can change without touching data, and no schema change is needed (Claude, technical).
- 2026-10-01 — For a `State` exam, `normalized_scope_detail` holds the normalized name and acronym (`SAO PAULO SP`), so the F-36 search keeps matching by either without a new column (Claude, technical).
- 2026-10-01 — The list is a static class, not a table — 27 fixed values change by constitutional amendment, not by an editor (Claude, technical).
- 2026-10-01 — No new packages: `AppLookupField`, EF migrations and the existing test stack cover it (Claude, technical).
- 2026-10-01 — Board id corrected from the old Azure id 763 to GitHub issue 81 (Claude, housekeeping).
- 2026-10-02 — Build: no `system-design` or `architect` pass. The item adds a static list to `Contracts`, one error code and a data-only migration (the model snapshot is unchanged); it creates no project, no message between modules and no schema change (Claude, technical).
- 2026-10-02 — Build: `AppLookupField` gains two optional parameters, `OpenOnFocus` (default false) and `MaxItems` (default 20), because AC1 needs the whole list on focus and the kit capped a list at 20 and opened only after a minimum of characters. Every other picker keeps its behavior, pinned by `AppLookupFieldTests` (Claude, technical).
- 2026-10-02 — Build: `AppLookupOption` carries a `Guid` id that a state does not have, so the state picker uses `Guid.Empty` and maps the picked option back to its acronym by the display text, which is unique in the list. A new id type in the kit would touch every other picker (Claude, technical).
- 2026-10-02 — Build: moving the scope between `State` and `Municipal` clears the detail. Before this item the field stayed on screen and kept its text; with a picker on one side and free text on the other, carrying `SP` into a municipality (or `Guarulhos` into the picker) would send a value the reader cannot see (Claude, technical).
- 2026-10-02 — Build: the migration `NormalizeExamStates` writes the 27 states out in SQL instead of reading `BrazilianStates`: a migration is history and must not change when the code does. It also matches on the stored text with runs of spaces collapsed (`mato   grosso do sul - ms`), which the item's wording allows ("normalized text") (Claude, technical).
- 2026-10-02 — Build: no app-host check by Claude. Starting it applies the migration to the owner's local database, which only the owner may change (`workflow.md`), and the exam form is behind sign-in, which Claude may not do. What the tests cannot see is in the validation script (Claude, process).
- 2026-10-02 — Build: seven existing Catalog tests and the Web fixtures saved a `State` exam with free text (`Sao Paulo`, `Goiás`, `Pará`); they now use acronyms, because BR2 refuses the old input. Their intent is unchanged (Claude, technical).
- 2026-10-02 — Review (`reviewer` agent, base `9b7d6a7`): no blocker, no major, four minors. (1) Fixed: `BrazilianStates.Search` also matches the displayed form `São Paulo (SP)`, so a filled picker that is focused again still offers its state; tests added in `BrazilianStatesTests` and `ExamFormTests`, and the validation script checks it on screen. (2) Fixed: the accent-stripping recipe was copied into `BrazilianStates`; it now lives once in `Simulab.SharedKernel.Text.ComparableText`, which `CatalogText.Normalize` and `BrazilianStates` both call. (3) Fixed: `/dev/ui` shows the fixed-list `AppLookupField` (`gallery-lookup-fixed-list`). (4) New at ship: the app manual (`docs/manual/<locale>/exams.md` and `catalog.md`) still describes the state as typed text; `/agile:ship` updates the three languages. Confirmed against the code before acting (Claude, process).

## Out of scope
- A state filter on the catalog search and the exam list — F-57.
- A municipality list (5570 municipalities) — a feature of its own, not captured yet.
- Portugal's districts — epic 704.

## Open questions
- (none)

## Change notes

## Validation script
Needed to validate: the app host started from this worktree (you start it), the seeded admin (`admin@simulab.local`, F-52), and one `State` exam saved as free text to see BR6 (step 1 makes one; the app host applies the migration to your local database on start, which is why Claude did not start it). In place now: nothing is running.

1. Before starting, in DataGrip on the local `simulab` database (`Host=127.0.0.1;Port=5432;Database=simulab;Username=postgres;Password=postgres`), turn two exams into old-style ones, so the migration has something to fix and something to leave alone (pick any two exam names that exist):
   `UPDATE catalog.exams SET scope = 'State', scope_detail = 'Sao Paulo' WHERE name = '<exam A>';`
   `UPDATE catalog.exams SET scope = 'State', scope_detail = 'Sampa' WHERE name = '<exam B>';`
2. Start the app (`dotnet run --project src/Hosts/Simulab.AppHost`, the same in Git Bash and PowerShell 7), sign in as the admin → the migration `NormalizeExamStates` applies on start. In DataGrip, `SELECT name, scope_detail, normalized_scope_detail FROM catalog.exams WHERE scope = 'State';` → exam A holds `SP` / `SAO PAULO SP`; exam B still holds `Sampa` (AC8).
3. Open **Exams** → exam A reads `São Paulo (SP)` under "State"; exam B reads `Sampa` (AC6, BR4, BR6).
4. Open exam B for editing → the state field is empty with the hint “This exam was saved with “Sampa”…”; choose **Save** → the page refuses with "Say where this exam applies."; type `paulo`, pick `São Paulo (SP)`, save → it saves and the list shows `São Paulo (SP)` (AC9, AC3).
5. Choose **Add exam**, scope **State**, click into the state field → all 27 states open, from `Acre (AC)` to `Tocantins (TO)`; type `sp`, then `sao`, then `mato` → the list narrows each time, with or without accents (AC1, AC2). Switch the scope to **Municipal** → a plain text field appears, empty; type any text and save → it saves as typed (AC4).
6. Keyboard only, on **Add exam** with scope **State**: Tab into the state field → the list opens; type `rio`, arrow down to `Rio Grande do Sul (RS)`, Enter → it is picked; Tab to **Save** → it saves. Then Shift+Tab back into the state field, now filled → the list opens and still offers `Rio Grande do Sul (RS)`, not "nothing found".
7. Sign in as a student, open the catalog search, type `sp`, then `sao paulo`, then `São Paulo` → the published State exam of São Paulo is found each time, and its row and its exam page read `São Paulo (SP)` (AC6, AC7). Set the exam's content language to pt-PT on the form → the same 27 states are offered (AC10).
8. Switch the language to pt-BR and pt-PT on the exam form with scope State → the label, placeholder, hint and the refusal text are translated (AC11).

## Delivery
<!-- Filled by /agile:ship. -->
- Branch: feature/F-42
- Merge: <commit>
- Tests: <count, duration>
- Manual pages: <paths>
