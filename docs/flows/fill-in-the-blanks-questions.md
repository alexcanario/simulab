---
title: Fill-in-the-blanks questions
summary: How a curator authors, publishes, versions and previews a fill-in-the-blanks question, how markers and blanks form the key, and the one rule that compares a typed answer.
items: [F-107]
---
# Fill-in-the-blanks questions

Technical terms: [glossary](../glossary.md)

## Trigger
A curator with `questions.manage` picks the "Fill in the blanks" card on `/admin/questions/new` (or on the transcription screen of an edition) and saves, or edits, publishes, annuls or previews a question of that type.

## Steps
1. The statement field gets the toolbar button "Insert blank" (`AppRichTextField.BlankMarkers`, drawn by `AppRichTextToolbar` only for this type). In the browser, `rich-text.js` reads the editor's own text, takes the highest marker number it holds and writes `[[n+1]]` at the caret (`[[1]]` when none). Typing a marker by hand works the same.
2. The form shows `FillInTheBlanksKeySection.razor` in place of the options card: one row per distinct marker of the statement, in statement order (`StatementBlanks.Markers`, the text between tags only), each with up to 5 plain-text answer fields. Removing a marker from the statement drops its row and its answers; typing it back starts an empty row. The answers are kept per number, so a change of type and back keeps them.
3. Save sends `POST|PUT /api/v1/question-bank/questions` with `type: FillInTheBlanks`, no options, and `blanks` (`{ number, acceptedAnswers }`), one per marker the statement holds now.
4. `SaveQuestionHandler` sanitizes the statement with `readBlankMarkers` on (only for this type): `QuestionHtmlSanitizer` reads the markers from the parsed text nodes, before any picture's alternative text is added, so a marker split by formatting or inside an attribute is plain text; each marker reads as `____` in the plain text, which the list excerpt and the search use. The accepted answers stay plain text.
5. `Question.Apply` builds the key with `AnswerKey.From(..., blanks, blankMarkers)`: a `FillInTheBlanksKey` with one blank per marker in statement order, answers trimmed. Its `Check` refuses what the type does not have, then the broken data (repeated marker or number, a blank with no marker, over 10 markers, the limits of each blank), and to publish it asks 1 to 10 markers, each answered. The blanks are stored in `question_bank.question_blanks`, one row per marker (a draft marker with no answer included), with its statement `position`; `ReplaceBlanks` reuses the row of a number that stays.
6. Publishing (or sending for review, or a reviewer's approval) rebuilds the key from the stored rows (their numbers are the markers, in `position` order) and runs the same check with `published: true`.
7. A save of a published question calls `Question.NextVersion`: the version snapshots the blanks in statement order in the JSON column `question_versions.blanks`. `SameVersionedContent` compares them exactly; the key changed (a reason is needed) when the set of numbers differs or the answers of a blank differ as a set under `AcceptedAnswer.Normalize`. An annulment or a reinstatement copies the blanks (`QuestionVersion.CopyAs`), and an edit of a linked base text loads them (`BaseTextStore`).
8. `GET` answers carry `blanks` (`{ number, acceptedAnswers }`, statement order) on `QuestionResponse` and `QuestionVersionResponse`, empty for the other types and for versions recorded before F-107.
9. The preview (`QuestionPreviewContent.razor`) draws each marker of the stored statement as an underlined box with its number, named "Blank n", then "Fill in each blank."; "show answer" fills each box with its first accepted answer and lists every answer as "Blank n: a, b" (or "no key yet") with the check icon and the text "Correct answer".

## Rules
- The type is chosen at creation and never changes (F-97 BR2).
- A marker is `[[n]]`, `n` 1 to 99 with no leading zero (`BlankMarker.Pattern`, shared by the Api and the Web), in the statement only; the numbers need not be consecutive.
- The comparison rule of typed answers, the one of the module (F-108 reuses it): trim, collapse white space, ignore case (invariant culture); accents and every other character count (`AcceptedAnswer.Matches`). It is not `ComparableText`, which strips accents for search.
- Any draft: at most 10 markers, no repeated marker or number, no blank without a marker, at most 5 answers per blank, each 1 to 100 characters after trimming, no two equal under the rule. A marker with no answer is allowed.
- Published: 1 to 10 markers, every one answered.
- Options, a true/false key or left items on this type are refused (`question.options_not_allowed`, `question.key_not_allowed`, `question.match_items_not_allowed`); blanks on any other type are `question.blanks_not_allowed`.
- The unique index `ux_question_blanks_tenant_question_number` covers live rows only (a removed blank is soft-deleted and keeps its number).
- No scoring is stored (epic E-6); grading a student's answer is the simulator's (E-5).

## Wireframe
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

## Errors
- `question.blanks_count` (400) — more than 10 markers, or a publish with none.
- `question.blank_repeated` (400) — the same number twice in the statement or in the blanks sent.
- `question.blank_marker_missing` (400) — a blank sent for a number with no marker.
- `question.blank_answers_required` (400) — a published blank with no answer; the problem's `position` is the blank number.
- `question.blank_answers_count` (400) — more than 5 answers; `position` is the blank number.
- `question.blank_answer_required` (400) — an empty answer; `position` is the blank number.
- `question.blank_answer_too_long` (400) — an answer over 100 characters after trimming; `position` is the blank number.
- `question.blank_answer_repeated` (400) — two answers of a blank equal under the rule; `position` is the blank number.
- `question.blanks_not_allowed` (400) — blanks sent for another type.
