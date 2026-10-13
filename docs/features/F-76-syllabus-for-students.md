---
feature: F-76
epic: Subject taxonomy
status: approved
board: 120
version: 1
---
# Syllabus for students

Technical terms: [glossary](../glossary.md)

## Summary
A signed-in student opening a published exam (the page of F-36, `/catalog/exams/{id}`) can expand any of its published editions and see that edition's syllabus: the notice subjects with their group and number of questions, and the canonical subjects and topics each one covers.

## Start
- Depends on: F-74 (notice subjects, `done`), F-75 (mapping to the taxonomy, `done`), F-79 (subjects and topics, `done`). All three are in `main`; nothing is undelivered.
- Waits on (to start): nothing.
- Needed to validate: an exam with a published edition that has notice subjects (some mapped, some not) and a second published edition with none. The owner or Claude creates them through the F-74 and F-75 screens of the back office; the real guard editions of F-78 (`refining`) would make it richer and are not required.
- Suggested path: `/agile:build F-76` (change to an existing page; no mockup, owner question 4).
- Parallel with: F-77, F-78.

## Goal
Make the taxonomy visible to the student for the first time: before choosing a target exam, a student sees what each edition's notice actually covers, in the notice's own words and in the product's canonical vocabulary.

## What exists (verified 2026-10-13)
- Student side (F-36, later F-42 and F-44): `/catalog` and `/catalog/exams/{id}` (`CatalogExam.razor`), gated by `catalog.browse`. The exam page lists published editions as read-only rows of `AppItemRows`; there is no edition page (the original summary's "page of an edition" was a false premise, found 2026-10-06).
- Read side: `IPublishedExamQueries` (`Simulab.Catalog.Contracts`): `ListAsync`, `FindAsync` (exam and its published editions as `PublishedExamEditionResponse`: id, year, position, board, reference, url, date) and `FiltersAsync`. Nothing about notice subjects. Route `GET /api/v1/catalog/published-exams/{id}` (`PublishedExamEndpoints.FindAsync`).
- Notice subjects (F-74, merged): `NoticeSubject` with `Group` (optional), `Label`, `QuestionCount` (optional) and a display order; `NoticeSubjectQueries` returns them in display order with their mappings (`NoticeSubjectResponse`, `NoticeSubjectMappingResponse`: subject id and name, topic id and name or null).
- Mapping (F-75, merged): 0 to 50 entries per notice subject, each a whole canonical subject or one canonical topic; a topic entry shows its topic's **current** subject (F-75 BR8); optional, so an edition may be partly mapped.
- Display rules to reuse: `NoticeSubjectCoverText` (order and text of a cover entry, F-75 BR13); in `NoticeSubjectsSection.razor` the grouping by run of equal group (F-74 BR6) and the sum footer (F-74 BR8) are private to that section, and the resource keys `NoticeSubjects.Group.None`, `NoticeSubjects.Total.*` and `NoticeSubjects.Covers.*` exist.
- The kit has no component to open and close a block; `AppIcons.Expand` (`ArrowDropDown`) exists. No page uses `MudCollapse` or `MudExpansion*`.
- Items that touched this code since the first refinement (2026-10-06): F-74 and F-75 merged, F-79 merged.

## Users and use cases
- UC1 A Student (any user with `catalog.browse`) opens a published exam and sees, on each published edition's row, a button that opens that edition's syllabus.
- UC2 The Student reads the notice subjects under their group headings, each with its number of questions and the canonical subjects and topics it covers.
- UC3 The Student sees at once when an edition has no syllabus registered yet and when a notice subject is not mapped yet.

## Business rules
- BR1 Each published edition on the exam page carries its notice subjects, in display order (F-74), as the same contract the back office reads: group, label, number of questions and the mapping entries. A draft edition never reaches the student side (F-36 BR1), so neither do its notice subjects; a deleted notice subject never appears.
- BR2 The syllabus of each edition is closed when the page opens. The button, on the edition's row, says how many notice subjects it holds; opening and closing is per edition and several can be open at once.
- BR3 An edition with no notice subjects shows no button; its row says its syllabus is not registered yet.
- BR4 Inside an open syllabus, one heading per run of notice subjects with the same group (case and accents ignored, first spelling used), as in F-74 BR6; no heading is drawn when no row has a group.
- BR5 Each notice subject shows its label and, apart from it, its number of questions, or "not stated" when it has none.
- BR6 Under the label, the canonical subjects and topics it covers, with the F-75 BR13 order and text: whole subject as "Mathematics · whole subject", topic as "Mathematics › Fractions". A notice subject with no entry shows the note "topics not mapped yet"; it is a plain note, not a warning (nothing is for the student to fix).
- BR7 Below the syllabus, the total of questions follows the F-74 BR8 rule: the sum of the stated numbers, and how many notice subjects have no number and stay out of it; "no subject states its number" when none does.
- BR8 Names (labels, groups, canonical subjects and topics) are content: shown as stored, never translated (ADR-0001 #27). The exam's content language marks the label and the group text.
- BR9 No new permission, route or role: `catalog.browse` gates the page and the endpoint as in F-36 BR10; a user without it gets what F-36 already gives.
- BR10 `ListAsync` (the catalog list) is unchanged: the syllabus travels only with the exam's page.

## Screens and API
- `/catalog/exams/{id}` (`CatalogExam.razor`): each edition row (`AppItemRows`) gains a second line with the syllabus control and the closed or open block described below. The aside, breadcrumb and header do not change.
- Kit: a small `AppDisclosure` component (a button with `aria-expanded` and `aria-controls`, `AppIcons.Expand` turning when open, the content below), documented in the kit's rules like the others. The group-run logic and the sum text move to a shared helper used by both the admin section and this page (extract, do not copy).
- `GET /api/v1/catalog/published-exams/{id}` (unchanged route): `PublishedExamEditionResponse` gains `noticeSubjects: NoticeSubjectResponse[]` (empty when none; the existing type, display order, mappings included). No other endpoint changes; OpenAPI document returns 200.
- Data: no new table, column or migration.
- Resource keys (pt-BR, pt-PT, en), neutral names to settle at the build: `Catalog.Syllabus.Show.One` / `.Many` (button, with the count), `Catalog.Syllabus.Hide`, `Catalog.Syllabus.NotRegistered`, `Catalog.Syllabus.NotMapped`, `Catalog.Syllabus.Questions.One` / `.Many` / `.Unstated` (with its spoken form). The group none heading, the total lines and the cover text reuse the `NoticeSubjects.*` keys.
- States of the syllabus block, in the page's own states: no new loading or error state, because it arrives with the exam; if the exam fails to load the page shows what F-36 already shows.
- Accessibility: the button is the first focus stop of the block and keeps its place when it opens; its accessible name changes with its state (`Show` / `Hide`); each group is a heading level under the section's heading; the cover list is a list; "not stated" has a spoken text, not only a dash.

## Acceptance criteria
- AC1 Given a published edition with notice subjects in two groups, when a Student opens the exam page and opens that edition's syllabus, then the notice subjects appear in display order, under one heading per group, each with its number of questions.
- AC2 Given a notice subject that covers a whole subject and a topic, when the syllabus is open, then both appear in the F-75 order and text ("Subject · whole subject", "Subject › Topic"), the topic under its current subject.
- AC3 Given a notice subject with no mapping entry, when the syllabus is open, then it shows the "topics not mapped yet" note and no warning chip.
- AC4 Given a published edition with no notice subject, when the page opens, then its row says the syllabus is not registered yet and has no button.
- AC5 Given the notice subjects of an edition with a mix of stated and unstated numbers, when the syllabus is open, then the total is the sum of the stated numbers and says how many are left out (F-74 BR8).
- AC6 Given the page just opened, when nothing was clicked, then every syllabus is closed; when the button of one edition is activated, then that block opens, the button says it is open (`aria-expanded`), and another edition's block stays as it was.
- AC7 Given an exam with a published and a draft edition, when the Api answers, then only the published edition is there and no notice subject of the draft edition is returned; a deleted notice subject is not returned.
- AC8 Given an exam page and a topic that moved to another subject, when the syllabus is read, then the entry shows the new subject (F-75 BR8).
- AC9 Given the catalog list, when it is read, then it carries no notice subject (BR10).
- AC10 All new texts appear in every locale of `UI locales:` (pt-BR, pt-PT, en) and the missing-key test is green.

## Criterion → test
| Criterion | Test |
| --- | --- |
| AC1 | pending |
| AC2 | pending |
| AC3 | pending |
| AC4 | pending |
| AC5 | pending |
| AC6 | pending |
| AC7 | pending |
| AC8 | pending |
| AC9 | pending |
| AC10 | pending |

## Decisions
- 2026-10-06 — Pause the refinement until F-75 is approved — this item only shows what F-75 defines; refining first would risk a change note — asked (owner, question 1).
- 2026-10-06 — The syllabus opens inside the edition's row on the exam page, no new route — the edition is already a row there — asked (owner, question 2).
- 2026-10-06 — Each notice subject shows its group, label and number of questions as the notice states them, and below it the canonical items it covers — asked (owner, question 3).
- 2026-10-06 — A notice subject not mapped yet is shown with a "topics not mapped yet" note; an edition without notice subjects says its syllabus is not registered yet — asked (owner, question 4).
- 2026-10-13 — All syllabus blocks are closed when the page opens, one button per edition, several can be open — keeps the page short and the student opens only the edition that matters — asked (owner, question 1).
- 2026-10-13 — Cover entries are a plain list in the F-75 form (whole subject, or "Subject › Topic"), not grouped by canonical subject — reuses the text and order the back office already has, one vocabulary on both screens — asked (owner, question 2).
- 2026-10-13 — The total of questions follows the F-74 BR8 rule — the student sees the weight of each block and there is no new rule — asked (owner, question 3).
- 2026-10-13 — No mockup (`/agile:screen`): the screen section above is the design — one open/close control on an existing row — asked (owner, question 4).
- 2026-10-13 — The notice subjects ride on the exam's detail call, one call, no endpoint of their own and no loading state on opening — an exam has a handful of editions of a few dozen notice subjects with at most 50 entries each; the lazy alternative adds a route, a loading and an error state for no measured gain — decided by Claude (technical).
- 2026-10-13 — The contract reuses `NoticeSubjectResponse` — the same shape the back office reads, no second type to keep equal — decided by Claude (technical).
- 2026-10-13 — The query reads the notice subjects and mappings of all the exam's published editions in the same fixed number of queries (not per edition, not per row), extending `NoticeSubjectQueries` — the F-75 note about never reading one query per row — decided by Claude (technical).
- 2026-10-13 — Reuse `catalog.browse`; draft editions never show (F-36 BR1); names are content and not translated; no new packages (production and tests) — confirmed: F-36 BR1, BR10 and `docs/features/F-36-catalog-browsing-for-students.md`; no package is needed, the kit, bunit and the test containers already cover it.

## Out of scope
- Searching or filtering the catalog by subject or topic ("exams that cover Fractions"): a later item if the owner wants it; not registered now.
- Links from a topic to practice, recommendations or analytics (epics E-10, E-11).
- Weight and minimum per notice subject (epic E-6).
- Editing anything: the page stays read-only.
- A page of its own for an edition, print or export of the syllabus.

## Open questions
- (none)

## Events

- 2026-10-13 question — asked 4, from context 5
