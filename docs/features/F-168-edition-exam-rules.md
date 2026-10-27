---
feature: F-168
epic: Exam Simulator
status: done
board: 234
version: 1
---
# Exam rules of an edition

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: done (2026-10-27)
- Stopped at: Shipped: merged as fb7d964e, version 1.25.0.
- Next action: Nothing: the item is done.
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

The new columns reach `docs/architecture/Catalog/data-dictionary.md` and `schema.dbml` through DocGen (generated files, checked by `DocGen --check`, not technical pages).

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

Tests locked: 1d4a43df29f443d24509c61e804a21689f16b1cb (7 files); unlocks: 0

| Criterion | Test |
| --- | --- |
| AC1 | `ExamRulesEndpointTests.Save_WithEveryRule_ReturnsAndKeepsThem`; `ExamRulesFormTests.Render_Add_ShowsTheExamRulesCardAfterPaperWithItsFields`, `Save_Add_SendsTheDurationInMinutesAndTheThreeScores`, `Edit_AnEditionWithRules_ShowsTheStoredValuesAndTheAsideSummary` |
| AC2 | `ExamRulesEndpointTests.Update_WithEveryRuleCleared_SavesTheEditionWithNoRule`; `ExamRulesFormTests.Save_Edit_EveryRuleCleared_SendsNoRule` |
| AC3 | `ExamRulesEndpointTests.Save_DurationOutOfRange_IsRefusedWithItsCode`, `Save_DurationAtEitherEnd_IsAccepted`; `ExamRulesFormTests.Save_DurationRefused_ShowsTheMessageOnTheDurationField` |
| AC4 | `ExamRulesEndpointTests.SaveNoticeSubject_PointsOutOfRange_IsRefusedWithItsCode`, `SaveNoticeSubject_PointsAtEitherEnd_AreKeptAndAnUpdateWithoutThemClearsThem`; `NoticeSubjectPointsTests.Dialog_Add_SendsThePointsPerQuestion`, `Dialog_Edit_ShowsTheStoredPointsAndClearingThemSendsNothing`, `Dialog_PointsRefused_ShowsTheMessageOnThePointsField` |
| AC5 | `ObjectiveMaxScoresTests` (4 tests); `ExamRulesEndpointTests.Find_ObjectiveMaxScore_IsKnownOnlyWhenEveryLiveSubjectHasCountAndPoints`; `NoticeSubjectPointsTests.Section_EachRow_ShowsItsPointsPerQuestionOrNothing`, `Section_EveryRowComplete_TheTotalLineShowsTheObjectiveTotal`, `Section_ARowWithoutPoints_TheTotalLineSaysTheTotalIsUnknown`; `ExamRulesFormTests.Edit_EverySubjectComplete_ShowsTheObjectiveTotal`, `Edit_ASubjectWithoutPoints_SaysTheTotalIsUnknownAndNamesTheSubject` |
| AC6 | `ExamRulesEndpointTests.Save_ObjectiveCutOffOutOfRange_IsRefusedWithItsCode`, `Save_ObjectiveCutOffAboveTheKnownTotal_IsSaved`; `ExamRulesFormTests.Edit_CutOffAboveTheKnownTotal_ShowsTheWarningAndStillSaves`, `Edit_CutOffWithinTheTotal_ShowsNoWarning` |
| AC7 | `ExamRulesEndpointTests.Save_EssayBreach_IsRefusedWithItsCode` (7 cases), `Save_EssayCutOffEqualToTheMaximum_IsAccepted`; `ExamRulesFormTests.Save_ScoreRefused_ShowsTheMessageOnItsField`; `SeedManausExamRulesTests.Table_AnEssayCutOffWithoutAMaximum_IsRefused` |
| AC8 | `ExamRulesEndpointTests.Save_PublishedEditionWithNoRule_StaysPublished`; `ExamRulesFormTests.Edit_PublishedWithNoRule_SaysWhichRulesAreMissingAndSavesAsPublished` |
| AC9 | `ExamRulesEndpointTests.CopyNoticeSubjects_CarriesThePointsPerQuestion` |
| AC10 | `ExamRulesEndpointTests.PublishedRead_CarriesTheRulesAndTheObjectiveTotal`; `CatalogExamRulesLineTests` (3 tests) |
| AC11 | `ExamRulesEndpointTests.SaveRoutes_WithoutTheManagePermission_AreForbidden_AndADraftsRulesAreNeverPublished` |
| AC12 | `ExamRulesEndpointTests.Directory_CarriesTheRulesTheObjectiveTotalAndThePoints` |
| AC13 | `SeedManausExamRulesTests` (6 migration tests) |
| AC14 | `ExamRulesResourcesTests.EveryNewText_ExistsInTheThreeLanguages`, `EveryNewText_KeepsEveryPlaceholderInEveryLanguage`; `ResourceParityTests`; `ErrorCodeTextTests` |

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
- 2026-10-27 — Design pass (system-design, architect), verified against the files (contract records, `NoticeSubject.CopyTo`, the Manaus edition id `0198f370-0004-7000-8000-000000000002` and its nine subjects totalling 100 points, `ErrorCodeTextTests`, glossary `ObjectiveMaxScore`, `NoticeSubjectGroups.Totals`). Accepted: every contract field appended with `= null`; `numeric(6,2)`/`numeric(5,2)` with check constraints and the entity enforcing 2 places; one batched read for the total; the seed matched by `normalized_label`, the nine labels listed explicitly; the essay cut-off seeded only when `COALESCE(essay_max_score, 20) >= 10` (architect 3, BR9 kept); each error code's text in the three resx files in the step that adds the code (architect 2); vertical slices instead of layers (architect 1); the helper named after the glossary, `ObjectiveMaxScores` (architect 7); the section total drawn from an extended `NoticeSubjectTotals` (architect 8); the seed's `Down` nulls only values still equal to the seeded ones. Dropped: the "Exam rules" card after Notice (system-design open point 3) — the item says after Paper (architect 5) — Claude (technical)
- 2026-10-27 — An edition with no live notice subject has an unknown objective total (null), never 0, with a test — a total of 0 would make F-171 fail every attempt (system-design open point 1, architect 6) — Claude (technical)
- 2026-10-27 — Essay codes are checked in the order `essay_max_score_invalid`, `essay_cut_off_invalid`, `essay_cut_off_without_max`, `essay_cut_off_above_max`: each value's range before the relation — Claude (technical)
- 2026-10-27: design trade-off 9 Where the Web gets BR4 — owner chose as proposed (a pure helper in `Simulab.Catalog.Contracts`)
- 2026-10-27: design trade-off 10 `ObjectiveMaxScore` in `IExamEditionDirectory` — owner chose as proposed (a second batched query)
- 2026-10-27: Design coverage — contracts (the six request/response records and two directory entries, their callers in QuestionBank and ExamEngine), data (columns, checks, `ix_notice_subjects_edition_order`, the F-78 seed), rules (`ModuleBoundaryTests`, `ErrorCodeTextTests`, `TableDescriptionTests`, `VocabularyTests`, glossary), layout (`build-plan.md` slices), scope, simplicity — all read; not checked: none
- 2026-10-27: traps — T1 store the objective total: stale on every subject edit; T2 refuse a cut-off above the total: order of edits would matter; T3 `double` for points: 99.999… flips the pass at the cut-off
- 2026-10-27: plan — 1 schema migration, edition rules in entity, API, form card "Exam rules" after Paper and aside, codes with their texts → AC1, AC2, AC3, AC7, AC11; 2 points per question in entity, `CopyTo`, API, dialog field and section row → AC4, AC9; 3 objective total (`ObjectiveMaxScores` in Contracts, `ExamEditionResponse`, section total line through `NoticeSubjectTotals`, form missing list and cut-off warning) → AC5, AC6, AC8; 4 directory entries with rules, total and points → AC12; 5 published response and the student's rules line → AC10; 6 seed data migration `SeedManausExamRules` → AC13; 7 flow page, `DocGen`, locale check → AC14, `DocGen --check`, `techdocs.js check`

- 2026-10-27 — The rules are set by `ExamEdition.SetRules`, called by the handler after `Create`/`Update`, instead of four more positional parameters: every existing caller and test keeps compiling — Claude (technical)
- 2026-10-27 — Three tests of earlier items changed with the layout (not locked): `ExamEditionFormTests.Render_Add_ShowsTheThreeSections…` gains "Exam rules" in the card list, and `NoticeSubjectsSectionTests.Dialog_Add_ShowsTheThreeFieldsInOrder…` and `NoticeSubjectCoversDialogTests.Dialog_Add_ShowsTheCoversFieldLast…` gain `notice-subject-points` in the input list — Claude (technical)
- 2026-10-27 — The section's total line adds the points only when some row has them: a list with no points at all reads as before (the F-74 tests hold) — Claude (technical)
- 2026-10-27 — App-host check: started on `simulab_f168`; the migrations and the seed ran, and the published read through the Api answered Manaus with 300 / 100.00 / 60.00 / 20.00 / 10.00. The screens were not opened: the seeded admin lands on "Our documents changed", and accepting terms needs the owner's yes; the screens are in the validation script (steps 2 to 8) — Claude (technical)
- 2026-10-27 — Test time: `Simulab.Catalog.Tests` ran 706 tests in 2 min 16 s, over the 2 min integration budget (retro finding) — Claude (technical)
- 2026-10-27 — Reuse: `NoticeSubjectGroups.Totals` extended for the objective total, `ObjectiveMaxScores.Compute` shared by the Api, the directory, the published read and the Web, `AppTextField`/`AppSectionCard`/`AppAlert`/`AppFormAside` of the kit, the F-78 seed pattern (frozen literals, only empty values) — Claude (technical)
- 2026-10-27 — Shortcuts: none — Claude (technical)

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

### English
Needed to validate: the seeded Manaus 2026 edition and the seeded admin (in place: the worktree's own database `simulab_f168` has them); a signed-in `Student` account for step 6 (owner: register one on that database, as `docs/infra.md` says for B-35).

1. Start the app on the item's own database (`simulab_f168`, nothing shared). Stop any other app host first (F-169's included): one app host at a time.
   - [ ] 1.1 Run, from `D:\wt\simulab\f-168-edition-exam-rules`:

```bash
Database__Name=simulab_f168 dotnet run --project src/Hosts/Simulab.AppHost --launch-profile https
```

```powershell
$env:Database__Name = 'simulab_f168'; dotnet run --project src/Hosts/Simulab.AppHost --launch-profile https
```

   - 1.2 Expected: the line `Dashboard: https://localhost:17162/login?t=...`; the app is at `https://localhost:7125`.
2. Sign in as `admin@simulab.local` and open the Manaus 2026 edition: sidebar > Content > Exams > "Guarda Municipal" (Manaus) > "Editions" section > 2026.
   - 2.1 The card "Exam rules" comes right after "Paper": Duration 5 h 0 min, objective cut-off 60.00, essay maximum 20.00, essay cut-off 10.00 (the seed).
   - 2.2 Under the cut-off: "Objective total: 100.00 points, from the notice subjects." The aside (the summary column on the right) shows Duration "5 h" and Cut-off "60 / 100".
3. Notice subjects of the same page (section "Notice subjects", below the form).
   - [ ] 3.1 Check that each row shows its points: 2.00 pt on Direito Constitucional and Legislação específica, 1.50 pt on the other seven; the total line ends with "100.00 points".
   - [ ] 3.2 Edit "Informática", clear "Points per question", save: the total line says "1 subject has no points per question…", and the form's blue notice "Still missing:" names Informática.
   - [ ] 3.3 Edit "Informática" again, type 1.50, save: the total is back to 100.00 points.
4. Refusals and the warning, in the "Exam rules" card.
   - [ ] 4.1 Hours 0, minutes 10, Save: the duration field says "The duration must be between 15 minutes and 8 hours."; set 5 h 0 min back.
   - [ ] 4.2 Essay cut-off 25, Save: the essay cut-off field says it cannot be above the essay maximum; set 10 back.
   - [ ] 4.3 Objective cut-off 120, Save: the edition saves and a yellow warning says the cut-off is above the objective total (100 points); set 60 back and save.
5. A published edition with no rule.
   - [ ] 5.1 In "Publication" choose Published; clear the five rule fields; Save: the edition saves and stays Published, and "Still missing:" lists Duration, Objective cut-off and Essay maximum.
   - [ ] 5.2 Type the rules back (5 h 0 min, 60, 20, 10) and Save.
6. The student's exam page and one permission check.
   - [ ] 6.1 Sign out; sign in with the `Student` account; sidebar > Study > Catalog > "Guarda Municipal" (Manaus).
   - [ ] 6.2 Check that the 2026 card shows the line "5 h · 100 points, pass at 60 · Essay 20 points, pass at 10"; an edition with no rules shows no line.
   - [ ] 6.3 Open `https://localhost:7125/admin/exams` as the student: access is denied.
7. One language switch, as the admin, on the Manaus edition form.
   - [ ] 7.1 Switch to Português (Portugal): the card reads "Regras da prova", "Nota mínima da prova objetiva (pontos)" and the decimals use a comma (60,00); switch to Português (Brasil): "Nota de corte da objetiva (pontos)".
8. Keyboard only, on the edition form.
   - [ ] 8.1 With Tab only, reach "Duration (hours)", type 4, Tab to "Minutes", type 30, Tab to Save and press Space: the edition saves and the aside shows "4 h 30 min"; set 5 h 0 min back the same way.

### pt-BR
Necessário para validar: a edição Manaus 2026 e o admin do seed (já existem: o banco próprio do worktree, `simulab_f168`, tem os dois); uma conta `Student` logada para o passo 6 (você: cadastre uma nesse banco, como o `docs/infra.md` descreve para o B-35).

1. Suba o app no banco próprio do item (`simulab_f168`, nada compartilhado). Antes, pare qualquer outro app host (o do F-169 também): um app host por vez.
   - [ ] 1.1 Rode, em `D:\wt\simulab\f-168-edition-exam-rules`:

```bash
Database__Name=simulab_f168 dotnet run --project src/Hosts/Simulab.AppHost --launch-profile https
```

```powershell
$env:Database__Name = 'simulab_f168'; dotnet run --project src/Hosts/Simulab.AppHost --launch-profile https
```

   - 1.2 Esperado: a linha `Dashboard: https://localhost:17162/login?t=...`; o app fica em `https://localhost:7125`.
2. Entre como `admin@simulab.local` e abra a edição Manaus 2026: menu lateral > Conteúdo > Provas > "Guarda Municipal" (Manaus) > seção "Edições" > 2026.
   - 2.1 O card "Regras da prova" vem logo depois de "Prova": Duração 5 h 0 min, nota de corte da objetiva 60,00, nota máxima da redação 20,00, nota de corte da redação 10,00 (o seed).
   - 2.2 Abaixo da nota de corte: "Total da prova objetiva: 100,00 pontos, pelas disciplinas do edital." O aside (a coluna de resumo à direita) mostra Duração "5 h" e Nota de corte "60 / 100".
3. Disciplinas do edital da mesma página (seção "Disciplinas do edital", abaixo do formulário).
   - [ ] 3.1 Confira que cada linha mostra seus pontos: 2,00 pt em Direito Constitucional e Legislação específica, 1,50 pt nas outras sete; a linha de total termina com "100,00 pontos".
   - [ ] 3.2 Edite "Informática", apague "Pontos por questão", salve: a linha de total diz "1 disciplina não tem pontos por questão…", e o aviso azul "Ainda falta:" do formulário cita Informática.
   - [ ] 3.3 Edite "Informática" de novo, digite 1,50, salve: o total volta a 100,00 pontos.
4. Recusas e o alerta, no card "Regras da prova".
   - [ ] 4.1 Horas 0, minutos 10, Salvar: o campo de duração diz "A duração deve ficar entre 15 minutos e 8 horas."; volte para 5 h 0 min.
   - [ ] 4.2 Nota de corte da redação 25, Salvar: o campo diz que ela não pode ficar acima da nota máxima; volte para 10.
   - [ ] 4.3 Nota de corte da objetiva 120, Salvar: a edição salva e um alerta amarelo diz que a nota de corte está acima do total da prova objetiva (100 pontos); volte para 60 e salve.
5. Uma edição publicada sem nenhuma regra.
   - [ ] 5.1 Em "Publicação" escolha Publicada; apague os cinco campos de regra; Salvar: a edição salva e continua Publicada, e "Ainda falta:" lista Duração, Nota de corte da objetiva e Nota máxima da redação.
   - [ ] 5.2 Digite as regras de volta (5 h 0 min, 60, 20, 10) e Salvar.
6. A página da prova para o aluno e uma checagem de permissão.
   - [ ] 6.1 Saia; entre com a conta `Student`; menu lateral > Estudar > Catálogo > "Guarda Municipal" (Manaus).
   - [ ] 6.2 Confira que o card de 2026 mostra a linha "5 h · 100 pontos, aprovação com 60 · Redação de 20 pontos, aprovação com 10"; uma edição sem regras não mostra linha.
   - [ ] 6.3 Abra `https://localhost:7125/admin/exams` como aluno: o acesso é negado.
7. Uma troca de idioma, como admin, no formulário da edição Manaus.
   - [ ] 7.1 Troque para Português (Portugal): o card diz "Regras da prova", "Nota mínima da prova objetiva (pontos)" e os decimais usam vírgula (60,00); troque para Português (Brasil): "Nota de corte da objetiva (pontos)".
8. Só teclado, no formulário da edição.
   - [ ] 8.1 Só com Tab, chegue a "Duração (horas)", digite 4, Tab até "Minutos", digite 30, Tab até Salvar e aperte Espaço: a edição salva e o aside mostra "4 h 30 min"; volte para 5 h 0 min do mesmo jeito.

## Events

- 2026-10-27 question — asked 8, from context 1
- 2026-10-27 question — asked 3, from context 0
- 2026-10-27 test-fail — build blocked by a running process (Simulab.slnx), 1 red

## Delivery
Gate time: 6 passes, 22 min 58 s — turn 2 (1 min 38 s), ship 2 (14 min 58 s), scan 2 (6 min 22 s); 1 red
Usage: 14323.5k weighted (estimate) — refine 4% · build 76% · ship 14% · other 6% — agents 7% — context per turn 134k → 546k — 2 sessions, 313 turns
- Branch: `feature/F-168`, merged into `main` as `fb7d964e` (2026-10-27); app version 1.25.0.
- Full suite (`gate.js ship`): 5621 tests, 0 failed, warnings baseline 0 — Catalog 706, QuestionBank 857, Identity 583, Web 2765, ExamEngine 86, Api 83, AppHost 122, Architecture 307, others 112; the slowest projects took 9 min 41 s to 9 min 54 s in parallel, over the 5 min suite budget (retro finding).
- Scan: `agile scan GREEN`; closed by Claude: none.
- Manual: `docs/manual/pt-BR/exam-editions.md` (exam rules, points per question, new fields and messages), `docs/manual/pt-BR/catalog.md` (the rules line). Technical page: `docs/flows/edition-exam-rules.md`. Glossary: 1 row (`numeric(p, s)`).
- Validation: the owner said "validado" on 2026-10-27 after step 3.2 (saving a subject without points is accepted, BR3). The boxes were not ticked through the widget, so the evidence reads "not tested" for every path; two boxes (1.1, 3.1) had been marked in the file by hand and were removed, as the file holds `[ ]` only.
- Validation evidence:

```text
1. Suba o app no banco próprio do item (`simulab_f168`, nada compartilhado). Antes, pare qualquer outro app host (o do F-169 também): um app host por vez.
   [ ] 1.1 not tested Rode, em `D:\wt\simulab\f-168-edition-exam-rules`:
2. Entre como `admin@simulab.local` e abra a edição Manaus 2026: menu lateral > Conteúdo > Provas > "Guarda Municipal" (Manaus) > seção "Edições" > 2026.
3. Disciplinas do edital da mesma página (seção "Disciplinas do edital", abaixo do formulário).
   [ ] 3.1 not tested Confira que cada linha mostra seus pontos: 2,00 pt em Direito Constitucional e Legislação específica, 1,50 pt nas outras sete; a linha de total termina com "100,00 pontos".
   [ ] 3.2 not tested Edite "Informática", apague "Pontos por questão", salve: a linha de total diz "1 disciplina não tem pontos por questão…", e o aviso azul "Ainda falta:" do formulário cita Informática.
   [ ] 3.3 not tested Edite "Informática" de novo, digite 1,50, salve: o total volta a 100,00 pontos.
4. Recusas e o alerta, no card "Regras da prova".
   [ ] 4.1 not tested Horas 0, minutos 10, Salvar: o campo de duração diz "A duração deve ficar entre 15 minutos e 8 horas."; volte para 5 h 0 min.
   [ ] 4.2 not tested Nota de corte da redação 25, Salvar: o campo diz que ela não pode ficar acima da nota máxima; volte para 10.
   [ ] 4.3 not tested Nota de corte da objetiva 120, Salvar: a edição salva e um alerta amarelo diz que a nota de corte está acima do total da prova objetiva (100 pontos); volte para 60 e salve.
5. Uma edição publicada sem nenhuma regra.
   [ ] 5.1 not tested Em "Publicação" escolha Publicada; apague os cinco campos de regra; Salvar: a edição salva e continua Publicada, e "Ainda falta:" lista Duração, Nota de corte da objetiva e Nota máxima da redação.
   [ ] 5.2 not tested Digite as regras de volta (5 h 0 min, 60, 20, 10) e Salvar.
6. A página da prova para o aluno e uma checagem de permissão.
   [ ] 6.1 not tested Saia; entre com a conta `Student`; menu lateral > Estudar > Catálogo > "Guarda Municipal" (Manaus).
   [ ] 6.2 not tested Confira que o card de 2026 mostra a linha "5 h · 100 pontos, aprovação com 60 · Redação de 20 pontos, aprovação com 10"; uma edição sem regras não mostra linha.
   [ ] 6.3 not tested Abra `https://localhost:7125/admin/exams` como aluno: o acesso é negado.
7. Uma troca de idioma, como admin, no formulário da edição Manaus.
   [ ] 7.1 not tested Troque para Português (Portugal): o card diz "Regras da prova", "Nota mínima da prova objetiva (pontos)" e os decimais usam vírgula (60,00); troque para Português (Brasil): "Nota de corte da objetiva (pontos)".
8. Só teclado, no formulário da edição.
   [ ] 8.1 not tested Só com Tab, chegue a "Duração (horas)", digite 4, Tab até "Minutos", digite 30, Tab até Salvar e aperte Espaço: a edição salva e o aside mostra "4 h 30 min"; volte para 5 h 0 min do mesmo jeito.
Evidence: 0 of 14 tested
```
