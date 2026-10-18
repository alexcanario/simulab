---
feature: F-86
epic: Subject taxonomy
status: approved
board: 129
version: 1
---
# Copy notice subjects from another edition

Technical terms: [glossary](../glossary.md)

## Summary
On an exam edition, the admin copies the `NoticeSubject` rows (group, label, number of questions, order) of another edition of the same exam, to save typing when a notice repeats from year to year. Captured from the F-74 refinement (owner, 2026-10-04).

## Start
- Depends on: F-74 (done, merged `a2a3dd5`) and F-75 (done, merged `58ab5a7`). The item branch is behind `main` and does not contain F-75 yet: the build merges `main` into it first.
- Waits on (to start): nothing.
- Needed to validate: one exam with two editions in the local database, one of them with notice subjects (mapped) and the other with none — the F-37 seed gives the editions; Claude adds the notice subjects and their mapping through the F-74 and F-75 screens (Claude).
- Suggested path: `/agile:build F-86`.
- Parallel with: F-77 (different tables and screens); not F-89 at build if it also changes the edition page (not verified).

## Goal
A notice that repeats from year to year is typed once: the next edition starts from a copy and the admin only fixes what changed.

## What exists (verified 2026-10-06)
- `ExamEdition` (F-35) in the `Catalog` module (`src/Modules/Catalog/Simulab.Catalog.Domain/Entities/ExamEdition.cs`): exam fixed at creation, organizer, notice year, position, `Draft`/`Published`, soft delete.
- Edition endpoints under `/api/v1/catalog/exams/{examId}/editions` (`ExamEndpoints.cs`, group `/exams` under the `catalog` prefix — the `Created` location is `/api/v1/catalog/exams/{examId}/editions/{id}`), guarded by `catalog.manage`.
- Edition page `ExamEditionForm.razor`; editions list on the exam page `ExamEditionsSection.razor`.
- `NoticeSubject` (F-74, done, verified on `main` 2026-10-11): table `catalog.notice_subjects`, endpoints in `NoticeSubjectEndpoints.cs` (group `/exams/{examId}/editions/{editionId}/notice-subjects` under the `catalog` prefix: list, create, update, `/{id}/move`, delete), section `NoticeSubjectsSection.razor` with the empty message `NoticeSubjects.Empty` and the Add button; uniqueness of label per edition and group (F-74 BR5), position per edition (F-74 BR6).
- Mapping (F-75, done, verified on `main` 2026-10-11): entity `NoticeSubjectMapping : TenantEntity` (soft delete, F-75 change note 1), table `catalog.notice_subject_mappings`; 0 to 50 entries per notice subject, each a whole subject or one topic (F-75 BR1).
- `AppIcons.Copy` (`ContentCopy`) already exists.
- Premise note: F-74's file writes its routes as `/api/v1/exams/...`; the code puts them under `/api/v1/catalog/exams/...`. This item uses the real prefix.

## Users and use cases
- UC1 An admin with `catalog.manage` opens an edition with no notice subjects and copies, in one step, the notice subjects of another edition of the same exam.
- UC2 The admin then fixes what changed in the new notice with the F-74 actions (edit, move, delete, add).

## Business rules
- BR1 The source is another edition of the same exam, not deleted, Draft or Published, with at least one live notice subject. The target itself is never a source.
- BR2 The copy is allowed only when the target edition has no live notice subject; otherwise 409 `notice_subject.copy_target_not_empty` and nothing is written.
- BR3 Every live notice subject of the source is copied, in the source's order: group, label, number of questions and position. Deleted rows of the source are not copied.
- BR4 The copies are new rows of the target edition (new ids, `TenantId` null like F-74 BR1); the source is not changed. Source and copies are independent afterwards.
- BR5 The copy is one save: either every row (and its mapping) is written or none is.
- BR6 The mapping of each source row to the canonical taxonomy (F-75) is copied with it, entry by entry, as it is in the source at the moment of the copy. An overlap the source holds (F-75 BR4, created by a later topic move) is copied as is, not refused.
- BR7 A source edition that does not exist, is deleted, belongs to another exam or is the target → 404 `exam_edition.copy_source_not_found`. A source with no live notice subject → 400 `notice_subject.copy_source_empty`.
- BR8 The copy works on a Draft or a Published target edition, like the other notice subject actions (F-74 BR9).
- BR9 The endpoint requires `catalog.manage`; the action appears only in the section of F-74, which is shown only to who has it (F-74 BR11).

## Screens and API
Screens follow the rules `ui` and `ui-project`. No mockup: one button and one picking dialog built from kit components already designed (owner, question 5).

### Screen 1 — Empty state of the "Notice subjects" section (F-74)
- When the list is empty, next to the Add button, a second outlined button `NoticeSubjects.Copy.Open` (icon `AppIcons.Copy`, Material Outlined `ContentCopy`, existing) opens the dialog of Screen 2.
- The button is shown only when the list is empty (BR2). When the exam has no other edition with notice subjects, the button stays visible, disabled, with the tooltip `NoticeSubjects.Copy.NoSource`.
- On success: snackbar `NoticeSubjects.Copy.Done` with the number of rows copied, the list reloaded, focus on the first row's Edit button.

### Screen 2 — Copy dialog (`CopyNoticeSubjectsDialog.razor`)
- `MudDialog` like `NoticeSubjectDialog.razor`: title `NoticeSubjects.Copy.Title`, text `NoticeSubjects.Copy.Intro`, `AppAlert` at the top for errors, `AppFormActions` at the bottom (primary `NoticeSubjects.Copy.Confirm`, Cancel text).
- One required choice: the source edition, as a radio list (`AppRadioCards` or the kit's single-choice list), one option per source of BR1, newest notice year first; each option shows notice year, position (or nothing), board name and `NoticeSubjects.Copy.SourceCount.One` / `.Many` with its number of live notice subjects.
- The confirm button is disabled until a source is picked; while copying it shows progress and the dialog cannot be closed.
- Errors: `notice_subject.copy_target_not_empty` (another admin added rows meanwhile), `exam_edition.copy_source_not_found`, `notice_subject.copy_source_empty` and server errors go to the dialog's alert; the dialog stays open; on `copy_target_not_empty` the section reloads behind it.
- Cancel or Esc closes at once (nothing typed to lose).

### API
- GET `/api/v1/catalog/exams/{examId}/editions/{editionId}/notice-subjects/copy-sources` — the editions that BR1 allows for this target: `[{ "editionId", "noticeYear", "position", "organizerName", "noticeSubjectCount" }]`, newest notice year first.
- POST `/api/v1/catalog/exams/{examId}/editions/{editionId}/notice-subjects/copy` — body `{ "sourceEditionId": guid }`; 200 with `{ "copiedCount": int }`.
- Error codes: `exam.not_found`, `exam_edition.not_found` (existing, for the target); `exam_edition.copy_source_not_found` (404), `notice_subject.copy_source_empty` (400), `notice_subject.copy_target_not_empty` (409).
- No new table or column: the copies are rows of `catalog.notice_subjects` and `catalog.notice_subject_mappings`.

### UI texts
| Key | pt-BR | pt-PT | en |
|---|---|---|---|
| `NoticeSubjects.Copy.Open` | Copiar de outra edição | Copiar de outra edição | Copy from another edition |
| `NoticeSubjects.Copy.NoSource` | Nenhuma outra edição deste exame tem disciplinas do edital. | Nenhuma outra edição deste exame tem disciplinas do aviso. | No other edition of this exam has notice subjects. |
| `NoticeSubjects.Copy.Title` | Copiar disciplinas de outra edição | Copiar disciplinas de outra edição | Copy notice subjects from another edition |
| `NoticeSubjects.Copy.Intro` | As disciplinas da edição escolhida, com grupos, número de questões, ordem e mapeamento, são copiadas para esta edição. Depois, ajuste o que mudou no edital. | As disciplinas da edição escolhida, com grupos, número de questões, ordem e mapeamento, são copiadas para esta edição. Depois, ajuste o que mudou no aviso. | The subjects of the chosen edition, with their groups, numbers of questions, order and mapping, are copied to this edition. Then adjust what changed in the notice. |
| `NoticeSubjects.Copy.SourceCount.One` | 1 disciplina | 1 disciplina | 1 subject |
| `NoticeSubjects.Copy.SourceCount.Many` | {0} disciplinas | {0} disciplinas | {0} subjects |
| `NoticeSubjects.Copy.Confirm` | Copiar | Copiar | Copy |
| `NoticeSubjects.Copy.Done` | {0} disciplinas copiadas. | {0} disciplinas copiadas. | {0} subjects copied. |
| `NoticeSubjects.Copy.Done.One` | 1 disciplina copiada. | 1 disciplina copiada. | 1 subject copied. |
| `exam_edition.copy_source_not_found` | A edição escolhida não está mais disponível. Escolha outra. | A edição escolhida já não está disponível. Escolha outra. | The chosen edition is no longer available. Pick another one. |
| `notice_subject.copy_source_empty` | A edição escolhida não tem disciplinas do edital para copiar. | A edição escolhida não tem disciplinas do aviso para copiar. | The chosen edition has no notice subjects to copy. |
| `notice_subject.copy_target_not_empty` | Esta edição já tem disciplinas do edital; a cópia só é feita numa lista vazia. A lista foi recarregada. | Esta edição já tem disciplinas do aviso; a cópia só é feita numa lista vazia. A lista foi recarregada. | This edition already has notice subjects; a copy is only made into an empty list. The list was reloaded. |

## Acceptance criteria
- AC1 Given an exam with editions 2023 (3 live notice subjects in two groups, one deleted) and 2025 (none), when the admin copies 2023 into 2025, then 2025 has 3 notice subjects with the same groups, labels, numbers and order, new ids, and 2023 is unchanged (UC1, BR3, BR4).
- AC2 Given the source rows are mapped to canonical subjects and topics, when copied, then each copy has the same mapping entries as its source row (BR6).
- AC3 Given the target already has a live notice subject, when the copy is requested, then 409 `notice_subject.copy_target_not_empty` and nothing is written (BR2).
- AC4 Given the target only has deleted notice subjects, when the copy is requested, then it succeeds (BR2).
- AC5 Given a source of another exam, a deleted source, a source that does not exist or the target itself, when the copy is requested, then 404 `exam_edition.copy_source_not_found` (BR1, BR7).
- AC6 Given a source with no live notice subject, when the copy is requested, then 400 `notice_subject.copy_source_empty` (BR7).
- AC7 Given a failure while writing the copies, when the save fails, then no notice subject and no mapping entry is left in the target (BR5).
- AC8 Given an exam with editions A (with subjects), B (empty, deleted later) and the target, when the copy sources are listed for the target, then only A appears, with its subject count, newest year first (BR1).
- AC9 Given a Published target edition, when the copy is requested, then it succeeds (BR8).
- AC10 Given a user without `catalog.manage`, when either endpoint is called, then 403 (BR9).
- AC11 Given the section is empty and a source exists, when the page shows, then "Copy from another edition" is enabled; given no source exists, then it is disabled with its reason; given the list is not empty, then it is not shown (Screen 1).
- AC12 Given the admin picks a source and confirms, when the copy succeeds, then the snackbar says how many were copied and the list shows the copies (Screen 2).
- AC13 All new texts appear in pt-BR, pt-PT and en, and the missing-key test is green.

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
| AC11 | pending |
| AC12 | pending |
| AC13 | pending |

## Decisions
- 2026-10-06 — Sources are the other editions of the same exam only — the real case is the notice that repeats year to year; the list stays short (owner, question 1).
- 2026-10-06 — The copy is allowed only into an empty list — no conflict or duplicate rule to define; whoever started typing adjusts by hand (owner, question 2).
- 2026-10-06 — The copy carries the F-75 mapping; F-86 is built after F-75 — the mapping is part of the row, copying without it means remapping everything (owner, question 3).
- 2026-10-06 — Entry point: a button in the section's empty state plus a picking dialog — works for editions already created; one click copies (owner, question 4).
- 2026-10-06 — No mockup; the screen is described here — one button and one dialog from kit components already designed in F-74 (owner, question 5).
- 2026-10-06 — Out of scope: the edition's other fields, copying across exams, undoing a copy; no new idea captured (owner, question 6).
- 2026-10-06 — No new packages, production or tests — the kit, EF Core and the existing test hosts cover it (Claude).
- 2026-10-06 — One POST `.../notice-subjects/copy` with the source id, plus a GET of eligible sources — the server checks BR1/BR2 and writes in one save; the dialog never assembles rows itself (Claude).
- 2026-10-06 — A source with an overlapping mapping (F-75 BR4) is copied as is — the copy reproduces the source; F-75 checks overlap only when a row is saved by the dialog (Claude).
- 2026-10-11 — The list of copy sources is not paged — at most a few editions per exam; the F-74 list of notice subjects is not paged either (Claude).
- 2026-10-11 — Approved by the owner ("aprovo f-86"); `## Start` and `## What exists` re-checked against `main` after the F-74 and F-75 merges (owner, Claude).
- 2026-10-06 — Routes use the real prefix `/api/v1/catalog/exams/...`; F-74's file says `/api/v1/exams/...` — a premise to correct in F-74 with `/agile:change F-74` (Claude, reported to the owner).

## Out of scope
- Copying the edition's other fields (year, position, board, notice reference).
- Copying from an edition of another exam.
- Appending to or replacing a list that already has notice subjects.
- Undoing a copy (rows are deleted one by one with the F-74 action).

## Open questions
- (none)

## Change notes

## Validation script
<!-- Written at the end of build. -->

## Delivery
<!-- Filled by /agile:ship. -->
