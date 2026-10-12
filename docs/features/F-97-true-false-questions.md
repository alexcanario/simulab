---
feature: F-97
epic: Question bank
status: refining
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
<!-- Written by /agile:build at its start. -->
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
<!-- Written at the end of the build. -->

## Events

- 2026-10-12 question — asked 4, from context 0

## Delivery
