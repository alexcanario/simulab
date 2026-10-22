---
feature: F-107
epic: Question bank
status: building
board: 153
version: 1
---
# Fill-in-the-blanks questions

Technical terms: [glossary](../glossary.md)

## Summary
The fill-in-the-blanks question type: the statement carries numbered markers `[[n]]`, and each marker is a blank with its list of accepted answers, typed by the student. The question stores only its key (the blanks and their accepted answers, and the one rule that compares a typed answer with them); how a partly right answer scores is the edition's scoring rule (epic E-6). Source: epic Question bank (E-4); Simulae `ConfigurationBase.cs` (T6, `ConfiguracaoLacunas`) as the import source, typed blanks only.

## Start
- Depends on: F-95 (done), F-97 (done: the type pattern), F-98 (done: key versioning), F-103 (done: image rules), F-106 (done: the latest type, its seams).
- Waits on (to start): nothing.
- Needed to validate: the local app host — owner.
- Model: opus (public API contract: the question request and responses gain the blanks; the schema gains a table).
- Suggested path: `/agile:build` (no mockup, owner 2026-10-21; the screens are described below with wireframes).
- Parallel with: F-108. Both edit `QuestionForm.razor`, `QuestionPreviewContent.razor`, `AnswerKey.From` and the three `SharedResources` files: expect conflicts and bring the branch up to date often. F-108 reuses the answer comparison rule of BR6.

## Goal
A curator can author, publish, list, preview and version a fill-in-the-blanks question, and the module gains it by adding one answer-key unit beside the four it has, not by branching through the other types.

## What exists (verified in the code, 2026-10-21)
- `QuestionType` has `SingleChoice`, `TrueFalse`, `MultipleAnswer` and `Matching` (`QuestionType.cs`); `questions.type` is text, so a new value needs no data conversion; only the column comments list the values (`EnumColumnCommentTests`). The glossary already lists `FillInTheBlanks` ("lacunas") in the `QuestionType` row.
- The answer key is a domain unit per type: `AnswerKey` with `SingleChoiceKey`, `TrueFalseKey`, `MultipleAnswerKey` and `MatchingKey`, one `switch` in `AnswerKey.From` (`AnswerKey.cs:48`), and an optional trailing list per extra shape (`MatchItems`, F-106 decision of 2026-10-21 #3). `Question.Apply` and `Question.Publish` hold no per-type branch.
- The F-98 key-change check (`Question.NextVersion`, `Question.cs:150-155`) compares the true/false key, the set of correct option positions and the matches of the left items. For a fill-in-the-blanks question all three are empty on both sides, so it would never see a key change; it must also compare the blanks (BR10).
- The versions snapshot options and match items as JSON columns of `question_versions` (`options`, `match_items`); a new list on a version is a new JSON column, not a new table.
- `QuestionHtmlSanitizer` allows text, and `[[1]]` is plain text: no change to its allow-list is needed. The list excerpt is the plain statement cut at 200 characters (`QuestionQueries.cs:633`).
- The editor toolbar formats are the list at `wwwroot/js/rich-text.js:12`; the statement field already has a custom button (the F-103 picture), which is the pattern for a second one.
- Error codes `question.options_not_allowed`, `question.key_not_allowed` and `question.match_items_not_allowed` exist (`QuestionBankErrorCodes.cs:65`, `:68`, `:95`).
- Items that touched this code since F-104: F-103 (images), F-105 (review freezes the question), F-102 (base text), F-106 (matching, the `MatchItems` seam, the base-text fan-out and `CopyAs` that must carry every key list). Their rules apply to this type unchanged.
- Simulae `ConfiguracaoLacunas` (`ConfigurationBase.cs:178-220`, `D:\dev\_control-alt-systems\simulae\repo`, read 2026-10-21): a list of blanks, each with an identifier, a kind (`TextoLivre`, `Dropdown`, `Codigo`), accepted answers, an optional regular expression, dropdown options, a code language and a weight; the statement carries `{{n}}` markers and the editor only warns when markers and blanks differ (`QuestionTypeContentEditor.razor:202`). Defects not to import: the weight (scoring is E-6), a key that may disagree with the markers, and a regular expression as an answer (unbounded and unreviewable by a curator).

## Users and use cases
- UC1 A curator creates a fill-in-the-blanks question as `Draft`: type chosen at creation, topic, content language, statement with markers, optionally base text, the accepted answers of each blank, explanation, difficulty, origin edition.
- UC2 A curator edits it, `Draft` or `Published`, in place; its type does not change.
- UC3 A curator publishes a complete fill-in-the-blanks `Draft`.
- UC4 A curator unpublishes it and deletes a `Draft`, as for any question.
- UC5 A curator tells the five types apart in the list.
- UC6 A curator previews it as a student will see it (a numbered empty box in place of each marker), then reveals the key and the explanation.
- UC7 A curator changes the accepted answers of a blank in a published question and gives the reason, as F-98 asks for any key change.

## Business rules
- BR1 `QuestionType` gets `FillInTheBlanks`. A fill-in-the-blanks question is the same `Question` row with `Type = FillInTheBlanks`, no options and no `true_false_key`. Its key is a new list of blanks: each blank has a number and its accepted answers.
- BR2 The type is chosen at creation and never changes (F-97 BR2): `SaveQuestionRequest.Type` accepts `FillInTheBlanks` on create; on update it must be omitted or equal the stored type, else 409 `question.type_changed`.
- BR3 A marker is the text `[[n]]` in the sanitized statement, `n` a whole number from 1 to 99 with no leading zero, written as one run of text (a marker split by formatting is plain text). Markers go in the statement only: `[[n]]` in the base text, an option or the explanation is plain text. The numbers need not be consecutive or in order; the blanks are shown in the order their markers appear in the statement.
- BR4 A fill-in-the-blanks `Draft` follows F-95 BR3 (statement, topic, content language) and may be incomplete: a marker with no accepted answer yet is allowed. Broken data is refused even on a draft: the same number twice in the statement (400 `question.blank_repeated`), a blank sent for a number with no marker in the statement (400 `question.blank_marker_missing`), more than 10 markers (400 `question.blanks_count`), more than 5 accepted answers in a blank (400 `question.blank_answers_count`), an accepted answer over 100 characters (400 `question.blank_answer_too_long`), an empty accepted answer (400 `question.blank_answer_required`) and two accepted answers of one blank that are equal under BR6 (400 `question.blank_answer_repeated`).
- BR5 `Published` needs, besides BR4: from 1 to 10 markers in the statement (none is 400 `question.blanks_count`) and every marker with at least one accepted answer (400 `question.blank_answers_required`). Editing a published question runs the same check and refuses a change that breaks it.
- BR6 A typed answer matches an accepted answer when both are equal after this normalization, the one rule of the module for typed answers (F-108 reuses it): trim, collapse every run of white space to one space, and compare without regard to case (invariant culture); accents and every other character count. An accepted answer is stored as the curator wrote it, trimmed; the normalization is only for comparing. Grading a student's answer is the simulator's (E-5); this item stores the key, applies the rule to refuse repeated answers, and pins the rule with a test.
- BR7 Options, a true/false key or match items sent with a fill-in-the-blanks question are refused 400 (`question.options_not_allowed`, `question.key_not_allowed`, `question.match_items_not_allowed`), as for the other types; nothing is ignored. Blanks sent with any other type are refused 400 `question.blanks_not_allowed`.
- BR8 Accepted answers are plain text, not HTML: a typed answer has no formatting. The statement keeps every shared rule (sanitizing, length, images under F-103).
- BR9 The list excerpt and the plain text of the statement show each marker as `____` (four underscores), so a search or a list never shows `[[1]]`.
- BR10 The F-98 key-change check compares the blanks, for every type: a fill-in-the-blanks question is a key change when the set of blank numbers differs from the latest version's, or when the accepted answers of any blank differ as a set under BR6; changing the statement text around the markers, the order of the markers, or the order of the answers in a blank is not. A version snapshots its blanks as a JSON column of the version, like the options. Version 1 is never a key change; the other four types behave as before and their tests pass unedited.
- BR11 Everything the types share is one rule, not five: sanitizing and limits, content language, status, publish and unpublish, delete, the topic guard, origin and paper number, annulment, versions, review, base text, permission `questions.manage`, 404 answers (F-95 BR1 to BR16, F-96, F-97 BR5, F-98, F-101, F-102, F-103, F-105, F-106 BR9).
- BR12 The type is declared in one place per layer, as F-97 BR9 sets: the domain (`FillInTheBlanksKey` beside the others, one line in `AnswerKey.From`, one new member `Blanks` on `AnswerKey`, empty for the other types), the contract (`QuestionType`), the form (the key section draws the blanks for this type, and the statement field gets the marker button only for this type) and the preview. No `if` chain in `Question.Apply`.
- BR13 The list shows the type in the existing "Type" chip (F-97 BR8); filter by type stays in F-100.
- BR14 No scoring is stored or computed here: neither "all or nothing" nor credit per blank nor a weight (epic E-6, decision of 2026-10-09). Shuffling is the simulator's (E-5).
- BR15 The student is told the task, not the answer: the preview says "Fill in each blank."; the box gives no hint of the answer's length.

## Screens and API
No mockup (owner, 2026-10-21): the form gains a fifth type card, one toolbar button on the statement and one key section of kit rows, and the preview gains the boxes; wireframes below.

- API (routes unchanged, `/api/v1/question-bank/questions`):
  - `SaveQuestionRequest.type` accepts `FillInTheBlanks` on create. New optional trailing field `blanks`: a list of `{ number, acceptedAnswers }`, `number` 1 to 99, `acceptedAnswers` a list of strings.
  - `QuestionResponse` gains `blanks` (same shape, in the order the markers appear in the statement, empty for the other types) and `QuestionVersionResponse` gains it too (empty for the other types and for versions recorded before this item). The list response is unchanged; its excerpt follows BR9.
  - Error codes (new): `question.blanks_count`, `question.blank_repeated`, `question.blank_marker_missing`, `question.blank_answers_required`, `question.blank_answers_count`, `question.blank_answer_required`, `question.blank_answer_too_long`, `question.blank_answer_repeated`, `question.blanks_not_allowed` (all 400). Reused: `question.options_not_allowed`, `question.key_not_allowed`, `question.match_items_not_allowed`, `question.type_changed`, `question.key_change_reason_required`, `question.image_*`.
  - Data: one migration in the `QuestionBank` context: new table `question_blanks` (question id, `number`, `accepted_answers` as a `text[]`, a unique index on (question id, `number`), whose columns are not nullable), a nullable JSON column `blanks` on `question_versions` (beside `options` and `match_items`), and the comments of `questions.type` and `question_versions.type` updated. Existing rows keep their type, options and items; nothing is backfilled. Unique indexes over nullable tenant columns: none added.
  - Limits (contracts): `QuestionLimits.BlanksMax` = 10, `BlankNumberMax` = 99, `BlankAnswersMax` = 5, `BlankAnswerMaxLength` = 100.
- `/admin/questions/new`: the "Question type" `AppRadioCards` gets a fifth card, "Fill in the blanks" (description "Blanks in the statement; the student types each answer."), after "Matching". Single choice stays preselected.
- `/admin/questions/{id}`: the type stays a read-only labelled value, now able to read "Fill in the blanks".
- Statement toolbar, only for this type: a new button "Insert blank" (an `AppIcons` entry, Material Outlined, chosen at the build) writes `[[n]]` at the caret, `n` one more than the highest number already in the statement (1 when none). Typing a marker by hand works the same.
- Key section for `FillInTheBlanks` (replaces the "Options" card for this type, same position, same kit card): subtitle "Insert a blank in the statement, then write the answers it accepts. Case and extra spaces are ignored; accents count."; one row per marker found in the statement, in statement order, read live as the curator types. Each row shows "Blank {n}" and up to 5 answer fields (accessible name "Accepted answer {k} of blank {n}"), "Add answer" with the count "{0} of 5 answers", and remove per answer. Removing a marker from the statement removes its row (the answers typed for it are dropped at save, so the Api never receives a blank without a marker); typing the marker back starts an empty row. With no marker, the section says "No blank yet. Use Insert blank in the statement."
  - Section errors (`question.blanks_count`, `question.blank_repeated`) appear under the section as the other types do; row errors (answers required, too long, repeated, count) mark the row. A frozen question (annulled or in review) shows everything disabled, as the other types.
  - Going to another type and back keeps the typed answers (as F-104 AC12).
- Publish checklist (F-95): for `FillInTheBlanks` the items are "1 to 10 blanks in the statement" and "Every blank has an accepted answer"; the aside shows the type.
- Preview dialog: the base text, the statement with each marker drawn as an empty underlined box with its number (accessible name "Blank {n}"), then "Fill in each blank."; after "show answer" each box shows the first accepted answer, the other accepted answers are listed after the statement as "Blank {n}: answer, answer", with the check icon and the text "Correct answer" (never colour alone); a draft with a marker that has no answer shows "no key yet" for that blank.
- `/admin/questions`: the "Type" chip says "Fill in the blanks"; the excerpt shows `____` for each marker.
- The transcription screen of F-99 offers the fifth type card; picking it there shows the same toolbar button and key section.
- Permissions: unchanged, `questions.manage`.

### Wireframe: Question form, statement and key section (fill in the blanks)
```text
+------------------------------------------------------------+
| Classification                                             |
|  Question type: ( ) Single choice  ( ) True/false          |
|   ( ) Multiple answer  ( ) Matching  (x) Fill in the blanks|
|                                                     [new]  |
+------------------------------------------------------------+
| Statement *                                                |
| [B][I][U][S][x2][x2][link][image][blank] [changed]         |
| +--------------------------------------------------------+ |
| | The capital of Brazil is [[1]] and it was founded in   | |
| | [[2]].                                                 | |
| +--------------------------------------------------------+ |
+------------------------------------------------------------+
| Blanks                                            [new]    |
| Insert a blank in the statement, then write the answers it |
| accepts. Case and extra spaces are ignored; accents count. |
|  Blank 1                                                   |
|   [ Brasília ...................... ] x                    |
|   [ Brasilia ...................... ] x                    |
|   [+ Add answer]                     2 of 5 answers        |
|  Blank 2                                                   |
|   [ 1960 .......................... ] x                    |
|   [+ Add answer]                     1 of 5 answers        |
|  ! Every blank needs an accepted answer. (error)           |
+------------------------------------------------------------+
| Before publishing: [v] 1 to 10 blanks in the statement     |
|   [ ] Every blank has an accepted answer                   |
+------------------------------------------------------------+
```

### Wireframe: Preview dialog (fill in the blanks)
```text
+--------------------------------------------+
| Preview                                    |
| <base text>                                |
| The capital of Brazil is [ 1 ____ ] and it |
| was founded in [ 2 ____ ].                 |
| Fill in each blank.                 [new]  |
| [ Show answer ]                            |
|  -- after reveal --                        |
| The capital of Brazil is [ Brasília ] and  |
| it was founded in [ 1960 ].                |
| (v) Correct answer                         |
|  Blank 1: Brasília, Brasilia               |
|  Blank 2: 1960                             |
| Explanation: <text>                        |
+--------------------------------------------+
```

## Docs
- flow: new docs/flows/fill-in-the-blanks-questions.md — how a curator authors, publishes, versions and previews the type, how markers and blanks form the key, and the comparison rule of BR6.

## Acceptance criteria
- AC1 Given a curator with `questions.manage`, when they create a question with `type` `FillInTheBlanks`, a topic, a language and a statement with no marker, then it is stored as `Draft` of that type and appears in the list with its type. (UC1, BR1, BR4)
- AC2 Given a fill-in-the-blanks draft whose statement holds `[[1]]` and `[[3]]`, when it is saved with answers for blank 3 only, then it is accepted and returns blanks 1 (no answer) and 3 in statement order; with 11 markers, 400 `question.blanks_count`. (BR3, BR4)
- AC3 Given a fill-in-the-blanks draft, when it is saved with `[[2]]` twice in the statement, then 400 `question.blank_repeated`; with a blank for number 4 and no `[[4]]` in the statement, 400 `question.blank_marker_missing`; in both cases nothing is stored. (BR4)
- AC4 Given a blank, when it is saved with 6 answers, then 400 `question.blank_answers_count`; with an answer of 101 characters, 400 `question.blank_answer_too_long` (100 is accepted); with an empty or blank answer, 400 `question.blank_answer_required`; with "Brasília" and " brasília  ", 400 `question.blank_answer_repeated`; with "Brasília" and "Brasilia", accepted. (BR4, BR6)
- AC5 Given a fill-in-the-blanks draft, when it is published with no marker, then 400 `question.blanks_count`; with a marker that has no answer, 400 `question.blank_answers_required`; with every marker answered, it becomes `Published`. (UC3, BR5)
- AC6 Given a published fill-in-the-blanks question, when an edit removes every answer of a blank or every marker, then it is refused and the stored question is unchanged; when the curator writes another valid set, it stays `Published`. (UC2, BR5)
- AC7 Given a `FillInTheBlanks` request that sends options, a `trueFalseKey` or `matchItems`, then 400 with `question.options_not_allowed`, `question.key_not_allowed` or `question.match_items_not_allowed` and nothing is stored; given a request of any other type that sends `blanks`, then 400 `question.blanks_not_allowed`. (BR7)
- AC8 Given a stored fill-in-the-blanks question, when an update names another type, then 409 `question.type_changed` and nothing changes; naming its own type is accepted. (BR2)
- AC9 Given the comparison rule, then "  Rio   de Janeiro " matches "rio de janeiro", "BRASÍLIA" matches "Brasília", and "Brasilia" does not match "Brasília"; the rule is one function the domain exposes, pinned by a test. (BR6)
- AC10 Given a published fill-in-the-blanks question with version 1 and blank 1 accepting "Brasília", when the curator adds "Brasilia" to blank 1 without a reason, then 400 `question.key_change_reason_required`; with a reason, version 2 is recorded with `answerKeyChanged` true, the reason and its `blanks`. Adding or removing a blank also needs a reason. (UC7, BR10)
- AC11 Given the same question, when only the text around the markers changes, the markers swap places, or the answers of a blank are reordered or change only in case, then version 2 is recorded with `answerKeyChanged` false and no reason is required; given single-choice, true/false, multiple-answer and matching questions, the F-98, F-104 and F-106 behaviour is unchanged and their tests pass unedited. (BR10)
- AC12 Given a fill-in-the-blanks question, then the shared rules hold for it: statement sanitized and limited, images counted under the F-103 cap, difficulty 0 or 6 refused, a taken paper number refused, a published one cannot be deleted, a topic with only this type is `topic.in_use`, annulment and reinstatement keep its blanks, the review flow works, and a base text linked to a published question of this type records a version that keeps its blanks. (BR8, BR11)
- AC13 Given questions of the five types, then the list shows each row's type and the response names it; a fill-in-the-blanks excerpt shows `____` in place of each marker; `QuestionResponse` and `QuestionVersionResponse` carry `blanks` (empty for the other types). (UC5, BR9, BR13)
- AC14 Given the form for a new question, when the curator picks "Fill in the blanks", then the statement toolbar shows "Insert blank" (and no other type shows it), each click writes the next number at the caret, the key section shows one row per marker in statement order, removing a marker removes its row and its answers are not sent, saving and publishing work, picking another type and back keeps the typed answers; on an existing question the type is read-only. (UC1, UC2, BR3, BR12)
- AC15 Given a fill-in-the-blanks question, when the curator opens the preview, then each marker shows as an empty numbered box with the line "Fill in each blank."; the key and the explanation stay hidden until "show answer", then each box shows its first accepted answer and the full lists appear with icon and text. (UC6, BR15)
- AC16 Given the migration, then existing rows and versions keep their type, options and items, `question_blanks` exists with its comments and its unique index, and the type column comments list the five values. (BR1)
- AC17 Given a user without `questions.manage`, then create and update of this type answer 403. (BR11)
- AC18 The marker button and the key section are operable by keyboard (the button in the toolbar's arrow order, each answer field and button one tab stop), each control has an accessible name with its blank number, the section errors are announced, and the preview boxes are named "Blank {n}" (WCAG 2.2 AA); checked on screen.
- AC19 Given the transcription screen of an edition, then it offers the five types and the marker button and key section work there. (F-99 BR5)
- AC20 All new texts appear in pt-BR, pt-PT and en, including the error code texts.

## Criterion → test

Tests locked: 5ad60ba0209774fb91aac0c29fa7fb70593524af (8 files); unlocks: 0

| Criterion | Test |
|---|---|
| AC1 | `FillInTheBlanksQuestionTests.Create_FillInTheBlanksWithNoMarker_IsADraftOfThatType`, `FillInTheBlanksQuestionEndpointTests.Create_WithNoMarker_IsStoredAsADraftOfThatType_AndListedWithIt` |
| AC2 | `FillInTheBlanksQuestionTests.Create_DraftWithMarkers1And3_AndAnswersForBlank3Only_KeepsBothBlanksInStatementOrder`, `.Create_WithElevenMarkers_IsRefusedWithBlanksCount`, `.Create_WithTenMarkers_IsAccepted`; `FillInTheBlanksQuestionEndpointTests.Create_DraftWithMarkers1And3_AndAnswersForBlank3Only_ReturnsBothBlanksInStatementOrder`, `.Create_WithElevenMarkers_IsRefusedWithBlanksCount` |
| AC3 | `FillInTheBlanksQuestionTests.Create_WithTheSameMarkerTwice_IsRefusedWithBlankRepeated`, `.Create_WithABlankForANumberWithNoMarker_IsRefusedWithBlankMarkerMissing`, `.Create_WithTheSameNumberTwiceInTheBlanksSent_IsRefusedWithBlankRepeated`; `FillInTheBlanksQuestionEndpointTests.Create_WithARepeatedMarkerOrABlankWithNoMarker_IsRefused_AndNothingIsStored` |
| AC4 | `FillInTheBlanksQuestionTests.Create_WithSixAnswersInABlank_IsRefusedWithBlankAnswersCount`, `.Create_WithAnAnswerOf101Characters_IsRefusedWithBlankAnswerTooLong_And100IsAccepted`, `.Create_WithAnEmptyAnswer_IsRefusedWithBlankAnswerRequired`, `.Create_WithTwoAnswersEqualUnderTheRule_IsRefusedWithBlankAnswerRepeated`, `.Create_WithTheSameAnswerWithAndWithoutAccent_IsAccepted_AndAnswersAreStoredTrimmed`; `FillInTheBlanksQuestionEndpointTests.Create_BreakingTheLimitsOfABlank_IsRefusedWithTheBlankNumberAsPosition`, `.Create_WithAnAnswerOf100Characters_AndTheSameAnswerWithAndWithoutAccent_IsAccepted` |
| AC5 | `FillInTheBlanksQuestionTests.Publish_WithNoMarker_IsRefusedWithBlanksCount`, `.Publish_WithAMarkerWithNoAnswer_IsRefusedWithBlankAnswersRequired`, `.Publish_WithEveryMarkerAnswered_BecomesPublished`; `FillInTheBlanksQuestionEndpointTests.Publish_WithNoMarkerOrAnUnansweredMarker_IsRefused_AndWithEveryMarkerAnswered_IsPublished` |
| AC6 | `FillInTheBlanksQuestionTests.Update_Published_RemovingEveryAnswerOfABlank_IsRefused_AndNothingChanges`, `.Update_Published_RemovingEveryMarker_IsRefusedWithBlanksCount`, `.Update_Published_WithAnotherValidSet_StaysPublished`; `FillInTheBlanksQuestionEndpointTests.Update_Published_RemovingTheAnswersOfABlankOrEveryMarker_IsRefused_AndTheStoredQuestionIsUnchanged`, `.Update_Published_WithAnotherValidSetAndAReason_StaysPublished` |
| AC7 | `FillInTheBlanksQuestionTests.Create_FillInTheBlanksWithOptions_IsRefusedWithOptionsNotAllowed`, `.Create_FillInTheBlanksWithATrueFalseKey_IsRefusedWithKeyNotAllowed`, `.Create_FillInTheBlanksWithMatchItems_IsRefusedWithMatchItemsNotAllowed`, `.Create_AnotherTypeWithBlanks_IsRefusedWithBlanksNotAllowed`; `FillInTheBlanksQuestionEndpointTests.Create_WithOptionsAKeyOrMatchItems_IsRefused_AndNothingIsStored`, `.Create_AnotherTypeWithBlanks_IsRefusedWithBlanksNotAllowed` |
| AC8 | `FillInTheBlanksQuestionTests.Update_WithContentOfAnotherType_IsAConflict`; `FillInTheBlanksQuestionEndpointTests.Update_NamingAnotherType_Is409TypeChanged_AndNamingItsOwnIsAccepted` |
| AC9 | `AcceptedAnswerTests.Matches_IgnoringCaseAndExtraWhiteSpace_IsTrue`, `.Matches_WhenAnAccentOrAnyOtherCharacterDiffers_IsFalse`, `.Normalize_TrimsCollapsesWhiteSpaceAndUppersCase_KeepingAccents`, `.Normalize_OfNothing_IsEmpty` |
| AC10 | `FillInTheBlanksQuestionTests.NextVersion_AddingAnAcceptedAnswer_NeedsAReason_AndThenRecordsAKeyChangeWithTheBlanks`, `.NextVersion_AddingABlank_NeedsAReason`, `.NextVersion_RemovingABlank_NeedsAReason`; `FillInTheBlanksQuestionEndpointTests.Update_Published_AddingAnAcceptedAnswer_NeedsAReason_AndThenRecordsAKeyChange`, `.Update_Published_AddingOrRemovingABlank_NeedsAReason` |
| AC11 | `FillInTheBlanksQuestionTests.NextVersion_ChangingOnlyTheTextAroundTheMarkers_IsAVersionWithNoKeyChange`, `.NextVersion_SwappingTheMarkers_IsAVersionWithNoKeyChange_InTheNewStatementOrder`, `.NextVersion_ReorderingAnswersOrChangingOnlyTheirCase_IsAVersionWithNoKeyChange`, `.NextVersion_NothingChanged_MakesNothing`; `FillInTheBlanksQuestionEndpointTests.Update_Published_SwappingMarkersAndReorderingAnswers_RecordsAVersionWithNoKeyChange`; the F-98, F-104 and F-106 version suites unedited |
| AC12 | `FillInTheBlanksQuestionTests.AnnulAndReinstate_CopyTheBlanksOfTheLatestVersion`; `FillInTheBlanksQuestionEndpointTests.Create_SanitizesTheStatement_AndKeepsTheMarkers`, `.Create_WithElevenImagesInTheStatement_IsRefusedWithImagesTooMany`, `.Create_WithADifficultyOutOfRange_IsRefused`, `.Create_WithAPaperNumberAlreadyTaken_IsRefusedWithPaperNumberTaken`, `.Delete_Published_IsRefused_AndADraftIsDeleted`, `.DeleteTopic_WithOnlyAFillInQuestion_IsRefusedWithTopicInUse`, `.AnnulAndReinstate_KeepTheBlanksInTheirVersions`, `.Submit_CompleteDraft_GoesToReview_AndAnIncompleteOneIsRefused`, `.UpdateBaseText_LinkedToAPublishedFillIn_RecordsAVersionThatKeepsTheBlanks` |
| AC13 | `FillInTheBlanksQuestionEndpointTests.List_QuestionsOfTheFiveTypes_NameTheTypeOfEachRow_AndTheExcerptHidesTheMarkers`, `.List_SearchingForAMarker_FindsNothing`, `.FindVersion_OfAnotherType_HasAnEmptyBlanksList`; `BlankMarkerSanitizerTests` (4); `FillInTheBlanksQuestionPreviewAndListTests.List_NamesTheFillInTheBlanksType` |
| AC14 | `FillInTheBlanksQuestionFormTests` (16: cards, Insert blank on this type only, rows in statement order, what is sent, removed marker, type switch, edit read-only, checklist, section and row refusals, reason, frozen); the caret insertion of the next number: validation step (to number) |
| AC15 | `FillInTheBlanksQuestionPreviewAndListTests.Preview_FillIn_DrawsANumberedEmptyBoxForEachMarker_AndTheTaskLine`, `.Preview_FillIn_BeforeShowAnswer_HidesTheKeyAndTheExplanation`, `.Preview_FillIn_ShowAnswer_FillsTheBoxesAndListsEveryAcceptedAnswer`, `.Preview_FillInDraftWithAnUnansweredBlank_SaysItHasNoKeyYet`, `.VersionPreview_FillIn_ShowsTheBlanksOfTheVersion` |
| AC16 | `AddFillInTheBlanksQuestionsMigrationTests.Up_AddsTheBlanksTableAndColumn_NamesTheFifthType_AndKeepsEveryRow` |
| AC17 | `FillInTheBlanksQuestionEndpointTests.CreateAndUpdate_WithoutThePermission_AnswerForbidden` |
| AC18 | `FillInTheBlanksQuestionFormTests.Add_FillIn_EachAnswerFieldNamesItsAnswerAndItsBlank`, `.Edit_FillIn_WhenTheApiRefusesTheBlanks_TheMessageIsUnderTheSection`, `.Add_ChoosingFillIn_ShowsInsertBlankOnTheStatementOnly_AndAnotherTypeHidesIt`; `FillInTheBlanksQuestionPreviewAndListTests.Preview_FillIn_DrawsANumberedEmptyBoxForEachMarker_AndTheTaskLine`; keyboard pass: validation step (to number) |
| AC19 | `QuestionTranscriptionTests.Open_OffersEveryTypeTheFormImplements` (now five); the button and the section there: validation step (to number) |
| AC20 | `FillInTheBlanksQuestionResourcesTests.EveryNewText_ExistsInTheThreeLanguagesWithTheItemsWords`, `ResourceParityTests` |

## Decisions
- 2026-10-21 — F-108 (short answer, idea) also compares a typed answer with accepted answers; both items stay: the shapes differ (blanks inside the statement against one answer), this item defines the comparison rule (BR6) and F-108 reuses it — asked (recommended option, owner)
- 2026-10-21 — The type is chosen at creation and fixed after the first save — confirmed: F-97, F-106
- 2026-10-21 — Scoring (all or nothing, credit per blank, weight) is the edition's rule (E-6); the question keeps only its key; Simulae's weight per blank is not imported — confirmed: docs/epics/question-bank.md, F-104, F-106
- 2026-10-21 — The transcription screen of F-99 offers the type as soon as it exists; `QuestionTranscriptionTests.Open_OffersEveryTypeTheFormImplements` goes from 4 to 5 types — confirmed: F-99 BR5, F-106
- 2026-10-21 — Shuffling is the simulator's (E-5); the preview shows the authored order, which for blanks is the order of the statement — confirmed: F-104, F-106
- 2026-10-21 — Blanks in the statement only, never in the base text — the base text is shared by several questions (F-102), so a blank there would split the key between two owners — asked (recommended option, owner)
- 2026-10-21 — Typed blanks only; Simulae's dropdown and code blanks and the regular expression stay out — keeps the item size M; a regular expression cannot be reviewed by a curator — asked (recommended option, owner)
- 2026-10-21 — Comparison: case and extra white space are ignored, accents count; a spelling without accents is valid only when the curator adds it — an accent can change the meaning — asked (recommended option, owner)
- 2026-10-21 — Limits: 1 to 10 blanks to publish, 1 to 5 accepted answers per blank, each up to 100 characters — asked (recommended option, owner)
- 2026-10-21 — The marker is the text `[[n]]`, written by an "Insert blank" button or by hand — plain text passes the sanitizer with no change to its allow-list; a Quill blot would need a new tag or attribute — asked (recommended option, owner)
- 2026-10-21 — The preview draws a numbered empty box in place of each marker and fills it on "show answer" — asked (recommended option, owner)
- 2026-10-21 — No mockup and no `/agile:screen`: a type card, a toolbar button and kit rows; wireframes in this file — asked (recommended option, owner)
- 2026-10-21 — Dropdown, code and regular-expression blanks stay out of scope, no item captured now: no known demand — asked (recommended option, owner)
- 2026-10-21 — Blank numbers are unique but need not be consecutive (1 to 99): deleting `[[2]]` from the middle leaves `[[1]]` and `[[3]]` valid, so the form never renumbers markers inside the editor; the blanks show in statement order — Claude, from the editor code read (renumbering text inside Quill on every edit is fragile)
- 2026-10-21 — A repeated marker, a blank without a marker, and the per-blank limits are refused even on a draft; a marker without answers is allowed on a draft — the first are broken data, the last is unfinished work (F-106 decision of 2026-10-21) — Claude
- 2026-10-21 — The key is a domain unit `FillInTheBlanksKey` beside the others; `AnswerKey` gains `Blanks` (empty for the other types) as the last optional parameter of `AnswerKey.From`, `SaveQuestionRequest`, `QuestionResponse`, `QuestionVersionResponse` and `QuestionContent`, as F-106 did for `MatchItems` — Claude
- 2026-10-21 — Accepted answers stored as `text[]` in `question_blanks`, one row per blank, unique on (question, number); the version stores the blanks as a nullable JSON column `blanks` — one row per blank keeps the number unique by the database; the answers are a small value list with no identity — Claude
- 2026-10-21 — Accepted answers are plain text, not rich text — a typed answer has no formatting — Claude
- 2026-10-21 — The excerpt and plain text show `____` per marker — Claude
- 2026-10-21 — Nine new error codes instead of reusing the option ones, whose texts say "option" — Claude
- 2026-10-21 — New glossary rows before first use in code: `Blank` (lacuna), `BlankMarker` (marcador de lacuna), `AcceptedAnswer` (resposta aceita) — Claude
- 2026-10-21 — No new package — the item needs nothing the solution lacks — Claude
- 2026-10-21 — No security section: no door (no upload, permission change, personal data or authentication); the routes and the permission are unchanged — Claude
- 2026-10-21 — Reuse: the `AnswerKey` seam (F-97, F-106), the F-98 reason field and `NextVersion`, the F-103 toolbar button pattern in `rich-text.js`, kit `AppItemRows`, `AppRowActions`, `AppTextField`, `AppRadioCards`, the migration test pattern of F-104 and F-106, the base-text fan-out and `CopyAs` lists of F-106 (its trap T3) — Claude
- 2026-10-21 — Size M, fits one session: one domain unit, one table, a migration, a fifth card, one toolbar button, one key section, one preview change and the key-change fix — the same shape as F-106 — Claude, from the code read
- 2026-10-22 — Approved by the owner ("aprovo F-107"); every dependency delivered (`deps.js block`: FREE).
- 2026-10-22 — Built in parallel with F-90 (building in its own worktree): owner confirmed; low collision (F-90 touches catalog screens and `ITopicUsageCheck`, no QuestionBank migration).
- 2026-10-22 — Design passes (system-design, architect; opus): accepted — `BlankMarker.Pattern` in Contracts, shared by the Api and the Web; markers read by the sanitizer from text nodes only (opt-in for this type), giving `SanitizedHtml.BlankMarkers` and a plain text with `____`; `AcceptedAnswer.Normalize`/`Matches` in the domain (not `ComparableText`, which strips accents); `QuestionBlank` rows reused by number; one row per marker, empty answer lists included; `AnswerKey.From` gets trailing `blanks` and `markers` parameters (the 2026-10-21 decision named only `Blanks`; behaviour unchanged); the four keys refuse blanks through a trailing `BlanksWritten` parameter as F-106 did; `SameVersionedContent` compares blanks exactly and `keyChanged` compares number sets and normalized answer sets; the next marker number is computed in `rich-text.js` from the editor text; a repeated number in the request list is `question.blank_repeated`; answers trimmed, then measured; `NormalizedStatement` derived from the `____` text; version blanks kept in statement order.
- 2026-10-22 — Architect findings accepted: #1 the unique index is `ux_question_blanks_tenant_question_number` on (tenant_id, question_id, number), `IsUniquePerTenant()` and filtered to live rows, plus `ix_question_blanks_question` (rule `api-contracts`, global data in a tenant table; the item's "(question id, number)" gains the tenant prefix, no behaviour change); #2 the version JSON column mapping lands in step 1 with the one migration; #3 the `blanks_count` (over 10) check lands in step 1; #4 for `question.blank_*` row codes the problem's `position` is the blank number (1 to 99), documented where it is produced and pinned by HTTP tests. Dropped: nothing.
- 2026-10-22 — Design coverage: scope (UC1-UC7, BR1-BR15, AC1-AC20 against the slices), rules (ModuleBoundaryTests, TableDescriptionTests, EnumColumnCommentTests, FieldLengthTests, VocabularyTests, BannedSymbols.txt, api-contracts, naming, question-bank-project), simplicity (reuse of the F-106 seams, no new project or layer); none left unchecked.
- 2026-10-22 — traps — T1 reusing `ComparableText.Normalize` for BR6: accents stripped, AC9 broken; T2 marker regex over the stored HTML or the plain text: counts alt/href and rejoins split runs; T3 next marker number computed in .NET from the debounced HTML: a reused number answers `blank_repeated`.
- 2026-10-22 — design trade-off 5 statement order of the blanks — owner chose as proposed: column `question_blanks.position` (statement order, written at save) plus check `ck_question_blanks_number_range` (1 to 99); schema beyond the item, approved by the owner.
- 2026-10-22: plan — 1 draft with blanks end to end (contracts, marker reading, key with the count check, entity, table, version column mapping, the one migration, loads and responses) → AC1, AC2, AC9, AC16; 2 draft refusals and cross-type refusals → AC3, AC4, AC7, AC8; 3 publish and edit a published question → AC5, AC6, AC17; 4 versions, annulment copy and base-text fan-out → AC10, AC11, AC12; 5 list excerpt, type name, OpenAPI document → AC13; 6 form: fifth card, Insert blank button, key section, checklist, errors, transcription, texts → AC14, AC19, AC20, AC18 (bUnit part); 7 preview boxes and key list → AC15; 8 flow page and manual → techdocs check.

## Out of scope
- Scoring: all or nothing, credit per blank, weight per blank — epic E-6 (owner, 2026-10-09).
- Dropdown blanks, code blanks, a regular expression as an accepted answer (Simulae `Dropdown`, `Codigo`, `ExpressaoRegular`).
- Blanks in the base text, an option or the explanation.
- Grading a student's answer — E-5, the simulator (BR6 defines the rule it will use).
- Filter by type — F-100. Changing the type of an existing question.
- Shuffling — E-5.
- A visual marker (chip) inside the editor.

## Open questions
- (none)

## Change notes

## Validation script
<!-- Written by /agile:build. -->

## Events

- 2026-10-21 question — asked 9, from context 4
- 2026-10-22 question — asked 2, from context 0
