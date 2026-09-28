---
feature: F-35
epic: Assessment catalog
status: refining
board: 754
version: 1
---
# Exam editions back office

## Summary
Admin screens to manage the editions of an exam. An edition is one paper actually applied: the year, the job it selects for (named in the edition itself), the application date, the official link to its notice, and whether it is a draft or published. An edital that opens several jobs with different papers becomes one edition per paper (owner, 2026-09-20). The closest Simulae source is `ExamNotice` and `ExamNoticeFormDialog`, but the model is new work. Needs /agile:screen.

The edition is also **where the exam board lives** — a required link to `Organizer`, not a field on the exam (owner, 2026-09-26). Two reasons, both checked:

- The Manaus municipal guard edital (`Edital nº 01, de 23 de março de 2026`, read 2026-09-26) names the contracting body on its cover ("A PREFEITURA DE MANAUS, por meio da SEMAD, torna pública…") and the board in item 1.1 ("sendo sua execução de responsabilidade do Instituto Consulplan"). The board is declared inside the edital, and the edital is the edition.
- The simulator needs it there. The owner's use case: *"select 20 Mathematics questions from board Consulplan, from the editais of 2021 to 2026"* — a student practising a board's style (epic Exam Simulator, 695). That query walks question → paper → edition → board, and only works if each edition carries its own board: the same exam changes board between years, so a board on the exam would return the wrong questions for every year it changed.

So the edition carries the board and the notice year, and both have to be filterable. Recorded here because the data has to exist before the engine can ask for it.

## Start
- Depends on: F-34 (`done`, merged as 3386b40) and F-43 (`done`) — `Exam`, `/admin/exams/{id}`, `AppLookupField`, `AppItemRows` and `AppStatusChip` are all in `main`.
- Waits on (to start): the `/agile:screen` mockup, run inside this refinement and approved by the owner together with the feature (owner, 2026-09-28).
- Needed to validate: the app host started from this worktree and an Admin account signed in by the owner (Claude does not enter credentials).
- Suggested path: glossary rows; `ExamEdition` in the domain with its `Result` rules and the migration; the endpoints and the two delete guards; `AppDateField` in the kit with its gallery entry; the editions section on the exam page; the edition page.
- Can run beside it: nothing in the catalog — F-36 and F-37 both depend on it. An item outside `Catalog` and the exam page could, with the owner's `--worktree`.

## What exists
- `Exam` (`Simulab.Catalog.Domain/Entities/Exam.cs`): `IssuingAuthorityId`, `Name`, `AssessmentType`, `Scope`, `ScopeDetail`, `ContentLanguage`, `NormalizedName`; `TenantEntity`, soft delete, null `TenantId`, rules answer with `Result`. Its unique index is `ux_exams_tenant_authority_normalized_name` (`IsUniquePerTenant()`, `NULLS NOT DISTINCT`).
- `Organizer` is the exam board and nothing else since F-34 v2 (three kinds, pt-BR "Banca"). Nothing points at it: `DeleteOrganizerHandler.cs:9` says the guard arrives "when F-35's editions start pointing here".
- `DeleteExamHandler.cs:9` soft-deletes with no check and says the guard for an exam with editions "arrives with F-35 (BR13)".
- `/admin/exams/{id}` (`ExamForm.razor`, 503 lines) is the exam's own page, recomposed by F-43 into section cards plus a read-only aside. Line 18 and line 455 reserve the editions section below the form; saving a new exam already turns the page into the edit form so the editions are "one scroll away".
- Kit (`Simulab.Web/Components/Ui/`): `AppItemRows` (child items with per-row actions, built by F-43 for exactly this section and used only in the gallery so far — `AppItemRowsTests.cs:7`), `AppStatusChip` (tones `Neutral`/`Success`/`Warning`/`Error`/`Info`), `AppLookupField` (server-backed parent picker), `AppRadioCards`, `AppSectionCard`, `AppFormGrid`, `AppFormAside`, `AppErrorSummary`, `AppFormActions`, `AppDateColumn`. **There is no date input** in the kit, and no page uses `MudDatePicker`.
- `CatalogApiClient` is the module's typed client; `ExamEndpoints` groups `/api/v1/catalog/exams` behind `PermissionPolicy.NameFor(CatalogPermissions.Manage)`.
- Glossary: `ExamEdition` (edição), `Notice` (edital) and the content states `Draft`/`InReview`/`Published` are already named. The job an edition selects for has no identifier yet, and `Cargo` is on the forbidden list.
- Packages: MudBlazor 9.9.0 (`MudDatePicker` included), EF Core with Npgsql, `xunit`, `AwesomeAssertions`, `Testcontainers.PostgreSql`, `bunit`, `Microsoft.AspNetCore.Mvc.Testing` — all in `Directory.Packages.props`.

Simulae (`repo/src/Modules/ContentCatalog/Modules.ContentCatalog.Domain/Entities/ExamNotice.cs`, read-only):
- `ExamNotice` is `ContestId`, `ExamBoardId`, `NoticeYear` (1900 to 2100), `PublicationDate` (optional, shown as "Data da Prova"), `Status` `Open`/`Closed` with `Close()`/`Reopen()`. No job, no notice link, no notice number, no uniqueness rule. It throws `ContentCatalogDomainException` with pt-BR messages.
- `ExamNoticeFormDialog.razor` is a dialog on `RegisterExamPage` with `ExamBoardSelect`, a `MudNumericField` for the year, a `MudDatePicker` and an Open/Closed button group; all texts hardcoded pt-BR, `Icons.Material.Filled`.
- What carries over: the board per edition, the notice year and an optional date. The rest is rewritten: states, job, notice fields, uniqueness, `Result` rules, the page instead of the dialog.

## Goal
Give the catalog its lowest level: the paper actually applied, with its board and its year. Questions, the syllabus and every simulation attach to an edition, and F-36 shows a student only what has a published one.

## Users and use cases
- UC1 An Admin opens an existing exam at `/admin/exams/{id}` and sees, below the form, its editions: notice year, position, board acronym, publication state and the application date when there is one, newest year first.
- UC2 An Admin adds an edition from that section on its own page: picks the board by typing part of its name or acronym, types the notice year, and optionally the position, the notice reference, the notice link and the application date; it is saved as a draft unless the Admin chooses Published.
- UC3 An Admin opens an edition on the same page, changes it — including publishing or unpublishing it — and saves.
- UC4 An Admin deletes a draft edition after confirming; it disappears from the exam's section.
- UC5 An Admin tries to delete a published edition and is told to unpublish it first.
- UC6 An Admin tries to delete an exam that has editions, or a board that some edition names, and is refused.
- UC7 A Student or Curator gets the ordinary Not Found page on the edition routes; the Api answers 403 `identity.forbidden`.

## Business rules
- BR1 `ExamEdition : TenantEntity` lives in `Simulab.Catalog.Domain/Entities/`, mapped to `catalog.exam_editions`. Global data: `TenantId` null, every unique index that includes it `NULLS NOT DISTINCT` (F-33 BR5). Deleting is a soft delete.
- BR2 An edition belongs to one exam: `ExamId` is a required foreign key to `catalog.exams`, fixed at creation — an edition never moves to another exam. The content language is the exam's; the edition has none of its own (ADR-0001 #27).
- BR3 An edition names its exam board: `OrganizerId` is a required foreign key to `catalog.organizers` (`exam_edition.organizer_required` when missing). An id that matches no organizer, or a deleted one, is 404 `organizer.not_found`; an exam id that matches no exam, or a deleted one, is 404 `exam.not_found`.
- BR4 `NoticeYear` is required, an integer from 1990 to the current year plus one, where the current year comes from `TimeProvider`; anything else is `exam_edition.notice_year_invalid`.
- BR5 `Position` — the job the paper selects for — is optional, trimmed, at most 200 characters (`exam_edition.position_too_long`); blank is stored as null. It is text, not an entity (brief, owner 2026-09-20).
- BR6 `NoticeReference` — how the notice names itself, such as "Edital nº 01/2026" — is optional, trimmed, at most 100 characters (`exam_edition.notice_reference_too_long`). Editions cut from the same edital share it.
- BR7 `NoticeUrl` is optional; when given it is an absolute `http` or `https` URL of at most 300 characters, else `exam_edition.notice_url_invalid` (the organizer website rule, F-33 BR10).
- BR8 `AppliedOn` — the date the paper was applied — is optional, a date without time (`DateOnly`); an edition may be published without it. When given it is not before 1 January of the notice year, else `exam_edition.applied_on_before_notice_year`.
- BR9 `Status` is `Draft` or `Published`, required, travelling as a string; an unknown value is `exam_edition.status_invalid`. A new edition is `Draft` unless the request says `Published`. The change goes both ways with no condition beyond BR4 to BR8: nothing points at an edition yet. `InReview` is not offered (it arrives with epic 698).
- BR10 An edition is unique within its exam by notice year, normalized position and board: a unique index over `TenantId`, `ExamId`, `NoticeYear`, `NormalizedPosition` and `OrganizerId`, `NULLS NOT DISTINCT`, deleted rows included (the F-33 and F-34 precedent). `NormalizedPosition` is `CatalogText.Normalize` of the position, or empty when there is none, so "no position" collides with "no position". A duplicate is 409 `exam_edition.duplicate`.
- BR11 Deleting a `Published` edition is refused with 409 `exam_edition.published`; the message says to unpublish it first. A draft is soft-deleted.
- BR12 Deleting an exam that has at least one edition, deleted editions excluded, is refused with 409 `exam.has_editions`. Deleting an organizer that at least one edition names, deleted editions excluded, is refused with 409 `organizer.has_editions`. Both carry no count (F-34 BR12: `ErrorText.For` takes no argument). Both foreign keys are `Restrict` in the database as well.
- BR13 An index over `(OrganizerId, NoticeYear)` exists so the simulator's "board X, years A to B" query (epic 695) has one to use. No query endpoint over it is built here.
- BR14 An exam's editions come back in one call, not paged, ordered by notice year descending, then position, then the board's name. An exam has a handful of editions; a paged table would be too much furniture for the section (F-43 `AppItemRows`).
- BR15 Domain rules return `Result`, never exceptions: `ExamEdition.Create` and `Update` validate BR3 to BR9, and `Delete` guards BR11.
- BR16 `AppDateField` is a new kit component wrapping `MudDatePicker`, with the anatomy of `AppTextField` (label above with the required marker, the control, then the hint or the error), shown on `/dev/ui` with its empty, filled, error and disabled states. It is the only date input from now on (rule `ui`). It shows and parses dates in the reader's culture and binds a `DateOnly?`.
- BR17 Every UI text of the new section, the edition page, `AppDateField` and every new error code exists in pt-BR, pt-PT and en, in `SharedResources` (F-33 v2).
- BR18 One permission covers it all: `catalog.manage` gates the endpoints, the section and the edition page (F-33 BR4).

## Screens and API

The mockup and the detailed screen section come from `/agile:screen` (owner, 2026-09-28). What is decided before it:

### Routes
| Route | Purpose | Gate |
|---|---|---|
| `/admin/exams/{id:guid}` | the exam page gains the editions section below the form card | `catalog.manage` |
| `/admin/exams/{examId:guid}/editions/new` | the edition page, adding | the same |
| `/admin/exams/{examId:guid}/editions/{id:guid}` | the edition page, editing | the same |
| `/dev/ui` | `AppDateField` added to the gallery | development only |

No menu item: an edition is reached through its exam. No global editions list (owner, question 1).

### The editions section on the exam page
- Only in edit mode. On `/admin/exams/new` the section says the exam must be saved first.
- `AppItemRows`, one row per edition: `NoticeYear · Position · board acronym`, the `AppStatusChip` (`Draft` neutral, `Published` success) and the application date when there is one. Row actions: edit (to the edition page) and delete (`AppConfirmDialog`). An "Add edition" action opens `/admin/exams/{examId}/editions/new`.
- Empty state: the exam has no edition yet.

### The edition page
- Complex entity by rule `ui-project`: its own page, the F-43 composition (section cards, grid, aside, actions at the bottom right).
- Breadcrumb `Content › Exams › <exam name> › <Add edition | notice year · position>`.
- Fields: board (`AppLookupField` over organizers, `Name (ACRONYM)`), notice year, position, notice reference, notice URL, application date (`AppDateField`), status (`Draft` / `Published`).
- Saving a new edition stays on the page as its edit form, the F-34 pattern; Cancel returns to the exam page.

### API
- `GET /api/v1/catalog/exams/{examId:guid}/editions` — the exam's editions, `ExamEditionResponse[]` (BR14); 404 `exam.not_found`.
- `GET /api/v1/catalog/exams/{examId:guid}/editions/{id:guid}` — one; 404 `exam_edition.not_found`.
- `POST /api/v1/catalog/exams/{examId:guid}/editions` — `SaveExamEditionRequest(OrganizerId, NoticeYear, Position, NoticeReference, NoticeUrl, AppliedOn, Status)`; 201.
- `PUT /api/v1/catalog/exams/{examId:guid}/editions/{id:guid}` — the same request; 200.
- `DELETE /api/v1/catalog/exams/{examId:guid}/editions/{id:guid}` — 204.
- `ExamEditionResponse(Id, ExamId, OrganizerId, OrganizerName, OrganizerAcronym, NoticeYear, Position, NoticeReference, NoticeUrl, AppliedOn, Status)`. `Status` is `string?` in the request, parsed by the handler (the F-33 review lesson); `AppliedOn` is an ISO date (`yyyy-MM-dd`) through `AppJson.Options`.
- An edition id under another exam's route is 404 `exam_edition.not_found`.
- Error codes: `exam_edition.not_found` (404); `exam_edition.organizer_required`, `exam_edition.notice_year_invalid`, `exam_edition.position_too_long`, `exam_edition.notice_reference_too_long`, `exam_edition.notice_url_invalid`, `exam_edition.applied_on_before_notice_year`, `exam_edition.status_invalid` (400); `exam_edition.duplicate`, `exam_edition.published`, `exam.has_editions`, `organizer.has_editions` (409). Reused: `exam.not_found`, `organizer.not_found` (404).

## Acceptance criteria
- AC1 Given a database with the previous migration, when the new migration runs, then `catalog.exam_editions` exists with restricted foreign keys to `catalog.exams` and `catalog.organizers`, the unique index of BR10 `NULLS NOT DISTINCT`, and the `(organizer_id, notice_year)` index. (BR1, BR10, BR12, BR13)
- AC2 Given an exam with editions of 2024 and 2026, when the Admin opens `/admin/exams/{id}`, then the section lists both, 2026 first, each with its year, position, board acronym, state chip and date when there is one. (UC1, BR14)
- AC3 Given `/admin/exams/new`, then the section says the exam must be saved first and offers no Add. (UC1)
- AC4 Given the edition page with a board and a notice year only, when the Admin saves, then the edition is created as `Draft`, the page becomes its edit form, and it appears in the exam's section. (UC2, BR9)
- AC5 Given an edition with a position, a notice reference, a notice URL and an application date, when it is saved and reopened, then every field comes back as saved and the date is the same calendar day. (UC2, UC3, BR5 to BR8)
- AC6 Given a notice year of 1989 or of the current year plus two, when the request is sent, then the Api answers 400 `exam_edition.notice_year_invalid` and nothing is written. (BR4)
- AC7 Given an application date before 1 January of the notice year, then 400 `exam_edition.applied_on_before_notice_year`; given a notice URL that is not an absolute http or https address, then 400 `exam_edition.notice_url_invalid`. (BR7, BR8)
- AC8 Given an edition of an exam, when the Admin saves another one for the same exam with the same year, the same board and a position that differs only in case or accents, then the Api answers 409 `exam_edition.duplicate`; with another board, or another year, it is created. (BR10)
- AC9 Given two rows inserted directly with `TenantId` null, the same exam, year, board and no position, when the second is saved, then PostgreSQL rejects it. (BR1, BR10)
- AC10 Given a request whose `organizerId` matches no organizer or a deleted one, then 404 `organizer.not_found`; whose exam is unknown or deleted, then 404 `exam.not_found`; and nothing is written. (BR3)
- AC11 Given a draft edition, when the Admin publishes it, then its chip reads Published; when they set it back to Draft, then it reads Draft. (UC3, BR9)
- AC12 Given a published edition, when the Admin confirms its deletion, then the Api answers 409 `exam_edition.published` and it stays; given a draft, then it disappears from the section and from the list call, and the row is still in the table with `IsDeleted` true. (UC4, UC5, BR11)
- AC13 Given an exam with one edition, when the Admin confirms deleting the exam, then 409 `exam.has_editions` and the translated message; once its only edition is deleted, the exam can be deleted. (UC6, BR12)
- AC14 Given a board that an edition names, when the Admin confirms deleting it on `/admin/organizers`, then 409 `organizer.has_editions` and the translated message; once that edition is deleted, the board can be deleted. (UC6, BR12)
- AC15 Given a status that is not `Draft` or `Published`, then 400 `exam_edition.status_invalid`, and the OpenAPI document names the two values. (BR9)
- AC16 Given a signed-in Student, when they open an edition route, then Not Found, and every `/api/v1/catalog/exams/{examId}/editions` route answers 403 `identity.forbidden`. (UC7, BR18)
- AC17 Given `AppDateField` on `/dev/ui` and on the edition page, then it shows its empty, filled, error and disabled states, reads and writes the date in pt-BR, pt-PT and en formats, and can be filled by keyboard alone. (BR16)
- AC18 Given `ExamEdition.Create` with a notice year of 1989, when it runs, then it returns a failed `Result` with `exam_edition.notice_year_invalid` and throws nothing. (BR15)
- AC19 All new texts appear in pt-BR, pt-PT and en, and the missing-key test is green. (BR17)

## Decisions
- 2026-09-28 — The editions are a section of the exam page, and each edition is edited on its own page; no global editions list — rule `ui-project` names the edition a complex entity, F-43 built `AppItemRows` for this section, and nobody asked for a cross-exam list yet (owner, question 1).
- 2026-09-28 — The position is optional free text — ENEM, entrance exams and certifications select for no job; no `Job` entity (brief, 2026-09-20) (owner, question 2).
- 2026-09-28 — The edition keeps an optional notice reference besides the link — it is what shows that several editions came from one edital (owner, question 3).
- 2026-09-28 — The application date is always optional, also to publish — the owner chose not to tie publication to it (owner, question 4; the recommendation was "required to publish").
- 2026-09-28 — An edition is unique by exam, notice year, position and board — a paper annulled and re-applied by another board in the same year is a real case (owner, question 5; the recommendation was year and position).
- 2026-09-28 — Deleting an exam or a board that has editions is refused with 409 — the F-34 pattern; cascade would remove a whole history in one click (owner, question 6).
- 2026-09-28 — Notice years from 1990 to the current year plus one, and a given application date not before the notice year — old papers are still practised, and an edital published in December is for next year's paper (owner, question 7).
- 2026-09-28 — Two states only, `Draft` and `Published`; `InReview` arrives with the AI import (epic 698) (owner, question 8).
- 2026-09-28 — Publishing goes both ways — nothing points at an edition yet; the lock arrives with questions and simulations (owner, question 9).
- 2026-09-28 — A published edition must be unpublished before it can be deleted — a student may be looking at it (owner, question 10).
- 2026-09-28 — The board and year are stored and indexed, with no query endpoint — the simulator (epic 695) owns the shape of that query (owner, question 11).
- 2026-09-28 — `/agile:screen` runs before approval — first child-entity page and first date field of the kit (owner, question 12).
- 2026-09-28 — `AppDateField` is a new kit component wrapping `MudDatePicker`; no new package, for production or for tests (rule `build-config`) (owner, question 13).
- 2026-09-28 — A section row reads year · position · board acronym, with the state chip and the date — two editions of the same year stay distinguishable (owner, question 14).
- 2026-09-28 — English identifiers: `ExamEdition`, `Position` (the job; `Cargo` is forbidden and `Job` would suggest an entity), `NoticeYear`, `NoticeReference`, `NoticeUrl`, `AppliedOn`, `ExamEditionStatus` with `Draft`/`Published` (the glossary's content states). The glossary gets the `Position`, `NoticeReference` and `AppliedOn` rows before their first use in code (rule `naming`).
- 2026-09-28 — The edition list under an exam is not paged (BR14) — a handful of rows; `AppItemRows` has no paging, and adding it would be structure without a use.
- 2026-09-28 — The status travels inside the save request, not through `publish`/`unpublish` routes — the form owns the choice, and a transition with no rule of its own does not need its own endpoint.
- 2026-09-28 — Simulae's `ExamNotice` is not imported: it throws with pt-BR messages, its `Open`/`Closed` means something else, and it has no job, notice link or uniqueness. What carries over is the board per edition, the notice year and the optional date.
- 2026-09-28 — No seed rows: F-37 brings the real editions (the F-33 and F-34 decision).

## Out of scope
- The notice document itself: only the official link is stored; the upload arrives with epic 698 (owner, 2026-09-23).
- Scoring rules, wrong-answer penalty and cut-off: epic 695 (owner, 2026-09-23).
- The notice's syllabus (`NoticeSubject`): epic 692.
- The `InReview` state and any review workflow: epic 698.
- A query endpoint by board and year range: epic 695.
- A list of every edition across exams: not asked.
- Students seeing published editions: F-36. Seed rows: F-37.
- Locking a published edition that questions or simulations point at: when those exist (epics 693 and 695).

## Open questions
- (none)

## Change notes

## Validation script
<!-- Written at the end of build. At most 8 steps the product owner follows on screen. -->

## Delivery
<!-- Filled by /agile:ship. -->
- Branch: feature/F-35
- Merge: <commit>
- Tests: <count, duration>
- Manual pages: <paths>
