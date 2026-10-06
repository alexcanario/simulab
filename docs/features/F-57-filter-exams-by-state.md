---
feature: F-57
epic: Assessment catalog
status: validating
board: 96
version: 1
---
# Filter exams by state

Technical terms: [glossary](../glossary.md)

## Summary
A state filter on the student catalog search (`CatalogSearch`) and on the back-office exam list (`Exams`), built on the 27-state list F-42 introduced. Raised while refining F-42 (owner, 2026-10-01): kept out of F-42 so that item stays one field on one screen.

## Start
- Depends on: F-42 (done, merge `74616f9`) — the state list (`BrazilianStates`), the stored acronym and the fixed-list `AppLookupField` come from there.
- Waits on (to start): nothing.
- Needed to validate: the app host started from this worktree (the owner starts it) and at least two published `State` exams of different states plus one `National` exam — the validation script makes them through the back office; the seeded admin (F-52) and a student account.
- Suggested path: `/agile:build F-57`.
- Parallel with: any item outside the Catalog module and the two catalog list pages. Not with F-80 (same query and page).

## Goal
A student or an editor narrows an exam list to one state in one step, and the list never offers a state that leads the student to an empty page.

## What already exists (verified 2026-10-04)
- Nothing in the Catalog pages or queries changed since the F-42 merge (`git log 74616f9..main` on `src/Modules/Catalog` and `src/Hosts/Simulab.Web/Components/Pages/Catalog` is empty).
- `Exam.Scope` (`National` / `State` / `Municipal`) and `Exam.ScopeDetail` (`string?`): for `State` it holds the upper-case acronym (`Exam.cs:128-137`); `National` forces null; `Municipal` holds free text (a city), with no state.
- `BrazilianStates` (`Simulab.Catalog.Contracts/BrazilianStates.cs`): the 27 states in the order of F-42's approved list, `FindByAcronym`, `Search` (name, acronym or `São Paulo (SP)`, accent-insensitive).
- Student search `CatalogSearch.razor` (`CatalogPermissions.Browse`): filters assessment type, scope, board (`organizerId`) and notice year, kept in the URL by `CatalogQueryString`; board and year options come from `GET /api/v1/catalog/published-exam-filters` (`PublishedExamFiltersResponse`), with `Catalog.Filter.OptionsFailed` when that call fails; "Clear filters" and the empty state `Catalog.Empty.Filters`. API: `GET /api/v1/catalog/published-exams` (`PublishedExamListQuery`).
- Back-office list `Exams.razor` (`CatalogPermissions.Manage`): filters issuing authority (`AppLookupField`), assessment type, scope; not kept in the URL. API: `GET /api/v1/catalog/exams` (`ExamListQuery`, `ExamQueries`).
- Text search: the student search matches the exam name, the authority and the state (`PublishedExamQueries.cs:23-30`); **the back-office search matches the exam name only** (`ExamQueries.cs:24-28`). The summary's premise that "the text search already finds an exam by the state" holds for the student search only (captured as F-80).
- No index on `scope` or `scope_detail` (`ExamConfiguration.cs`).

## Users and use cases
- UC1 A student opens the catalog search, picks `São Paulo (SP)` in the state filter and sees only the published `State` exams of São Paulo; the scope filter now reads `State`.
- UC2 A student shares or reloads the search URL with the state filter and gets the same list.
- UC3 A catalog editor opens the back-office exam list, picks a state and sees only the `State` exams of that state, published or not.
- UC4 A user changes the scope filter to `National`, `Municipal` or all; the state filter empties and the list follows the new scope.
- UC5 An API client lists exams with `state=sp` and gets the São Paulo `State` exams; with `state=XX` it gets a validation error.

## Business rules
- BR1 The state filter takes one state or none (all), identified by its acronym.
- BR2 A state filter shows only `State` exams whose stored state is that acronym. `National` and `Municipal` exams never match a state filter.
- BR3 Picking a state sets the scope filter to `State`. Changing the scope filter to anything other than `State` (including all) clears the state filter. Clearing the state keeps the scope as it is.
- BR4 The student state filter offers only the states that have at least one published `State` exam, in the order of F-42's approved list. The back-office state filter offers the 27 states in that order.
- BR5 The state filter combines with every other filter and with the text search (all must match), and "Clear filters" clears it too.
- BR6 The API matches the acronym ignoring case and surrounding spaces; an acronym off the list is refused with `exam.filter_unknown_state`. The student page drops an unknown `state` from the URL instead of calling the API with it.

## Screens and API
- `CatalogSearch.razor` — a state filter in the filter bar, after scope: an `AppLookupField` with the fixed list (`OpenOnFocus`), items `São Paulo (SP)`, typing filters by name or acronym. Its options come from the filters endpoint with board and year; while they load or when they fail it is disabled like those two. Kept in the URL as `state=SP`.
- `Exams.razor` — the same field after scope, with the 27 states; not kept in the URL (like its other filters).
- `GET /api/v1/catalog/published-exams?state=SP` — new optional parameter.
- `GET /api/v1/catalog/exams?state=SP` — new optional parameter.
- `GET /api/v1/catalog/published-exam-filters` — `PublishedExamFiltersResponse` gains `states`: the acronyms with at least one published `State` exam, in list order.
- Error code: `exam.filter_unknown_state` (new, 400).
- Texts: `Catalog.Filter.State` / `Exams.Filter.State` label and placeholder; `Catalog.Filter.OptionsFailed` now names the state too ("board, year and state options").

## Acceptance criteria
- AC1 Given published `State` exams of SP and RJ and a published `National` exam, when a student picks `São Paulo (SP)`, then only the SP exam is listed and the scope filter reads `State`. (UC1, BR2, BR3)
- AC2 Given published `State` exams of SP and RJ only (and an unpublished `State` exam of MG), when the student opens the state filter, then it offers exactly `Rio de Janeiro (RJ)` and `São Paulo (SP)`, in list order. (BR4)
- AC3 Given the back office with any data, when the editor opens the state filter, then it offers the 27 states of F-42's approved list in that order; picking `Minas Gerais (MG)` lists the unpublished MG exam. (UC3, BR4)
- AC4 Given the state filter set to SP, when the user changes the scope filter to `National` or to all, then the state filter is empty and the list follows the new scope; when the user clears only the state, the scope stays `State`. (UC4, BR3)
- AC5 Given the student search filtered by SP, when the URL is reloaded or opened in a new tab, then the same state is selected and the same list is shown; given `state=XX` in the URL, then the page loads unfiltered by state and without an error. (UC2, BR6)
- AC6 Given the state SP plus an assessment type and a text search, then only exams matching all three are listed; "Clear filters" empties the state too. (BR5)
- AC7 Given the API, `state=sp` and `state= SP ` return the SP `State` exams on both list endpoints; `state=XX` returns 400 with `exam.filter_unknown_state`. (UC5, BR6)
- AC8 Given a `Municipal` exam whose detail is `São Paulo`, when filtering by SP, then it is not listed. (BR2)
- AC9 Given the filters endpoint fails, then the state filter is disabled with board and year and the message names the three. (Screens)
- AC10 All new and changed texts appear in pt-BR, pt-PT and en, and the missing-key test is green.

## Decisions
- 2026-10-04 — One state at a time — the same shape as every other filter in the bar (owner, question 1).
- 2026-10-04 — A state filter shows only `State` exams of that state, never nationals — the filter says exactly what it shows; nationals have their scope filter (owner, question 2).
- 2026-10-04 — Picking a state sets the scope to `State`; any other scope clears the state — one step for the student and no impossible combination (owner, question 3).
- 2026-10-04 — The student filter offers only states with a published exam; the back office offers the 27 — the student never lands on an empty list, like the board and year filters; the editor sees the same list as the form (owner, question 4).
- 2026-10-04 — The field is the fixed-list `AppLookupField` that opens whole on focus, as in the F-42 form — one way to pick a state in the app (owner, question 5).
- 2026-10-04 — The back-office text search stays name-only; matching authority and state there is captured as F-80 (#123) — F-57 stays one filter (owner, question 6).
- 2026-10-04 — `Municipal` exams are not found by state; it needs the municipality list F-42 already left out — the text search still finds the city name (owner, question 7).
- 2026-10-04 — The API parameter is `state` with the acronym, and an unknown acronym is a 400 `exam.filter_unknown_state`, not ignored — an ignored filter would silently return every exam (Claude, technical).
- 2026-10-04 — The student page validates `state` from the URL against `BrazilianStates` and drops an unknown one, so a stale link opens the search instead of an error (Claude, technical).
- 2026-10-04 — The student state options travel in `PublishedExamFiltersResponse` with board and year: one call, one failure path (Claude, technical).
- 2026-10-04 — No new index: not measured yet. The build runs `EXPLAIN` of the filtered list on the test container and adds an index only if it shows one is needed (Claude, technical).
- 2026-10-04 — No schema change and no migration: the filter reads `scope` and `scope_detail` as F-42 left them (Claude, technical).
- 2026-10-04 — No new packages: `AppLookupField`, the Catalog test fixtures and bUnit already cover it (Claude, technical).
- 2026-10-04 — No `/agile:screen` mockup: one field added to two existing filter bars with an existing kit component (Claude, process).
- 2026-10-04 — Approved by the owner ("aprovo F-57").
- 2026-10-06 — Build: the filter reads `State` exams with `scope = State and scope_detail = <acronym>`; the endpoints parse `state` in one internal helper (`StateFilter`) shared by both lists, and the queries match the acronym again through `BrazilianStates.FindByAcronym`, so a caller of `IPublishedExamQueries` gets the same rule (Claude, technical).
- 2026-10-06 — Build: a stored `State` acronym the list does not know (a value saved before F-42) is not offered by the student filter — it could not be named — and an unknown state in a student URL is dropped twice: by the query-string parser (not in the 27) and again once the options arrive (not in the published ones) (Claude, technical).
- 2026-10-06 — Build: the `EXPLAIN` of the filtered list was not measured: on the test container the table holds a few dozen rows and PostgreSQL chooses a sequential scan whatever the index. No index is added; a measurement on real volume is the trigger to add one (Claude, technical).
- 2026-10-06 — Build: the OpenAPI document `docs/api/Simulab.Api.json` was regenerated by the Api host test (the new `state` parameter and `states` property); `DocGen --check` is green (Claude, technical).

## Out of scope
- Several states at once.
- Finding `Municipal` exams by the state of their city — needs a municipality list (not captured yet, as in F-42).
- Back-office text search by authority and state — F-80.
- Keeping the back-office filters in the URL.
- Portugal's districts — epic 704.

## Open questions
- (none)

## Change notes

## Validation script
Needed to validate: the app host started from this worktree (the owner starts it, in place now), the seeded admin `admin@simulab.local` (F-52, password in the Api user secrets) and a student account; the steps make their own data.

1. Stop any other app host, then start this one with its own database (Git Bash: `cd /d/wt/simulab/f-57-filter-exams-by && Database__Name=simulab_f57 dotnet run --project src/Hosts/Simulab.AppHost`; PowerShell 7: `cd D:\wt\simulab\f-57-filter-exams-by; $env:Database__Name = "simulab_f57"; dotnet run --project src/Hosts/Simulab.AppHost`). Expect the dashboard URL and no `fail` line; the Web is https://localhost:7125.
2. Sign in as the admin. Make one issuing authority and one board, then four exams under it: two `State` (São Paulo `SP`, Rio de Janeiro `RJ`), one `National`, one `State` Minas Gerais `MG`. Give the SP, RJ and National exams a `Published` edition (any year); leave MG with none.
3. Back office, Exams list: the **State** filter is next to Scope; opening it shows all 27 states, Acre first. Pick `Minas Gerais (MG)`: only the MG exam is listed (unpublished) and Scope reads `State`. Change Scope to `National`: the state field empties and the list follows. Pick MG again, then clear only the state: Scope stays `State`.
4. Sign in as a student (sign up at `/sign-up` and open the link in Mailpit if you have none) and open `/catalog`. The State filter offers exactly `Rio de Janeiro (RJ)` and `São Paulo (SP)`, in that order (not MG: nothing published there). Pick `São Paulo (SP)`: only the SP exam is listed, Scope reads `State`, the address ends with `state=SP`.
5. Reload that address: same state, same list. Open `/catalog?state=XX`: the full list, no error, and the address cleans itself. Type a word of the SP exam in the search and pick Assessment type: the list keeps all three; **Clear filters** empties the state too.
6. Switch the language to Português (Brasil) from the top bar: the filter reads `Estado` with `Todos os estados`; on both pages.
7. Keyboard only, on `/catalog`: Tab to the State field (the whole list opens), arrow down, Enter to pick, Tab on; Backspace clears it (Enter arrives empty in the browser pane, so this one is yours).
8. Light and dark: the open list and the field read well in both (the screen check of the library's own states was not made by Claude).

## Delivery
<!-- Filled by /agile:ship. -->
