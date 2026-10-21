---
feature: F-106
epic: Question bank
status: building
Autopilot: approved
board: 152
version: 1
---
# Matching questions

Technical terms: [glossary](../glossary.md)

## Summary
The matching question type: two columns and the pairs that form the answer key. The left column is numbered, the right column is lettered, and the key says which letter each number matches (one to one; the right column may have extra items that match nothing). The question stores only its key; how a partly right answer scores is the edition's scoring rule (epic E-6), never a field of the question. Source: epic Question bank (E-4); Simulae `ConfigurationBase.cs` (T4, `ConfiguracaoAssociacao`) as the import source, without its cardinality modes and its correction mode.

## Start
- Depends on: F-95 (done), F-97 (done: the type pattern), F-98 (done: key versioning), F-103 (done: image rules).
- Waits on (to start): nothing.
- Needed to validate: the local app host — owner.
- Model: opus (public API contract: the question request and responses gain the left items; the schema gains a table).
- Suggested path: `/agile:build` (no mockup, owner 2026-10-21; the screens are described below with wireframes).
- Parallel with: F-107, F-108. All three edit `QuestionForm.razor`, `QuestionPreviewContent.razor` and the three `SharedResources` files: expect conflicts and bring the branch up to date often.

## Goal
A curator can author, publish, list, preview and version a matching question, and the module gains it by adding one answer-key unit beside the three it has, not by branching through the other types.

## What exists (verified 2026-10-21)
- `QuestionType` has `SingleChoice`, `TrueFalse` and `MultipleAnswer` (`QuestionType.cs`); `questions.type` is text, so a new value needs no data conversion; only the column comments list the values (`EnumColumnCommentTests`). The glossary already lists `Matching` ("associação").
- The answer key is a domain unit per type: `AnswerKey` (abstract record) with `SingleChoiceKey`, `TrueFalseKey` and `MultipleAnswerKey`, and one `switch` in `AnswerKey.From` (`AnswerKey.cs:33`). `Question.Apply` and `Question.Publish` hold no per-type branch.
- The options (`QuestionOption`: position, `TextHtml`, `IsCorrect`) are the only list a question stores; a left-to-right pair does not fit it. The versions snapshot their options as a JSON column (`QuestionVersionConfiguration.cs:99`), so a new list on a version is not a new table.
- F-98 key-change check (`Question.NextVersion`, `Question.cs:139-142`): it compares the true/false key and the set of correct option positions. For a matching question that set is empty on both sides, so it would never see a key change; it must compare the matches (BR8).
- `QuestionRules.CheckEachOption` (option length and, to publish, non-empty text) is the shared helper the two option-based keys use (`QuestionRules.cs:90`); `QuestionLimits.OptionsMax` is 5 and `MultipleAnswerOptionsMin` is 3.
- `QuestionImageRules.Check` takes the statement and the option texts together and caps images at ten per question (`Question.cs:487`, F-103 BR7).
- `SaveQuestionRequest` carries `Options` and `TrueFalseKey` (`SaveQuestionRequest.cs:27`); `QuestionResponse` carries `Options` and `TrueFalseKey`; `QuestionVersionResponse` carries `Options`.
- The form draws the type as `AppRadioCards` (`TypeCards`, `QuestionForm.razor:929`), checks `IsTrueFalse` and `IsMultiple` to draw the key section (`:925`, `:927`); `QuestionTranscriptionTests.Open_OffersEveryTypeTheFormImplements` (`tests/Hosts/Simulab.Web.Tests/QuestionBank/QuestionTranscriptionTests.cs:151`) counts the types the transcription offers.
- Items that touched this code since F-104: F-103 (images in options), F-105 (review: `InReview` freezes the question like annulment), F-102 (base text). Their rules apply to this type unchanged.
- Simulae `ConfiguracaoAssociacao` (`ConfigurationBase.cs:106`, source read 2026-10-21; the path in `CLAUDE.md` is stale, the source is `D:\dev\_control-alt-systems\simulae\repo`): columns A and B of 2 to 8 items each, at least one pair in the key, three cardinalities (`UmParaUm`, `NParaUm`, `Livre`) and a correction mode per question. Defects not to import: the correction mode (scoring belongs to E-6), the other two cardinalities, a stored list of pairs beside the items (two copies of the key), and a key that need not cover every left item.

## Users and use cases
- UC1 A curator creates a matching question as `Draft`: type chosen at creation, topic, content language, statement, optionally base text, left items, right items, the match of each left item, explanation, difficulty, origin edition.
- UC2 A curator edits it, `Draft` or `Published`, in place; its type does not change.
- UC3 A curator publishes a complete matching `Draft`.
- UC4 A curator unpublishes it and deletes a `Draft`, as for any question.
- UC5 A curator tells the four types apart in the list.
- UC6 A curator previews it as a student will see it (numbered left items, lettered right items), then reveals the key and the explanation.
- UC7 A curator changes which letter a number matches in a published question and gives the reason, as F-98 asks for any key change.

## Business rules
- BR1 `QuestionType` gets `Matching`. A matching question is the same `Question` row with `Type = Matching`. Its right column is its `options` (lettered by position, `IsCorrect` always false), its left column is a new list of match items (numbered by position), and each match item names the position of the right item it matches. There is no `true_false_key`, and no pair list is stored beside the items: the match of an item is the only copy of the key.
- BR2 The type is chosen at creation and never changes (F-97 BR2): `SaveQuestionRequest.Type` accepts `Matching` on create; on update it must be omitted or equal the stored type, else 409 `question.type_changed`.
- BR3 A matching `Draft` follows F-95 BR3: statement, topic and content language; from 0 to 8 left items and from 0 to 8 right items, each within the option length limit; a match may be unset for any left item. A match that names a right item that does not exist, or the same right item as another left item (one to one), is refused 400 `question.matching_key_invalid` / `question.matching_key_repeated` even on a draft: they are broken data, not unfinished data.
- BR4 `Published` needs, besides BR3: from 2 to 8 left items and from as many right items as left items up to 8, each with non-empty text, and every left item with a match. Counts outside those limits are 400 `question.matching_items_count`; a left item with no match, or a match naming a right item that does not exist, is 400 `question.matching_key_invalid`; an empty right item text is `question.option_text_required` (existing) and an empty left item text is `question.match_item_text_required`. Editing a published question runs the same check and refuses a change that breaks it. Right items that no left item matches are allowed (extra items).
- BR5 A `TrueFalse` key sent with a matching question is refused 400 `question.key_not_allowed`, as for the other option types; nothing is ignored. A `Matching` request that marks a right item `isCorrect` is refused the same way (`question.key_not_allowed`).
- BR6 Match items of any other type are refused 400 `question.match_items_not_allowed`: a single-choice, true/false or multiple-answer request that carries them is not silently ignored.
- BR7 Left and right items are rich text like options: sanitized HTML, at most `QuestionLimits.OptionMaxLength` characters each, and images allowed under the F-103 rule: the statement, the options (right items) and the left items together hold at most ten images, each with its alternative text (F-103 BR4, BR7).
- BR8 The F-98 key-change check compares the match of every left item, for every type: a matching question is a key change when the number of left items or the right position any left item matches differs from the latest version's; changing only the text of an item is not. A matching question's version snapshots its options and its match items (the match items as a JSON column of the version, like the options). Version 1 is never a key change; the other three types behave as before and their tests pass unedited.
- BR9 Everything the types share is one rule, not four: sanitizing and limits, content language, status, publish and unpublish, delete, the topic guard, origin and paper number, annulment, versions, review, permission `questions.manage`, 404 answers (F-95 BR1 to BR16, F-96, F-97 BR5, F-98, F-101, F-102, F-103, F-105).
- BR10 The type is declared in one place per layer, as F-97 BR9 sets: the domain (`MatchingKey` beside the others, one line in `AnswerKey.From`, one new member `MatchItems` on `AnswerKey`, empty for the other types), the contract (`QuestionType`), the form (the key section draws the two lists for this type) and the preview. No `if` chain in `Question.Apply`.
- BR11 The list shows the type in the existing "Type" chip (F-97 BR8); filter by type stays in F-100.
- BR12 No scoring is stored or computed here: neither "all or nothing" nor partial credit nor a penalty (epic E-6, decision of 2026-10-09). No cardinality field: the key is one to one by construction.
- BR13 The student is told the task, not the answer: the preview says "Match each number with one letter."; nothing about how many letters are extra.

## Screens and API
No mockup (owner, 2026-10-21): the form gains a fourth type card and one new section made of kit components (two lists and a select per left row), and the preview gains one block; wireframes below.

- API (routes unchanged, `/api/v1/question-bank/questions`):
  - `SaveQuestionRequest.type` accepts `Matching` on create. New optional field `matchItems`: a list of `{ textHtml, matchPosition }`, `matchPosition` the 0-based position of the right item (an option) the left item matches, or null when not set yet. The right items travel in `options` as today (`isCorrect` false or omitted).
  - `QuestionResponse` gains `matchItems` (same shape, `textHtml` and `matchPosition`, empty for the other types) and `QuestionVersionResponse` gains it too (empty for the other types and for versions recorded before this item). The list response is unchanged.
  - Error codes (new): `question.matching_items_count` (400), `question.matching_key_invalid` (400), `question.matching_key_repeated` (400), `question.match_item_text_required` (400), `question.match_item_too_long` (400), `question.match_items_not_allowed` (400). Reused: `question.option_text_required`, `question.option_too_long`, `question.key_not_allowed`, `question.type_changed`, `question.key_change_reason_required`, `question.image_*`.
  - Data: one migration in the `QuestionBank` context: new table `question_match_items` (question id, position, `text_html`, nullable `match_position`, no unique index over (question, position) as `question_options` has none, an index on the question), a nullable JSON column `match_items` on `question_versions` (beside the `options` one), and the comments of `questions.type`, `question_versions.type` and `question_options.is_correct` are updated. Existing rows keep their type and options; nothing is backfilled. Unique indexes over nullable tenant columns: none added.
  - Limits (contracts): `QuestionLimits.MatchingItemsMin` = 2 and `MatchingItemsMax` = 8; the option cap of 5 stays for the other types.
- `/admin/questions/new` — the "Question type" `AppRadioCards` gets a fourth card, "Matching" (description "Two columns: match each numbered item with a lettered one."), after "Multiple answer". Single choice stays preselected.
- `/admin/questions/{id}` — the type stays a read-only labelled value, now able to read "Matching".
- Key section for `Matching` (replaces the "Options" card for this type, same position, same kit card): subtitle "2 to 8 items on the left. The right column needs at least as many items; extra items match nothing. Pick the letter each number matches."; two lists in one card on wide screens, stacked on narrow ones.
  - Left list "Items" numbered 1, 2, ...: each row has the item text field (accessible name "Text of item {n}"), an `AppSelect` "Matches" listing the letters of the right column that are not taken by another row, empty by default (accessible name "Letter matched by item {n}"), move up, move down and remove, and "Add item" with the count "{0} of 8 items".
  - Right list "Matches" lettered A, B, ...: each row has the text field (accessible name "Text of match {letter}"), a note "Used by item {n}" or "Extra" derived from the left rows, and the same move and remove controls; "Add match" with the count "{0} of 8 matches". Removing a right row that a left row uses clears that row's match; moving a right row keeps each match attached to its item (the letter follows the text).
  - Section errors `question.matching_items_count`, `question.matching_key_invalid` and `question.matching_key_repeated` appear under the lists as `question.multiple_options_count` does; row errors (empty text, too long) mark the row. A frozen question (annulled or in review) shows everything disabled, as the other types.
  - Going to another type and back keeps both typed lists (as F-104 AC12).
- Publish checklist (F-95): for `Matching` the items are "2 to 8 items on the left", "At least as many matches on the right", "Every item has text and a match"; the aside shows the type.
- Preview dialog: the statement, then "Match each number with one letter.", then the left items numbered and the right items lettered, in authored order, with no mark; after "show answer" the key shows as "1 → C" next to every number, with the check icon and the text "Correct answer" on the matched right item and its border (never colour alone); an extra item shows nothing; a draft with no match set shows "no key yet", as today.
- `/admin/questions`: the "Type" chip says "Matching"; nothing else changes.
- The transcription screen of F-99 offers the fourth type card; picking it there shows the same key section.
- Permissions: unchanged, `questions.manage`.

### Wireframe: Question form, key section (matching)
```text
+------------------------------------------------------------+
| Classification                                             |
|  Question type: ( ) Single choice  ( ) True/false          |
|                 ( ) Multiple answer  (x) Matching  [new]   |
+------------------------------------------------------------+
| Matches                                          [changed] |
| 2 to 8 items on the left. The right column needs at least  |
| as many; extra items match nothing.                        |
|  Items                          Matches                    |
|  1 [ Item text ......] [C v] ^ v x   A [ Text ....] item 2 |
|  2 [ Item text ......] [A v] ^ v x   B [ Text ....] Extra  |
|  3 [ Item text ......] [  v] ^ v x   C [ Text ....] item 1 |
|  [+ Add item]          3 of 8 items  [+ Add match] 3 of 8  |
|  ! Every item needs a match. (error)                       |
+------------------------------------------------------------+
| Before publishing: [v] 2 to 8 items on the left            |
|   [v] At least as many matches   [ ] Every item has a match|
+------------------------------------------------------------+
```

### Wireframe: Preview dialog (matching)
```text
+--------------------------------------------+
| Preview                                    |
| <base text>                                |
| <statement>                                |
| Match each number with one letter.  [new]  |
|  1. Item text            A. Match text     |
|  2. Item text            B. Match text     |
|  3. Item text            C. Match text     |
| [ Show answer ]                            |
|  -- after reveal --                        |
|  1 -> C   2 -> A   3 -> B                  |
|  C. Match text   (v) Correct answer        |
| Explanation: <text>                        |
+--------------------------------------------+
```

## Docs
- flow: new docs/flows/matching-questions.md — how a curator authors, publishes, versions and previews the type, where its key is declared and how the left items and the options form it.

## Acceptance criteria
- AC1 Given a curator with `questions.manage`, when they create a question with `type` `Matching`, a topic, a language, a statement and no items, then it is stored as `Draft` of that type and appears in the list with its type. (UC1, BR1, BR3)
- AC2 Given a matching draft, when it is saved with any number of left items and right items up to 8, matches set or not, then it is accepted and returns the items and the matches as sent; with a ninth left item or a ninth right item, 400 `question.matching_items_count`. (BR3)
- AC3 Given a matching draft, when it is saved with a left item whose match names a right position that does not exist, then 400 `question.matching_key_invalid`; when two left items name the same right position, 400 `question.matching_key_repeated`; in both cases nothing is stored. (BR3)
- AC4 Given a matching draft, when it is published with 1 left item, or with fewer right items than left items, then 400 `question.matching_items_count`; with 2 left items and an empty text in a right item, 400 `question.option_text_required`; with an empty left text, 400 `question.match_item_text_required`. (BR4)
- AC5 Given a matching draft with 2 to 8 left items, when it is published with a left item that has no match, then 400 `question.matching_key_invalid`; with every item matched, extra right items included, it becomes `Published`. (UC3, BR4)
- AC6 Given a published matching question, when an edit leaves a left item without a match or reduces the left items to one, then it is refused and the stored question is unchanged; when the curator matches another valid set, it stays `Published`. (UC2, BR4)
- AC7 Given a `Matching` request that sends `trueFalseKey`, or marks a right item `isCorrect`, then 400 `question.key_not_allowed` and nothing is stored; given a single-choice, true/false or multiple-answer request that sends `matchItems`, then 400 `question.match_items_not_allowed`. (BR5, BR6)
- AC8 Given a stored matching question, when an update names another type, then 409 `question.type_changed` and nothing changes; naming its own type is accepted. (BR2)
- AC9 Given a published matching question with version 1 and the matches 1→C, 2→A, 3→B, when the curator saves 1→A, 2→C, 3→B without a reason, then 400 `question.key_change_reason_required`; with a reason, version 2 is recorded with `answerKeyChanged` true and the reason and its `matchItems`. (UC7, BR8)
- AC10 Given the same question, when only the text of a left item or a right item changes, then version 2 is recorded with `answerKeyChanged` false and no reason is required; when a left item is added or removed, the reason is required; given single-choice, true/false and multiple-answer questions, the F-98 and F-104 behaviour is unchanged and their tests pass unedited. (BR8)
- AC11 Given a matching question, then the shared rules hold for it: HTML sanitized in left and right items, limits refused, images in items counted with the statement and options under the F-103 cap, difficulty 0 or 6 refused, a taken paper number refused, a published one cannot be deleted, a topic with only this type is `topic.in_use`, annulment, reinstatement and the review flow work. (BR7, BR9)
- AC12 Given questions of the four types, then the list shows each row's type and the response names it; `QuestionResponse` and `QuestionVersionResponse` carry `matchItems` (empty for the other types). (UC5, BR11)
- AC13 Given the form for a new question, when the curator picks "Matching", then the key section draws the two lists, each left row has a select of the free letters, a letter taken by another row is not offered, saving and publishing work; removing a right row that an item uses clears that item's match; moving a right row keeps each match with its text; picking another type and back keeps the typed lists; on an existing question the type is read-only. (UC1, UC2, BR2, BR10)
- AC14 Given a matching question, when the curator opens the preview, then the numbered left items and the lettered right items show with the line "Match each number with one letter."; the key and the explanation stay hidden until "show answer", then each matched right item is marked with icon, text and border and the key line "n → letter" appears. (UC6, BR13)
- AC15 Given the migration, then existing rows and versions keep their type and options, `question_match_items` exists with its comments, and the type column comments list the four values. (BR1)
- AC16 Given a user without `questions.manage`, then create and update of this type answer 403. (BR9)
- AC17 The two lists are operable by keyboard (each field, select and button one tab stop, the select by arrows and Enter), each control has an accessible name with its number or letter, the section errors are announced, and the `Matches` text note is not the only way to know which letter an item uses (WCAG 2.2 AA); checked on screen.
- AC18 Given the transcription screen of an edition, then it offers the four types and the matching key section works there. (F-99 BR5)
- AC19 All new texts appear in pt-BR, pt-PT and en, including the error code texts.

## Criterion → test
<!-- Written by /agile:build at its start, one row per criterion above, in the same order. Test: the test name(s), `pending` (no test yet) or `validation step <n>` (only checkable on screen). -->
| Criterion | Test |
| --- | --- |
| AC1 | `MatchingQuestionEndpointTests.Create_MatchingWithNoItems_IsStoredAsADraftOfThatType_AndListedWithIt`; `MatchingQuestionTests.Create_MatchingWithNoItems_IsADraftOfThatType` |
| AC2 | `MatchingQuestionEndpointTests.Create_MatchingDraft_WithEightItemsEachSide_ReturnsTheItemsAndMatchesAsSent`, `.Create_MatchingWithANinthItemOnEitherSide_IsRefusedWithMatchingItemsCount`; `MatchingQuestionTests.Create_MatchingDraftWithEightItemsEachSide_*`, `.Create_MatchingWithANinth*` |
| AC3 | `MatchingQuestionEndpointTests.Create_MatchingDraft_WithAMissingOrARepeatedRightItem_IsRefused_AndNothingIsStored`; `MatchingQuestionTests.Create_MatchingDraftNamingARightItemThatDoesNotExist_*`, `.Create_MatchingDraftWithTwoItemsOnTheSameRightItem_*` |
| AC4 | `MatchingQuestionEndpointTests.Publish_Matching_WithOneLeftItemOrTooFewRightItems_IsRefusedWithMatchingItemsCount`, `.Publish_Matching_WithAnEmptyRightOrLeftText_IsRefusedWithTheTextCode`; `MatchingQuestionTests.Publish_MatchingWith*` |
| AC5 | `MatchingQuestionEndpointTests.Publish_Matching_WithAnItemWithNoMatch_IsRefused_AndWithEveryItemMatchedAndExtras_IsPublished` |
| AC6 | `MatchingQuestionEndpointTests.Update_PublishedMatching_BreakingTheKey_IsRefused_AndTheStoredQuestionIsUnchanged`, `.Update_PublishedMatching_WithAnotherValidSetAndAReason_StaysPublished` |
| AC7 | `MatchingQuestionEndpointTests.Create_MatchingWithATrueFalseKeyOrAMarkedRightItem_IsRefusedWithKeyNotAllowed`, `.Create_AnotherTypeWithMatchItems_IsRefusedWithMatchItemsNotAllowed` |
| AC8 | `MatchingQuestionEndpointTests.Update_NamingAnotherType_Is409TypeChanged_AndNamingItsOwnIsAccepted`; `MatchingQuestionTests.Update_MatchingWithContentOfAnotherType_IsAConflict` |
| AC9 | `MatchingQuestionEndpointTests.Update_PublishedMatching_ChangingTheMatches_NeedsAReason_AndThenRecordsAKeyChange`; `MatchingQuestionTests.NextVersion_ChangingWhatANumberMatches_*` |
| AC10 | `MatchingQuestionEndpointTests.Update_PublishedMatching_ChangingOnlyTexts_RecordsAVersionWithNoKeyChange`, `.Update_PublishedMatching_AddingALeftItem_NeedsAReason`, `.UpdateBaseText_LinkedToAPublishedMatching_RecordsAVersionWithNoKeyChange`; the F-98 and F-104 suites unedited |
| AC11 | `MatchingQuestionEndpointTests.Create_Matching_SanitizesTheLeftAndRightItems`, `.Create_Matching_WithALeftItemOverTheLimit_*`, `.Create_Matching_WithADifficultyOutOfRange_IsRefused`, `.Create_Matching_WithAPaperNumberAlreadyTaken_*`, `.Delete_PublishedMatching_*`, `.DeleteTopic_WithOnlyAMatchingQuestion_*`, `.AnnulAndReinstate_Matching_*`, `.Submit_CompleteMatchingDraft_*`; `MatchingQuestionTests.Create_MatchingWithElevenImages*`, `.AnnulAndReinstate_*`; `MatchingQuestionImageTests.*` |
| AC12 | `MatchingQuestionEndpointTests.List_QuestionsOfTheFourTypes_*`, `.FindVersion_OfAnotherType_HasAnEmptyMatchItemsList`; `MatchingQuestionPreviewAndListTests.List_NamesTheMatchingType` |
| AC13 | `MatchingQuestionFormTests.*` (four cards, free letters, save, remove and move of a right row, type switch, read-only type, checklist, refusals, reason, frozen) |
| AC14 | `MatchingQuestionPreviewAndListTests.Preview_Matching_*`, `.Preview_MatchingDraftWithNoMatches_*`, `.VersionPreview_Matching_ShowsTheLeftItemsOfTheVersion` |
| AC15 | `AddMatchingQuestionsMigrationTests.Up_AddsTheMatchItemsTableAndColumn_NamesTheFourthType_AndKeepsEveryRow` |
| AC16 | `MatchingQuestionEndpointTests.CreateAndUpdate_OfAMatchingQuestion_WithoutThePermission_AnswerForbidden` |
| AC17 | `MatchingQuestionFormTests.Add_Matching_EachControlNamesItsNumberOrLetter`; keyboard pass: validation step (set with the script) |
| AC18 | `QuestionTranscriptionTests.Open_OffersEveryTypeTheFormImplements`; `MatchingQuestionFormTests.Add_OffersTheFourTypesWithSingleChoicePreselected` |
| AC19 | `MatchingQuestionResourcesTests.EveryNewText_ExistsInTheThreeLanguagesWithTheItemsWords`; `ResourceParityTests` |

## Decisions
- 2026-10-21 — The type is chosen at creation and fixed after the first save — changing it would drop items or a key without saying so (F-97) — confirmed: F-97, F-104
- 2026-10-21 — Scoring (all or nothing, partial credit, penalty) is the edition's rule (E-6); the question keeps only its key; Simulae's per-question correction mode is not imported — confirmed: docs/epics/question-bank.md, F-104
- 2026-10-21 — The transcription screen of F-99 offers the type as soon as it exists; `QuestionTranscriptionTests.Open_OffersEveryTypeTheFormImplements` goes from 3 to 4 types — confirmed: F-99 BR5, F-104
- 2026-10-21 — Shuffling the items is the simulator's (E-5); the question keeps the authored order and the preview shows both columns in that order, with no scrambling — confirmed: F-104 (out of scope). Note: the card said the preview would show the right column in another order; with an independent key (BR1) the authored order gives nothing away, so no scrambling is needed.
- 2026-10-21 — Size: 2 to 8 left items, 8 right items at most and never fewer than the left — same limits as Simulae's columns, a longer right column than left is allowed — asked (recommended option, owner)
- 2026-10-21 — The right column may have extra items that match nothing (distractors); a published question matches every left item — common in public service exams — asked (recommended option, owner)
- 2026-10-21 — One to one only: each right item is used at most once — Simulae's `NParaUm` and `Livre` are not imported — asked (recommended option, owner)
- 2026-10-21 — Left and right items accept images under the F-103 rule (ten per question, alternative text) — items are rich text like options — asked (recommended option, owner)
- 2026-10-21 — The curator builds the key as two lists: left numbered, right lettered, each left row picks its letter — the key is a real map (1 to C) as in the papers, and F-98 can tell a key change from a text edit; the one-row-per-pair layout would make the key only the row position and hide a swap — asked (recommended option, owner)
- 2026-10-21 — No mockup and no `/agile:screen`: two lists of kit rows and a select; wireframes in this file — asked (recommended option, owner)
- 2026-10-21 — The right column is the question's existing `options` (letter = position, `IsCorrect` false); the left column is a new table `question_match_items` with `match_position`; no pair list is stored — one copy of the key, and the option rules (length, text, images, options JSON of the version) are reused — Claude, from the code read
- 2026-10-21 — A match that names a missing right item, or the same right item twice, is refused even on a draft; an item with no match is allowed on a draft — the first two are broken data the form never produces, the third is unfinished work — Claude
- 2026-10-21 — The key is a domain unit `MatchingKey` beside the others with its own `Check`; `AnswerKey` gains `MatchItems` (empty for the other types); the shared option length and text checks stay one helper — Claude, to keep one rule for what the types share (F-97 BR5)
- 2026-10-21 — Six new error codes instead of reusing the option ones whose texts say "option" and "2 to 5" — Claude
- 2026-10-21 — The version stores the left items as a second JSON column on `question_versions`, nullable, so older versions keep reading — Claude
- 2026-10-21 — New glossary rows before first use in code: `MatchItem` (item da esquerda), `MatchPosition` (posição da correspondência) — Claude
- 2026-10-21 — No new package — the item needs nothing the solution lacks — Claude
- 2026-10-21 — Reuse: the `AnswerKey` seam of F-97, `QuestionRules.CheckEachOption`, kit `AppSelect`, `AppRadioCards`, the F-98 reason field and `NextVersion`, the F-103 image rule, the migration test pattern of F-104 — Claude
- 2026-10-21 — Size M, fits one session: one domain unit, one table, a migration, a fourth card, one section with two lists, one preview block and the key-change fix — Claude, from the code read
- 2026-10-21: design passes (system-design, architect) verified against the code: `BaseTextStore.cs:29` loads published linked questions with options only; `TableDescriptionTests.cs:69` and `InitialQuestionBankMigrationTests.cs:38` hold exact lists; `QuestionForm.razor:1463` builds a `QuestionResponse` from a version. Accepted: architect #1 (blocker) include match items in the base-text fan-out, with a test; #2 pass the version's match items to the version preview; #3 `MatchItems` is the last optional parameter (`= null`) of `SaveQuestionRequest`, `QuestionResponse`, `QuestionVersionResponse`, `QuestionContent` and `AnswerKey.From`, the Api always sends a list, the Web reads null as empty; #4 the JSON exemption and the table list gain the new column and table; #5 the plan is vertical slices (below). #6 kept: `MatchItemResponse` carries `position` like `QuestionOptionResponse`, so a reader never derives it from the array order. system-design R1 to R5 accepted (null JSON read as empty, images of the left items owned and scanned, `CheckEachOption` shared, the section in its own component `MatchingKeySection.razor` to keep the F-107/F-108 conflicts small).
- 2026-10-21: Design coverage — data (migrations, configurations, image scan), contracts (records and their constructors, version preview, base-text fan-out), rules (architecture test lists), layout (plan order), simplicity (one key record, shared option rule) read; trade-offs: none raised.
- 2026-10-21: traps — T1 right column in a second table: forks the option rules and the version JSON; T2 reuse `OptionsMax` for the right column: caps it at 5 with the wrong code; T3 `CopyAs` without the match items: annulment versions lose the key.
- 2026-10-21: plan — 1 create and save a matching draft end to end (contract, key, entity, table, migration, handler) → AC1, AC2, AC3, AC7, AC8, AC15; 2 publish and edit rules, shared rules and images → AC4, AC5, AC6, AC11, AC16; 3 versions and the key-change check, base-text fan-out included → AC9, AC10; 4 responses → AC12; 5 form section and transcription → AC13, AC17, AC18; 6 preview, version preview included → AC14; 7 resources and flow page → AC19, techdocs check.

## Out of scope
- Scoring: all or nothing, partial credit, penalty — epic E-6 with the edition's scoring rules (owner, 2026-10-09).
- Many-to-one and free matching (Simulae `NParaUm`, `Livre`) — a later item when asked.
- Filter by type — F-100. Changing the type of an existing question.
- Shuffling items or letters — E-5, the simulator.
- Drag and drop in the form or in the student view — the select is the one way to match.
- A base text shared by several statements — F-102 (already works with this type).

## Open questions
- (none)

## Change notes

## Validation script
<!-- Written at the end of build. -->

## Events

- 2026-10-21 question — asked 6, from context 4

## Delivery
