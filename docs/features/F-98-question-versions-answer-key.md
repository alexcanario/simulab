---
feature: F-98
epic: Question bank
status: building
board: 144
version: 1
---
# Question versions and answer key changes

Technical terms: [glossary](../glossary.md)

## Summary
Editing a published question creates a new version and keeps the earlier ones; an answer key change (after an appeal) records its reason and date; the curator sees the history. An attempt of the simulators can name the version that graded it (ADR-0001 #10), so versions must exist before epic E-5 records any attempt. Source: epic Question bank (E-4), brief capability 3 "changed answer keys".

## Start
- Depends on: F-95 (done), F-96 (done), F-97 (validating: the version stores the answer key it reshapes; building waits for its merge, owner 2026-10-12).
- Waits on (to start): nothing.
- Needed to validate: the local app host and one user with `questions.manage` (the seeded admin) — owner.
- Suggested path: `/agile:build` (no mockup: the screens reuse F-95 patterns, epic says screen design "no").
- Parallel with: F-100 or F-102 (both read questions only; F-101 depends on this item).

## Goal
Make the question's history real before any attempt exists: every state a published question was shown in is kept, an answer key change says why and when, and a later attempt can point at the exact version that graded it.

## What exists (verified 2026-10-12)
- `question_bank.questions` holds one current state. `Question.Update` replaces the content in place (`Question.cs`, `Apply`); editing a published question overwrites the statement, options and key with no trace (F-95 AC7). There is no version number, table or endpoint.
- Audit columns (`CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`, soft delete) come from `TenantEntity` through interceptors (ADR-0001 #10); the Identity contracts expose no user-name lookup, so a stored author id cannot be shown by name from this module.
- Publishing and unpublishing change the status only (F-95 BR12); a published question cannot be deleted (BR11); an edit of a published question runs the publish check (BR4).
- F-96 (done) added the origin columns `ExamEditionId`, `PaperNumber`, `NoticeSubjectId` to `questions` (migration `AddQuestionOrigin`).
- F-97 (`feature/F-97`, validating, not merged) changes the same files: `Question.cs`, `SaveQuestionHandler`, `QuestionConfiguration`, the model snapshot and adds migration `AddTrueFalseKey`, the `AnswerKey` record with `SingleChoiceKey` and `TrueFalseKey`, and a `TrueFalseKey` column. A version must snapshot that key, hence the dependency.
- `QuestionPreviewDialog.razor` renders a question as a student sees it with the key hidden until "show answer"; the form `QuestionForm.razor` has a Publication card below the form layout.
- ADR-0001 #10 and #39 already require history for question content and tests for "answer key versions".

## Users and use cases
- UC1 A curator publishes a question for the first time; version 1 is recorded.
- UC2 A curator edits a published question; each change to what students see or are graded on records a new version.
- UC3 A curator changes the answer key of a published question (after an appeal) and writes why; the version is marked "answer key changed" with the reason and date.
- UC4 A curator unpublishes, fixes and publishes again; a new version is recorded only when the content differs from the latest version.
- UC5 A curator reads the history of a question and opens any version read-only.
- UC6 A later simulator (E-5, E-6) names the version that graded an attempt, by version id or by question id and number.

## Business rules
- BR1 A version is an immutable snapshot of the versioned content: type, content language, statement, base text, explanation, options with their correct mark, and the true/false key. Topic, difficulty and origin (edition, paper number, notice subject) are catalog data, are not in the snapshot and creating or changing them alone makes no version (owner, 2026-10-12).
- BR2 A draft that was never published has no versions. The first publication creates version 1.
- BR3 Saving a `Published` question creates version N+1 when its versioned content differs from the latest version; an identical save creates none. Saving a draft creates none. Publishing a draft that has versions creates version N+1 when its versioned content differs from the latest version, otherwise nothing.
- BR4 The answer key of a version changed when, against the latest version, the letter of the correct option differs (a reorder that moves the correct option counts) or the true/false value differs. Version 1 is never marked changed. The version carries `AnswerKeyChanged`.
- BR5 A version whose key changed needs a reason: 1 to 500 characters of plain text after trimming, sent with the save or the publish that creates it. Missing or blank is refused and nothing is stored. A reason sent with a version that did not change the key is kept as a note on it; a reason sent when no version is created is ignored.
- BR6 Versions are never edited or deleted. Deleting a draft that has versions soft-deletes the question and leaves the versions (owner, 2026-10-12); a published question still cannot be deleted (F-95 BR11).
- BR7 A version has a stable id, a number (1, 2, ...) unique per question, the date it was created and the author, both from the audit columns. The latest version is the highest number; there is no column on `questions` for it.
- BR8 Reading versions needs `questions.manage`, never a role name. A number that does not exist for the question answers 404 `question.version_not_found`.
- BR9 Existing published questions, not deleted, get version 1 in the migration, with the stored content, no reason and no author. Drafts get none.
- BR10 Two saves racing for the same next number: the unique index keeps one; the other answers 409 `question.version_conflict` and the curator reloads.
- BR11 `QuestionResponse` carries `CurrentVersion` (the latest number, null while never published) so a screen knows without a second call.

## Screens and API
No mockup: the screens reuse the patterns of F-95 (epic: screen design "no"). If the owner wants one, `/agile:screen` before the build.

### API (`/api/v1/question-bank`, all `questions.manage`)
- `GET /questions/{id}/versions` — `QuestionVersionListItemResponse(Id, Number, CreatedAt, AnswerKeyChanged, Reason, IsCurrent)`, newest first, no paging (a question has few).
- `GET /questions/{id}/versions/{number}` — `QuestionVersionResponse(Id, QuestionId, Number, Type, ContentLanguage, StatementHtml, BaseTextHtml, ExplanationHtml, the key fields `QuestionResponse` has after F-97, Options, AnswerKeyChanged, Reason, CreatedAt)`. The HTML is the sanitized HTML stored (F-95 BR8).
- `SaveQuestionRequest` gains optional `ChangeReason`; `POST /questions/{id}/publish` accepts an optional body `{ "changeReason": "..." }`.
- `QuestionResponse` gains `CurrentVersion`.
- Error codes: `question.key_change_reason_required` (400), `question.change_reason_too_long` (400), `question.version_not_found` (404), `question.version_conflict` (409).
- Migration `question_bank`: table `question_versions` (id, tenant, question id, number, type, content language, statement, base text, explanation, true/false key, options as a JSON column, answer key changed, reason, audit and soft-delete columns, column comments), unique index `ux_question_versions_tenant_question_number` (`NULLS NOT DISTINCT`), index on `question_id`; the backfill of BR9. No foreign key to anything outside the module; the key to `questions` is inside it.

### Screens
- **Question form** (`/admin/questions/{id}`), new section card **History** (`question-section-history`) below the Publication card, shown for a saved question with `questions.manage`. `AppDataTable` of the versions: version number (a "current" chip on the latest), date and time, change (`AppStatusChip`: "First publication" for 1, "Content edited" neutral, "Answer key changed" in the warning tone), reason (truncated with tooltip), row action "View". Empty state for a question never published: "The first version is recorded when the question is published." The author is stored but not shown: no contract resolves names (see Decisions).
- **View a version**: `QuestionPreviewDialog` opened on the snapshot, title "Version {n}", a note with its date; key and explanation stay behind "show answer" as in the preview.
- **Reason field**: on a `Published` question, when the key in the form differs from the stored key, an `AppTextField` (multi-line, 500 characters, required marker) "Reason for the answer key change" appears at the end of the Options section; Save is refused client-side without it and the Api is the authority. Publishing a draft that has versions whose key differs from the latest: the Api answers 400 `question.key_change_reason_required`, the Publication card then shows the same field (focused), and the second click sends the reason. A publish from the list row refused this way shows the code's text in the alert and the curator completes it on the question's page.
- Typed client: `QuestionBankApiClient` gets `ListVersionsAsync` and `GetVersionAsync`; save and publish take the reason.

## Acceptance criteria
- AC1 Given a complete draft, when it is published, then version 1 exists with the stored content, `AnswerKeyChanged` false and `CurrentVersion` 1; saving that draft before publishing created none. (UC1, BR2)
- AC2 Given a published question at version 1, when the curator changes the statement and saves, then version 2 holds the new statement, version 1 still holds the old one and `CurrentVersion` is 2. (UC2, BR1, BR3, BR6)
- AC3 Given a published question, when it is saved with the same content, or with only a different topic, difficulty or origin, then no version is created. (BR1, BR3)
- AC4 Given a published question, when the key moves to another option without a reason, then 400 `question.key_change_reason_required` and the stored question and versions are unchanged; with a reason of 1 to 500 characters, version N+1 is created with `AnswerKeyChanged` true, the reason and its date. (UC3, BR4, BR5)
- AC5 Given a reason of only spaces, then it counts as missing; given one of 501 characters, then 400 `question.change_reason_too_long`. (BR5)
- AC6 Given a published question whose options are reordered so the correct option changes letter, then the version is marked as a key change and needs the reason. (BR4)
- AC7 Given a published true/false question (F-97), when its key flips, then the version is marked as a key change and needs the reason. (BR4)
- AC8 Given a published question, when only the statement changes and a reason is sent, then the version is created with `AnswerKeyChanged` false and the reason kept as its note; given an identical save with a reason, then nothing is created. (BR5)
- AC9 Given a question with versions, when it is unpublished, edited and published with a different statement, then version N+1 is created at the publish; with no edit, then none; a changed key at that publish needs the reason. (UC4, BR3)
- AC10 Given a question with versions, when the list is read, then it returns every version newest first with `IsCurrent` on the highest; reading `/versions/{number}` returns that snapshot; an unknown number, or one of another question, answers 404 `question.version_not_found`. (UC5, BR7, BR8)
- AC11 Given a user without `questions.manage`, then both version reads answer 403, and the History card is absent. (BR8)
- AC12 Given an unpublished draft that has versions, when it is deleted, then it leaves the list and its version rows stay with their content; a published question is still refused with `question.delete_requires_draft`. (BR6)
- AC13 Given questions already stored before this item, when the migration runs, then each non-deleted published question has version 1 with its content and each draft has none. (BR9)
- AC14 Given two saves of the same published question that both create the next number, then exactly one version with that number exists and the loser answers 409 `question.version_conflict`. (BR10)
- AC15 Given a saved question, when the curator opens the form, then the History card lists the versions with the current chip, the change label and the reason, "View" opens the snapshot read-only with the key hidden until "show answer", and a never-published question shows the empty text. (UC5)
- AC16 Given a published question in the form, when the key is changed, then the reason field appears and Save is refused without it; given a draft whose publish was refused for the missing reason, then the Publication card shows the field and the second click publishes. (UC3, BR5)
- AC17 All new texts appear in every locale of `UI locales:`.

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

## Decisions
- 2026-10-12 — Versioned content is the statement, base text, options, key, explanation and language; topic, difficulty and origin stay catalog data and make no version — owner chose it (question "what versions"), so reclassifying many questions does not flood the history.
- 2026-10-12 — A key change records a required reason plus the automatic date and author; no separate official-date field — owner chose it; E-5/E-6 grade by version, so the recording date is enough.
- 2026-10-12 — A draft with history can be deleted and its versions stay — owner chose it; the delete is soft and a version is a copy, so an attempt still finds its version. Annulment is F-101.
- 2026-10-12 — The history is a card with a list and a read-only "View" in the existing preview dialog — owner chose it; no new mockup.
- 2026-10-12 — F-98 depends on F-97 and its build waits for the merge — owner chose it; the version snapshots the key F-97 reshapes and both change `Question.cs`, the model snapshot and the migrations.
- 2026-10-12 — One way to edit: the reason is a field of the ordinary Save (and, for a draft with history, of Publish), not a separate "change answer key" action — rule `ui-project` (one way to edit an item) and one rule at two moments: a version is created only by a save of a published question or by a publish.
- 2026-10-12 — Versions are created only for published states, so a draft's iterations make no noise and "version 1" is what students first saw — Claude, from ADR-0001 #10 ("an attempt must know which version graded it").
- 2026-10-12 — The latest version is the highest number, with no `current_version` column on `questions` — one source of truth, no field to keep in step; the unique index serves the lookup.
- 2026-10-12 — Options of a version are stored as a JSON column of the version row, not as a child table — an immutable snapshot read whole, never queried by option; the build confirms the EF mapping on the test container.
- 2026-10-12 — Everything is under `questions.manage`; no new permission — the same people who edit a question read its history.
- 2026-10-12 — The author is stored (audit column) but not shown by name — Identity contracts offer no user-name lookup and `users.manage` is not a curator permission; showing it needs a small new contract, left out of this item (see Out of scope).
- 2026-10-12 — No new package — the snapshot, the JSON column and the tests use what the module already references.
- 2026-10-12 — The glossary gets `QuestionVersion` and `AnswerKeyChange` before first use in code (naming rule).

## Out of scope
- Restoring an old version as the current one — an `idea` if the curators ask.
- A side-by-side comparison of two versions.
- Showing the author's name in the history: needs a user-name lookup contract from Identity (the author id is stored now).
- Reading versions for students, and re-grading attempts when a key changes — epics E-5 and E-6, which will define the contract they need.
- Annulling a question — F-101.
- Bulk key changes from an imported official answer key — epic E-9.
- Versions of the catalog data (topic, difficulty, origin).

## Open questions
- (none)

## Change notes
<!-- Added by /agile:change during build. -->
