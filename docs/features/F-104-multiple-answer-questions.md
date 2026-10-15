---
feature: F-104
epic: Question bank
status: approved
board: 150
version: 1
---
# Multiple-answer questions

Technical terms: [glossary](../glossary.md)

## Summary
The multiple-answer question type: a statement with three to five options of which two or more are correct, and the student marks all of them. The question stores only its answer key (which options are correct); how a partly right answer scores is the edition's scoring rule (epic E-6), never a field of the question. The item also fixes the F-98 answer-key change check, which looks at one correct option only and so would not see a key moving from `{A,B}` to `{A,C}`. Source: epic Question bank (E-4); Simulae `ConfigurationBase.cs` (T2, `ConfiguracaoMultiplaResposta`) as the import source, without its stored count and its scoring mode.

## Start
- Depends on: F-95 (done), F-97 (done: the type pattern), F-98 (done: key versioning).
- Waits on (to start): nothing.
- Needed to validate: the local app host — owner.
- Suggested path: `/agile:build` (the screen is described in this file; no mockup, owner 2026-10-15).
- Parallel with: F-102 (building) and F-103. F-102 also edits `QuestionForm.razor` and `QuestionPreviewDialog.razor`: expect conflicts and bring the branch up to date often.

## Goal
A curator can author, publish, list, preview and version a multiple-answer question, and the module gains it by adding one answer-key unit beside the two it has, not by branching through the single-choice code.

## What exists (verified 2026-10-15)
- `QuestionType` has `SingleChoice` and `TrueFalse`; `questions.type` is text (`HasConversion<string>`, 40), so a new value needs no data conversion; only the column comment lists the values (`EnumColumnCommentTests`).
- The answer key is a domain unit per type: `AnswerKey` (abstract record), `SingleChoiceKey`, `TrueFalseKey`, one `switch` in `AnswerKey.From` (F-97 BR9). `Question.Apply` and `Question.Publish` hold no per-type branch.
- `IsCorrect` is already stored per option (`QuestionOption`, `QuestionVersionOption`) and sent per option (`SaveQuestionOptionRequest.IsCorrect`, `QuestionOptionResponse.IsCorrect`): no new column, no new table.
- `QuestionRules.CheckOptions` counts 2 to 5 options and exactly one correct: its two limits and its key rule are single-choice rules, so the new key does not call it as it is.
- F-98 key-change check, `Question.cs:129-132`: it compares `FirstOrDefault(option => option.IsCorrect)?.Position` of the latest version and of the stored options. With several correct options it misses a change of any option but the first. `SameVersionedContent` (`Question.cs:162-175`) compares every option's mark and is right.
- The form draws options in a `MudRadioGroup` (`QuestionForm.razor:285`) with one position as value; `KeyDone` is `Count(IsCorrect) == 1` (`:763`); the save sends each option's `IsCorrect` (`:1588`); the preview marks `option.IsCorrect` (`QuestionPreviewDialog.razor:78`) and shows "no key" when none is marked (`:114`).
- UI kit has `AppCheckbox` (`Components/Ui`) and `AppRadioCards`; the type field of F-97 is two `AppRadioCards` (`TypeCards`, `QuestionForm.razor:699`).
- Glossary: `QuestionType` already lists `MultipleAnswer` ("múltiplas respostas").
- Items that touched this code since F-97: F-98 (versions), F-101 (annulment, freezes the form), F-96 (origin). F-102 (shared base text) is building and edits the same form.
- Simulae defects not to import: `NumCorretas` and the list of correct identifiers stored beside the option marks (three copies of one fact), the scoring mode (`Binario`/`Parcial`) on each question, the 6-option cap.

## Users and use cases
- UC1 A curator creates a multiple-answer question as `Draft`: type chosen at creation, topic, content language, statement, optionally base text, options with the correct ones marked, explanation, difficulty, origin edition.
- UC2 A curator edits it, `Draft` or `Published`, in place; its type does not change.
- UC3 A curator publishes a complete multiple-answer `Draft`.
- UC4 A curator unpublishes it and deletes a `Draft`, as for any question.
- UC5 A curator tells the three types apart in the list.
- UC6 A curator previews it as a student will see it, with the number of answers to mark, then reveals the key and the explanation.
- UC7 A curator changes which options are correct in a published question and gives the reason, as F-98 asks for any key change.

## Business rules
- BR1 `QuestionType` gets `MultipleAnswer`. A multiple-answer question is the same `Question` row with `Type = MultipleAnswer`, its options as rows with `IsCorrect`, and no `true_false_key`. The number of correct answers is never stored: it is the count of marked options.
- BR2 The type is chosen at creation and never changes (F-97 BR2 as it stands): `SaveQuestionRequest.Type` accepts `MultipleAnswer` on create; on update it must be omitted or equal the stored type, else 409 `question.type_changed`.
- BR3 A multiple-answer `Draft` follows F-95 BR3: statement, topic and content language; from 0 to 5 options, each within the option length limit; any number of them marked, including none.
- BR4 `Published` needs, besides BR3: from 3 to 5 options, each with non-empty text; at least 2 marked correct and at least 1 not marked. Fewer than 3 or more than 5 options is 400 `question.multiple_options_count`; any other marking is 400 `question.multiple_key_invalid`; an empty option text is `question.option_text_required` (existing). Editing a published question runs the same check and refuses a change that breaks it.
- BR5 A `TrueFalse` key sent with a multiple-answer question is refused 400 `question.key_not_allowed`, as for single choice; nothing is ignored.
- BR6 The student is told how many options to mark: the preview and any later renderer derive "Mark {N} options" from the count of correct options (owner, 2026-10-15). No stored number, no scoring field.
- BR7 The F-98 key-change check compares the set of correct positions of the latest version with the stored one, for every type: moving `{A,B}` to `{A,C}`, adding or removing a correct option each need the reason (`question.key_change_reason_required`), and version 1 is never a key change. A single-choice question behaves as before (a set of one position).
- BR8 Everything the types share is one rule, not three: sanitizing and limits, content language, status, publish and unpublish, delete, the topic guard, origin and paper number, annulment, versions, permission `questions.manage`, 404 answers (F-95 BR1 to BR16, F-96, F-97 BR5, F-98, F-101).
- BR9 The type is declared in one place per layer, as F-97 BR9 sets: the domain (`MultipleAnswerKey` beside `SingleChoiceKey`, one line in `AnswerKey.From`), the contract (`QuestionType`), the form (the options section draws checkboxes for this type) and the preview. No `if` chain in `Question.Apply`.
- BR10 The list shows the type in the existing "Type" chip (F-97 BR8); filter by type stays in F-100.
- BR11 No scoring is stored or computed here: neither "all or nothing" nor partial credit (epic E-6, decision of 2026-10-09).

## Screens and API
No mockup (owner, 2026-10-15): two controls change inside the form of F-95/F-97 and one line in the preview, drawn with kit components and identity tokens only.

- API (routes unchanged, `/api/v1/question-bank/questions`):
  - `SaveQuestionRequest.type` accepts `MultipleAnswer` on create. Options carry `isCorrect` as today; no new field.
  - Responses unchanged in shape: `type` is already in `QuestionResponse` and `QuestionListItemResponse`; `trueFalseKey` is null for this type.
  - Error codes (new): `question.multiple_options_count` (400), `question.multiple_key_invalid` (400). Reused: `question.options_count` (single choice only), `question.key_invalid` (single choice only), `question.option_text_required`, `question.key_not_allowed`, `question.type_changed`, `question.key_change_reason_required`.
  - Data: one migration in the `QuestionBank` context that only updates the comment of `questions.type` and `question_versions.type` (`SingleChoice, TrueFalse or MultipleAnswer`). No column, no check, no backfill.
- `/admin/questions/new` — the "Question type" `AppRadioCards` gets a third card, "Multiple answer" (description "One statement with three to five options; two or more are correct."), after "True/false". Single choice stays preselected.
- `/admin/questions/{id}` — the type stays a read-only labelled value (F-97), now able to read "Multiple answer".
- Options section for `MultipleAnswer`: same card, rows and "Add option" as F-95; in each row the radio is replaced by an `AppCheckbox` (accessible name "Mark {letter} as correct", the F-95 text) so several can be checked; subtitle "3 to 5 options to publish. Mark every correct one: at least two, and at least one wrong."; count "{0} of 5 options". Section errors `question.multiple_options_count` and `question.multiple_key_invalid` appear under the list as `question.options_count` and `question.key_invalid` do. A frozen (annulled) question shows the checkboxes disabled, as the radios are.
- Publish checklist (F-95): for `MultipleAnswer` the items are "3 to 5 options", "Every option has text" and "At least two correct and one wrong"; the aside shows the type.
- Preview dialog: the options list is the one of single choice, with a first line "Mark {N} options." (N from BR6); after "show answer" every correct option gets the icon, the text "Correct answer" and the border (never colour alone), the others none; a draft with no correct option marked shows "no key yet", as today.
- `/admin/questions`: the "Type" chip says "Multiple answer"; nothing else changes.
- Permissions: unchanged, `questions.manage`.

### Wireframe: Question form, options section (multiple answer)
```text
+--------------------------------------------------------+
| Classification                                         |
|  Question type: ( ) Single choice  ( ) True/false      |
|                 (x) Multiple answer            [new]   |
+--------------------------------------------------------+
| Options                                        [changed]|
| 3 to 5 options to publish. Mark every correct one:      |
| at least two, and at least one wrong.                   |
|  [A] [x] Option text ........................ ^ v x    |
|  [B] [ ] Option text ........................ ^ v x    |
|  [C] [x] Option text ........................ ^ v x    |
|  [+ Add option]                       3 of 5 options    |
|  ! At least two correct and one wrong. (error)          |
+--------------------------------------------------------+
| Before publishing: [v] 3 to 5 options                   |
|   [v] Every option has text  [ ] 2+ correct, 1+ wrong   |
+--------------------------------------------------------+
```

### Wireframe: Preview dialog (multiple answer)
```text
+------------------------------------------+
| Preview                                  |
| <base text>                              |
| <statement>                              |
| Mark 2 options.                  [new]   |
|  A. Option text                          |
|  B. Option text                          |
|  C. Option text                          |
| [ Show answer ]                          |
|  -- after reveal --                      |
|  A. Option text   (v) Correct answer     |
|  C. Option text   (v) Correct answer     |
| Explanation: <text>                      |
+------------------------------------------+
```

## Docs
- flow: new docs/flows/multiple-answer-questions.md — how a curator authors, publishes, versions and previews the type, and where its key is declared.

## Acceptance criteria
- AC1 Given a curator with `questions.manage`, when they create a question with `type` `MultipleAnswer`, a topic, a language, a statement and no options, then it is stored as `Draft` of that type and appears in the list with its type. (UC1, BR1, BR3)
- AC2 Given a multiple-answer draft with options marked in any way (none, one, two, all), when it is saved, then it is accepted; with a sixth option, 400 `question.options_count`. (BR3)
- AC3 Given a multiple-answer draft, when it is published with 2 options, or with 6, then 400 `question.multiple_options_count`; with 3 options and an empty text, 400 `question.option_text_required`. (BR4)
- AC4 Given a multiple-answer draft with 3 to 5 options, when it is published with no option, one option, or every option marked correct, then 400 `question.multiple_key_invalid`; with two or more marked and at least one not, it becomes `Published`. (UC3, BR4)
- AC5 Given a published multiple-answer question, when an edit leaves one correct option or marks all, then it is refused and the stored question is unchanged; when the curator marks another valid set, it stays `Published`. (UC2, BR4)
- AC6 Given a `MultipleAnswer` request that sends `trueFalseKey`, then 400 `question.key_not_allowed` and nothing is stored. (BR5)
- AC7 Given a stored multiple-answer question, when an update names another type, then 409 `question.type_changed` and nothing changes; naming its own type is accepted. (BR2)
- AC8 Given a published multiple-answer question with version 1 and correct options `{A,B}`, when the curator saves `{A,C}` without a reason, then 400 `question.key_change_reason_required`; with a reason, version 2 is recorded with `answerKeyChanged` true and the reason. (UC7, BR7)
- AC9 Given a published question with correct options `{A,B}`, when only the text of option C changes, then version 2 is recorded with `answerKeyChanged` false and no reason is required; given a single-choice question whose correct option moves from A to B, the F-98 behaviour is unchanged and its tests pass unedited. (BR7)
- AC10 Given a multiple-answer question, then the shared rules hold for it: HTML sanitized, limits refused, difficulty 0 or 6 refused, a taken paper number refused, a published one cannot be deleted, a topic with only this type is `topic.in_use`, annulment and its reinstatement work. (BR8)
- AC11 Given questions of the three types, then the list shows each row's type and the response names it. (UC5, BR10)
- AC12 Given the form for a new question, when the curator picks "Multiple answer", then the options section draws checkboxes, several can be checked, saving and publishing work; picking another type and back keeps the typed options; on an existing question the type is read-only. (UC1, UC2, BR2, BR9)
- AC13 Given a multiple-answer question, when the curator opens the preview, then the line "Mark {N} options." shows N from the count of correct options; the key and explanation stay hidden until "show answer", then each correct option is marked with icon, text and border. (UC6, BR6)
- AC14 Given the migration, then existing rows and versions keep their type and options and the type column comments list the three values. (BR1)
- AC15 Given a user without `questions.manage`, then create and update of this type answer 403. (BR8)
- AC16 The checkbox group is operable by keyboard (each box one tab stop, Space toggles), every box has an accessible name with its letter, and the two new error texts are announced (WCAG 2.2 AA); checked on screen.
- AC17 All new texts appear in pt-BR, pt-PT and en, including the two error code texts.

## Criterion → test
<!-- Written by /agile:build at its start, one row per criterion above. -->
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
| AC15 | pending |
| AC16 | pending |
| AC17 | pending |

## Decisions
- 2026-10-15 — Scoring (all or nothing, partial credit) is the edition's scoring rule (E-6); the question keeps only its answer key and no scoring mode — same reasoning as the penalty of true/false (epic Question bank, 2026-10-09) — confirmed: docs/epics/question-bank.md
- 2026-10-15 — The type is chosen at creation and fixed after the first save — changing it would drop options or a key without saying so (F-97) — confirmed: F-97
- 2026-10-15 — No mockup and no `/agile:screen`: the change is a third type card, checkboxes in place of radios and one preview line inside forms already designed; two wireframes in this file — confirmed: F-97, docs/epics/question-bank.md
- 2026-10-15 — A published multiple-answer question has 3 to 5 options, at least 2 correct and at least 1 wrong; the number correct is derived from the marks, not stored (Simulae stored `NumCorretas` and a list of identifiers beside the marks) — same cap as F-95, no change to `QuestionLimits` or the counter text; a stored count is a second copy that can disagree — asked (recommended option, owner)
- 2026-10-15 — The student is told how many options to mark, derived from the key — Simulae exposed `RequiredCorrectCount`, and the question is ambiguous without it — asked (recommended option, owner)
- 2026-10-15 — The F-98 key-change check compares the set of correct positions for every type, not the first correct position — `Question.cs:129-132` would miss `{A,B}` to `{A,C}`; single choice is the set of one, so F-98 tests stay green — Claude, verified in `Question.cs`
- 2026-10-15 — Two new error codes (`question.multiple_options_count`, `question.multiple_key_invalid`) instead of reusing `question.options_count` and `question.key_invalid` — the existing texts say "2 to 5" and "exactly one", which would be false for this type — Claude
- 2026-10-15 — The key is a domain unit `MultipleAnswerKey` beside the other two, with its own `Check`; it does not call `QuestionRules.CheckOptions` as it is (that rule counts 2 to 5 and exactly one) but shares its option length and text checks through one helper both use — Claude, to keep one rule for what the types share (F-97 BR5)
- 2026-10-15 — Migration only updates the comments of the two `type` columns; no check constraint: the number of correct options is a publish rule, not a row invariant (a draft may hold any marking) — Claude
- 2026-10-15 — No new package — the item needs nothing the solution lacks — Claude
- 2026-10-15 — Size M, fits one session: one domain unit, one comment-only migration, a third card, checkboxes in the options section, one preview line and the F-98 fix — Claude, from the code read

## Out of scope
- Scoring: all or nothing, partial credit, penalty — epic E-6 with the edition's scoring rules (owner, 2026-10-09).
- Filter by type — F-100. Changing the type of an existing question.
- Shuffling the options (Simulae `PermiteEmbaralhamento`) — E-5, the simulator.
- Six options — the cap of 5 is the module's (owner, 2026-10-15).
- A base text shared by several statements — F-102. Images in options — F-103.

## Open questions
- (none)

## Change notes

## Validation script
<!-- Written at the end of build, in `### English` and `### pt-BR` (references/board.md). -->

## Events

- 2026-10-15 question — asked 2, from context 3

## Delivery
