---
title: Open-answer and essay questions
summary: How a curator authors, publishes, versions and previews an open-answer or essay question, what a rubric is, why its points only split the question's grade, and what is left to the correction items F-150 to F-152.
items: [F-109]
---
# Open-answer and essay questions

Technical terms: [glossary](../glossary.md)

## Trigger
A curator with `questions.manage` picks the "Open answer" or "Essay" card on `/admin/questions/new` (or on the transcription screen of an edition) and saves, or edits, publishes, annuls or previews a question of either type.

## Steps
1. The form shows `RubricKeySection.razor` in place of the options card: up to 10 criterion rows (name, description, points, a remove action), "Add criterion" with the count "n of 10 criteria", the line "Total: n points", and below the rows the size limit of the type (an essay: minimum and maximum lines; an open answer: maximum characters). A new question starts with one empty row. The rows live in the form's own list (`_criteria`), apart from the options and the answers of the other types, so a change of type and back keeps them. The explanation field of these two types is labelled "Model answer" (the same field, the same sanitizing); the grader always sees it and a student sees it after answering.
2. Save sends `POST|PUT /api/v1/question-bank/questions` with `type: OpenAnswer` or `Essay`, no options, `rubric` (the rows with something typed, in order, trimmed) and `answerSizeMin` / `answerSizeMax`. An untouched empty row is not sent; an update replaces the stored rubric with the list sent, and a request that omits `rubric` counts as an empty one.
3. `SaveQuestionHandler` sanitizes the statement with `readBlankMarkers` off, so a `[[1]]` in the statement stays plain text, and passes the criteria as plain text (a name and a description are never HTML) to `QuestionContent`.
4. `Question.Apply` builds the key with `AnswerKey.From`, which returns an `OpenAnswerKey` or an `EssayKey` (both are a `RubricKey`, which holds the shared checks). Its `Check` refuses what the type does not have (options, a true/false key, left items, blanks), then more than 10 criteria, then each criterion in order (name required, name too long, description too long, points outside 1 to 100, a name equal to an earlier one once case and extra white space are ignored), then the size limit; to publish it asks at least one criterion. A refusal that is about one criterion carries its position, from 1, among the criteria sent.
5. The six older keys (`SingleChoiceKey`, `TrueFalseKey`, `MultipleAnswerKey`, `MatchingKey`, `FillInTheBlanksKey`, `ShortAnswerKey`) take a rubric and a size limit as trailing optional parameters and refuse them (`question.rubric_not_allowed`, `question.answer_size_not_allowed`): nothing is ignored.
6. The rubric is stored in `question_bank.question_rubric_criteria` (one row per criterion, `position` from 1, unique among the live rows; a row is reused by position when the rubric changes) and the size limit in `questions.answer_size_min` and `answer_size_max`, whose unit follows the type. A check constraint keeps the limit to the two types and inside their ranges. Publishing, sending for review and a reviewer's approval rebuild the key from the stored rows (`Question.StoredKey`) and run the same check with `published: true`.
7. A save of a published question calls `Question.NextVersion`. The version copies the rubric as the JSON column `question_versions.rubric` (null on a version recorded before this item, read as an empty rubric) and the size limit as two columns. The key of a rubric is the set of its (name, maximum points) pairs under the comparison rule of typed answers: adding, removing or renaming a criterion, or changing its points, is a key change and asks for a reason; a description, the order of the criteria, the statement, the explanation or the size limit is versioned content but not a key change. Annulment, reinstatement and the base-text fan-out copy the rubric and the size with no change (`QuestionVersion.CopyAs`; `BaseTextStore` and `QuestionStore` load the criteria).
8. The preview (`QuestionPreviewContent.razor`) shows the statement, an inert multi-line box labelled "Your answer" or "Your essay" (the kit's `AppTextField`, disabled and always empty), the task line ("Write your answer." / "Write your essay.") and the limit line ("Up to n characters.", "Between a and b lines.", "At least a lines.", "Up to b lines."; none when no limit is set). "Show answer" lists the rubric as a table (criterion, description, maximum points, total) under "Grading rubric", with the check icon and the text, then the model answer; a draft with no criterion says "no rubric yet".

## Rules
- The type is chosen at creation and never changes (F-97 BR2).
- The key is the rubric. The grade of a question is the points given divided by the total of the maximums, so the rubric points only split the question's own grade; how much the question is worth in an exam is the edition's scoring rule (epic E-6), and this item stores no points or weight for the question itself.
- A criterion has a name (1 to 100 characters after trimming, unique in the rubric), an optional description (up to 500) and maximum points (a whole number from 1 to 100). A rubric has at most 10 criteria, and the total of the points is not fixed.
- The size limit is optional. Essay: a minimum and a maximum number of lines, each from 1 to 100, the minimum not above the maximum. Open answer: a maximum number of characters from 1 to 5,000 and no minimum. It is data for the simulator; nothing here enforces it on a student.
- Any draft may have no criterion yet; a published question has at least one, and an edit of a published question runs the same check.
- A rubric or a size limit on any other type is refused; options, a true/false key, left items or blanks on these two types are refused with the existing codes.
- A grade names the rubric that graded it by the question's version number and the criterion's position (a version keeps no criterion id). F-151 decides whether that is enough.
- Not here: grading, a grader role, the correction queue and the suggested grade (F-150, F-151, F-152); a student's attempt; performance levels per criterion.

## Wireframe
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

## Errors
- `question.rubric_criteria_count` (400) — more than 10 criteria.
- `question.rubric_criteria_required` (400) — a publish with no criterion.
- `question.criterion_name_required` (400) — an empty name; `position` is the criterion's, from 1.
- `question.criterion_name_too_long` (400) — a name over 100 characters after trimming; `position` is the criterion's.
- `question.criterion_description_too_long` (400) — a description over 500 characters; `position` is the criterion's.
- `question.criterion_points_out_of_range` (400) — points missing, or outside 1 to 100; `position` is the criterion's.
- `question.criterion_name_repeated` (400) — a name equal to an earlier one; `position` is the later criterion's.
- `question.answer_size_invalid` (400) — a limit out of range, a minimum above the maximum, or a minimum on an open answer.
- `question.rubric_not_allowed`, `question.answer_size_not_allowed` (400) — a rubric or a size limit sent for another type.
- `question.options_not_allowed`, `question.key_not_allowed`, `question.match_items_not_allowed`, `question.blanks_not_allowed` (400) — what the type does not have.
- `question.key_change_reason_required` (400) — a published question's rubric key changed without a reason.
- `question.type_changed` (409) — an update naming another type.
