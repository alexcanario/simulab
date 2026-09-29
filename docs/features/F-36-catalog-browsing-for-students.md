---
feature: F-36
epic: Assessment catalog
status: refining
board: 755
version: 1
---
# Catalog browsing for students

## Summary
The student-facing half of the catalog: a signed-in student searches published exams and filters by assessment type, exam board, year and scope, then opens an exam and sees its published editions. It is how a student finds the exam they are preparing for, and the read side every simulator will call. Needs /agile:screen.

## Start
- Depends on: F-35 (`done`, merged as 0d547e2) — `ExamEdition` with `Draft`/`Published`, the `(OrganizerId, NoticeYear)` index and the exam page kit are in `main`.
- Waits on (to start): the `/agile:screen` mockup of the two screens, approved by the owner with this feature.
- Needed to validate: the app host started from this worktree, at least one exam with a published edition and one with only drafts (created by the owner in the back office, or by F-37 if it lands first), and two accounts signed in by the owner — a Student and an Admin (Claude does not enter credentials).
- Suggested path: glossary rows; `catalog.browse` in `CatalogPermissions` and its one-time grant in Identity's seed; `NormalizedScopeDetail` and its migration; `IPublishedExamQueries` and the three endpoints; the search page; the exam page; the menu item.
- Can run beside it: F-37 (seed data only, no screen or contract in common) with the owner's `--worktree`; B-20 touches the `exams.scope` column comment and may conflict in the migration snapshot — merge it first or rebase after.

## What exists
- `Exam` (`Simulab.Catalog.Domain/Entities/Exam.cs`): `IssuingAuthorityId`, `Name`, `AssessmentType`, `Scope`, `ScopeDetail`, `ContentLanguage`, `NormalizedName`. No normalized form of `ScopeDetail`.
- `IssuingAuthority` and `Organizer` both carry `NormalizedName` and `NormalizedAcronym`; `CatalogText.Normalize` is the one normalizer (trim, upper invariant, no accents).
- `ExamEdition` (F-35): `ExamId`, `OrganizerId` (the board), `NoticeYear`, `Position`, `NoticeReference`, `NoticeUrl`, `AppliedOn`, `Status` (`Draft`/`Published`). Index over `(OrganizerId, NoticeYear)` (F-35 BR13).
- Api: every `/api/v1/catalog/*` route requires `catalog.manage`; `ExamQueries.ListAsync` searches `NormalizedName` with one `Contains` of the whole text and filters by authority, type and scope. There is no read side for students.
- Permissions: `CatalogPermissions.All` is `[catalog.manage]`. `IdentityModule.EnsureRolesAndPermissionsAsync` creates every declared permission and grants it to Admin on every start; the other seed roles get theirs only from the back office (F-6 BR3). A new account is a Student (F-30 BR4) and the Student role holds no permission today.
- Web: `NavigationSection.Study` exists and has no item; `NavigationItems.All` hides an item behind its `RequiredPermission`. `/` is a placeholder home. Kit: `AppDataTable` (server paging), `AppSelectField`, `AppItemRows`, `AppStatusChip`, `AppPageHeader`, the empty/loading/error states.
- Items touching this code since F-35: none merged. B-20 (`approved`, worktree `b-20-exam-scope-column`) changes the `exams.scope` column comment. F-42 (state picker) and F-37 (seed) are `idea`.
- Packages: nothing new. The production code and the tests use what `Directory.Packages.props` already has (EF Core with Npgsql, MudBlazor, xunit, AwesomeAssertions, Testcontainers.PostgreSql, bunit, Mvc.Testing).

## Goal
Let a student find the exam they are preparing for among what the catalog has published, and give every later module (simulators, target exam, coach) one read side that shows only published content.

## Users and use cases
- UC1 A Student opens "Catalog" in the Study section of the menu and sees the published exams, one row per exam, ordered by name, paged.
- UC2 The Student types part of an exam's name, its issuing authority's name or acronym, or its state or municipality, and the list narrows to the matching exams.
- UC3 The Student filters by assessment type, scope, exam board and notice year, alone or together with the text, and clears the filters.
- UC4 The Student opens an exam and sees what it is (name, issuing authority, assessment type, scope, content language) and its published editions, newest year first, each with its board, position, notice reference, application date and a link to the official notice.
- UC5 A Student, Curator or Admin never sees a draft edition, nor an exam whose editions are all drafts, in the catalog, the exam page or the filter options.
- UC6 A user whose roles lack `catalog.browse` sees no menu item and gets the ordinary Not Found page on the catalog routes; the Api answers 403 `identity.forbidden`.
- UC7 An Admin removes `catalog.browse` from a role in the roles back office and it stays removed after the app restarts.

## Business rules
- BR1 An exam is published when at least one of its editions is `Published` (deleted editions excluded). Only published exams exist for the student side: the list, the exam page, the filter options and `IPublishedExamQueries` never return an unpublished exam or a draft edition, whoever asks (owner, question 8).
- BR2 The list has one row per published exam: name, issuing authority name and acronym, assessment type, scope and scope detail, content language, the number of published editions and the latest published notice year. Ordered by exam name A to Z, then id; paged with the shared rule (default 25, cap 100).
- BR3 The search text is normalized with `CatalogText.Normalize` and split into words on whitespace; an exam matches when every word appears in at least one of: the exam's normalized name, its issuing authority's normalized name or acronym, its normalized scope detail. "guarda sp" finds "Guarda Municipal" of "Prefeitura de São Paulo (SP)". Blank text is no filter.
- BR4 `Exam` gains `NormalizedScopeDetail` (`CatalogText.Normalize` of `ScopeDetail`, empty when there is none), kept by `Exam.Create`/`Update`. The migration adds the column and fills it for the existing rows.
- BR5 Filters: assessment type (one of the four), scope (`National`, `State`, `Municipal`; no state or municipality picker — F-42), exam board (one organizer id) and notice year (one year). They combine with AND, and with the text. The board and the year match on the same published edition: an exam with a 2024 FGV edition and a 2025 CEBRASPE edition does not match "FGV, 2025".
- BR6 A filter value the Api cannot read (an unknown type, scope, id or year) is no filter, as in the back office list (F-34 BR14): a stale bookmark shows results, not an error.
- BR7 The filter options come from the published catalog: the boards that name at least one published edition (name and acronym, ordered by name) and the notice years of published editions (newest first). A board or a year with nothing published is not offered.
- BR8 The exam page shows the exam (BR2 fields) and every published edition of it — all of them, whatever filters led there — ordered by notice year descending, then position, then board name (F-35 BR14 order). Each edition shows notice year, position, board name and acronym, notice reference, application date and the notice link. An exam that does not exist, is deleted or is not published is 404 `exam.not_found`.
- BR9 The notice link opens the official page in a new tab (`target="_blank"`, `rel="noopener noreferrer"`) and says so to screen readers. Content is shown as stored, never translated (ADR-0001 #27); the exam's content language is shown.
- BR10 A new permission `catalog.browse` gates the three endpoints, the menu item and both pages. Identity grants it to Student, Curator and Admin only in the start where the permission row is created; afterwards only the roles back office changes who holds it, and a later start never grants it again (Admin keeps receiving every permission on each start, F-6 BR3). Having `catalog.manage` alone does not open the student catalog (owner, question 7).
- BR11 `IPublishedExamQueries` in `Simulab.Catalog.Contracts` is the read side other modules call (list, find with editions, filter options); the endpoints use the same interface.
- BR12 Every new UI text and error code exists in pt-BR, pt-PT and en in `SharedResources`; the enum labels reuse the back office keys.

## Screens and API
Detailed by `/agile:screen` (mockup `docs/features/mockups/F-36-catalog-browsing-for-students.html`) before approval.

### Routes
| Route | Purpose | Gate |
|---|---|---|
| `/catalog` | search and filters, one row per published exam | `catalog.browse` |
| `/catalog/exams/{id:guid}` | the exam and its published editions | `catalog.browse` |

Menu: item "Catalog" (`Nav.Catalog`) in the Study section, `RequiredPermission: CatalogPermissions.Browse`.

### API
- `GET /api/v1/catalog/published-exams?page&pageSize&search&assessmentType&scope&organizerId&noticeYear` — `PublishedExamPageResponse(Items, Total)` of `PublishedExamResponse`.
- `GET /api/v1/catalog/published-exams/{id}` — `PublishedExamDetailResponse` (the exam plus `Editions`: `PublishedExamEditionResponse` list); 404 `exam.not_found`.
- `GET /api/v1/catalog/published-exam-filters` — `PublishedExamFiltersResponse(Organizers, NoticeYears)`.
- All three: `catalog.browse`, else 403 `identity.forbidden`. No new error code (`exam.not_found` exists).

## Acceptance criteria
- AC1 Given exam A with one published and one draft edition and exam B with only drafts, when a Student lists published exams, then A appears with 1 edition and its latest published year, and B does not. (BR1, BR2, UC1, UC5)
- AC2 Given three published exams, when the list is requested with no text, then they come ordered by name, paged with the given size, and `Total` counts all three. (BR2)
- AC3 Given exam "Guarda Municipal" of "Prefeitura de São Paulo" (acronym "PMSP") with scope detail "São Paulo", when the search is "guarda sao", "PMSP" or "GUARDA  paulo", then it is found; when it is "guarda rio", then it is not. (BR3, UC2)
- AC4 Given an existing exam saved before the migration with scope detail "Goiânia", when the migration runs, then its `NormalizedScopeDetail` is "GOIANIA"; and saving an exam with a new scope detail updates it. (BR4)
- AC5 Given a published exam with a 2024 FGV edition and a 2025 CEBRASPE edition, when filtered by FGV and 2025, then it is not listed; by FGV and 2024, it is; by assessment type or scope that differs, it is not. (BR5, UC3)
- AC6 Given the filters `assessmentType=Foo`, `scope=Bar`, an unknown `organizerId` and `noticeYear=abc`, when the list is requested, then 200 with the unfiltered result. (BR6)
- AC7 Given a board that names only draft editions and a year with only drafts, when the filter options are requested, then neither is offered; boards come by name and years newest first. (BR7)
- AC8 Given a published exam with editions 2023 (published), 2025 (published) and 2024 (draft), when a Student opens its page, then the two published editions show with board, position, notice reference, application date and notice link, 2025 first, and the 2024 draft does not. (BR8, UC4)
- AC9 Given an exam with only drafts, a deleted exam and a random id, when its page or `GET /published-exams/{id}` is requested, then Not Found / 404 `exam.not_found`. (BR8)
- AC10 Given an edition with a notice URL, when its row renders, then the link has `target="_blank"`, `rel="noopener noreferrer"` and an accessible name that says it opens in a new tab. (BR9)
- AC11 Given a fresh database, when the app starts, then Student, Curator and Admin hold `catalog.browse`; given the Admin removed it from Student and the app restarts, then Student still lacks it. (BR10, UC7)
- AC12 Given a user whose roles hold `catalog.manage` but not `catalog.browse`, then the menu shows no Catalog item, `/catalog` and `/catalog/exams/{id}` show Not Found, and the three endpoints answer 403 `identity.forbidden`; with `catalog.browse`, the item shows in the Study section. (BR10, UC6)
- AC13 Given a signed-in Admin with both permissions, when the catalog is listed, then draft-only exams are not shown. (BR1, UC5)
- AC14 Given `IPublishedExamQueries` resolved from the container, then it returns the same results as the endpoints for AC1, AC5 and AC8. (BR11)
- AC15 All new texts appear in pt-BR, pt-PT and en, and the missing-key test is green. (BR12)

## Decisions
- 2026-09-29 — One row per exam; its editions show on the exam page — the student looks for "the exam", and a row per edition repeats the exam name (owner, question 1).
- 2026-09-29 — Two screens (search, exam page); no edition page yet — an edition has six fields today; its own page arrives with the syllabus (epic 692) (owner, question 2).
- 2026-09-29 — Menu item "Catalog" in the Study section at `/catalog`; Home untouched — the section exists and is empty, and the Home is another item (owner, question 3).
- 2026-09-29 — Default order by exam name — predictable and the same as the back office (owner, question 4).
- 2026-09-29 — The "organizer" filter is the edition's exam board, not the issuing authority — it is what the brief calls organizer and what the Question Bank Simulator filters by; the authority is reachable through the text search (owner, question 5).
- 2026-09-29 — One notice year, not a range — one field on screen (owner, question 6).
- 2026-09-29 — Scope filter without state or municipality; the detail enters the text search — F-42 is still an idea and free text would split spellings (owner, question 7).
- 2026-09-29 — Search over exam name, issuing authority name and acronym, and scope detail, word by word — a student types "guarda sp", not the exact name (owner, question 8).
- 2026-09-29 — New permission `catalog.browse`, granted once to the three seed roles when it is created — permissions are checked by name, not by role, and the Admin can restrict it later (plans, institutions) without a start undoing it (owner, question 9).
- 2026-09-29 — Nobody sees drafts in the student catalog, Admin included — one rule; the back office is where drafts live (owner, question 10).
- 2026-09-29 — Board and year must match on the same edition — otherwise "FGV 2025" would list an exam FGV never applied in 2025 (Claude, technical).
- 2026-09-29 — Filter options are served by the Api from the published catalog, not by the back office lookup — `ListOrganizers` requires `catalog.manage`, and offering a board with nothing published leads to an empty list (Claude, technical).
- 2026-09-29 — `NormalizedScopeDetail` is a stored column, backfilled by the migration — the search stays in the database without a PostgreSQL extension, the F-33 BR9 precedent (Claude, technical). The backfill's accent stripping is checked by AC4.
- 2026-09-29 — Routes `published-exams` and `published-exam-filters` instead of a `browse` verb — the naming rule wants plural nouns (Claude, technical).
- 2026-09-29 — A Student with no published exam to show sees the empty state, not an error; the query cost is measured with `EXPLAIN` on the test container during build, not assumed (Claude, technical).

## Out of scope
- Choosing and saving a target exam with a date: that belongs to the AI coach's study plan (epic 701).
- An edition's own page, its syllabus and its questions — epics 692 and 693.
- A state or municipality picker in the filters — F-42.
- Visitors who are not signed in: the catalog is for signed-in users.
- Changing the Home page.

## Open questions
- (none)

## Change notes

## Validation script
1. <Step> → <expected result>

## Delivery
- Branch: feature/F-36
- Merge: <commit>
- Tests: <count, duration>
- Manual pages: <paths>
