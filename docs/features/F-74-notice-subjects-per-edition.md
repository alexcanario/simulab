---
feature: F-74
epic: Subject taxonomy
status: refining
board: 118
version: 1
---
# Notice subjects per edition

Technical terms: [glossary](../glossary.md)

## Summary
On an exam edition, an admin records the `NoticeSubject` rows exactly as the notice states them: the label, the grouping and the number of questions (ADR-0001 #44). Weight and minimum score per notice subject are not part of it: they move to epic Exam Simulator (E-6) with the other scoring rules (epic decision, 2026-10-04).

## Start
- Depends on: nothing (F-79 removed, owner, 2026-10-04: F-74 does not reference `Subject` or `Topic`).
- Waits on (to start): nothing. Not built at the same time as F-79: both change the `Catalog` migration snapshot.
- Needed to validate: an exam with an edition in the local database — the F-37 municipal guard seed provides it (Claude).
- Suggested path: `/agile:refine` with `/agile:screen` → `/agile:build`.
- Parallel with: F-51, F-77 (refinement); not F-79 at build.

## Goal
Give each edition its syllabus in the notice's own words, so the Exam Simulator (E-6) can show the real paper and F-75 can map each row to the canonical taxonomy.

## What exists (verified 2026-10-04)
- `ExamEdition` (F-35) in the `Catalog` module, edited on its own page `/admin/exams/{examId}/editions/{id}` (`ExamEditionForm.razor`) with `AppSectionCard` sections; soft delete; `Draft`/`Published`; a published edition stays editable.
- Endpoints under `/api/v1/exams/{examId}/editions` (`ExamEndpoints.cs`), guarded by `catalog.manage`.
- No `NoticeSubject` in `src/`. Simulae has no notice subject (only `ExamNoticeTopic`, notice → topic): nothing to import.
- `docs/glossary.md` row `NoticeSubject` still mentions weight and minimum: corrected by this item.

## Users and use cases
- UC1 An admin with `catalog.manage` opens an edition and sees its notice subjects, grouped and in the notice's order, with the sum of the stated question counts.
- UC2 The admin adds a notice subject: group (optional), label, number of questions (optional).
- UC3 The admin edits a notice subject's group, label or number of questions.
- UC4 The admin moves a notice subject up or down inside its group.
- UC5 The admin deletes a notice subject.
- UC6 The admin deletes an edition; its notice subjects leave with it.

## Business rules
- BR1 A notice subject belongs to one edition, fixed at creation. Global data: `TenantId` is null (ADR-0001 #8).
- BR2 Label: required, trimmed, 2 to 200 characters.
- BR3 Group: optional free text, trimmed, at most 100 characters; blank is stored as null ("no group"). The dialog offers the groups already used in the edition.
- BR4 Number of questions: optional; when given, an integer from 1 to 500. Blank means "the notice does not say".
- BR5 Within one edition, the label is unique inside its group, ignoring case and accents (same normalization as the edition's position); "no group" is one group. Deleted rows do not count.
- BR6 Order: every notice subject has a position inside its edition. A new one goes last in its group (last in the edition when its group is new). Moving up or down swaps with the neighbour in the same group; the first row of a group cannot move up and the last cannot move down. Groups are shown in the order of their first row.
- BR7 Changing the group of a notice subject moves it to the end of the new group.
- BR8 The sum shown is the sum of the stated numbers; rows without a number are left out of it and the screen says how many have no number. It is not stored and not checked against anything.
- BR9 Notice subjects can be added, edited, moved and deleted on a Draft or a Published edition, like the edition's other fields.
- BR10 Deleting a notice subject is a soft delete. Deleting an edition soft-deletes its notice subjects in the same save; the edition's delete confirmation says so.
- BR11 Every endpoint requires `catalog.manage`; the section and its actions are shown only to who has it.

## Screens and API
- `/admin/exams/{examId}/editions/{id}` — new section card "Notice subjects" below the edition's fields (only for a saved edition): rows grouped under their group heading, each row with label, number of questions (or "—"), move up, move down, edit, delete; an "Add notice subject" button; footer with the sum (BR8). Add and edit in a dialog (rule `ui-project`: simple entity). Detailed in `## Screen` by `/agile:screen`.
- GET `/api/v1/exams/{examId}/editions/{editionId}/notice-subjects` — the list in display order.
- POST `/api/v1/exams/{examId}/editions/{editionId}/notice-subjects` — add (UC2).
- PUT `/api/v1/exams/{examId}/editions/{editionId}/notice-subjects/{id}` — edit (UC3).
- POST `/api/v1/exams/{examId}/editions/{editionId}/notice-subjects/{id}/move` — body `{ "direction": "up" | "down" }` (UC4).
- DELETE `/api/v1/exams/{examId}/editions/{editionId}/notice-subjects/{id}` — soft delete (UC5).
- Error codes: `exam.not_found`, `exam_edition.not_found` (existing); `notice_subject.not_found`, `notice_subject.label_required`, `notice_subject.label_too_short`, `notice_subject.label_too_long`, `notice_subject.group_too_long`, `notice_subject.question_count_invalid`, `notice_subject.duplicate`, `notice_subject.move_invalid`.
- Table `catalog.notice_subjects`: `id`, `tenant_id`, `exam_edition_id`, `group_label`, `label`, `normalized_group`, `normalized_label`, `question_count`, `position`, audit and soft-delete columns, column comments; unique index `ux_notice_subjects_tenant_edition_group_label` over (`tenant_id`, `exam_edition_id`, `normalized_group`, `normalized_label`) `NULLS NOT DISTINCT`, filtered to rows not deleted.

## Acceptance criteria
- AC1 Given an edition with notice subjects in two groups, when the admin opens the edition page, then the section lists the groups in the order of their first row and the rows in their order, each with its number of questions or "—" (UC1, BR6).
- AC2 Given rows with 10, 20 and one without a number, when the section loads, then the footer shows 30 and says one row has no number (BR8).
- AC3 Given a saved edition, when the admin adds group "Basic knowledge", label "Portuguese", 10 questions, then the row is stored and appears last in that group (UC2, BR6).
- AC4 Given a blank label, a 1-character label or a 201-character label, when saved, then the API answers 400 with `notice_subject.label_required`, `notice_subject.label_too_short` or `notice_subject.label_too_long` and nothing is stored (BR2).
- AC5 Given a 101-character group, when saved, then 400 `notice_subject.group_too_long`; given a blank group, then it is stored as null (BR3).
- AC6 Given a number of questions of 0, 501 or a negative number, when saved, then 400 `notice_subject.question_count_invalid`; given none, then it is stored as null (BR4).
- AC7 Given "Português" in group "Básicos", when the admin adds "portugues" in "básicos", then 409 `notice_subject.duplicate`; when added in group "Específicos", then it is stored (BR5).
- AC8 Given a deleted "Portuguese" in a group, when the admin adds "Portuguese" to the same group again, then it is stored (BR5).
- AC9 Given an existing row, when the admin edits its label and number, then the new values are stored and its position is unchanged (UC3).
- AC10 Given a row moved to another group, when saved, then it appears last in the new group (BR7).
- AC11 Given three rows A, B, C in one group, when the admin moves B up, then the order is B, A, C; when the admin moves the first row up or the last row down, then 400 `notice_subject.move_invalid` and the order is unchanged (UC4, BR6).
- AC12 Given a row, when the admin deletes it after confirming, then it disappears from the list and stays in the table marked deleted (UC5, BR10).
- AC13 Given a Draft edition with notice subjects, when the admin deletes the edition, then its notice subjects are marked deleted in the same save, and the confirmation text says the notice subjects leave with it (UC6, BR10).
- AC14 Given a Published edition, when the admin adds, edits, moves or deletes a notice subject, then it succeeds (BR9).
- AC15 Given an edition id that does not exist or belongs to another exam, when any endpoint is called, then 404 `exam_edition.not_found`; given a notice subject id of another edition, then 404 `notice_subject.not_found` (BR1).
- AC16 Given a user without `catalog.manage`, when any endpoint is called, then 403; and the edition page is not reachable (BR11).
- AC17 Given a new edition not saved yet, when the page shows, then the section says to save the edition first and offers no add (UC2).
- AC18 All new texts appear in pt-BR, pt-PT and en, and the missing-key test is green.

## Decisions
- 2026-10-04 — Group is optional free text on each row, no entity of its own — faithful to the notice and less code; the dialog suggests groups already used to avoid typos (owner, question 1).
- 2026-10-04 — Number of questions is optional, 1 to 500 when given — some notices give only the paper's total; blank means "the notice does not say", E-6 decides how to use it (owner, question 2; upper bound by Claude: no real paper has 500 questions for one subject).
- 2026-10-04 — No total-questions field on the edition; the screen shows the sum — no rule that could block a notice with annulled questions (owner, question 3).
- 2026-10-04 — Label unique per edition and group, ignoring case and accents — the same name under two headings happens in real notices (owner, question 4).
- 2026-10-04 — Notice subjects are editable on a Published edition — the edition's other fields already are; students only see them from F-76 (owner, question 5).
- 2026-10-04 — Deleting an edition soft-deletes its notice subjects, and the confirmation says so — rule `ui-project` (owner, question 6).
- 2026-10-04 — A section card on the edition page with an add/edit dialog — a notice subject is a simple entity; one way to edit (owner, question 7).
- 2026-10-04 — Order by move up/down buttons inside the group — keyboard and screen-reader friendly, no new kit component (owner, question 8).
- 2026-10-04 — Dependency on F-79 removed; F-74 and F-79 are not built at the same time — no data link between them, but both change the `Catalog` migration snapshot (owner, question 9).
- 2026-10-04 — Copying notice subjects from another edition is out of scope, captured as F-86 (owner, question 10).
- 2026-10-04 — No new packages, production or tests — the kit, EF Core and the existing test hosts cover it (Claude).
- 2026-10-04 — The unique index excludes deleted rows (a filtered index), unlike `exam_editions` — an admin who deletes a row by mistake must be able to add it again; a notice subject has no history worth keeping its name reserved (Claude).
- 2026-10-04 — Move is its own endpoint with a direction, not a position in the PUT — two rows change in one save, and the server, not the screen, knows the neighbours (Claude).
- 2026-10-04 — `NoticeSubject` lives in the `Catalog` module under `ExamEdition`, as the epic decided (Claude, epic decision).

## Out of scope
- Weight and minimum score per notice subject — epic E-6.
- Mapping to canonical subjects and topics — F-75.
- The student's view of the syllabus — F-76.
- Copying notice subjects from another edition — F-86.
- The notice subjects of the F-37 municipal guard editions — F-78.

## Open questions
- (none)

## Change notes

## Validation script
<!-- Written at the end of build. -->

## Delivery
<!-- Filled by /agile:ship. -->
