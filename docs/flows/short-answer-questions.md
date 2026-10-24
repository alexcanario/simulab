---
title: Short-answer questions
summary: How a curator authors, publishes, versions and previews a short-answer question, why its key is one blank without a marker, and the comparison rule it shares with fill-in-the-blanks.
items: [F-108]
---
# Short-answer questions

Technical terms: [glossary](../glossary.md)

## Trigger
A curator with `questions.manage` picks the "Short answer" card on `/admin/questions/new` (or on the transcription screen of an edition) and saves, or edits, publishes, annuls or previews a question of that type.

## Steps
1. The form shows `ShortAnswerKeySection.razor` in place of the options card: up to 5 plain-text answer fields (a new question starts with one empty field), "Add answer" with the count, a remove action per answer. The statement toolbar has no "Insert blank" button for this type, and a `[[1]]` typed in the statement stays plain text. The answers live in the form's own list (`_shortAnswers`), apart from the fill-in answers, so a change of type and back keeps both.
2. Save sends `POST|PUT /api/v1/question-bank/questions` with `type: ShortAnswer`, no options, and `blanks` holding one entry `{ number: 1, acceptedAnswers }` (the trimmed, non-empty fields). The field `blanks` is the one F-107 added; this item adds no table, column, route or response field.
3. `SaveQuestionHandler` sanitizes the statement with `readBlankMarkers` off (only fill-in-the-blanks reads markers), so the statement keeps `[[1]]` as text and the list excerpt shows it as typed.
4. `Question.Apply` builds the key with `AnswerKey.From`, which returns a `ShortAnswerKey`. Its `Check` refuses what the type does not have (options, a true/false key, left items), then more than one blank (`question.blanks_count`) or one not numbered 1 (`question.blank_marker_missing`), then the limits of the one blank through `FillInTheBlanksKey.CheckAnswers` (one place for the limits); to publish it asks at least one accepted answer. `ShortAnswerKey.Blanks` always holds the one blank, even when no answer was written.
5. The blank is stored in `question_bank.question_blanks` like a fill-in blank (number 1, position 0). Publishing, sending for review and a reviewer's approval rebuild the key from the stored row and run the same check with `published: true`.
6. A save of a published question calls `Question.NextVersion`; `BlanksKeyChanged` already compares the blanks of every type, so adding or removing an accepted answer is a key change (a reason is asked) and a new statement, another order or only another case or spacing is not. Annulment, reinstatement and the base-text fan-out copy the blank with no change.
7. The preview (`QuestionPreviewContent.razor`) shows the statement, an inert text box labelled "Your answer" (the kit's `AppTextField`, disabled and always empty, so it gives no hint of the answer's length) and "Type your answer."; "show answer" lists "Accepted answers: a, b" (or "no key yet") with the check icon and the text "Correct answer".
8. Error texts: the codes say "blank" (`question.blank_answers_required`, `question.blank_answers_count`, `question.blank_answer_repeated`); the form shows the short-answer words through `QuestionForm.BlankErrorText`, which maps those three codes to `Questions.ShortAnswer.Error.*` keys. The other blank codes read the same for both types.

## Rules
- The type is chosen at creation and never changes (F-97 BR2).
- The key is exactly one blank numbered 1. A request may omit `blanks`; a response always carries the blank, empty on a draft with no answer. The simulator (E-5) reads `blanks[0]` for this type.
- The comparison rule of typed answers is the one of the module, written by F-107 (`AcceptedAnswer.Matches`): trim, collapse white space, ignore case (invariant culture); accents count. This item applies it only to refuse two equal answers; grading a student's answer is the simulator's (E-5).
- Any draft: at most 5 answers, each 1 to 100 characters after trimming, no two equal under the rule; no answer is allowed. Published: at least one answer.
- Options, a true/false key or left items on this type are refused; blanks on single choice, true/false, multiple answer and matching are `question.blanks_not_allowed`.
- No scoring is stored (epic E-6).

## Wireframe
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

## Errors
- `question.blanks_count` (400) — more than one blank sent.
- `question.blank_marker_missing` (400) — one blank sent but not numbered 1.
- `question.blank_answers_required` (400) — a publish with no accepted answer; `position` is 1.
- `question.blank_answers_count` (400) — more than 5 answers; `position` is 1.
- `question.blank_answer_required` (400) — an empty answer; `position` is 1.
- `question.blank_answer_too_long` (400) — an answer over 100 characters after trimming; `position` is 1.
- `question.blank_answer_repeated` (400) — two answers equal under the rule; `position` is 1.
- `question.options_not_allowed`, `question.key_not_allowed`, `question.match_items_not_allowed` (400) — what the type does not have.
- `question.blanks_not_allowed` (400) — blanks sent for single choice, true/false, multiple answer or matching.
- `question.type_changed` (409) — an update naming another type.
