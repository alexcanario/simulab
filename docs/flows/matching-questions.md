---
title: Matching questions
summary: How a curator authors, publishes, versions and previews a matching question, and where its key lives.
items: [F-106]
---
# Matching questions

Technical terms: [glossary](../glossary.md)

## Trigger
A curator with `questions.manage` picks the "Matching" card on `/admin/questions/new` (or on the transcription screen of an edition) and saves, or edits, publishes, annuls or previews a question of that type.

## Steps
1. The form shows the key section `MatchingKeySection.razor` in place of the options card: the left list (numbered items, `MatchItemDraft`) and the right list (the question's options, lettered by position). Each left row has a select of the letters no other row took; a row holds the right item itself, so moving a right row keeps the match and removing it clears the match.
2. Save sends `POST|PUT /api/v1/question-bank/questions` with `type: Matching`, `options` (right items, `isCorrect` false) and `matchItems` (`{ textHtml, matchPosition }`, `matchPosition` the 0-based option position or null).
3. `SaveQuestionHandler` sanitizes the left items like the options, builds `QuestionContent.MatchItems` and hands the left items' image ids to the image ownership (F-103).
4. `Question.Apply` checks the images of the statement, the options and the left items together (ten at most), then the key: `AnswerKey.From` builds a `MatchingKey`, whose `Check` is `QuestionRules.CheckMatching`. The left items are stored in `question_bank.question_match_items`; `match_position` is the only copy of the key.
5. Publishing (or sending for review, or a reviewer's approval) runs the same check with `published: true`.
6. A save of a published question calls `Question.NextVersion`: the version snapshots the left items in the JSON column `question_versions.match_items`; a change of the number of left items or of any match position is an answer key change and needs a reason. An annulment or a reinstatement copies the left items (`QuestionVersion.CopyAs`), and an edit of a linked base text loads them (`BaseTextStore.ListPublishedLinkedAsync`), so neither sees a key change that did not happen.
7. `GET` answers carry `matchItems` (`{ position, textHtml, matchPosition }`) on `QuestionResponse` and `QuestionVersionResponse`, empty for the other types and for versions recorded before F-106.
8. The preview (`QuestionPreviewContent.razor`) shows "Match each number with one letter.", the numbered items and the lettered matches in authored order; "show answer" adds "n → letter" to each item and marks each matched right item with an icon, a text and a border. An extra right item shows nothing.

## Rules
- The type is chosen at creation and never changes (F-97 BR2).
- Any draft: at most 8 items on each side; a match that names a missing right item is `question.matching_key_invalid`, two items on one right item `question.matching_key_repeated`; an unset match is allowed.
- Published: 2 to 8 left items, at least as many right items, every text written, every left item matched. Extra right items are allowed.
- A true/false key or a marked right item on a matching question is `question.key_not_allowed`; left items on any other type are `question.match_items_not_allowed`.
- No scoring is stored: all-or-nothing or partial credit belongs to the edition's scoring rules (epic E-6).
- Images: the left items count with the statement and the options under the F-103 cap, and a version that shows an image in a left item keeps its file (`QuestionImageStore.IsHeldByAVersionAsync` scans `match_items`).

## Wireframe
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

## Errors
- `question.matching_items_count` (400) — more than 8 on a side, or a publish with fewer than 2 left items or fewer right items than left items.
- `question.matching_key_invalid` (400) — a match names a missing right item, or a published left item has no match.
- `question.matching_key_repeated` (400) — two left items name the same right item.
- `question.match_item_text_required` (400) — a published left item has no text; the problem carries its position.
- `question.match_item_too_long` (400) — a left item over 2,000 characters; the problem carries its position.
- `question.match_items_not_allowed` (400) — left items sent for another type.
