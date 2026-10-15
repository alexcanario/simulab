---
feature: F-102
epic: Question bank
status: refining
board: 148
version: 1
---
# Shared base text

Technical terms: [glossary](../glossary.md)

## Summary
One `BaseText` (a reading passage, a table, a case) serves several questions, as in reading comprehension blocks: the curator writes it once and links the questions to it; the simulators show it beside each of them. F-95 keeps only an inline base text per question. Source: epic Question bank (E-4), glossary `BaseText`.

## Start
- Depends on: F-95 (done).
- Waits on (to start): nothing.
- Needed to validate: the local app host — owner.
- Suggested path: `/agile:refine` with `/agile:screen` → `/agile:build`.
- Parallel with: F-104.

## Goal
A curator writes a passage once and links every question that refers to it, so a reading block of ten questions is one text that is fixed in one place, not ten copies that drift apart.

## What exists (verified in the code, 2026-10-15)
- `Question.BaseTextHtml` is an inline, optional field: column `question_bank.questions.base_text_html`, 20 000 characters of sanitized HTML (`QuestionLimits.BaseTextMaxLength`); error `question.base_text_too_long`. F-95.
- `QuestionVersion.BaseTextHtml` copies it into the immutable snapshot (F-98 BR1); `Question.NextVersion` compares it with the latest version; an annulled question is frozen (F-101 BR5).
- `SaveQuestionRequest.BaseTextHtml`, `QuestionResponse.BaseTextHtml`, `QuestionVersionResponse.BaseTextHtml`; the form section "Base text" (`AppRichTextField Id="question-base-text"`), the preview (`QuestionPreviewDialog`) and the transcription screen (F-99 BR3 clears it after each save) read that field only.
- There is no `BaseText` entity, table, route or screen. The glossary already defines `BaseText` ("Text or image shared by one or more questions").
- Permission `questions.manage` gates every question route and screen (F-95 BR14). A topic in use is protected by an inverted usage check (ADR-0005 #3).

## Users and use cases
- UC1 A curator creates a base text: a title, a content language and the text, written in the visual editor.
- UC2 A curator edits a base text; every published question linked to it gets a new version.
- UC3 A curator links a question to a base text in the question form, changes the link or removes it.
- UC4 A curator opens a base text and sees the questions linked to it.
- UC5 A curator finds base texts by title or language and deletes one that no question uses.
- UC6 A curator transcribes a paper: the base text chosen for one question stays chosen for the next, until it is changed.

## Business rules
- BR1 A base text has a title (1 to 200 characters after trimming), a content language (one of the app's languages, canonical, never translated) and a text of sanitized HTML, required, up to 20 000 characters of sanitized HTML. A refused value is never truncated. Global data: `TenantId` is null in v1.
- BR2 A question has at most one base text, held by `BaseTextId`; none is the normal case. The inline field is gone: a question never carries base text of its own.
- BR3 A question and its base text are in the same content language. Linking a question of another language, or changing the language of a question that is linked, is refused (`question.base_text_language_mismatch`); the language of a base text that has linked questions cannot change (`base_text.language_in_use`).
- BR4 Editing the text of a base text records, in the same transaction, a new version (F-98) of every linked question that is `Published` and whose latest version holds a different base text HTML: same content, the new base text HTML, not an answer key change, no reason. A `Draft` follows the live text and makes no version; an `Annulled` question is frozen and makes none (F-101 BR5). Editing only the title makes no version (it is not versioned content).
- BR5 What a student was shown stays what the version says: a version still copies the base text HTML (F-98 BR1 is unchanged), and a version already recorded is never touched.
- BR6 A base text that any live question links, whatever its status, cannot be deleted (`base_text.in_use`, 409); the curator removes the links first. A base text with no question is deleted softly, as the other bank data.
- BR7 A link to a base text that is unknown or deleted answers 404 `question.base_text_not_found` before the content is judged, as the topic does (F-95 BR16).
- BR8 The text is sanitized by the Api on every write against the one allow-list of the bank (ADR-0005 #5); the Web shows what was stored.
- BR9 Where a question shows its base text (preview, student preview), the linked text is shown before the statement, as the inline text was (F-95 BR5).
- BR10 Existing data: every question with a non-empty inline base text (live or soft-deleted) gets one `BaseText` of its own, linked to it: title = the first 80 characters of the plain text, language = the question's, text = the stored HTML; then the inline column is dropped. No version is made (the HTML is the same) and no two texts are merged by the migration.
- BR11 The list of base texts shows title, language and the number of linked questions, sorted by title; it filters by language and searches by title. The detail shows the questions linked (excerpt, status, paper number, a link to the question), most recently changed first.
- BR12 Every route and screen of base texts needs `questions.manage`, checked by the endpoint and by the screen (menu, page, action), by permission (F-95 BR14).
- BR13 The transcription screen (F-99) keeps the chosen base text across saves, as it keeps the edition, notice subject and content language; the type, statement, difficulty and key are still cleared (this changes F-99 BR3 for the base text only). If the question's language is changed to one the text does not have, the choice is cleared with a message.

## Screens and API
- `/admin/base-texts` — [new] the list: search by title, language filter, columns of BR11, create, open, delete (refused with the reason when in use).
- `/admin/base-texts/new` and `/admin/base-texts/{id}` — [new] the form: title, language, text (`AppRichTextField`), the linked questions (detail only), delete. One way to edit.
- `/admin/questions/new`, `/admin/questions/{id}` — [changed] the "Base text" section: a picker of the base texts in the question's language, "open" and "remove link", and a link to create a new one; the inline editor is removed. Details of the pick-and-create flow are settled by `/agile:screen`.
- `/admin/questions/{id}` preview, transcription screen (F-99) — [changed] as BR9 and BR13.
- Routes under `/api/v1/question-bank`, gated by `questions.manage`:
  - `GET /base-texts` (`search`, `language`, `page`, `pageSize`) → page of `BaseTextListItemResponse(Id, Title, ContentLanguage, QuestionCount, UpdatedAt)`.
  - `GET /base-texts/{id}` → `BaseTextResponse(Id, Title, ContentLanguage, TextHtml, QuestionCount, UpdatedAt)`.
  - `POST /base-texts`, `PUT /base-texts/{id}` with `SaveBaseTextRequest(Title, ContentLanguage, TextHtml)`; `DELETE /base-texts/{id}`.
  - `GET /base-texts/{id}/questions` → page of the question list items linked to it.
  - `SaveQuestionRequest.BaseTextHtml` is replaced by `BaseTextId` (nullable); `QuestionResponse.BaseTextHtml` by `BaseText` (`Id`, `Title`, `TextHtml`; null when none). `QuestionVersionResponse.BaseTextHtml` is unchanged.
- Migration `question_bank`: table `base_texts` (id, tenant, title, content language, text html, audit and soft-delete columns, column comments), `questions.base_text_id` with a foreign key inside the module that restricts delete and an index, the backfill of BR10, `base_text_html` dropped from `questions`.
- Error codes: `base_text.not_found`, `base_text.title_required`, `base_text.title_too_long`, `base_text.text_required`, `base_text.text_too_long`, `base_text.content_language_invalid`, `base_text.in_use`, `base_text.language_in_use`, `question.base_text_not_found`, `question.base_text_language_mismatch`; `question.base_text_too_long` leaves with the inline field.

### Wireframe: Base texts
```text
+--------------------------------------------------------+
| Base texts                              [ New base text ] [new]
| [ Search by title ______ ] [ Language v ]              |
| Title                       Language  Questions        |
| Reading text 1 - the dam    pt-BR     8       [open]   |
| Case study - the clinic     pt-BR     3       [open]   |
| Table of tariffs            en        0       [delete] |
+--------------------------------------------------------+
```

### Wireframe: Base text
```text
+--------------------------------------------------------+
| Base text                                [new]         |
| Title      [ Reading text 1 - the dam ____________ ]   |
| Language   [ pt-BR v ]   (locked while questions use it)|
| Text       [ visual editor ........................ ]  |
| Questions that use this text (8)                       |
|   Q12 Published  "According to the text, the dam..."   |
|   Q13 Draft      "The word 'cota' in the third..."     |
| [ Save ]  [ Delete ]  (refused while in use)           |
+--------------------------------------------------------+
```

### Wireframe: Question form, base text section
```text
+--------------------------------------------------------+
| Base text                                    [changed] |
| [ Reading text 1 - the dam            v ] [open] [x]   |
| Only texts in the question's language are offered.     |
| No text yet? [ Create a base text ]                    |
+--------------------------------------------------------+
```

## Docs
- flow: new docs/flows/shared-base-text.md — manage a base text, link and unlink, the versions an edit makes, the delete guard, the transcription.
- flow: update docs/flows/transcribe-paper.md#after-each-save — the base text stays chosen.

## Acceptance criteria
- AC1 Given a curator with `questions.manage`, when they create a base text with a title, a language and a text, then it is stored with sanitized HTML and answered with 201; a blank title, a title over 200, a text that is empty once its tags are removed, a text over 20 000 characters or an unknown language answer 400 with `base_text.title_required`, `base_text.title_too_long`, `base_text.text_required`, `base_text.text_too_long`, `base_text.content_language_invalid`, and nothing is stored. (UC1, BR1, BR8)
- AC2 Given a base text, when a question in the same language is saved with its `BaseTextId`, then the question response carries the text; when saved with another language, then 409 `question.base_text_language_mismatch`; when the id is unknown or deleted, then 404 `question.base_text_not_found`; when `BaseTextId` is null, the link is removed. (UC3, BR2, BR3, BR7)
- AC3 Given a base text linked by a published question at version N and by a draft and an annulled question, when its text changes, then the published question has version N+1 with the new base text HTML, `AnswerKeyChanged` false and no reason, the draft and the annulled question get none, and versions 1 to N are unchanged; when only the title changes, no version is made. (UC2, BR4, BR5)
- AC4 Given a base text linked by two published questions, when its text changes, then both versions are recorded in one save and a failure of either leaves the text and both questions as they were. (BR4)
- AC5 Given a base text that has linked questions, when its language is changed, then 409 `base_text.language_in_use`; when it has none, the language changes. (BR3)
- AC6 Given a base text linked by a live question of any status, when it is deleted, then 409 `base_text.in_use` and it stays; given a base text with no question, then it is deleted softly and disappears from the list and from the picker. (UC5, BR6)
- AC7 Given the migration on a bank with questions that have inline base text, then each of those gets one linked base text with the stored HTML, the question's language and the 80-character title, the questions without base text have no link, the column `base_text_html` is gone and the number of versions is unchanged. (BR10)
- AC8 Given base texts in two languages, when the list is opened, then it shows title, language and the number of linked questions sorted by title; the search narrows by title and the language filter by language. (UC5, BR11)
- AC9 Given a base text, when its detail is opened, then it lists the linked questions with excerpt, status and paper number, and each opens the question. (UC4, BR11)
- AC10 Given a user without `questions.manage`, when any base text route is called, then 403, and the menu entry, the page and the actions are not offered. (BR12)
- AC11 Given a question with a linked base text, when it is previewed, then the base text is shown before the statement, and a question without one shows none. (BR9)
- AC12 Given the transcription screen, when a question is saved with a base text chosen, then the next question still has it chosen while type, statement, difficulty and key are blank; when its language is changed to another, the choice is cleared with a message. (UC6, BR13)
- AC13 Given the question form, when the curator picks, changes and removes the base text, then only texts of the question's language are offered, the choice is saved with the question and "unsaved changes" is shown while it differs from the stored one. (UC3, BR3)
- AC14 All new texts appear in every locale of `UI locales:` (pt-BR, pt-PT, en), the missing-key test is green, and every new error code has its text in the three languages.

## Criterion → test
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

## Decisions
- 2026-10-15 — One concept: every question links to a `BaseText` or has none; the inline field is dropped and the migration turns each existing text into a base text linked to its question — one way to edit, the data keeps nothing in two places — asked.
- 2026-10-15 — Editing a base text records a new version of each linked published question in the same transaction; the version keeps copying the HTML — attempts keep what the student saw and the F-98 model is untouched — asked.
- 2026-10-15 — A base text carries title and content language; the language is locked while questions use it — a mixed-language block is a data error, and the question picker can filter by language — asked.
- 2026-10-15 — A base text in use is not deleted: `base_text.in_use`, like a topic in use (ADR-0005 #3) — asked.
- 2026-10-15 — Own list and form pages under `/admin/base-texts`; the question form only picks and unpicks — one way to edit an item (rule `ui-project`); the screen design settles the pick-and-create flow — asked.
- 2026-10-15 — Permission `questions.manage`; no new permission to seed — asked.
- 2026-10-15 — The transcription keeps the chosen base text across saves — a reading block is transcribed in sequence — asked.
- 2026-10-15 — Size judged: one module, one migration, three screens; doubt keeps it whole, as the epic sizes it M — technical, no owner card.
- 2026-10-15 — Draft questions follow the live text and an annulled question makes no version, so the back office of an annulled question may show the edited text while its latest version is what was graded — F-101 BR5 freezes content, and a version is what attempts use — technical.
- 2026-10-15 — The foreign key from `questions.base_text_id` to `base_texts` is inside the module and restricts delete; the topic and edition stay ids with no foreign key — ADR-0005 #2 — confirmed: docs/decisions/ADR-0005-question-bank-module.md.
- 2026-10-15 — The text is sanitized HTML written in the visual editor and sanitized by the Api on every write — confirmed: docs/decisions/ADR-0005-question-bank-module.md (#5).
- 2026-10-15 — No new package; the migration backfill and the editor are the existing ones — technical.

## Out of scope
- Images in a base text — F-103 (`IFileStorage`).
- Merging duplicate base texts the migration created, or finding near-duplicates — a later item if it hurts.
- A filter of the question list by base text — the base text's detail lists its questions; an idea if the list needs it.
- How a simulator lays the base text beside the question — epics E-5 and E-6.
- Sharing a statement stem or an option set between questions — not asked.

## Open questions
- (none)

## Change notes

## Validation script
<!-- Written at the end of the build. -->

## Events

## Delivery
