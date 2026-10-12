---
feature: F-97
epic: Question bank
status: validating
board: 143
version: 1
---
# True/false questions

Technical terms: [glossary](../glossary.md)

## Summary
The true/false question type (Cebraspe style, "certo/errado"): the curator writes the statement and sets the answer key as true or false. The question stores no wrong-answer penalty: the penalty is the edition's scoring rule, in epic E-6 (owner, 2026-10-09). This item also creates the question type pattern the later types follow (F-104, F-106 to F-108): F-95 fixed `SingleChoice` in the entity and the form and left no seam. Source: epic Question bank (E-4); Simulae `ConfigurationBase.cs` (T3, `ConfiguracaoCertoErrado.GabaritoCerto`) as the import source, without its penalty fields.

## Start
- Depends on: F-95 (done), F-96 (done).
- Waits on (to start): nothing.
- Needed to validate: the local app host — owner.
- Suggested path: `/agile:build` (the screen is described in this file; no mockup, owner 2026-10-12).
- Parallel with: F-98 (versions). Both edit `Question.cs`, `SaveQuestionHandler` and `QuestionForm.razor`: expect conflicts and bring the branch up to date often. F-98 must version the answer key of this type too (see Out of scope).

## Goal
A curator can author, publish, list and preview a true/false question, and the module has one clear place where a question type is declared, validated and drawn, so the next types add to it instead of branching through F-95's single-choice code.

## What exists (verified 2026-10-12)
- `QuestionType` has only `SingleChoice`; the column `questions.type` is already text (`HasConversion<string>`, 40), so a new value needs no data conversion (`QuestionConfiguration.cs`).
- `Question.Create` sets `Type = QuestionType.SingleChoice` itself; `SaveQuestionRequest` has no type; `QuestionResponse` carries `Type` already.
- The key of a single-choice question is "exactly one option marked correct" (`QuestionRules.CheckOptions`, called from `Question.Apply` and `Question.Publish`). A true/false question has no options: its key is one boolean.
- Origin (edition, paper number, notice subject; F-96), classification, base text, explanation, difficulty, sanitizing, limits, status, delete guard and topic guard in `Question.Apply`, `SaveQuestionHandler` and the topic usage check are independent of the type and are reused as they are.
- The form (`QuestionForm.razor`, 1 217 lines) and the preview dialog (`QuestionPreviewDialog.razor`) only know options A to E; the list (`Questions.razor`) has no type column.
- UI kit has `AppRadioCards` (used for choices of a few options); the glossary row "Tipo de questão" already lists `TrueFalse` (certo/errado).
- Items that touched this code since F-95: F-96 (origin), B-28 (lookups). Neither changes how the key works.
- Simulae defects not to import: the penalty model and fraction stored on each true/false question (`ModeloPenalizacao`, `FracaoPenalidade`).

## Users and use cases
- UC1 A curator creates a true/false question as `Draft`: type chosen at creation, topic, content language, statement, optionally base text, explanation, difficulty, origin edition, and the key (true or false) when known.
- UC2 A curator edits a true/false question, `Draft` or `Published`, in place; its type does not change.
- UC3 A curator publishes a complete true/false `Draft`.
- UC4 A curator unpublishes it and deletes a `Draft`, as for any question.
- UC5 A curator tells the two types apart in the list.
- UC6 A curator previews a true/false question as a student will see it, then reveals the key and the explanation.

## Business rules
- BR1 `QuestionType` gets `TrueFalse`. A true/false question is the same `Question` row with `Type = TrueFalse`, no option rows, and one new nullable column `true_false_key` (boolean): true means "certo", false "errado", null means not set yet. No penalty, weight or score is stored on the question (epic E-6).
- BR2 The type is chosen when the question is created and never changes. `SaveQuestionRequest.Type` is optional: omitted on create means `SingleChoice` (clients written for F-95 keep working); on update it must be omitted or equal the stored type, else 409 `question.type_changed` and nothing changes.
- BR3 A true/false question has no options: a save that sends options is refused, 400 `question.options_not_allowed`, nothing is truncated or ignored. A single-choice question that is sent a key is refused, 400 `question.key_not_allowed`.
- BR4 A true/false `Draft` needs a statement, a topic and a content language (F-95 BR3); the key may be null. `Published` needs, besides that, a non-null key, else 400 `question.true_false_key_required`. Editing a published true/false question runs the same check and refuses a change that clears the key.
- BR5 Everything the two types share is one rule, not two: statement, base text, explanation and difficulty limits and sanitizing (F-95 BR6, BR8, BR9, BR10), content language (BR7), status and publish/unpublish (BR12), delete (BR11), the topic guard (BR13), the origin of a past edition and its unique paper number (F-96 BR1 to BR3), permission (BR14), error answers 404 (BR16).
- BR6 The type and the key are part of the stored question, so any later version or attempt can name them (ADR-0001 #10); F-98 versions the key.
- BR7 The preview of a true/false question shows base text, statement and the two answers "Certo" and "Errado" (pt-BR, pt-PT) / "True" and "False" (en) as inert, labelled text; the key and the explanation stay hidden until "show answer", then the key is shown with an icon, text and border (never colour alone), as F-95 BR5/AC12 does for an option.
- BR8 The list shows the type of each row (text chip). Filtering by type is F-100; the list still offers only the status filter and the statement search (F-95 BR15).
- BR9 The type of a question is declared in one place per layer: the domain (the rules of its answer key), the contract (`QuestionType`), the form (the section that edits its key) and the preview (the block that shows its answers), each a small unit a later type adds beside, never an `if` chain in `Question.Apply`.

## Screens and API
No mockup (owner, 2026-10-12): one new control inside the form of F-95, drawn with kit components and identity tokens only.

- API (all under `/api/v1/question-bank/questions`, unchanged routes):
  - `POST` and `PUT` take `SaveQuestionRequest` with two optional fields added: `type` (`SingleChoice` | `TrueFalse`, create only) and `trueFalseKey` (`true` | `false` | null).
  - `QuestionResponse` gains `trueFalseKey` (null for single choice and for an unset key); `Options` is empty for `TrueFalse`.
  - `QuestionListItemResponse` gains `type`.
  - Error codes (new): `question.type_changed` (409), `question.options_not_allowed` (400), `question.key_not_allowed` (400), `question.true_false_key_required` (400).
  - Data: migration in the `QuestionBank` context adds `true_false_key boolean null` and the check `ck_questions_true_false_key` (`true_false_key IS NULL OR type = 'TrueFalse'`). Existing rows are all `SingleChoice`: no backfill.
- `/admin/questions/new` — a first field "Question type" (`AppRadioCards`: "Single choice", "True/false") preselected on single choice. Choosing a type shows its answer section; the answers typed for the other type are kept in the form until it is saved, but only the chosen type's are sent.
- `/admin/questions/{id}` — the same field, read-only (a labelled value with the hint "The type cannot be changed after the question is created").
- Answer section for `TrueFalse`, in place of "Options": `AppSectionCard` "Answer key" with `AppRadioCards` "Certo" / "Errado" (en: "True" / "False"), none preselected (a draft may have no key), a hint saying the penalty for a wrong answer is set by the edition's scoring rules, never by the question. Required only to publish; the error `question.true_false_key_required` appears on the card and in the error summary.
- Publish checklist (F-95): for `TrueFalse` the three option items are replaced by one "Answer key set"; the aside shows the type instead of the option count.
- `/admin/questions` — new column "Type" after the statement (`AppStatusChip`, `Tone=Neutral`, text per type); row actions unchanged.
- Preview dialog: for `TrueFalse` the options list is replaced by two inert answer lines "Certo" and "Errado" with the key marked after the reveal; for `SingleChoice` it is untouched.
- Permissions: all unchanged, `questions.manage` (F-95 BR14).

## Acceptance criteria
- AC1 Given a curator with `questions.manage`, when they create a question with `type` `TrueFalse`, a topic, a language and a statement and no key, then it is stored as `Draft` of type `TrueFalse` with no options and `trueFalseKey` null, and appears in the list with its type. (UC1, BR1, BR4)
- AC2 Given a `TrueFalse` request that sends options, then 400 `question.options_not_allowed`; given a `SingleChoice` request that sends `trueFalseKey`, then 400 `question.key_not_allowed`; nothing is stored in either case. (BR3)
- AC3 Given a `TrueFalse` draft with no key, when it is published, then 400 `question.true_false_key_required`; with the key `true` or `false` it becomes `Published`. (UC3, BR4)
- AC4 Given a published `TrueFalse` question, when the key is changed from true to false, then it stays `Published` with the new key; when an edit clears the key, then 400 `question.true_false_key_required` and the stored question is unchanged. (UC2, BR4)
- AC5 Given a create request with no `type`, then the question is `SingleChoice` and every F-95 behaviour holds; the F-95 and F-96 tests stay green without being edited. (BR2)
- AC6 Given a stored question, when an update names the other type, then 409 `question.type_changed` and the stored question is unchanged; naming its own type is accepted. (BR2)
- AC7 Given a `TrueFalse` question, then the shared rules hold for it: HTML in the four text fields is sanitized, a statement over its limit is refused, difficulty 0 or 6 is refused, an edition with a paper number already taken is refused 409 `question.paper_number_taken`, a published one cannot be deleted, and a topic with only a `TrueFalse` question is refused `topic.in_use`. (BR5)
- AC8 Given a `TrueFalse` question, then its response and its table row carry only the key: no penalty, weight or score field exists on the contract or the table. (BR1)
- AC9 Given questions of both types, then the list shows each row's type, and the response of the list item names it. (UC5, BR8)
- AC10 Given the form for a new question, when the curator picks "True/false", then the options section is replaced by the answer key section, saving and publishing work, and picking "Single choice" again brings the typed options back; on an existing question the type is read-only. (UC1, UC2, BR2, BR9)
- AC11 Given a `TrueFalse` question, when the curator opens the preview, then it shows the statement and the two answers; the key and the explanation stay hidden until "show answer", then the key is marked with icon, text and border. (UC6, BR7)
- AC12 Given a user without `questions.manage`, then the create and update of a true/false question answer 403, as every question endpoint does. (BR5)
- AC13 Given the migration, then existing rows keep their type and options, `true_false_key` is null for them, and the database refuses a key on a `SingleChoice` row. (BR1)
- AC14 The answer key control is operable by keyboard, has an accessible name, and its error is announced (WCAG 2.2 AA); checked on screen.
- AC15 All new texts appear in pt-BR, pt-PT and en, including the four error code texts.

## Criterion → test

Tests locked: d6c5cd247e9e5356090f52cb1a0f8f946733190d (6 files); unlocks: 1

<!-- Written by /agile:build at its start. -->
| Criterion | Test |
| --- | --- |
| AC1 | `TrueFalseQuestionEndpointTests.Create_TrueFalseWithNoKey_IsStoredAsADraftOfThatType_AndListedWithIt`; `TrueFalseQuestionTests.Create_TrueFalseWithNoKey_IsADraftWithNoOptionsAndNoKey`; `TrueFalseQuestionFormTests.Add_TrueFalseWithNoKey_SavesADraftWithANullKey` |
| AC2 | `TrueFalseQuestionEndpointTests.Create_TrueFalseWithOptions_IsRefusedWithOptionsNotAllowed_AndNothingIsStored`; `TrueFalseQuestionEndpointTests.Create_SingleChoiceWithAKey_IsRefusedWithKeyNotAllowed_AndNothingIsStored`; `TrueFalseQuestionTests.Create_TrueFalseWithOptions_IsRefusedWithOptionsNotAllowed`; `TrueFalseQuestionTests.Create_SingleChoiceWithAKey_IsRefusedWithKeyNotAllowed` |
| AC3 | `TrueFalseQuestionEndpointTests.Publish_TrueFalseWithNoKey_IsRefusedWithTrueFalseKeyRequired`; `TrueFalseQuestionEndpointTests.Publish_TrueFalseWithAKey_BecomesPublished`; `TrueFalseQuestionTests.Publish_TrueFalseWithNoKey_IsRefusedWithTrueFalseKeyRequired` |
| AC4 | `TrueFalseQuestionEndpointTests.Update_PublishedTrueFalse_ChangingTheKey_StaysPublishedWithTheNewKey`; `TrueFalseQuestionEndpointTests.Update_PublishedTrueFalse_ClearingTheKey_IsRefused_AndTheStoredQuestionIsUnchanged`; `TrueFalseQuestionTests.Update_PublishedTrueFalse_ClearingTheKey_IsRefused_AndTheStoredQuestionIsUnchanged` |
| AC5 | `TrueFalseQuestionEndpointTests.Create_WithNoType_IsSingleChoice_WithNoKey`; `TrueFalseQuestionTests.Create_ContentWithNoType_IsSingleChoiceWithNoKey`; the F-95 and F-96 test classes, run unedited |
| AC6 | `TrueFalseQuestionEndpointTests.Update_NamingAnotherType_Is409TypeChanged_AndTheStoredQuestionIsUnchanged`; `TrueFalseQuestionEndpointTests.Update_NamingItsOwnType_IsAccepted`; `TrueFalseQuestionTests.Update_WithAnotherType_IsRefusedWithTypeChanged_AndNothingChanges` |
| AC7 | `TrueFalseQuestionEndpointTests.Create_TrueFalse_SanitizesTheHtmlOfTheTextFields`; `TrueFalseQuestionEndpointTests.Create_TrueFalse_StatementOverItsLimitDifficultyOutOfRangeAndEmptyStatement_AreRefused`; `TrueFalseQuestionEndpointTests.Create_TrueFalse_WithAPaperNumberAlreadyTaken_IsRefusedWithPaperNumberTaken`; `TrueFalseQuestionEndpointTests.Delete_PublishedTrueFalse_IsRefused_AndADraftIsDeleted`; `TrueFalseQuestionEndpointTests.DeleteTopic_WithOnlyATrueFalseQuestion_IsRefusedWithTopicInUse` |
| AC8 | `TrueFalseQuestionTests.TheEntityAndTheContracts_CarryNoPenaltyWeightOrScore`; `TrueFalseQuestionEndpointTests.TheQuestionsTable_HasTheKeyAndNoPenaltyWeightOrScoreColumn`; `AddTrueFalseKeyMigrationTests.Up_AddsTheKeyColumnAndItsCheck_AndKeepsEveryExistingQuestionWithItsTypeAndNoKey` |
| AC9 | `TrueFalseQuestionEndpointTests.List_QuestionsOfBothTypes_NameTheTypeOfEachRow`; `TrueFalseQuestionPreviewAndListTests.List_NamesTheTypeOfEachRowInATextChip`; `TrueFalseQuestionListTypeCellTests.List_TheTypeChip_SitsInAOneLineCell` |
| AC10 | `TrueFalseQuestionFormTests.Add_OffersTheTwoTypesWithSingleChoicePreselected_AndShowsTheOptionsSection`; `TrueFalseQuestionFormTests.Add_ChoosingTrueFalse_ReplacesTheOptionsWithTheAnswerKeySection_WithNoKeyPreselected`; `TrueFalseQuestionFormTests.Add_GoingToTrueFalseAndBack_BringsTheTypedOptionsBack`; `TrueFalseQuestionFormTests.Add_TrueFalseWithAKey_SavesAsADraftOfThatTypeWithItsKeyAndNoOptions`; `TrueFalseQuestionFormTests.Add_TrueFalseAfterTypingOptions_DoesNotSendTheOptions`; `TrueFalseQuestionFormTests.Edit_TrueFalse_ShowsTheTypeAsAReadOnlyValueWithItsHint_AndTheStoredKey` |
| AC11 | `TrueFalseQuestionPreviewAndListTests.Preview_TrueFalse_ShowsBaseTextStatementAndTheTwoAnswersAsInertText`; `TrueFalseQuestionPreviewAndListTests.Preview_TrueFalse_BeforeShowAnswer_HidesTheKeyAndTheExplanation`; `TrueFalseQuestionPreviewAndListTests.Preview_TrueFalse_ShowAnswer_MarksTheKeyWithIconTextAndBorder_AndShowsTheExplanation` |
| AC12 | `TrueFalseQuestionEndpointTests.CreateAndUpdate_OfATrueFalseQuestion_WithoutThePermission_AnswerForbidden` |
| AC13 | `AddTrueFalseKeyMigrationTests.Up_AddsTheKeyColumnAndItsCheck_AndKeepsEveryExistingQuestionWithItsTypeAndNoKey`; `AddTrueFalseKeyMigrationTests.TheDatabase_RefusesAKeyOnASingleChoiceRow_ButAcceptsOneOnATrueFalseRowAndNoneAnywhere` |
| AC14 | `TrueFalseQuestionFormTests.Add_TrueFalseKeyControl_IsANamedRadioGroupWithOneTabStop` (structure); `TrueFalseQuestionFormTests.Edit_PublishedTrueFalse_WhenTheApiRefusesTheMissingKey_TheErrorIsOnTheKeyCardAndInTheSummary` (error text); keyboard pass and announcement: validation step 3 |
| AC15 | `TrueFalseQuestionResourcesTests.EveryNewText_ExistsInTheThreeLanguagesWithTheItemsWords` |

## Decisions
- 2026-10-12 — The item is size M and not "small and clear": F-95 left no type seam (`Question.Create` fixes `SingleChoice`, the form and preview only draw options), so this item builds the pattern — Claude, verified in `Question.cs`, `SaveQuestionRequest.cs`, `QuestionForm.razor`.
- 2026-10-12 — The type is chosen on `/admin/questions/new` and fixed after the first save (recommended option, owner) — changing it would drop options or a key silently, and a fixed type is the simplest pattern for F-104 and F-106 to F-108.
- 2026-10-12 — A true/false `Draft` may have no key; only publishing needs it (recommended option, owner) — the same "incomplete draft" logic as F-95 BR3/BR4, and F-99 transcribes papers before the answer sheet is at hand.
- 2026-10-12 — The list gets a "Type" column now; the type filter stays in F-100 (recommended option, owner) — two types mixed in one list need telling apart.
- 2026-10-12 — No mockup: the screen change is one control in a form already designed (recommended option, owner; the epic marks "Screen design: no").
- 2026-10-12 — The key is a nullable boolean column on `questions`, not two option rows — a true/false question has no options (Simulae also stores `GabaritoCerto` as one boolean); a check constraint keeps the key off single-choice rows.
- 2026-10-12 — Sending options to a true/false question, or a key to a single-choice one, is refused, never ignored — silent dropping would hide a client bug and break the answer-is-what-was-stored rule of F-95 BR8.
- 2026-10-12 — The answer labels follow the UI language, not the question's content language: "Certo/Errado" in pt-BR and pt-PT, "True/False" in en — resources are per UI locale (ADR-0001 #27 translates no question content, and these are interface words); revisit if a student practices in another language than their UI.
- 2026-10-12 — No new package — the item needs nothing the solution lacks.
- 2026-10-12 — Design passes (`system-design`, `architect`), verified against the files. Accepted: the answer key becomes a domain unit (`AnswerKey` with `SingleChoiceKey` and `TrueFalseKey`, one file each, one `From(type, options, key)` switch and a polymorphic `Check`) so `Question.Apply` and `Publish` hold no per-type branch; `QuestionContent` carries `Type` and `TrueFalseKey` as trailing optional members (F-95 and F-96 tests compile unedited); the options/key shape refusals live in the key's `Check` (the entity is the authority), the handler only answers 409 `question.type_changed` before the shape checks; `Check` returns the existing `OptionFault` (position null for the key); one migration `AddTrueFalseKey` carries the column, its comment, the check and the `Type` column comment (`SingleChoice or TrueFalse`, `EnumColumnCommentTests`); "sends options" means `Options.Count > 0` (the F-95 form sends `[]`). Dropped: the default `Type = SingleChoice` on `QuestionListItemResponse` (api-contracts: no struct defaults; it is a required member placed before `Origin`); extracting two child components for the answer sections (one new type does not justify it). The type field sits inside the Classification section card, not in a section of its own, so the F-95 test of the section list stays as it is.
- 2026-10-12 — Tests unlocked (owner, card, recommended option): four tests of `TrueFalseQuestionFormTests` looked up `#question-section-options` and `#question-section-true-false-key`, ids the section card does not render (it gives the id to the heading, `...-title`), and expected the message in the error summary, which lists the field's name. A test defect, no criterion changed; rewritten and locked again.
- 2026-10-12 — The list's "Type" column is the last one and a read-only `AppChip` (text), not the column after the statement and an `AppStatusChip` the screen section sketched — Claude: the F-95 list tests read cells by index (1 to 5) and count the neutral status chips, and AC5 says they stay unedited; the type is a label, not a state. Moving it next to the statement is a one-line change plus the column indexes of those tests, if the owner prefers it at validation. Owner chose to keep it last (2026-10-12, recommended option).

## Out of scope
- The wrong-answer penalty, weight and scoring of annulled questions — epic E-6 with the edition's scoring rules (owner, 2026-10-09).
- Versions of the key and its change history — F-98 (must include `trueFalseKey`; the project rule "published content is versioned" is met there).
- Filter by type — F-100. Annulling a question — F-101.
- A base text shared by several statements (the Cebraspe reading passage with many items) — F-102.
- Changing the type of an existing question.
- Sample questions for tests — F-110.

## Open questions
- (none)

## Change notes

## Validation script
Needed to validate: the app host on its own database and the seeded subjects (F-37 seed) — in place now; nothing to provide. Stop any other app host first: it locks the build output.

1. **Start the app.** From the worktree, with its own database so the migration `AddTrueFalseKey` never lands in the shared one.
   - PowerShell 7: `$env:Database__Name = "simulab_f97"; dotnet run --project src/Hosts/Simulab.AppHost`
   - Git Bash: `Database__Name=simulab_f97 dotnet run --project src/Hosts/Simulab.AppHost`
   Expected: every resource reaches Running and the migration is applied with no `fail` line. Open Web at https://localhost:7125 and sign in with the seeded administrator (`SeedAdmin` of `src/Hosts/Simulab.Api/appsettings.Development.json`).
2. **The type field.** Content > Questions > Add. The first field of **Classification** is "Question type" with two cards, "Single choice" (selected) and "True/false". Pick a subject and a topic. Choose "True/false": the **Options** card is replaced by **Answer key** with two cards, True and False, none selected, and a hint saying the penalty comes from the edition's scoring rules. The aside's checklist shows four items, the last "Answer key set", and a "Type" row.
3. **Keyboard and announcement.** With the keyboard only, Tab to the answer key cards: one tab stop, arrows move between True and False, Space or Enter selects. Write a statement, leave the key empty and Save: a draft is saved. Open it, change nothing and choose Publish: it is disabled with the checklist reason. Select False, Save, then Publish: it becomes Published. (The refusal text "Set the answer key (true or false) to publish." appears on the card and in the error summary when the Api refuses a publish without a key.)
4. **Read-only type.** On the saved question the type is a labelled value "True/false" with the hint "The type cannot be changed after the question is created." and no cards. Change the key to True and Save: it stays Published with the new key.
5. **Back to single choice.** Add a new question, add two options with text, then choose "True/false" and back to "Single choice": the options you typed are still there. Save: it stores a single-choice question with its options.
6. **Preview.** Open the true/false question and choose **Preview as a student**: the statement and two lines, True and False, as plain text. Choose "Show answer": the key line gets the icon, the text "Correct answer" and a border, and the explanation appears.
7. **The list, languages and permission.** Content > Questions: the **Type** column (last) says "True/false" or "Single choice" in a label. Switch to Português (Brasil): the field, cards, hint and column read "Certo/errado", "Certo", "Errado", "Gabarito". Sign in as a Student: the questions pages are not offered.

Stop the app host (Ctrl+C) and, when done, drop the database: `DROP DATABASE simulab_f97;` (server up, nobody connected).

## Events

- 2026-10-12 question — asked 4, from context 0
- 2026-10-12 blocked — test wrong: four tests of TrueFalseQuestionFormTests looked up ids the section card does not render and the summary text; test defect, no criterion changes
- 2026-10-12 question — asked 1, from context 0
- 2026-10-12 test-fail — build blocked by a running process (src/Hosts/Simulab.Web/Simulab.Web.csproj), 2 reds

## Delivery
