---
feature: F-95
epic: Question bank
status: refining
board: 141
version: 1
---
# Single-choice questions in the back office

Technical terms: [glossary](../glossary.md)

## Summary
A curator writes a single-choice question in the back office: statement, an inline base text, options, answer key, explanation, difficulty, topic and content language; saves it as `Draft`, publishes it, lists it and previews it as a student will see it. This item creates the new `QuestionBank` module (schema `question_bank`, five-project shape, references `Topic` by id through `Catalog.Contracts`), the safe content format for statements and options (sanitized HTML edited in a visual editor), the question permission and the guard that blocks deleting a topic in use (F-79 BR9). Source: epic Question bank (E-4), decisions of 2026-10-09; Simulae `Question.cs` and `ConfigurationBase.cs` (T1) as the import source.

## Start
- Depends on: F-79 (done), F-75 (done).
- Waits on (to start): nothing.
- Needed to validate: the local app host — owner.
- Suggested path: `/agile:refine` with `/agile:screen` → `/agile:build`.
- Parallel with: none; every other E-4 feature needs it. F-77 also edits Catalog (aliases): expect a small conflict in `Catalog.Contracts` and `DeleteTopicHandler`.

## Goal
Give the question bank its first content: a curator can author, publish, find and preview a single-choice question, so every later E-4 feature adds to a working module instead of creating it.

## What exists (verified 2026-10-10)
- No question code in `src/`; the `Catalog` module has `Topic` and `Subject` (F-79) and a topic delete guard for one reason only: a live notice subject maps it, `topic.in_use` (F-75, `DeleteTopicHandler`, `ITopicStore.IsMappedAsync`).
- `docs/agile/profile.md` line 11 says `Catalog` also holds the question bank; the epic decision of 2026-10-09 makes `QuestionBank` its own module. This item corrects the profile and records it in ADR-0005.
- The content-language list is one list for the whole app: `SupportedLanguages` in `Identity.Contracts`; `Exam.ContentLanguage` already uses it.
- Markdig 1.3.2 is in `Directory.Packages.props` and used by `LegalDocumentProvider` with `DisableHtml()`. Measured on 2026-10-10: that pipeline still emits `<a href="javascript:...">` and `<img src="data:...">`. Hence sanitized HTML, not Markdown (comparison: `docs/features/mockups/F-95-content-format-comparison.md`).
- UI kit has `AppDataTable`, `AppChecklistItem`, `AppLookupField`, `AppConfirmDialog`, `AppRadioCards`; it has no rich-text field.
- Permissions: a module declares a `PermissionCatalog` with `InitialGrants` (F-33, F-36); the Web lists the same names in `WebPermissions`.
- Simulae defects not to import: raw HTML typed in a text field with no sanitizer; `EmRevisao` and `Rascunho` never assigned; the wrong-answer penalty on each question (E-6 owns it).

## Users and use cases
- UC1 A curator creates a question as `Draft`: topic, content language, statement, optionally base text, options with the correct one marked, explanation, difficulty.
- UC2 A curator edits a question, `Draft` or `Published`, in place.
- UC3 A curator publishes a complete `Draft`.
- UC4 A curator unpublishes a `Published` question back to `Draft`.
- UC5 A curator lists questions, filters by status and searches the statement text.
- UC6 A curator previews a question as a student will see it, then reveals the answer key and explanation.
- UC7 A curator deletes a `Draft`.
- UC8 An admin deletes a topic; the topic is refused while a live question points at it.

## Business rules
- BR1 `Question : TenantEntity` maps to `question_bank.questions`, its options to `question_bank.question_options`. Global data: `TenantId` null; delete is a soft delete.
- BR2 A question has a type (`QuestionType.SingleChoice` is the only value this item defines; each later type adds its own), a status (`Draft` or `Published`), a statement, an optional base text, an optional explanation, an optional difficulty, a topic and a content language.
- BR3 `Draft` needs a non-empty statement (empty after tags are removed counts as empty), a topic and a content language; it may have from 0 to 5 options and no correct one yet.
- BR4 `Published` needs, besides BR3: from 2 to 5 options, each with non-empty text, and exactly one marked correct. Explanation and difficulty stay optional. Editing a published question runs the same check and refuses a change that breaks it.
- BR5 Options keep a fixed position; the letters A to E come from the position, never stored. Reordering is by moving an option up or down in the form.
- BR6 Difficulty is a whole number from 1 to 5 or empty.
- BR7 The content language is one of `SupportedLanguages`, canonicalized, and is never translated (ADR-0001 #27). The form preselects the curator's UI language.
- BR8 Every text field (statement, base text, option, explanation) is HTML written by a person, so the Api sanitizes it on every write (create, update) against an allow-list and stores only the sanitized result. The response carries what is stored. Allow-list: `p br strong b em i u s sub sup ul ol li blockquote table thead tbody tr th td` and `a` with `http`/`https` hrefs only (`rel="noopener noreferrer"`, `target` dropped). No `img` (F-103), no `style`, `class` or `data-*`, no script, no event attributes. List markup produced by the editor is normalized by the sanitizer step to plain `ul`/`ol` (BR8b, build spike).
- BR8b The allow-list is one list in one place; a test pins it, so widening it is a visible change.
- BR9 Limits (characters of sanitized HTML): statement 10 000, base text 20 000, explanation 10 000, option 2 000. Over the limit is refused, never truncated.
- BR10 The question keeps a plain-text copy of the statement (tags removed, normalized like `CatalogText.Normalize`) for the list search; it is derived on every write and never edited.
- BR11 A `Published` question cannot be deleted; unpublish first. A `Draft` delete is a soft delete.
- BR12 Publishing and unpublishing change the status only; they do not touch the content.
- BR13 Deleting a topic is refused with 409 `topic.in_use` while at least one live question (any status, deleted ones excluded) points at it. Moving a topic to another subject stays allowed.
- BR14 Pages and endpoints check the permission `questions.manage`, never a role name. Identity grants it once to `Curator` when the row is created (`InitialGrants`); `Admin` has every permission.
- BR15 The list is paginated, newest first, never shows deleted questions, and offers only the status filter and the statement search; the other filters are F-100.
- BR16 Unknown or deleted ids answer 404 `question.not_found`; an unknown or deleted topic answers 404 `question.topic_not_found`.

## Screens and API
- `/admin/questions` — "Questions" in the Content menu section (item shown with `questions.manage`). `AppDataTable`: statement excerpt, topic (with its subject), difficulty, language, status chip; search box, status filter; row actions edit, publish or unpublish, delete (disabled with the reason when `Published`). Empty and empty-search states.
- `/admin/questions/new` and `/admin/questions/{id}` — the question form (screen design in `/agile:screen`, mockup `docs/features/mockups/F-95-single-choice-questions.html`): classification (subject then topic, language, difficulty), statement, base text, options (2 to 5, mark the correct one, move, add, remove), explanation, a publish checklist (BR4 shown before the click), and the student preview with a "show answer" reveal.
- New kit component `AppRichTextField` in `Components/Ui` and the gallery: wraps the editor, label and error text, keyboard operable; the only place that knows the editor package.
- Typed client: `QuestionBankApiClient` in the Web, next to `CatalogApiClient`.
- Api, group `/api/v1/question-bank`, policy `questions.manage`:
  - `GET /questions?status=&q=&page=&pageSize=` — list.
  - `GET /questions/{id}` — one question with options and key.
  - `POST /questions` — create as `Draft`.
  - `PUT /questions/{id}` — update (replaces the content).
  - `POST /questions/{id}/publish`, `POST /questions/{id}/unpublish`.
  - `DELETE /questions/{id}` — soft delete of a draft.
- Catalog contracts gain two interfaces, so the modules never read each other's tables: `ITopicDirectory` (topic name and subject by id, for validation and the list) and `ITopicUsageCheck` (does anything use this topic?), implemented by `QuestionBank`; `DeleteTopicHandler` asks every registered check besides `IsMappedAsync`.
- Error codes: `question.not_found`, `question.topic_not_found`, `question.statement_required`, `question.statement_too_long`, `question.base_text_too_long`, `question.explanation_too_long`, `question.option_too_long`, `question.options_count`, `question.option_text_required`, `question.key_invalid`, `question.difficulty_invalid`, `question.content_language_invalid`, `question.delete_requires_draft`, `question.already_published`, `question.not_published`; reused `topic.in_use`.

## Acceptance criteria
- AC1 Given a curator with `questions.manage`, when they save a question with a topic, language and statement, then it is stored as `Draft`, appears in the list with its topic and subject, and the response carries the sanitized statement. (UC1, BR1, BR2, BR3)
- AC2 Given a draft being saved, when its statement is empty or only empty tags (`<p><br></p>`), then 400 `question.statement_required`; with no options and no key the draft is accepted. (BR3)
- AC3 Given text with `<script>`, an `onclick`, a `javascript:` link and a `data:` image in any of the four text fields, when it is saved, then the stored and returned HTML has none of them; `strong`, `sup`, `sub`, lists, a table and an `https` link survive. (BR8, BR8b)
- AC4 Given a question, when a sixth option is sent, then 400 `question.options_count`; when it is published with one option, then 400 `question.options_count`; an empty option text on publish is 400 `question.option_text_required`. (BR3, BR4)
- AC5 Given a draft with options, when it is published with no correct option or with two, then 400 `question.key_invalid`; with exactly one, it becomes `Published`. (BR4, UC3)
- AC6 Given a published question, when it is unpublished, then it is `Draft` again with the same content; publishing a published one answers 409 `question.already_published`, unpublishing a draft 409 `question.not_published`. (UC4, BR12)
- AC7 Given a published question, when the curator edits the statement, then it stays `Published` with the new sanitized text; an edit that leaves one option is refused and the stored question is unchanged. (UC2, BR4)
- AC8 Given a draft, when it is deleted, then it leaves the list and its rows stay with `IsDeleted` true; given a published question, then the delete action is disabled with the reason and a `DELETE` answers 409 `question.delete_requires_draft`. (UC7, BR11)
- AC9 Given a topic with one live question of any status, when it is deleted, then 409 `topic.in_use` and its delete action is disabled with the reason; after the question is deleted the topic can be deleted; moving the topic to another subject is allowed. (UC8, BR13)
- AC10 Given a user without `questions.manage`, then every endpoint answers 403, the menu item is absent and the pages deny; a new `Curator` has the permission and `Admin` too. (BR14)
- AC11 Given questions in several statuses, when the curator filters by status and types a word of a statement ignoring case and accents, then only the matches are listed, paginated, newest first; with none, the empty-search state shows. (UC5, BR10, BR15)
- AC12 Given a question, when the curator opens the preview, then it shows the statement, the base text and the options lettered A to E from the stored HTML, with the key and explanation hidden until "show answer". (UC6, BR5)
- AC13 Given a difficulty of 0 or 6, then 400 `question.difficulty_invalid`; empty and 1 to 5 are accepted. (BR6)
- AC14 Given a language outside `SupportedLanguages`, then 400 `question.content_language_invalid`; `pt-br` is stored as `pt-BR`; the form preselects the UI language. (BR7)
- AC15 Given a statement over its limit, then 400 `question.statement_too_long` and nothing is truncated; the same for the other three fields with their own codes. (BR9)
- AC16 Given the architecture rules, then `QuestionBank` references other modules only through their `Contracts`, its `Domain` does not depend on EF Core or ASP.NET, the vocabulary test passes on its assemblies, and the architecture overview lists the module. (profile)
- AC17 The visual editor is operable by keyboard, has an accessible name and announces its validation error (WCAG 2.2 AA); checked on screen. (UC1)
- AC18 All new texts appear in pt-BR, pt-PT and en, including every error code text.

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
| AC15 | pending |
| AC16 | pending |
| AC17 | pending |
| AC18 | pending |

## Decisions
- 2026-10-09 — New module `QuestionBank`, five projects, references `Topic` by id through `Catalog.Contracts`; only `Draft` and `Published`; first release cut F-95 to F-99 (owner, epic decisions).
- 2026-10-10 — Question text is sanitized HTML, not Markdown — owner chose the visual editor after reading the comparison; a visual editor produces HTML, and measured Markdig `DisableHtml()` still emits `javascript:` links and `data:` images (owner, refinement; `docs/features/mockups/F-95-content-format-comparison.md`).
- 2026-10-10 — Editor: visual (WYSIWYG) — owner chose "Visual editor" over the recommended "Markdown text box with preview" (average 5.3 vs 6.5, gap 1.3: nothing asked); kept.
- 2026-10-10 — New packages, for the owner's yes at approval: `HtmlSanitizer` 9.2.1039 (MIT, 2026-08-28) and `Blazored.TextEditor` 1.1.3 (MIT, wraps Quill 2.0.3, 2025-04-11), both checked against the NuGet registry on 2026-10-10; no test package is needed (the sanitizer is a plain unit test, the editor sits behind `AppRichTextField` and bUnit renders the component with a fake) (Claude).
- 2026-10-10 — From 2 to 5 options, letters from position — same as Simulae T1; ENEM and most exams use 4 or 5 (owner, refinement).
- 2026-10-10 — Difficulty 1 to 5, optional — same as Simulae `DificuldadeEstimada`, so the import loses nothing (owner, refinement).
- 2026-10-10 — Publish needs statement, 2+ options with one correct, topic and language; explanation and difficulty optional — past-exam questions rarely have an explanation (owner, refinement).
- 2026-10-10 — `Draft` ↔ `Published`, a published question is edited in place, only drafts are deleted — nothing consumes questions yet and F-98 adds versions before any attempt exists (owner, refinement).
- 2026-10-10 — Permission `questions.manage`, granted to `Curator` at creation, `Admin` through the union; review gets its own in F-105 (owner, refinement).
- 2026-10-10 — The list offers status and statement search only; the rest is F-100 (owner, refinement).
- 2026-10-10 — Profile line 11 is corrected and ADR-0005 records `QuestionBank` as its own module — the profile still says Catalog holds the question bank, which the 2026-10-09 decision replaced (Claude, premise verified in `docs/agile/profile.md`).
- 2026-10-10 — The topic guard is an inverted dependency: `Catalog.Contracts` declares `ITopicUsageCheck`, `QuestionBank` implements it, `DeleteTopicHandler` asks every check — Catalog never references `QuestionBank`, so no cycle and each later item with a topic reference adds its own check without editing Catalog (Claude).
- 2026-10-10 — A draft needs a topic and a language — the list shows both and the delete guard (BR13) needs the topic; a topic is always known for a question a curator has started (Claude).
- 2026-10-10 — A topic with only draft questions is also protected — otherwise a draft would point at a deleted topic and silently lose its classification (Claude).
- 2026-10-10 — The Api sanitizes, the Web shows what is stored — one place decides what is safe; the Web never calls the sanitizer (Claude).
- 2026-10-10 — The editor's list markup is a build spike: Quill 2 writes bullet lists as `<ol><li data-list="bullet">`, and a sanitizer that drops `data-*` would turn them into numbered lists. First build step measures what `Blazored.TextEditor` returns and sets the normalization (BR8) before anything else is built (Claude).
- 2026-10-10 — Quill's script and style are hosted by the app, not loaded from a CDN — the Web sets security headers and works offline in development; the build confirms the package allows it, else raises it as a stop (Claude).
- 2026-10-10 — Route group `/api/v1/question-bank/questions`, like `/api/v1/catalog/...` (Claude).

## Out of scope
- Images in statement, base text and options — F-103 (`IFileStorage`).
- One base text shared by several questions — F-102; here the base text is inline in the question.
- Question versions and answer-key change history — F-98. Until then an edit of a published question overwrites it.
- Origin: edition, number in the paper, notice subject, source line — F-96.
- Filters by subject, topic, organizer, edition, type, difficulty — F-100.
- Annulment — F-101. Review queue and `InReview` — F-105.
- Other question types — F-97, F-104, F-106 to F-109.
- A per-question "may shuffle options" flag — E-5, because options such as "all of the above" depend on order.
- Anything a student sees: the student side belongs to E-5 and E-6.
- Importing Simulae questions and seeded sample questions — F-96 and F-110.

## Open questions
- (none)

## Change notes

## Validation script
<!-- Written at the end of build. -->

## Delivery
<!-- Filled by /agile:ship. -->
