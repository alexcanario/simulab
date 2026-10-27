---
feature: F-168
epic: Exam Simulator
status: building
board: 234
version: 1
---
# Exam rules of an edition

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: building (2026-10-27)
- Stopped at: Build started: status building.
- Next action: Design passes (system-design, architect), then the plan.
- Waiting on: nothing

## Summary
An admin records, on an exam edition, the rules the notice sets for the paper: its duration, the points per question of each notice subject, the cut-off of the objective paper and of the essay. The Manaus 2026 edition gets its rules seeded (5 hours; 1.50 or 2.00 points per question; pass at 60 of 100 points; essay of 20 points, pass at 10). Today `ExamEdition` and `NoticeSubject` carry none of this (epic E-06, "What exists").

## Start
- Depends on: nothing.
- Waits on (to start): nothing.
- Needed to validate: the seeded Manaus 2026 edition (exists, F-37 and F-78); the seeded admin (exists); a signed-in `Student` account to read the exam page — owner.
- Model: opus (a public API contract: new fields in the Catalog edition and notice subject requests and responses, and in `IExamEditionDirectory`, which other modules read).
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: B-36. F-169 is building and may also change `CatalogExam.razor`, `PublishedExamEditionResponse` and the Catalog resource files: expect small merge conflicts, or build F-168 after F-169 merges.

## Goal
Give the timer (F-170) and the score (F-171) the rules they read, recorded once per edition as the notice states them, so a simulated paper lasts and scores like the real one.

## What already exists
Checked in the code on 2026-10-27, on `main` at `bacc7d5`.
- `ExamEdition` (`src/Modules/Catalog/Simulab.Catalog.Domain/Entities/ExamEdition.cs`): exam, organizer, notice year, position, notice reference and address, applied date, status, notice PDF (F-35, F-89). No duration, score or cut-off.
- `NoticeSubject` (`.../Entities/NoticeSubject.cs`): edition, group, label, question count, display order (F-74). No points. `CopyTo` copies a row to another edition (F-86).
- Admin screens: `ExamEditionForm.razor` (`/admin/exams/{examId}/editions/{id}`) with one section card "Paper" and an aside summary; below it, outside the form, `NoticeSubjectsSection.razor` (list with a total line) and `NoticeSubjectDialog.razor` (create and edit a subject).
- API: `POST/PUT /api/v1/catalog/exams/{examId}/editions[/{id}]` and `POST/PUT /api/v1/catalog/exams/{examId}/editions/{editionId}/notice-subjects[/{id}]` under `catalog.manage`; `PublishedExamEndpoints` under `catalog.browse` feed the student's exam page `CatalogExam.razor` (`/catalog/exams/{id}`, F-36) through `PublishedExamEditionResponse`.
- `IExamEditionDirectory` (Catalog contracts) is what other modules read about editions and notice subjects; `ExamEditionDirectoryEntry` and `NoticeSubjectDirectoryEntry` carry no rule.
- Seed: the Manaus 2026 edition and its 9 notice subjects (60 questions) by `SeedMunicipalGuardCatalog` and `SeedGuardNoticeSubjects`.
- Glossary had `CutOffScore` and `ScoringRule`; the terms of this item were added at this refinement.
- The Manaus 2026 notice (owner's folder, `edital-gcm-manaus-2026.md`): Quadro I gives 1.50 points per question to every subject but Direito Constitucional (5 questions) and Legislação específica (15), which get 2.00, total 100 (item 5.1.1); pass at 60 points (5.1.4); essay from 0 to 20 points (5.2.7), pass at 10 (5.2.9); duration 5h00 (6.1.2). No minimum per subject.

## Users and use cases
- UC1 An admin (`catalog.manage`) records on an edition its duration, objective cut-off, essay maximum and essay cut-off, in the edition form, and saves them with the edition.
- UC2 An admin records the points per question of a notice subject in the notice subject dialog, and sees the objective total of the edition in the notice subjects section.
- UC3 An admin sees, in the edition form, which rules are still missing; the edition can be published anyway.
- UC4 A student (`catalog.browse`) sees the rules of a published edition in one line of its card on the exam page.
- UC5 Another module (the timer of F-170, the score of F-171) reads the rules of an edition and the points of its notice subjects through `IExamEditionDirectory`.

## Business rules
- BR1 Every rule is optional. An edition with no rule, or some, is valid in every status; publishing is never blocked by a missing rule (UC3).
- BR2 Duration: whole minutes from 15 to 480 (8 h), entered as hours and minutes. Empty means no time limit.
- BR3 Points per question: on a notice subject, a decimal with at most 2 places, from 0.01 to 100.00.
- BR4 Objective total = the sum of question count × points per question over the live notice subjects of the edition. It is known only when every live notice subject has both a question count and points; otherwise it is unknown and the screens say which subjects lack them. It is computed, never stored.
- BR5 Objective cut-off: points, a decimal with at most 2 places, from 0.01 to 1000.00. When the objective total is known and the cut-off is above it, the edition form shows a warning; saving is not refused (the points live on the notice subjects, saved apart).
- BR6 Essay maximum: points, a decimal with at most 2 places, from 0.01 to 1000.00. Essay cut-off: the same range, only with a maximum, and not above it; each breach is refused with its code.
- BR7 Copying notice subjects to another edition (F-86) copies their points per question with them.
- BR8 The rules are global data like the edition (`TenantId` null in v1), read and changed only through `catalog.manage` on the admin routes; the published read under `catalog.browse` shows them for published editions only.
- BR9 The seed sets the Manaus 2026 rules of "What already exists" in every environment, each value only where it is still empty: a value an admin already typed is kept.

## Screens and API
- `/admin/exams/{examId}/editions/{id}` — edition form: new section card "Exam rules" after "Paper"; the aside summary gains the rules rows.
- Notice subjects section (same page) — the list shows the points per question of each row; the total line adds the objective total or what is missing; the notice subject dialog gains "Points per question".
- `/catalog/exams/{id}` — the card of each published edition gains one rules line, only with the rules it has; no line when it has none.
- `POST/PUT /api/v1/catalog/exams/{examId}/editions[/{id}]` — `SaveExamEditionRequest` gains `DurationMinutes`, `ObjectiveCutOffScore`, `EssayMaxScore`, `EssayCutOffScore` (all optional); `ExamEditionResponse` returns them plus `ObjectiveMaxScore` (null when unknown).
- `POST/PUT /api/v1/catalog/exams/{examId}/editions/{editionId}/notice-subjects[/{id}]` — `SaveNoticeSubjectRequest` gains `PointsPerQuestion` (optional); `NoticeSubjectResponse` returns it.
- `GET /api/v1/catalog/published-exams/{id}` (`catalog.browse`) — `PublishedExamEditionResponse` gains `DurationMinutes`, `ObjectiveMaxScore`, `ObjectiveCutOffScore`, `EssayMaxScore`, `EssayCutOffScore`.
- `IExamEditionDirectory` — `ExamEditionDirectoryEntry` gains the four stored rules and `ObjectiveMaxScore`; `NoticeSubjectDirectoryEntry` gains `PointsPerQuestion`. Additive: existing callers keep compiling.
- Error codes: `exam_edition.duration_invalid`, `exam_edition.objective_cut_off_invalid`, `exam_edition.essay_max_score_invalid`, `exam_edition.essay_cut_off_invalid`, `exam_edition.essay_cut_off_without_max`, `exam_edition.essay_cut_off_above_max`, `notice_subject.points_per_question_invalid`.
- Data: columns `duration_minutes integer`, `objective_cut_off_score numeric(6,2)`, `essay_max_score numeric(6,2)`, `essay_cut_off_score numeric(6,2)` on `exam_editions`; `points_per_question numeric(5,2)` on `notice_subjects`; all nullable, with check constraints for the ranges. One schema migration plus one data migration for the Manaus seed.

### Wireframe: Edition form
```text
+--------------------------------------------------------+
| Edition: Guarda Municipal de Manaus 2026               |
| [ Paper ]  organizer, year, position, notice, status   |
|                                                        |
| [ Exam rules ]                                   [new] |
|  Duration   [ 5 ] h [ 00 ] min                         |
|  Objective cut-off  [ 60,00 ] points                   |
|  Objective total: 100,00 points (from the subjects)    |
|  Essay maximum      [ 20,00 ] points                   |
|  Essay cut-off      [ 10,00 ] points                   |
|  (i) Missing: points in 2 subjects               [new] |
|  /!\ Cut-off above the objective total           [new] |
|                                      [Cancel] [Save]   |
+--------------------------------------------------------+
| Aside: ... Duration 5 h · Cut-off 60 / 100       [chg] |
+--------------------------------------------------------+
```

### Wireframe: Notice subjects section and dialog
```text
+--------------------------------------------------------+
| Notice subjects                         [Copy] [+ Add] |
|  Língua Portuguesa        10 questions  1,50 pt  [chg] |
|  Direito Constitucional    5 questions  2,00 pt  [chg] |
|  Legislação específica    15 questions  2,00 pt  [chg] |
|  ...                                                   |
|  60 questions · 100,00 points                    [chg] |
+--------------------------------------------------------+
| Dialog: Group, Label, Questions,                       |
|         Points per question [ 1,50 ]             [new] |
+--------------------------------------------------------+
```

### Wireframe: Exam page (student)
```text
+--------------------------------------------------------+
| Guarda Municipal — Manaus 2026 · Consulplan            |
|  Notice nº 01/2026                                     |
|  5 h · 100 points, pass at 60 · essay 20, pass at 10   |
|                                                  [new] |
|  Syllabus ...                                          |
+--------------------------------------------------------+
```

## Docs
- flow: new docs/flows/edition-exam-rules.md — how an admin records the rules and where the student and the other modules read them.
- architecture: update docs/architecture/Catalog/data-dictionary.md and schema.dbml — the new columns (generated, `DocGen --check`).

## Acceptance criteria
- AC1 Given an edition, when the admin saves it with duration 5 h 00 min, objective cut-off 60, essay maximum 20 and essay cut-off 10, then reopening the form shows those values and the API returns `DurationMinutes` 300 and the three scores (UC1).
- AC2 Given an edition with rules, when the admin clears every rule field and saves, then the edition saves with no rule (BR1).
- AC3 Given a duration under 15 minutes or over 8 hours, when the admin saves, then the save is refused with `exam_edition.duration_invalid` on the duration field (BR2).
- AC4 Given points per question of 0, over 100 or with 3 decimal places, when the admin saves the notice subject, then it is refused with `notice_subject.points_per_question_invalid` (BR3).
- AC5 Given every notice subject of the edition with question count and points, then the section's total line and the form show the objective total (Manaus: 100,00); given one subject without points or count, then the total is unknown and the form lists the subjects that lack them (BR4, UC2, UC3).
- AC6 Given an objective cut-off of 0 or over 1000, then the save is refused with `exam_edition.objective_cut_off_invalid`; given a cut-off above a known objective total, then the edition saves and the form shows the warning (BR5).
- AC7 Given an essay cut-off without an essay maximum, or above it, or either value out of range, then the save is refused with `exam_edition.essay_cut_off_without_max`, `exam_edition.essay_cut_off_above_max`, `exam_edition.essay_max_score_invalid` or `exam_edition.essay_cut_off_invalid` (BR6).
- AC8 Given a published edition with no rule, when the admin saves it, then it stays published, and the form says which rules are missing (BR1, UC3).
- AC9 Given notice subjects with points, when the admin copies them to another edition, then the copies carry the same points per question (BR7).
- AC10 Given a published edition with rules, when a student opens the exam page, then its card shows one line with only the rules it has (duration, objective total and cut-off, essay maximum and cut-off); given an edition with none, then no line is shown (UC4).
- AC11 Given an account without `catalog.manage`, when it calls the edition or notice subject save routes with rules, then it gets 403 as today; the published read never returns a draft edition's rules (BR8).
- AC12 Given an edition with rules and points, when another module calls `IExamEditionDirectory.FindAsync` and `ListNoticeSubjectsAsync`, then the entries carry the rules, `ObjectiveMaxScore` and `PointsPerQuestion` (UC5).
- AC13 Given a database with the seeded Manaus 2026 edition, when the migrations run, then it has duration 300, objective cut-off 60, essay maximum 20, essay cut-off 10, 2.00 points on Direito Constitucional and Legislação específica, 1.50 on the other seven, and objective total 100; given an admin value already typed, then the seed keeps it (BR9).
- AC14 All new texts appear in every locale of `UI locales:` (pt-BR, pt-PT, en), and the missing-key test is green.

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
| AC14 | pending |

## Decisions
- 2026-10-27 — Scoring rules live on the edition (Catalog) and the score is computed in ExamEngine; the question keeps only its key — the epic's decision holds for this item — confirmed: docs/epics/exam-simulator.md
- 2026-10-27 — Duration optional, entered as hours and minutes, stored as whole minutes from 15 to 480; empty is no time limit — editions already registered stay valid; F-170 does not time an edition without it — asked
- 2026-10-27 — Points per question optional on the notice subject, 2 decimal places, 0.01 to 100; the objective total is shown in the section's total line — subjects already registered stay valid — asked
- 2026-10-27 — Cut-offs in points, as the notice writes them; essay maximum and cut-off both on the edition — the Manaus essay question (F-163) does not exist yet, and a notice states the essay scale on its own — asked
- 2026-10-27 — No minimum per notice subject here; captured as F-180 (board #249) — Manaus 2026 has none (notice item 5.1.4) — asked
- 2026-10-27 — Publishing is never blocked by missing rules; the form says what is missing — published editions without rules stay valid — asked
- 2026-10-27 — The student sees the rules in one line of the published edition's card on the exam page — the student knows what the paper is before starting it (F-169) — asked
- 2026-10-27 — The Manaus 2026 values come from the notice file the owner gave (`edital-gcm-manaus-2026.md`, Quadro I, items 5.1.4, 5.2.7, 5.2.9, 6.1.2) — the owner pointed to the file instead of a download — asked
- 2026-10-27 — The objective total is computed (count × points over the live subjects), never stored, and unknown while any subject lacks count or points — a stored total would go stale when a subject changes — Claude (technical)
- 2026-10-27 — A cut-off above the objective total is a warning, not a refusal — the total depends on rows saved apart from the edition; refusing would make the order of edits matter — Claude (technical)
- 2026-10-27 — `IExamEditionDirectory` carries the rules and the points now, additively — F-170 and F-171 read them there; a module never reads another module's tables — Claude (technical)
- 2026-10-27 — Copying notice subjects copies their points — the points are part of the subject as the notice states it — Claude (technical)
- 2026-10-27 — The seed fills only empty values, in a new data migration — the seed migrations of F-37 and F-78 stay untouched, and an admin's value wins, as F-161 does — Claude (technical)
- 2026-10-27 — No screen mockup — fields added to an existing form, a dialog and one line; not complex by the `screen-design` line — Claude (technical)
- 2026-10-27 — No new package — Claude (technical)
- 2026-10-27 — Kept whole (size M): one use case family, one module, one schema change — item-size signals absent — Claude (technical)
- 2026-10-27 — No similar item (about 180 items read); F-170, F-171, F-174 and F-175 consume or extend these rules — Claude (technical)

## Out of scope
- Minimum score per notice subject: F-180.
- Penalties, partial credit and annulment scoring models: F-174.
- Computing a score or a pass: F-171 (objective), F-175 (essay).
- Rules of the other three seeded guard editions (Curitiba, Salvador, Maceió), which F-161 removes.
- The essay question of Manaus 2026 and its rubric: F-163.
- Showing points per subject in the student's syllabus.

## Open questions
- (none)

## Change notes

## Validation script

## Events

- 2026-10-27 question — asked 8, from context 1
- 2026-10-27 question — asked 3, from context 0

## Delivery
