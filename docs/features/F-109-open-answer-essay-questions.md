---
feature: F-109
epic: Question bank
status: building
board: 155
version: 1
---
# Open-answer and essay questions

Technical terms: [glossary](../glossary.md)

## Summary
The open-answer and essay question types with their rubric (criteria and maximum points). They are useful to students only once someone grades them. Source: epic Question bank (E-4), brief capability 3 and its open note "Essays and open answers"; Simulae `ConfigurationBase.cs` (T7, T8) as the import source.
Added 2026-10-24 (D-5): this item delivers only the types and the rubric. Grading is a person, by a new `Grader` role (F-150); the correction queue is F-151 and the AI-suggested grade is F-152. The two types share one rubric of criteria (name, description, maximum points); the essay has a size limit in lines, the open answer in characters. The rubric is part of `QuestionVersion`. A curator may publish these types before grading exists; the simulators decide whether to offer them.

## Start
- Depends on: F-95 (done), F-98 (done: versions and the key-change check), F-108 (done: the sixth type, whose steps this item repeats for two more).
- Waits on (to start): nothing — the grading decision is settled in D-5 (2026-10-24).
- Needed to validate: the local app host — owner.
- Model: sonnet (no signal: the migration only adds a table and columns, no door, no new module, size M)
- Suggested path: `/agile:build` (no mockup, owner 2026-10-24; the screens are described below with wireframes).
- Parallel with: none known. F-150 (idea) works in Identity, not in the question form.

## Goal
A curator can author, publish, list, preview and version an open-answer or an essay question with a rubric, so that the question bank holds the content a grader will need once the correction items (F-150 to F-152) and a student attempt exist.

## What exists (verified in the code, 2026-10-24)
- `QuestionType` has six values, the last `ShortAnswer` (`QuestionType.cs`); `questions.type` is text, so two more values need no data conversion; only the column comments list the values. The glossary already lists `OpenAnswer` and `Essay` in the `QuestionType` row; no code mentions `Rubric`, `OpenAnswer` or `Essay` yet.
- A type declares its key in one record behind `AnswerKey.From` (`AnswerKey.cs:56`), e.g. `ShortAnswerKey`, which refuses what its type does not have and checks the draft and the published rule (`Check(bool published)`). The entity never branches on the type.
- Child rows of a question are tables owned by the `Question`: options, match items and `QuestionBlank` (number, position, accepted answers). A version snapshots them: options and match items as JSON columns of `question_versions`, blanks as `question_versions.blanks` (`QuestionVersion.cs`, `QuestionVersionConfiguration.cs`).
- `Question.NextVersion` records a version when versioned content differs from the latest and requires a reason when the key changed (`Question.cs:120-195`); `SameVersionedContent` compares the content, `BlanksKeyChanged` the blanks. `CopyAs` copies a version for annul and reinstate. The base-text fan-out goes through `NextVersion`.
- The sanitizer reads `[[n]]` markers only for fill-in-the-blanks (`SaveQuestionHandler.cs`), so in these statements `[[1]]` stays plain text.
- Web: `QuestionForm.razor` branches by type (`TypeCards`, the key sections), `QuestionPreviewContent.razor` is the preview, `QuestionText.cs` the type texts, `AppIcons.cs` the icons; `QuestionTranscriptionTests.Open_OffersEveryTypeTheFormImplements` counts the cards (six today).
- Items that touched this code since F-108: none (F-108 was the last).
- Simulae (`ConfigurationBase.cs`): T7 "Subjetiva" graded by a rubric and T8 essay; nothing is imported but the idea (the code is renamed and rewritten on this module's pattern).

## Users and use cases
- UC1 A curator creates an open-answer or an essay question as `Draft`: type chosen at creation, topic, content language, statement (for an essay, the statement is the theme), optionally base text, the rubric, the size limit, the model answer, difficulty, origin edition.
- UC2 A curator edits it, `Draft` or `Published`, in place; its type does not change.
- UC3 A curator publishes a complete `Draft` (at least one rubric criterion).
- UC4 A curator unpublishes it and deletes a `Draft`, as for any question.
- UC5 A curator tells the eight types apart in the list.
- UC6 A curator previews it as a student will see it (a writing box with its limit), then reveals the rubric and the model answer.
- UC7 A curator changes the rubric of a published question and gives the reason, as F-98 asks for any key change.

## Business rules
- BR1 `QuestionType` gets `OpenAnswer` and `Essay`. Each is the same `Question` row with its `Type`, no options, no `true_false_key`, no match items and no blanks. Its key is the rubric: an ordered list of criteria stored as rows of a new child table.
- BR2 The type is chosen at creation and never changes (F-97 BR2, confirmed): `SaveQuestionRequest.Type` accepts the two values on create; on update it must be omitted or equal the stored type, else 409 `question.type_changed`.
- BR3 A criterion has a name, a description and maximum points. The name is trimmed, 1 to 100 characters, and unique in the rubric once case and extra white space are ignored. The description is plain text, trimmed, up to 500 characters, optional. The maximum points are a whole number from 1 to 100. A rubric has at most 10 criteria. The order the curator writes is kept. The total of the points is not fixed (it does not have to be 100).
- BR4 The rubric points are the split of the question's own grade: the grade of a question is the points given divided by the total of the maximums. How much the question is worth in an exam stays the edition's scoring rule (epic E-6, confirmed): this item stores no points or weight for the question itself.
- BR5 A `Draft` follows F-95 BR3 (statement, topic, content language) and may have no criterion yet. Broken data is refused even on a draft: more than 10 criteria (400 `question.rubric_criteria_count`), an empty name (400 `question.criterion_name_required`), a name over 100 characters (400 `question.criterion_name_too_long`), a description over 500 (400 `question.criterion_description_too_long`), points missing or outside 1 to 100 (400 `question.criterion_points_out_of_range`), two names equal under BR3 (400 `question.criterion_name_repeated`). Each error names the position of the criterion, from 1.
- BR6 `Published` needs, besides BR5, at least one criterion (400 `question.rubric_criteria_required`). Editing a published question runs the same check and refuses a change that breaks it.
- BR7 Size limit, optional in both types. Essay: a minimum and a maximum number of lines, each a whole number from 1 to 100, the minimum not above the maximum. Open answer: a maximum number of characters, a whole number from 1 to 5,000, and no minimum. Any other value, a minimum on an open answer, or a minimum above the maximum is 400 `question.answer_size_invalid`. The limit is data for the simulator; this item does not enforce it on a student.
- BR8 A rubric sent to any other type, or a size limit sent to any other type, is refused 400 (`question.rubric_not_allowed`, `question.answer_size_not_allowed`). Options, a true/false key, match items or blanks sent with these two types are refused with the existing codes (`question.options_not_allowed`, `question.key_not_allowed`, `question.match_items_not_allowed`, `question.blanks_not_allowed`). Nothing is ignored. In these statements `[[n]]` is plain text.
- BR9 The model answer is the existing explanation (`explanationHtml`, up to 10,000 characters), with the shared sanitizing: the grader sees it always, a student sees it only after answering, as for every type (owner, 2026-10-24). No new field.
- BR10 The F-98 key-change check covers the rubric: the key is the set of (name, maximum points) pairs under BR3's comparison; adding, removing or renaming a criterion, or changing its points, is a key change that needs a reason. Changing only a description, the order of the criteria, the statement, the explanation or the size limit is not. The rubric and the size limit are part of what a version snapshots, so any change to them records a version. Version 1 is never a key change; the other six types behave as before and their tests pass unedited.
- BR11 Everything the types share is one rule, not eight: sanitizing and limits, content language, status, publish and unpublish, delete, the topic guard, origin and paper number, annulment, versions, review, base text, permission `questions.manage`, 404 answers (F-95 BR1 to BR16, F-96, F-97 BR5, F-98, F-101, F-102, F-103, F-105, F-108 BR10).
- BR12 The type is declared in one place per layer, as F-97 BR9 sets: the domain (one key record per type and one line each in `AnswerKey.From`), the contract (`QuestionType`), the form (the rubric section) and the preview. No `if` chain in `Question.Apply`.
- BR13 The list shows the type in the existing "Type" chip (F-97 BR8); filter by type stays in F-100.
- BR14 No grade is stored or computed here: the grader's score, the student's answer and who may grade belong to F-150 to F-152 and the simulators. Shuffling is the simulator's (E-5).

## Screens and API
No mockup (owner, 2026-10-24): the form gains two type cards and one rubric section of kit rows, and the preview gains a writing box; wireframes below.

- API (routes unchanged, `/api/v1/question-bank/questions`):
  - `SaveQuestionRequest.type` accepts `OpenAnswer` and `Essay` on create. New optional fields: `rubric` (list of `{ name, description, maxPoints }`, in order; null counts as none, and on update the list sent replaces the stored one), `answerSizeMin` and `answerSizeMax` (whole numbers, null when not set).
  - `QuestionResponse` and `QuestionVersionResponse` carry `rubric` (the criteria in order, empty for every other type and for a version recorded before this item) and `answerSizeMin` and `answerSizeMax` (null when not set). The list response is unchanged (the type is already named); the excerpt is the plain statement.
  - Error codes new: `question.rubric_criteria_count`, `question.rubric_criteria_required`, `question.criterion_name_required`, `question.criterion_name_too_long`, `question.criterion_description_too_long`, `question.criterion_points_out_of_range`, `question.criterion_name_repeated`, `question.rubric_not_allowed`, `question.answer_size_invalid`, `question.answer_size_not_allowed`. Reused: `question.options_not_allowed`, `question.key_not_allowed`, `question.match_items_not_allowed`, `question.blanks_not_allowed`, `question.type_changed`, `question.key_change_reason_required`, `question.image_*`.
  - Data: one migration in the `QuestionBank` context: a new table `question_rubric_criteria` (question, position, name, description, maximum points; with the table and column descriptions every table has), the nullable columns `questions.answer_size_min` and `answer_size_max`, the columns `question_versions.rubric` (JSON, null for old versions) and `answer_size_min` and `answer_size_max`, and the comments of the two `type` columns listing eight values. Existing rows keep their type, options, items and blanks; nothing is backfilled.
  - Limits (`QuestionLimits`): `RubricCriteriaMax` = 10, `CriterionNameMaxLength` = 100, `CriterionDescriptionMaxLength` = 500, `CriterionPointsMin` = 1, `CriterionPointsMax` = 100, `EssayLinesMax` = 100, `OpenAnswerCharactersMax` = 5,000.
- `/admin/questions/new`: the "Question type" `AppRadioCards` gets a seventh and an eighth card, "Open answer" (description "A written answer graded by a person against a rubric.") and "Essay" ("A long written text graded by a person against a rubric."), after "Short answer". Single choice stays preselected.
- `/admin/questions/{id}`: the type stays a read-only labelled value, now able to read "Open answer" or "Essay".
- Key section for these types (replaces the "Options" card, same position and kit card): title "Rubric", subtitle "Write the criteria a grader scores. The question's grade is the points given divided by the total."; up to 10 criterion rows (name field, description field, points field, remove; accessible names "Criterion {k} name", "Criterion {k} description", "Criterion {k} points"), "Add criterion" with the count "{0} of 10 criteria", and a line "Total: {n} points". A new question starts with one empty row. Below the rubric, the size limit: for an essay "Minimum lines" and "Maximum lines" (both optional), for an open answer "Maximum characters" (optional).
  - Errors: the codes of BR5 and BR7 show the texts of this section under the field of the criterion (or under the section or the field), as the other key sections do. A frozen question (annulled or in review) shows everything disabled.
  - Going to another type and back keeps the typed rows (as F-104 AC12).
- The explanation field of these two types is labelled "Model answer" with the hint "The grader always sees it; a student sees it after answering."
- Publish checklist (F-95): for these types the item is "At least one rubric criterion"; the aside shows the type.
- Preview dialog: the base text, the statement, then an inert multi-line writing box labelled "Your answer" (open answer) or "Your essay" (essay), the task line ("Write your answer." / "Write your essay.") and the limit line ("Up to {n} characters.", "Between {a} and {b} lines.", "At least {a} lines.", "Up to {b} lines."; none when no limit is set). After "show answer": the rubric as a table (criterion, description, maximum points, total) with the check icon and the text "Grading rubric", then the model answer; a draft with no criterion shows "no rubric yet".
- `/admin/questions`: the "Type" chip says "Open answer" or "Essay".
- The transcription screen of F-99 offers the two new type cards; picking one shows the same rubric section.
- Permissions: unchanged, `questions.manage`.

### Wireframe: Question form, rubric section (essay)
```text
+------------------------------------------------------------+
| Classification                                             |
|  Question type: ( ) Single choice  ( ) True/false          |
|   ( ) Multiple answer  ( ) Matching  ( ) Fill in the blanks|
|   ( ) Short answer  ( ) Open answer  (x) Essay      [new]  |
+------------------------------------------------------------+
| Statement *  (for an essay, the theme)                     |
| [B][I][U][S][x2][x2][link][image]                          |
| +--------------------------------------------------------+ |
| | Write an essay about ...                               | |
| +--------------------------------------------------------+ |
+------------------------------------------------------------+
| Rubric                                            [changed]|
| Write the criteria a grader scores. The question's grade   |
| is the points given divided by the total.                  |
|  1 [ Theme ........ ] [ Meets the prompt ...... ] [ 30 ] x |
|  2 [ Argument ..... ] [ Defends a thesis ...... ] [ 40 ] x |
|  3 [ Language ..... ] [ Grammar and spelling .. ] [ 30 ] x |
|  [+ Add criterion]  3 of 10 criteria   Total: 100 points   |
|  ! Two criteria have the same name. (error)                |
+------------------------------------------------------------+
| Answer size (optional)                                     |
|  Minimum lines [ 7 ]   Maximum lines [ 30 ]          [new] |
+------------------------------------------------------------+
| Model answer                                               |
| The grader always sees it; a student sees it after         |
| answering.                                                 |
+------------------------------------------------------------+
| Before publishing: [v] At least one rubric criterion       |
+------------------------------------------------------------+
```

### Wireframe: Preview dialog (essay)
```text
+--------------------------------------------+
| Preview                                    |
| <base text>                                |
| Write an essay about ...                   |
| Your essay                                 |
| [ ____________________________________ ]   |
| [ ____________________________________ ]   |
| Write your essay.                   [new]  |
| Between 7 and 30 lines.             [new]  |
| [ Show answer ]                            |
|  -- after reveal --                        |
| (v) Grading rubric                         |
|  Theme      Meets the prompt        30     |
|  Argument   Defends a thesis        40     |
|  Language   Grammar and spelling    30     |
|  Total                             100     |
| Model answer: <text>                       |
+--------------------------------------------+
```

## Docs
- flow: new docs/flows/open-answer-and-essay-questions.md — how a curator authors, publishes, versions and previews the two types, what a rubric is, why its points only split the question's grade, and what is left to F-150 to F-152.

## Acceptance criteria
- AC1 Given a curator with `questions.manage`, when they create a question with `type` `Essay` or `OpenAnswer`, a topic, a language and a statement and no rubric, then it is stored as `Draft` of that type, appears in the list with its type, and the response carries an empty `rubric` and no size limit. (UC1, BR1, BR5)
- AC2 Given an open-answer draft, when it is saved with three criteria, then they are stored trimmed in the order written with their points, and the response returns them in that order. (UC1, BR3)
- AC3 Given a rubric request, when it has 11 criteria, then 400 `question.rubric_criteria_count`; with an empty name, 400 `question.criterion_name_required`; with a name of 101 characters, 400 `question.criterion_name_too_long` (100 is accepted); with a description of 501 characters, 400 `question.criterion_description_too_long` (500 is accepted); with points missing, 0 or 101, 400 `question.criterion_points_out_of_range` (1 and 100 are accepted); with "Theme" and " theme  ", 400 `question.criterion_name_repeated`; the position of each problem is that of its criterion and nothing is stored. (BR3, BR5)
- AC4 Given a draft, when it is published with no criterion, then 400 `question.rubric_criteria_required`; with at least one, it becomes `Published`. (UC3, BR6)
- AC5 Given a published question, when an edit removes every criterion, then it is refused and the stored question is unchanged; when the curator writes another valid rubric, it stays `Published`. (UC2, BR6)
- AC6 Given an essay, when it is saved with a minimum of 7 and a maximum of 30 lines, then both are stored; with a minimum above the maximum, a value of 0 or over 100, 400 `question.answer_size_invalid`; with only a maximum or none, it is accepted. Given an open answer with a maximum of 500 characters, it is stored; with a minimum, 0 or over 5,000, 400 `question.answer_size_invalid`. (BR7)
- AC7 Given a request of any of the other six types that sends a `rubric` or a size limit, then 400 `question.rubric_not_allowed` or `question.answer_size_not_allowed`; given an open-answer or essay request that sends options, a `trueFalseKey`, `matchItems` or `blanks`, then 400 with `question.options_not_allowed`, `question.key_not_allowed`, `question.match_items_not_allowed` or `question.blanks_not_allowed`; nothing is stored. (BR8)
- AC8 Given a stored question of either type, when an update names another type, then 409 `question.type_changed` and nothing changes; naming its own type is accepted. (BR2)
- AC9 Given a statement of either type that contains the text `[[1]]`, when it is saved, then the stored statement keeps the text, the list excerpt shows it as typed and no `question.blank_*` error is raised. (BR8)
- AC10 Given a published question with version 1 and a criterion "Argument" of 40 points, when the curator changes the points, adds a criterion, removes one or renames one without a reason, then 400 `question.key_change_reason_required`; with a reason, version 2 is recorded with `answerKeyChanged` true, the reason and its `rubric`. (UC7, BR10)
- AC11 Given the same question, when only a description, the order of the criteria, the statement, the explanation or the size limit changes, then version 2 is recorded with `answerKeyChanged` false and no reason is required; the F-98, F-104, F-106, F-107 and F-108 version suites pass unedited. (BR10)
- AC12 Given a question of either type, then the shared rules hold for it: statement sanitized and limited, images counted under the F-103 cap, difficulty 0 or 6 refused, a taken paper number refused, a published one cannot be deleted, a topic with only this type is `topic.in_use`, annulment and reinstatement keep its rubric and size limit, the review flow works, and a base text linked to a published question of this type records a version that keeps its rubric. (BR9, BR11)
- AC13 Given questions of the eight types, then the list shows each row's type and the response names it; `QuestionResponse` and `QuestionVersionResponse` carry the rubric and size limit of these two types and an empty rubric and no size for the others, including versions recorded before this item. (UC5, BR13)
- AC14 Given the form for a new question, when the curator picks "Open answer" or "Essay", then the Options card is replaced by "Rubric" with one empty row and the size fields of the type, "Add criterion" adds up to 10 rows and a removal drops one, the total of the points updates, saving sends `rubric` and the size, publishing works, picking another type and back keeps the typed rows, and on an existing question the type is read-only. (UC1, UC2, BR1, BR12)
- AC15 Given a question of either type in the form, then the explanation field is labelled "Model answer" with its hint, and the publish checklist item is "At least one rubric criterion", done only when a criterion is named with its points. (UC3, BR9)
- AC16 Given a question of either type, when the curator opens the preview, then it shows the statement, an inert multi-line box labelled "Your answer" or "Your essay", the task line and the limit line (none when no limit is set); the rubric and the model answer stay hidden until "show answer", then the rubric is listed with its total and the model answer follows; a draft with no criterion says "no rubric yet". (UC6)
- AC17 Given the migration, then existing rows and versions keep their type, options, items and blanks, the new table and columns exist, and the comments of the `type` columns list the eight values. (BR1)
- AC18 Given a user without `questions.manage`, then create and update of these types answer 403. (BR11)
- AC19 The rubric section is operable by keyboard (each field and the remove action one tab stop, "Add criterion" one), each control has an accessible name with the criterion's number, the section errors are announced and the preview box is named "Your answer" or "Your essay" (WCAG 2.2 AA); checked on screen.
- AC20 Given the transcription screen of an edition, then it offers the eight types and the rubric section works there. (F-99 BR5)
- AC21 All new texts appear in pt-BR, pt-PT and en, including the texts of the new error codes.

## Criterion → test

Tests locked: 31f3f4b22b5f2964a75dcf0afa5e0354f8c5f42b (7 files); unlocks: 0

| Criterion | Test |
|---|---|
| AC1 | `RubricQuestionTests.Create_WithNoRubric_IsADraftOfThatType_WithAnEmptyRubricAndNoSize`; `RubricQuestionEndpointTests.Create_WithNoRubric_IsStoredAsADraftOfThatType_WithAnEmptyRubric_AndListedWithIt` |
| AC2 | `RubricQuestionTests.Create_WithThreeCriteria_StoresThemTrimmedInOrderFromPosition1`; `RubricQuestionEndpointTests.Create_WithThreeCriteria_StoresThemTrimmedInOrder` |
| AC3 | `RubricQuestionTests` (`Create_WithElevenCriteria_*`, `Create_WithAnEmptyName_*`, `Create_WithANameOf101Characters_*`, `Create_WithADescriptionOf501Characters_*`, `Create_WithPointsMissingOrOutsideOneTo100_*`, `Create_WithPointsAtTheEdges_IsAccepted`, `Create_WithTwoNamesEqualUnderTheRule_*`, `Update_WithABrokenRubric_*`); `RubricQuestionEndpointTests.Create_BreakingTheLimitsOfTheRubric_*`, `Create_WithTheLimitsThemselves_IsAccepted` |
| AC4 | `RubricQuestionTests.Publish_WithNoCriterion_*`; `RubricQuestionEndpointTests.Publish_WithNoCriterion_IsRefused_AndWithOne_IsPublished`, `Submit_CompleteDraft_GoesToReview_*` |
| AC5 | `RubricQuestionTests.Update_Published_RemovingEveryCriterion_*`, `Update_Published_WithAnotherValidRubric_StaysPublished`; `RubricQuestionEndpointTests.Update_Published_RemovingEveryCriterion_*` |
| AC6 | `RubricQuestionTests` (`Create_AnEssayWith*`, `Create_AnOpenAnswerWith*`); `RubricQuestionEndpointTests.Create_AnEssayWithALineLimit_*`, `Create_AnOpenAnswerWithACharacterLimit_*` |
| AC7 | `RubricQuestionTests` (`Create_OfAnotherType_WithARubric_*`, `Create_OfAnotherType_WithASizeLimit_*`, `Create_WithOptions_*`, `Create_WithATrueFalseKey_*`, `Create_WithMatchItems_*`, `Create_WithBlanks_*`); `RubricQuestionEndpointTests` (`Create_OfAnotherType_WithARubricOrASize_*`, `Create_WithOptionsAKeyMatchItemsOrBlanks_*`) |
| AC8 | `RubricQuestionTests.Update_WithContentOfAnotherType_IsAConflict`; `RubricQuestionEndpointTests.Update_NamingAnotherType_Is409TypeChanged_AndNamingItsOwnIsAccepted` |
| AC9 | `RubricQuestionTests.Create_WithStatementTextThatLooksLikeAMarker_KeepsItAsText`; `RubricQuestionEndpointTests.Create_WithStatementTextThatLooksLikeAMarker_*` |
| AC10 | `RubricQuestionTests.NextVersion_ChangingTheKeyOfTheRubric_NeedsAReason_*`; `RubricQuestionEndpointTests` (`Update_Published_ChangingThePointsOfACriterion_*`, `Update_Published_AddingRemovingOrRenamingACriterion_NeedsAReason`) |
| AC11 | `RubricQuestionTests` (`NextVersion_ChangingOnlyADescription_*`, `NextVersion_ReorderingTheCriteria_*`, `NextVersion_ChangingTheNameOnlyInCaseOrSpacing_*`, `NextVersion_ChangingOnlyTheStatementTheExplanationOrTheSize_*`, `NextVersion_RecordsTheSizeLimitTheQuestionHad`, `NextVersion_NothingChanged_MakesNothing`); `RubricQuestionEndpointTests.Update_Published_ChangingOnlyDescriptionOrderStatementExplanationOrSize_*`; the F-98, F-104, F-106, F-107 and F-108 suites unedited |
| AC12 | `RubricQuestionTests.AnnulAndReinstate_CopyTheRubricAndTheSizeOfTheLatestVersion`; `RubricQuestionEndpointTests` (`UpdateBaseText_LinkedToAPublishedEssay_*`, `Create_SanitizesTheStatement`, `Create_WithElevenImagesInTheStatement_*`, `Create_WithADifficultyOutOfRange_*`, `Create_WithAPaperNumberAlreadyTaken_*`, `Delete_Published_IsRefused_*`, `DeleteTopic_WithOnlyAnEssayQuestion_*`, `AnnulAndReinstate_KeepTheRubricAndTheSizeInTheirVersions`) |
| AC13 | `RubricQuestionEndpointTests.List_QuestionsOfTheEightTypes_*`, `VersionsRecordedBeforeTheRubric_*`; `RubricQuestionPreviewAndListTests.List_NamesTheOpenAnswerAndEssayTypes` |
| AC14 | `RubricQuestionFormTests` (`Add_OffersTheSeventhAndEighthTypeCards_*`, `Add_ChoosingEssay_*`, `Add_ChoosingOpenAnswer_*`, `Add_Rubric_AddCriterionGoesUpToTen_*`, `Add_Essay_SavesTheTypeTheRubricInOrderAndTheLineLimit`, `Add_OpenAnswer_SavesTheCharacterLimitAndNoMinimum`, `Add_GoingToAnotherTypeAndBack_*`, `Edit_Essay_ShowsTheTypeAsAReadOnlyValue_*`, the refusal, reason and annulled tests) |
| AC15 | `RubricQuestionFormTests.Edit_Essay_LabelsTheExplanationAsTheModelAnswer_WithItsHint`, `Edit_Essay_ChecklistIsDoneOnlyWithACriterionNamedWithItsPoints` |
| AC16 | `RubricQuestionPreviewAndListTests` (`Preview_Essay_*`, `Preview_OpenAnswer_*`, `Preview_EssayDraftWithNoCriterion_SaysNoRubricYet`) |
| AC17 | `AddOpenAnswerAndEssayQuestionsMigrationTests` (both); `TableDescriptionTests` (JSON exemption) |
| AC18 | `RubricQuestionEndpointTests.CreateAndUpdate_WithoutThePermission_AnswerForbidden` |
| AC19 | pending (validation step: keyboard and screen reader on the rubric section; the accessible names are asserted by `RubricQuestionFormTests.Add_ChoosingEssay_ShowsTheRubricWithOneEmptyRow_AndTheLineFields` and the error announcement by `Edit_Essay_WhenTheApiRefusesTheRubricOrTheSize_*`) |
| AC20 | `QuestionTranscriptionTests.Open_OffersEveryTypeTheFormImplements`, `Open_ChoosingTheEssay_ShowsTheRubricSection` |
| AC21 | `RubricQuestionResourcesTests.EveryNewText_ExistsInTheThreeLanguagesWithTheItemsWords`; `ResourceParityTests`; `ErrorCodeTextTests` |

## Decisions
- 2026-10-24 — F-109 delivers only the `OpenAnswer` and `Essay` types with a rubric; grading is a person (a `Grader` role, F-150), the queue is F-151 and the AI-suggested grade F-152 — no student attempt exists to produce answers to grade — confirmed: docs/discussions/D-5-essay-grading-and-grader-role.md
- 2026-10-24 — The type is chosen at creation and fixed after the first save — confirmed: F-97, F-106, F-107, F-108 (owner said Yes)
- 2026-10-24 — The question stores no points or weight for the exam; the edition's scoring rule (E-6) decides — confirmed: docs/epics/question-bank.md, F-108 (owner said Yes)
- 2026-10-24 — The rubric points only split the question's own grade (points given ÷ total of the maximums); they are not exam points — asked (recommended option, owner)
- 2026-10-24 — Rubric limits: 1 to 10 criteria, name up to 100 characters, description up to 500, whole points 1 to 100; at least one criterion to publish — asked (recommended option, owner)
- 2026-10-24 — Size limit: an essay in lines (minimum and maximum, optional), an open answer in characters (maximum, optional) — asked (recommended option, owner)
- 2026-10-24 — No separate theme field: the statement and the base text are the theme — asked (recommended option, owner)
- 2026-10-24 — The model answer reuses the explanation field, relabelled for these two types; the grader always sees it and a student after answering. This narrows D-5, which wrote "an optional model answer": the same need is met with no new column — asked (recommended option, owner)
- 2026-10-24 — No mockup and no `/agile:screen`: two type cards, one rubric section of kit rows, a size field and a writing box in the preview; wireframes in this file — asked (recommended option, owner)
- 2026-10-24 — Performance levels per criterion (ENEM style, fixed steps with descriptions) are out of scope and no separate item was captured; a note in F-151 (432c7ec) brings the question back when the correction screen is refined — asked (recommended option, owner)
- 2026-10-24 — Taken from rules, not asked: the transcription screen offers the types as soon as they exist and `QuestionTranscriptionTests.Open_OffersEveryTypeTheFormImplements` goes from 6 to 8 (F-99 BR5, F-108); shuffling is the simulator's (E-5); every shared rule applies unchanged (F-108 BR10) — confirmed: F-108
- 2026-10-24 — The rubric is a new child table `question_rubric_criteria` owned by the `Question`, copied into `question_versions.rubric` as JSON, as blanks are — the same pattern keeps the version, annul and reinstate copies and the base-text fan-out working; trade-off: one table and one JSON column more than reusing blanks, but a criterion has a name, a description and points that a list of answers does not — Claude, from the code read
- 2026-10-24 — Each type is a key record (`OpenAnswerKey`, `EssayKey`) beside the others with one line each in `AnswerKey.From`, sharing the criteria checks in one place; they refuse options, a true/false key, match items and blanks through the trailing-parameter pattern of F-106 to F-108 — Claude
- 2026-10-24 — The key of a rubric for the F-98 check is the set of (name, maximum points) pairs, so a rename is a key change and a description or order change is not; a grade must name the rubric that graded it — Claude
- 2026-10-24 — The size limit is versioned (two columns on `question_versions`) because it is what the student was shown, but it is not a key change — Claude
- 2026-10-24 — A request that omits `rubric` on update counts as an empty rubric (the form always sends the whole state), so a published question cannot lose its rubric silently: BR6 refuses it — Claude
- 2026-10-24 — The migration adds one table and five nullable columns and changes two comments; nothing is backfilled — Claude
- 2026-10-24 — The glossary gets the rows `Rubric` and `Rubric criterion` (pt-BR "rubrica" and "critério da rubrica") and the model snapshot changes with the migration — Claude
- 2026-10-24 — No new package, production or test — the item needs nothing the solution lacks — Claude
- 2026-10-24 — No security section: no door (no upload, permission change, personal data or authentication); routes and permission unchanged — Claude
- 2026-10-24 — Size M, fits one session: one module, one additive migration, one flow (author, publish, preview, version); doubt keeps it whole — Claude, from the code read
- 2026-10-24 — Approved by the owner ("aprovo F-109"); every dependency delivered (`deps.js block`: FREE); main session model: sonnet.
- 2026-10-24 — Design passes (`system-design`, `architect`, sonnet, no signal), verified against the files. Accepted: rubric as child table plus `question_versions.rubric` JSON; trailing optional parameters on the six existing keys (refusal helpers beside `MatchItemsRefused`); `OptionCodes` carries the criterion codes so a refusal names its position; standalone `RubricKeySection.razor` with one `IsRubric` flag; `Include(Rubric)` in `QuestionStore` and `BaseTextStore`; `question_versions.rubric` added to the JSON exemption in `TableDescriptionTests`; no criterion id (a grade names version + position, F-151 decides). Changed by the architect: the slice order is vertical, not by layer (plan below); glossary rows move to step 1. Position base: stored `position` and `CriterionResponse.Position` start at 1, as the error positions do, so the form maps without an offset; `criterion_name_repeated` names the later criterion; an empty description is stored and returned as null; `Down` is documented as dev-only in the migration comment. Dropped: nothing.
- 2026-10-24 — Design coverage: scope, contracts (callers of the positional records listed), data (table, indexes, checks, comments, JSON exemption), simplicity, layout; not run: `ErrorCodeTextTests`, vocabulary test (need compiled code).
- 2026-10-24 — Traps: T1 rubric in `blanks` (wrong error codes), T2 one JSON column on `questions` (no child-row pattern, no future FK), T3 inline branches in `QuestionForm.razor` (already ~2,470 lines).
- 2026-10-24 — design trade-off 5 size-column ranges — owner chose as proposed (checks in the database by type, like `ck_questions_difficulty_range`).
- 2026-10-24 — plan — 1 glossary rows `Rubric` and `Rubric criterion`, then the six keys take the trailing parameters and refusal helpers → build + F-98, F-104, F-106, F-107, F-108 suites unedited; 2 create and save an `Essay` and an `OpenAnswer` end to end (enum, limits, codes, keys, entity, `question_rubric_criteria`, size columns, migration, contracts, handler, queries) → AC1, AC2, AC3, AC6, AC7, AC8, AC9, AC13, AC17, AC18; 3 publish and edit a published rubric → AC4, AC5; 4 versions (`question_versions` rubric and size, `RubricKeyChanged`, `CopyAs`, base-text fan-out, annul and reinstate) → AC10, AC11, AC12; 5 form: rubric section, size fields, labels, checklist, transcription 6 → 8, three locales → AC14, AC15, AC20, AC21 (AC19 on screen); 6 preview and list → AC16; 7 flow page, pt-BR manual, regenerated docs, validation script → `techdocs.js check F-109`. No `frontend` agent (no mockup).

## Out of scope
- Grading, a grader role, the correction queue and the AI-suggested grade — F-150, F-151, F-152.
- A student attempt and what a student sees of the rubric before answering — epics E-5 and E-6.
- Performance levels per criterion (fixed steps with descriptions) — brought back in F-151 if wanted.
- Enforcing the size limit on a student's answer — the simulator.
- Scoring: points, weight, partial credit in an exam — epic E-6.
- Filter by type — F-100. Changing the type of an existing question.
- A separate theme field and a grader-only model answer field.

## Open questions
- (none)

## Change notes

## Validation script
<!-- Written at the end of the build. -->

## Events

- 2026-10-24 question — asked 7, from context 3

## Delivery
