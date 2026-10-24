---
feature: F-108
epic: Question bank
status: validating
board: 154
version: 1
---
# Short-answer questions

Technical terms: [glossary](../glossary.md)

## Summary
The short-answer question type: the statement asks something, the student types a short answer, and the answer is checked against a list of accepted answers. The question stores only its key (the accepted answers); the comparison is the one rule of F-107 (BR6) and how an answer scores is the edition's rule (epic E-6). Simulae has no separate short-answer type (its T7 is a rubric-graded open answer, F-109). Source: epic Question bank (E-4), brief capability 3.

## Start
- Depends on: F-95 (done), F-107 (done: the comparison rule, the blanks storage and the key-change check this type reuses).
- Waits on (to start): nothing.
- Needed to validate: the local app host — owner.
- Model: sonnet (no signal: no new route, field or table; one enum value and a comment-only migration)
- Suggested path: `/agile:build` (no mockup, owner 2026-10-24; the screens are described below with wireframes).
- Parallel with: none known. F-109 (idea) edits the same form files but waits on the grading decision.

## Goal
A curator can author, publish, list, preview and version a short-answer question, and the module gains it by adding one answer-key unit beside the five it has, reusing the blanks storage of F-107 instead of a new table.

## What exists (verified in the code, 2026-10-24)
- `QuestionType` has five values, the last `FillInTheBlanks` (`QuestionType.cs`); `questions.type` is text, so a sixth value needs no data conversion; only the column comments list the values (`EnumColumnCommentTests`). The glossary already lists `ShortAnswer` ("resposta curta") in the `QuestionType` row.
- The answer key is a domain unit per type behind one `switch` in `AnswerKey.From` (`AnswerKey.cs:56`); `AnswerKey.Blanks` is the list a question stores (`QuestionBlank` rows: number, position, accepted answers as `text[]`), and `Question.ReplaceBlanks` writes it for every type (`Question.cs:642`). `FillInTheBlanksKey` already holds the limits and checks of the accepted answers (count, empty, length, repeated under BR6).
- `AcceptedAnswer.Normalize` and `AcceptedAnswer.Matches` (`AcceptedAnswer.cs`) are the module's one rule for typed answers; F-107 wrote them for this item to reuse.
- The F-98 key-change check (`Question.NextVersion`, `Question.cs:128-170`) already compares the blanks of every type (`BlanksKeyChanged`, `Question.cs:202`), and versions snapshot them as the JSON column `question_versions.blanks`. A short-answer key stored as blanks needs no change there, in `CopyAs`, in the annul and reinstate copies or in the base-text fan-out.
- The sanitizer reads `[[n]]` markers from the statement only when the type is fill-in-the-blanks (`SaveQuestionHandler.cs:132`, `readBlankMarkers`), so in a short-answer statement `[[1]]` stays plain text.
- Web: `QuestionForm.razor` branches by type in about 40 places for fill-in-the-blanks (`IsFillIn`), the key section is `FillInTheBlanksKeySection.razor` (181 lines, one row per marker), the preview is `QuestionPreviewContent.razor`, the type texts are `QuestionText.cs`, icons `AppIcons.cs` (the fill-in icon is `ShortText`).
- Items that touched this code since F-107: only F-126 (numbered sections and the publication block of the form).
- Simulae (`ConfigurationBase.cs:244`, read 2026-10-24): the only free-text type is T7 "Subjetiva", graded by a rubric; there is no accepted-answers type. Nothing to import except the idea.

## Users and use cases
- UC1 A curator creates a short-answer question as `Draft`: type chosen at creation, topic, content language, statement, optionally base text, the accepted answers, explanation, difficulty, origin edition.
- UC2 A curator edits it, `Draft` or `Published`, in place; its type does not change.
- UC3 A curator publishes a complete short-answer `Draft`.
- UC4 A curator unpublishes it and deletes a `Draft`, as for any question.
- UC5 A curator tells the six types apart in the list.
- UC6 A curator previews it as a student will see it (a text box), then reveals the accepted answers and the explanation.
- UC7 A curator changes the accepted answers of a published question and gives the reason, as F-98 asks for any key change.

## Business rules
- BR1 `QuestionType` gets `ShortAnswer`. A short-answer question is the same `Question` row with `Type = ShortAnswer`, no options, no `true_false_key` and no match items. Its key is one blank (F-107) with number 1 and no marker in the statement; the blank holds the accepted answers.
- BR2 The type is chosen at creation and never changes (F-97 BR2): `SaveQuestionRequest.Type` accepts `ShortAnswer` on create; on update it must be omitted or equal the stored type, else 409 `question.type_changed`.
- BR3 The key shape: a response always carries exactly one blank, number 1, with its accepted answers (empty on a draft with none yet). A request may omit `blanks` (a blank with no answer) or send one entry numbered 1. More than one entry is 400 `question.blanks_count`; one numbered other than 1 is 400 `question.blank_marker_missing`. Nothing is ignored.
- BR4 A short-answer `Draft` follows F-95 BR3 (statement, topic, content language) and may have no accepted answer yet. Broken data is refused even on a draft, with the F-107 codes and the position 1: more than 5 accepted answers (400 `question.blank_answers_count`), an accepted answer over 100 characters (400 `question.blank_answer_too_long`), an empty accepted answer (400 `question.blank_answer_required`) and two accepted answers equal under BR6 (400 `question.blank_answer_repeated`). The limits are those of F-107, unchanged (owner, confirmed 2026-10-24).
- BR5 `Published` needs, besides BR4, at least one accepted answer (400 `question.blank_answers_required`). Editing a published question runs the same check and refuses a change that breaks it.
- BR6 A typed answer matches an accepted answer under the rule of F-107 BR6, unchanged and not written again: trim, collapse white space, compare without regard to case; accents count. Grading a student's answer is the simulator's (E-5); this item stores the key and applies the rule to refuse repeated answers.
- BR7 Options, a true/false key or match items sent with a short-answer question are refused 400 (`question.options_not_allowed`, `question.key_not_allowed`, `question.match_items_not_allowed`), as for the other types. Blanks sent with any type but fill-in-the-blanks and short answer stay `question.blanks_not_allowed`. In a short-answer statement `[[n]]` is plain text, never a marker.
- BR8 Accepted answers are plain text, not HTML (F-107 BR8). The statement keeps every shared rule (sanitizing, length, images under F-103).
- BR9 The F-98 key-change check is unchanged and covers this type through its blank: a key change is a different set of accepted answers under BR6; changing the statement, the order of the answers or only their case or spacing is not. A version snapshots the blank like any other. Version 1 is never a key change; the other five types behave as before and their tests pass unedited.
- BR10 Everything the types share is one rule, not six: sanitizing and limits, content language, status, publish and unpublish, delete, the topic guard, origin and paper number, annulment, versions, review, base text, permission `questions.manage`, 404 answers (F-95 BR1 to BR16, F-96, F-97 BR5, F-98, F-101, F-102, F-103, F-105, F-106 BR9, F-107 BR11).
- BR11 The type is declared in one place per layer, as F-97 BR9 sets: the domain (`ShortAnswerKey` beside the others and one line in `AnswerKey.From`), the contract (`QuestionType`), the form (the key section for this type) and the preview. No `if` chain in `Question.Apply`.
- BR12 The list shows the type in the existing "Type" chip (F-97 BR8); filter by type stays in F-100.
- BR13 No scoring is stored or computed here: neither points nor a weight (epic E-6, decision of 2026-10-09, confirmed 2026-10-24). Shuffling is the simulator's (E-5).
- BR14 The student is told the task, not the answer: the preview says "Type your answer."; the box gives no hint of the answer's length (F-107 BR15).

## Screens and API
No mockup (owner, 2026-10-24): the form gains a sixth type card and one key section of kit rows, and the preview gains a text box; wireframes below.

- API (routes unchanged, `/api/v1/question-bank/questions`):
  - `SaveQuestionRequest.type` accepts `ShortAnswer` on create. The existing optional field `blanks` carries the key: for this type zero or one entry `{ number: 1, acceptedAnswers }`.
  - `QuestionResponse` and `QuestionVersionResponse` already carry `blanks`; for this type it is always one entry, number 1. The list response is unchanged (the type is already named); the excerpt is the plain statement.
  - Error codes: none new. Reused with position 1: `question.blanks_count`, `question.blank_marker_missing`, `question.blank_answers_required`, `question.blank_answers_count`, `question.blank_answer_required`, `question.blank_answer_too_long`, `question.blank_answer_repeated`, `question.options_not_allowed`, `question.key_not_allowed`, `question.match_items_not_allowed`, `question.type_changed`, `question.key_change_reason_required`, `question.image_*`. The codes say "blank"; the Web shows the short-answer text ("answer", not "blank") when the question is of this type.
  - Data: one migration in the `QuestionBank` context that only changes comments: `questions.type` and `question_versions.type` list six values, and the descriptions of `question_blanks` stop saying that a blank always has a marker. No column, table or index; existing rows keep their type, options, items and blanks; nothing is backfilled.
  - Limits: none new (`QuestionLimits.BlankAnswersMax` = 5, `BlankAnswerMaxLength` = 100).
- `/admin/questions/new`: the "Question type" `AppRadioCards` gets a sixth card, "Short answer" (description "A short typed answer, checked against accepted answers."), after "Fill in the blanks". Single choice stays preselected.
- `/admin/questions/{id}`: the type stays a read-only labelled value, now able to read "Short answer".
- Key section for `ShortAnswer` (replaces the "Options" card for this type, same position, same kit card): title "Accepted answers", subtitle "Write the answers the student may type. Case and extra spaces are ignored; accents count."; up to 5 answer fields (accessible name "Accepted answer {k}"), "Add answer" with the count "{0} of 5 answers", remove per answer. A new question starts with one empty field. The statement toolbar has no "Insert blank" button for this type.
  - Errors: the F-107 codes show the short-answer texts under the field of the answer (or under the section), as the blanks section does. A frozen question (annulled or in review) shows everything disabled, as the other types.
  - Going to another type and back keeps the typed answers (as F-104 AC12).
- Publish checklist (F-95): for `ShortAnswer` the item is "At least one accepted answer"; the aside shows the type.
- Preview dialog: the base text, the statement, then an inert text box labelled "Your answer" and the line "Type your answer."; after "show answer" the accepted answers are listed ("Accepted answers: answer, answer") with the check icon and the text "Correct answer" (never colour alone); a draft with no answer shows "no key yet".
- `/admin/questions`: the "Type" chip says "Short answer".
- The transcription screen of F-99 offers the sixth type card; picking it there shows the same key section.
- Permissions: unchanged, `questions.manage`.

### Wireframe: Question form, key section (short answer)
```text
+------------------------------------------------------------+
| Classification                                             |
|  Question type: ( ) Single choice  ( ) True/false          |
|   ( ) Multiple answer  ( ) Matching  ( ) Fill in the blanks|
|   (x) Short answer                                  [new]  |
+------------------------------------------------------------+
| Statement *                                                |
| [B][I][U][S][x2][x2][link][image]                          |
| +--------------------------------------------------------+ |
| | What is the capital of Brazil?                         | |
| +--------------------------------------------------------+ |
+------------------------------------------------------------+
| Accepted answers                                  [changed]|
| Write the answers the student may type. Case and extra     |
| spaces are ignored; accents count.                         |
|   [ Brasília ...................... ] x                    |
|   [ Brasilia ...................... ] x                    |
|   [+ Add answer]                     2 of 5 answers        |
|  ! Two answers are the same once case and spaces are       |
|    ignored. (error)                                        |
+------------------------------------------------------------+
| Before publishing: [v] At least one accepted answer        |
+------------------------------------------------------------+
```

### Wireframe: Preview dialog (short answer)
```text
+--------------------------------------------+
| Preview                                    |
| <base text>                                |
| What is the capital of Brazil?             |
| Your answer                                |
| [ ______________________________ ]  [new]  |
| Type your answer.                   [new]  |
| [ Show answer ]                            |
|  -- after reveal --                        |
| (v) Correct answer                         |
|  Accepted answers: Brasília, Brasilia      |
| Explanation: <text>                        |
+--------------------------------------------+
```

## Docs
- flow: new docs/flows/short-answer-questions.md — how a curator authors, publishes, versions and previews the type, why its key is one blank without a marker, and the comparison rule it shares with F-107.

## Acceptance criteria
- AC1 Given a curator with `questions.manage`, when they create a question with `type` `ShortAnswer`, a topic, a language and a statement and no `blanks`, then it is stored as `Draft` of that type, appears in the list with its type, and the response carries one blank numbered 1 with no accepted answer. (UC1, BR1, BR3, BR4)
- AC2 Given a short-answer draft, when it is saved with the answers "Brasília" and "Brasilia", then both are stored trimmed, in the order written, and "Brasilia" is accepted beside "Brasília". (UC1, BR4, BR6)
- AC3 Given a short-answer question, when it is saved with 6 answers, then 400 `question.blank_answers_count`; with an answer of 101 characters, 400 `question.blank_answer_too_long` (100 is accepted); with an empty or blank answer, 400 `question.blank_answer_required`; with "Brasília" and " brasília  ", 400 `question.blank_answer_repeated`; the position of each problem is 1 and nothing is stored. (BR4, BR6)
- AC4 Given a short-answer request, when it sends two blanks, then 400 `question.blanks_count`; when it sends one numbered 2, then 400 `question.blank_marker_missing`; nothing is stored. (BR3)
- AC5 Given a short-answer draft, when it is published with no accepted answer, then 400 `question.blank_answers_required`; with at least one, it becomes `Published`. (UC3, BR5)
- AC6 Given a published short-answer question, when an edit removes every answer, then it is refused and the stored question is unchanged; when the curator writes another valid set, it stays `Published`. (UC2, BR5)
- AC7 Given a `ShortAnswer` request that sends options, a `trueFalseKey` or `matchItems`, then 400 with `question.options_not_allowed`, `question.key_not_allowed` or `question.match_items_not_allowed` and nothing is stored; given a request of single-choice, true/false, multiple-answer or matching type that sends `blanks`, then 400 `question.blanks_not_allowed`. (BR7)
- AC8 Given a stored short-answer question, when an update names another type, then 409 `question.type_changed` and nothing changes; naming its own type is accepted. (BR2)
- AC9 Given a short-answer statement that contains the text `[[1]]`, when it is saved, then the stored statement keeps the text, the list excerpt shows it as typed (not `____`) and no `question.blank_*` error is raised. (BR7)
- AC10 Given a published short-answer question with version 1 accepting "Brasília", when the curator adds "Brasilia" without a reason, then 400 `question.key_change_reason_required`; with a reason, version 2 is recorded with `answerKeyChanged` true, the reason and its `blanks`. (UC7, BR9)
- AC11 Given the same question, when only the statement changes, the answers are reordered or change only in case or spacing, then version 2 is recorded with `answerKeyChanged` false and no reason is required; the F-98, F-104, F-106 and F-107 version suites pass unedited. (BR9)
- AC12 Given a short-answer question, then the shared rules hold for it: statement sanitized and limited, images counted under the F-103 cap, difficulty 0 or 6 refused, a taken paper number refused, a published one cannot be deleted, a topic with only this type is `topic.in_use`, annulment and reinstatement keep its blank, the review flow works, and a base text linked to a published question of this type records a version that keeps its blank. (BR8, BR10)
- AC13 Given questions of the six types, then the list shows each row's type and the response names it; `QuestionResponse` and `QuestionVersionResponse` of a short-answer question carry one blank. (UC5, BR12)
- AC14 Given the form for a new question, when the curator picks "Short answer", then the Options card is replaced by "Accepted answers" with one empty field, no "Insert blank" button shows, "Add answer" adds up to 5 fields and a removal drops one, saving sends `blanks` with number 1 and the trimmed non-empty answers, publishing works, picking another type and back keeps the typed answers; on an existing question the type is read-only. (UC1, UC2, BR1, BR11)
- AC15 Given a short-answer question, when the curator opens the preview, then it shows the statement, an inert text box labelled "Your answer" and the line "Type your answer."; the accepted answers and the explanation stay hidden until "show answer", then the answers are listed with icon and text. (UC6, BR14)
- AC16 Given the migration, then existing rows and versions keep their type, options, items and blanks, and the comments of the `type` columns list the six values. (BR1)
- AC17 Given a user without `questions.manage`, then create and update of this type answer 403. (BR10)
- AC18 The key section is operable by keyboard (each answer field, its remove action and "Add answer" one tab stop), each control has an accessible name with the answer's number, the section errors are announced and the preview box is named "Your answer" (WCAG 2.2 AA); checked on screen.
- AC19 Given the transcription screen of an edition, then it offers the six types and the key section works there. (F-99 BR5)
- AC20 All new texts appear in pt-BR, pt-PT and en, including the short-answer texts of the reused error codes.

## Criterion → test

Tests locked: a1a875bde93430590b2f2329c6ba90ab4f1cf073 (3 files); unlocks: 1


| Criterion | Test |
|---|---|
| AC1 | ShortAnswerQuestionTests.Create_WithNoBlanks_IsADraftOfThatType_WithOneEmptyBlankNumbered1; ShortAnswerQuestionEndpointTests.Create_WithNoBlanks_IsStoredAsADraftOfThatType_WithOneEmptyBlank_AndListedWithIt |
| AC2 | ShortAnswerQuestionTests.Create_WithBrasiliaAndBrasilia_StoresBothTrimmedInOrder; ShortAnswerQuestionEndpointTests.Create_WithBrasiliaAndBrasilia_StoresBothTrimmedInOrder |
| AC3 | ShortAnswerQuestionTests.Create_WithSixAnswers_IsRefusedWithBlankAnswersCount_AtPosition1 and 3 more; ShortAnswerQuestionEndpointTests.Create_BreakingTheLimits_IsRefusedWithPosition1_AndNothingIsStored; ShortAnswerQuestionEndpointTests.Create_WithAnAnswerOf100Characters_IsAccepted |
| AC4 | ShortAnswerQuestionTests.Create_WithTwoBlanks_IsRefusedWithBlanksCount; ShortAnswerQuestionTests.Create_WithABlankNumbered2_IsRefusedWithBlankMarkerMissing; ShortAnswerQuestionEndpointTests.Create_WithTwoBlanksOrABlankNumbered2_IsRefused_AndNothingIsStored |
| AC5 | ShortAnswerQuestionTests.Publish_WithNoAnswer_IsRefusedWithBlankAnswersRequired_AndWithOne_BecomesPublished; ShortAnswerQuestionEndpointTests.Publish_WithNoAnswer_IsRefused_AndWithOne_IsPublished |
| AC6 | ShortAnswerQuestionTests.Update_Published_RemovingEveryAnswer_IsRefused_AndNothingChanges; ShortAnswerQuestionEndpointTests.Update_Published_RemovingEveryAnswer_IsRefused_AndTheStoredQuestionIsUnchanged_AndAnotherValidSetStaysPublished |
| AC7 | ShortAnswerQuestionTests.Create_WithOptions_IsRefusedWithOptionsNotAllowed and 2 more; ShortAnswerQuestionEndpointTests.Create_WithOptionsAKeyOrMatchItems_IsRefused_AndNothingIsStored; other types with blanks: FillInTheBlanksQuestionEndpointTests.Create_AnotherTypeWithBlanks_IsRefusedWithBlanksNotAllowed (F-107, unchanged) |
| AC8 | ShortAnswerQuestionTests.Update_WithContentOfAnotherType_IsAConflict; ShortAnswerQuestionEndpointTests.Update_NamingAnotherType_Is409TypeChanged_AndNamingItsOwnIsAccepted |
| AC9 | ShortAnswerQuestionTests.Create_WithStatementTextThatLooksLikeAMarker_KeepsItAsText_AndStoresOneBlank; ShortAnswerQuestionEndpointTests.Create_WithStatementTextThatLooksLikeAMarker_KeepsItAsTyped_InTheStatementAndTheExcerpt |
| AC10 | ShortAnswerQuestionTests.NextVersion_AddingAnAcceptedAnswer_NeedsAReason_AndThenRecordsAKeyChangeWithTheBlank; ShortAnswerQuestionEndpointTests.Update_Published_AddingAnAcceptedAnswer_NeedsAReason_AndThenRecordsAKeyChange |
| AC11 | ShortAnswerQuestionTests.NextVersion_ChangingOnlyTheStatement_IsAVersionWithNoKeyChange and 2 more; ShortAnswerQuestionEndpointTests.Update_Published_ChangingOnlyTheStatement_OrTheOrderAndCaseOfTheAnswers_RecordsAVersionWithNoKeyChange |
| AC12 | ShortAnswerQuestionEndpointTests: Create_SanitizesTheStatement, Create_WithElevenImagesInTheStatement_IsRefusedWithImagesTooMany, Create_WithADifficultyOutOfRange_IsRefused, Create_WithAPaperNumberAlreadyTaken_IsRefusedWithPaperNumberTaken, Delete_Published_IsRefused_AndADraftIsDeleted, DeleteTopic_WithOnlyAShortAnswerQuestion_IsRefusedWithTopicInUse, AnnulAndReinstate_KeepTheBlankInTheirVersions, Submit_CompleteDraft_GoesToReview_AndAnIncompleteOneIsRefused, UpdateBaseText_LinkedToAPublishedShortAnswer_RecordsAVersionThatKeepsTheBlank; ShortAnswerQuestionTests.AnnulAndReinstate_CopyTheBlankOfTheLatestVersion |
| AC13 | ShortAnswerQuestionEndpointTests.List_QuestionsOfTheSixTypes_NameTheTypeOfEachRow_AndOnlyFillInAndShortAnswerCarryBlanks; ShortAnswerQuestionPreviewAndListTests.List_NamesTheShortAnswerType |
| AC14 | ShortAnswerQuestionFormTests (Web): Add_OffersTheSixthTypeCard_WithSingleChoiceStillPreselected, Add_ChoosingShortAnswer_ShowsAcceptedAnswersWithOneEmptyField_AndNoInsertBlank, Add_ShortAnswer_AddAnswerGoesUpToFive_AndARemovalDropsOne, Add_ShortAnswer_SavesTheTypeAndOneBlankNumbered1, Add_GoingToAnotherTypeAndBack_KeepsTheTypedAnswers, Edit_ShortAnswer_ShowsTheTypeAsAReadOnlyValue_AndTheStoredAnswers, Edit_ShortAnswer_ChecklistIsDoneOnlyWithAnAcceptedAnswer, Edit_ShortAnswer_WhenTheApiRefusesTheAnswers_TheShortAnswerTextIsUnderTheSection, Edit_PublishedShortAnswer_AddingAnAnswer_AsksForTheReason, Edit_PublishedShortAnswer_ChangingOnlyTheCaseOfAnAnswer_DoesNotAskForTheReason, Edit_AnnulledShortAnswer_ShowsTheAnswersDisabled_AndNoAddButton |
| AC15 | ShortAnswerQuestionPreviewAndListTests (Web): Preview_ShortAnswer_ShowsTheStatement_AnInertBoxNamedYourAnswer_AndTheTaskLine, Preview_ShortAnswer_BeforeShowAnswer_HidesTheAnswersAndTheExplanation, Preview_ShortAnswer_ShowAnswer_ListsTheAcceptedAnswersWithIconAndText, Preview_ShortAnswerDraftWithNoAnswer_SaysItHasNoKeyYet |
| AC16 | AddShortAnswerQuestionsMigrationTests.Up_NamesTheSixthType_ChangesOnlyComments_AndKeepsEveryRow |
| AC17 | ShortAnswerQuestionEndpointTests.CreateAndUpdate_WithoutThePermission_AnswerForbidden |
| AC18 | validation step |
| AC19 | QuestionTranscriptionTests.Open_OffersEveryTypeTheFormImplements (six cards); the key section is the form's own, tested by ShortAnswerQuestionFormTests |
| AC20 | ShortAnswerQuestionResourcesTests.EveryNewText_ExistsInTheThreeLanguagesWithTheItemsWords; ResourceParityTests; ErrorCodeTextTests |

## Decisions
- 2026-10-24 — F-107 (fill-in-the-blanks, done) also compares a typed answer with accepted answers; both items stay: the shapes differ (blanks inside the statement against one answer slot) and F-108 reuses the rule and the storage F-107 built — confirmed: F-107
- 2026-10-24 — The comparison rule is the F-107 one: case and extra spaces ignored, accents count — confirmed: F-107 (BR6; owner said Yes)
- 2026-10-24 — Limits: 1 to 5 accepted answers (1 to publish), each up to 100 characters — confirmed: F-107 (owner said Yes)
- 2026-10-24 — No scoring, weight or points on the question; the edition's scoring rule (E-6) decides — confirmed: docs/epics/question-bank.md, F-107 (owner said Yes)
- 2026-10-24 — The type is chosen at creation and fixed after the first save — confirmed: F-97, F-106, F-107 (owner said Yes)
- 2026-10-24 — Lenient matching (ignore punctuation, number formats such as 3,14 and 3.14, typo tolerance) is out of scope and no idea is captured: the curator lists up to 5 variants, and no demand is known — asked (recommended option, owner)
- 2026-10-24 — No mockup and no `/agile:screen`: a type card, one key section of kit rows and a text box in the preview; wireframes in this file — asked (recommended option, owner)
- 2026-10-24 — Taken from rules, not asked: the transcription screen offers the type as soon as it exists, and `QuestionTranscriptionTests.Open_OffersEveryTypeTheFormImplements` goes from 5 to 6 (F-99 BR5, F-106, F-107); shuffling is the simulator's (E-5); accepted answers are plain text (F-107 BR8); the preview gives no hint of the answer's length (F-107 BR15) — confirmed: F-107
- 2026-10-24 — The key is stored as one blank numbered 1 with no marker, and the contract reuses `blanks` (one entry) — the key-change check, version snapshots, annul and reinstate copies and the base-text fan-out already handle blanks for every type, so the item adds no table, column, route or response field; trade-off: the simulator (E-5) reads `blanks[0]` for this type, and the error codes say "blank" while the Web shows "answer" — Claude, from the code read
- 2026-10-24 — The key is a domain unit `ShortAnswerKey` beside the others, with one line in `AnswerKey.From`; it refuses options, a true/false key and match items through the trailing-parameter pattern of F-106 and F-107, and shares the answer checks with `FillInTheBlanksKey` (one place for the limits) — Claude
- 2026-10-24 — The response always carries the blank, even with no answer, as F-107 stores one row per marker including empty answer lists; the request may omit `blanks` — Claude
- 2026-10-24 — The migration changes comments only (the type columns, the `question_blanks` descriptions); the model snapshot changes with it — Claude
- 2026-10-24 — The glossary row `Blank` is widened at the build to cover the single slot of a short answer; no new business term (`ShortAnswer` and `AcceptedAnswer` exist) — Claude
- 2026-10-24 — Statement text `[[n]]` is plain text for this type because the sanitizer reads markers only for fill-in-the-blanks — Claude, from `SaveQuestionHandler.cs:132`
- 2026-10-24 — No new package — the item needs nothing the solution lacks — Claude
- 2026-10-24 — No security section: no door (no upload, permission change, personal data or authentication); the routes and the permission are unchanged — Claude
- 2026-10-24 — Size S, fits one session: one domain unit, one enum value, a comment-only migration, one card, one key section, one preview change — Claude, from the code read
- 2026-10-24 — Approved by the owner ("aprovo F-108"); every dependency delivered (`deps.js block`: FREE); main session model: sonnet.
- 2026-10-24 — Reuse: `AnswerKey`, `AcceptedAnswer`, `QuestionBlank`, `NextVersion`, `CopyAs`, the F-107 form row logic and `FillInTheBlanksKeySection` rows (extract what is shared, do not copy), kit `AppSectionCard`, `AppTextField`, `AppRowActions`, `AppRadioCards`, the F-107 endpoint, migration and form test shapes — Claude

- 2026-10-24: plan — 1 `ShortAnswer` enum value, `ShortAnswerKey` (answer checks shared with `FillInTheBlanksKey`), one line in `AnswerKey.From`, comment-only migration → AC1-AC9, AC16 (domain, endpoint and migration tests); 2 key change, versions, shared rules, list and permission over HTTP → AC10-AC13, AC17; 3 form: sixth type card, `ShortAnswerKeySection`, publish checklist, errors, resources in 3 locales → AC14, AC19, AC20; 4 preview text box and reveal, list chip text → AC15; 5 glossary, flow page, manual, validation script → AC18 (on screen), `techdocs.js check`.
- 2026-10-24: tests unlocked (owner yes) — three bugs of the test author, no criterion changed: a tuple holding a list compared by reference in two tests, and the seed JSON of `question_versions.blanks` written camelCase where EF's `ToJson` reads PascalCase; fixed and locked again. Two guard theories ("another type with blanks") were dropped before the first lock: they passed without code and F-107 tests already cover them.
- 2026-10-24: Reuse: `FillInTheBlanksKey.CheckAnswers` and `Fault` (made `internal` and called by `ShortAnswerKey`, one place for the limits), `AcceptedAnswer.Normalize`, `QuestionBlank` rows and `question_versions.blanks`, `NextVersion`/`CopyAs`/base-text fan-out unchanged, kit `AppSectionCard`, `AppTextField`, `AppRowActions`, the `app-blank-answer` and `app-options-*` styles, and the F-107 test shapes. The key section is its own small component (`ShortAnswerKeySection`, one list instead of one list per marker), so nothing of `FillInTheBlanksKeySection` was copied.
- 2026-10-24: Shortcuts: none.
- 2026-10-24: `/agile:review` not run: no door (routes, permission and tables unchanged), the migration changes comments only. Scan GREEN (two older medium `unsafe-path-combine` findings in `tools/Simulab.DocGen`, outside this diff). Screen check on the app host: started on `simulab_f108`, the sign-in page and the sign-in of the development admin worked, then the page "Our documents changed" asks to accept the legal documents; Claude does not accept terms, so the signed-in screen is covered by the validation script and by bUnit (2560 Web tests green).
- 2026-10-24: the error texts that differ for a short answer (`blank_answers_required`, `blank_answers_count`, `blank_answer_repeated`) are resource keys `Questions.ShortAnswer.Error.*` chosen in `QuestionForm.BlankErrorText`; they are not named like error codes because `ErrorCodeTextTests` treats any `question.*` key as a code.

## Out of scope
- Lenient matching: ignoring punctuation, number formats, typo tolerance (owner, 2026-10-24).
- Scoring: points, weight, partial credit — epic E-6 (owner, 2026-10-09).
- Grading a student's answer — E-5, the simulator (BR6 defines the rule it will use).
- Open-answer and essay with a rubric — F-109.
- Regular expressions as accepted answers.
- Filter by type — F-100. Changing the type of an existing question.
- Shuffling — E-5.

## Open questions
- (none)

## Change notes

## Validation script

### English
Needed to validate: the local app host — the owner; not started yet, step 1 starts it from this worktree on its own database.

1. Stop any app host you have open (other worktrees share its ports), then start this worktree's app host on its own database `simulab_f108` (the item's migration, comments only, lands only there), from the worktree folder `D:\wt\simulab\f-108-short-answer`:
   Git Bash:
   ```bash
   Database__Name=simulab_f108 dotnet run --project src/Hosts/Simulab.AppHost
   ```
   PowerShell 7:
   ```powershell
   $env:Database__Name = "simulab_f108"; dotnet run --project src/Hosts/Simulab.AppHost
   ```
   Expected: the line `Dashboard:` with a link; the site answers at https://localhost:7125. To repeat, press Ctrl+C and run the same command.
2. [ ] Open https://localhost:7125 and sign in as `admin@simulab.local` (the development password of `appsettings.Development.json`); if the page "Our documents changed" appears, click "Accept and continue". The database `simulab_f108` is new: if the subject field of step 3.2 finds nothing, first add a subject with one topic in the sidebar (menu lateral esquerdo) at Content > Subjects.
3. Create a short-answer draft:
   - [ ] 3.1 In the sidebar click Content > Questions, then Add. In "Question type" click the type card (cartão de tipo: each card of the "Question type" field) "Short answer": the Options card is replaced by the card "Accepted answers" (cartão de respostas aceitas) with one empty field and "1 of 5 answers", and the Statement toolbar has no Insert blank button.
   - [ ] 3.2 Pick a subject and a topic. In Statement type "What is the capital of Brazil? Type [[1]] as shown.". In the first answer field type Brasília, click "Add answer" and type Brasilia ("2 of 5 answers").
   - [ ] 3.3 Click Save: the page becomes the edit page, the type reads "Short answer", not editable; the text [[1]] stays in the statement; the checklist "Before publishing" (lista "Before publishing" no painel lateral) shows "At least one accepted answer" done and Publish is enabled.
4. Rules of the answers:
   - [ ] 4.1 Click "Add answer" and type " brasília " (spaces around, lower case), then Save: under the answers the message "Two answers are the same once case and extra spaces are ignored." appears. Remove that answer with its remove button.
   - [ ] 4.2 Remove both remaining answers: the checklist shows "At least one accepted answer" pending and Publish is disabled. Type Brasília and Brasilia again.
   - [ ] 4.3 Click Save, then Publish: the status becomes Published.
5. Preview:
   - [ ] 5.1 In the Publication card click "Preview as a student": the statement, then a disabled empty box "Your answer" (no answer, no hint of its length) and the line "Type your answer.".
   - [ ] 5.2 Click "Show answer": the check icon with "Correct answer" and "Accepted answers: Brasília, Brasilia", and the explanation. The box stays empty. Close the dialog.
6. Answer key change:
   - [ ] 6.1 Change "Brasilia" to "BRASILIA" (case only): no reason field appears.
   - [ ] 6.2 Click "Add answer" and type "Capital Federal": the field "Reason for the answer key change" appears. Write a reason and Save: the History card lists the new version with "Answer key changed" and your reason; View on version 1 shows only "Brasília, Brasilia".
7. [ ] Language: in the app bar (barra superior) click the globe and pick Português (Brasil): the type card reads "Resposta curta", the section "Respostas aceitas", the checklist "Pelo menos uma resposta aceita" and the preview line "Digite sua resposta.". Switch back to English.
8. List, keyboard and transcription:
   - [ ] 8.1 In Content > Questions the row of your question shows the chip "Short answer".
   - [ ] 8.2 Without the mouse, on Questions > Add pick the Short answer card with the arrows; Tab through each answer field, its remove action and "Add answer": each takes one Tab stop and the screen reader names "Accepted answer 1".
   - [ ] 8.3 In the sidebar click Content > Exams, open the exam and then an edition, and on its Questions card click Transcribe: the "Question type" field offers six cards; pick Short answer and the "Accepted answers" card appears (AC19).

### pt-BR
Necessário para validar: o app host local — o dono; ainda não iniciado, o passo 1 sobe a partir deste worktree no banco próprio.

1. Pare qualquer app host aberto (outros worktrees usam as mesmas portas) e suba o app host deste worktree no banco próprio `simulab_f108` (a migração do item, só de comentários, cai só nele), a partir da pasta do worktree `D:\wt\simulab\f-108-short-answer`:
   Git Bash:
   ```bash
   Database__Name=simulab_f108 dotnet run --project src/Hosts/Simulab.AppHost
   ```
   PowerShell 7:
   ```powershell
   $env:Database__Name = "simulab_f108"; dotnet run --project src/Hosts/Simulab.AppHost
   ```
   Esperado: a linha `Dashboard:` com um link; o site responde em https://localhost:7125. Para repetir, Ctrl+C e o mesmo comando.
2. [ ] Abra https://localhost:7125 e entre como `admin@simulab.local` (a senha de desenvolvimento de `appsettings.Development.json`); se aparecer a página "Our documents changed", clique em "Accept and continue". O banco `simulab_f108` é novo: se o campo de disciplina do passo 3.2 não achar nada, antes cadastre uma disciplina com um tópico no menu lateral (sidebar, menu lateral esquerdo) em Content > Subjects.
3. Crie um rascunho de resposta curta:
   - [ ] 3.1 No menu lateral clique em Content > Questions e depois em Add. Em "Question type" clique no cartão de tipo (cada cartão do campo "Question type") "Short answer": o cartão Options é trocado pelo cartão "Accepted answers" (cartão de respostas aceitas) com um campo vazio e "1 of 5 answers", e a barra do Statement não tem o botão Insert blank.
   - [ ] 3.2 Escolha disciplina e tópico. Em Statement digite "What is the capital of Brazil? Type [[1]] as shown.". No primeiro campo de resposta digite Brasília, clique em "Add answer" e digite Brasilia ("2 of 5 answers").
   - [ ] 3.3 Clique em Save: a página vira a de edição, o tipo aparece como "Short answer", sem edição; o texto [[1]] continua no enunciado; a lista "Before publishing" (lista "Before publishing" no painel lateral) mostra "At least one accepted answer" concluído e Publish fica habilitado.
4. Regras das respostas:
   - [ ] 4.1 Clique em "Add answer" e digite " brasília " (com espaços, minúscula) e depois Save: sob as respostas aparece "Two answers are the same once case and extra spaces are ignored.". Remova essa resposta com o botão de remover dela.
   - [ ] 4.2 Remova as duas respostas restantes: a lista mostra "At least one accepted answer" pendente e Publish fica desabilitado. Digite Brasília e Brasilia de novo.
   - [ ] 4.3 Clique em Save e depois em Publish: o status vira Published.
5. Pré-visualização:
   - [ ] 5.1 No cartão Publication clique em "Preview as a student": o enunciado, depois uma caixa vazia e desabilitada "Your answer" (sem resposta e sem pista do tamanho) e a linha "Type your answer.".
   - [ ] 5.2 Clique em "Show answer": o ícone com "Correct answer" e "Accepted answers: Brasília, Brasilia", e a explicação. A caixa continua vazia. Feche o diálogo.
6. Troca de gabarito:
   - [ ] 6.1 Troque "Brasilia" por "BRASILIA" (só maiúsculas): não aparece campo de motivo.
   - [ ] 6.2 Clique em "Add answer" e digite "Capital Federal": aparece o campo "Reason for the answer key change". Escreva um motivo e clique em Save: o cartão History lista a nova versão com "Answer key changed" e o seu motivo; View na versão 1 mostra só "Brasília, Brasilia".
7. [ ] Idioma: na barra superior (app bar, a faixa no alto das páginas) clique no globo e escolha Português (Brasil): o cartão de tipo mostra "Resposta curta", a seção "Respostas aceitas", a lista "Pelo menos uma resposta aceita" e a linha da pré-visualização "Digite sua resposta.". Volte para English.
8. Lista, teclado e transcrição:
   - [ ] 8.1 Em Content > Questions a linha da sua questão mostra o chip "Short answer".
   - [ ] 8.2 Sem o mouse, em Questions > Add escolha o cartão Short answer com as setas; com Tab percorra cada campo de resposta, sua ação de remover e "Add answer": cada um é uma parada de Tab e o leitor de tela diz "Accepted answer 1".
   - [ ] 8.3 No menu lateral clique em Content > Exams, abra o concurso e depois uma edição e, no cartão Questions dela, clique em Transcribe: o campo "Question type" oferece seis cartões; escolha Short answer e aparece o cartão "Accepted answers" (AC19).

## Events

- 2026-10-24 question — asked 2, from context 4
- 2026-10-24 blocked — test wrong: three test bugs of the test author: tuple-with-list reference equality (2) and the seed JSON property name of question_versions.blanks (1); no criterion changes
