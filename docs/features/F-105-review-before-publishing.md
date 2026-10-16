---
feature: F-105
epic: Question bank
status: refining
board: 151
version: 1
---
# Review before publishing

Technical terms: [glossary](../glossary.md)

## Summary
Questions gain the `InReview` status: a curator sends a complete draft for review, a person with the review permission approves it (it is published) or rejects it with a reason (it goes back to `Draft`), from a review queue. Publishing straight from `Draft` stays as it is; review is a second, optional way to publish. Every send and every decision is kept in an append-only history. Source: epic Question bank (E-4), brief capability 3 "Workflow" (draft, review, publish) and the AI rule "every AI-extracted item is reviewed by a person before it is published" (E-9 will create its questions already `InReview`); owner decisions of 2026-10-16.

## Start
- Depends on: F-95 (done), F-98 (done), F-101 (done).
- Waits on (to start): nothing.
- Needed to validate: the local app host, a `Curator` account, an `Admin` account and one account whose role holds only `questions.review` (created on the roles screen) — owner.
- Suggested path: `/agile:screen F-105` (the review queue and the review screen) → approval → `/agile:build`.
- Parallel with: none; it changes `QuestionStatus`, the question handlers and the question form, which every other E-4 item touches.

## Goal
Give the question bank the middle step of "draft, review, publish": a question can wait for a second pair of eyes, with a clear queue, a decision and a recorded reason, and the AI import (E-9) gets the state and the permission it needs.

## What exists (verified 2026-10-16)
- `QuestionStatus` is `Draft`, `Published`, `Annulled` (F-101 appended the last one); stored as a string, and `questions.status` is an EF concurrency token (a lost race answers 409 `question.version_conflict` through `IQuestionStore.TrySaveChangesAsync`). The glossary already lists `InReview` for the state (`docs/glossary.md` lines 56 and 72).
- `Question` (`Domain/Entities/Question.cs`) has `Publish`, `Unpublish`, `Annul`, `Reinstate`, `EnsureDeletable` and a private `Frozen()` for annulled questions; publishing makes no version by itself, `NextVersion` does (F-98: a first publication makes version 1).
- One permission, `questions.manage` (`QuestionBankPermissions`), seeded to `Curator` through `PermissionCatalog.InitialGrants`; `Admin` has every permission. Roles are `Student`, `Curator`, `Admin`: there is no reviewer role.
- Routes under `/api/v1/question-bank/questions` all require `questions.manage` (`QuestionEndpoints.cs`). The Web has the list (`Questions.razor`), the form with the Publication and History cards (`QuestionForm.razor`), a preview dialog and a reason dialog (`QuestionStatusReasonDialog.razor`, F-101).
- The audit interceptor fills `CreatedBy` (the signed-in user id) on every `TenantEntity` it saves. The `QuestionBank` module has no way to turn a user id into a name (nothing in `Identity.Contracts` does), and the version history shows no author either.
- E-9 has no epic file yet, so there is nowhere to move this item to (the epic's "may move to E-9" is closed by the owner's answer below).
- The Simulae source (`FilaRevisaoPage`, `QuestionReviewDecision`) is not on this machine (`D:\dev\_icontrol\simulae` does not exist), and the epic notes its `EmRevisao` status is never assigned. Nothing is imported: the design starts from this module.

## Users and use cases
- UC1 A curator sends a complete draft for review.
- UC2 A curator withdraws a question from review; it is a draft again.
- UC3 A reviewer opens the review queue and sees the questions waiting, oldest first.
- UC4 A reviewer opens one question, sees it as a student will, with the answer key and the explanation, and approves it: it is published.
- UC5 A reviewer rejects it with a reason: it goes back to `Draft`.
- UC6 A curator sees why a question was rejected and the history of sends and decisions, fixes the draft and sends it again.
- UC7 A curator still publishes a complete draft directly, and still edits a published question directly (unchanged from F-95 and F-98).

## Business rules
- BR1 `QuestionStatus` gains `InReview`, appended last. A question is `InReview` from a send until a decision or a withdrawal. Nothing that serves questions to students may read it as published: it is a distinct value, like `Annulled`.
- BR2 Sending needs `questions.manage`, a `Draft`, and the same completeness check as publishing (BR4 of F-95 and the type's key rules): an incomplete draft is refused with the codes publishing uses. A send makes no version (versions belong to published content).
- BR3 While `InReview` the question is frozen: save, publish, unpublish and delete answer 409 `question.in_review` and change nothing; reading, listing, previewing and the version history still work. The only ways out are a decision and a withdrawal. Annul keeps its own rule (`question.not_published`).
- BR4 Withdrawing needs `questions.manage` (any holder, not only the one who sent it) and an `InReview` question; it becomes `Draft`. No reason.
- BR5 Approving needs `questions.review` and an `InReview` question. It does exactly what Publish does (status `Published`, and the version F-98 makes) and adds a note, optional, 1 to 500 characters once trimmed. The person who sent the question may approve it (owner, 2026-10-16); the history records who sent and who decided.
- BR6 Rejecting needs `questions.review` and an `InReview` question; the reason is required, 1 to 500 characters once trimmed (the F-101 rule). The question becomes `Draft`; there is no `Rejected` status.
- BR7 Every send, withdrawal, approval and rejection adds one `QuestionReviewEntry` (kind, reason or note, who, when). Entries are never edited or deleted. "Who" is the user id the audit interceptor stores; this item shows no names (see Decisions).
- BR8 The review queue lists the `InReview` questions, oldest send first (the time of the latest `Submitted` entry), paginated (cap 100), never deleted ones. It has no search or filter in this item.
- BR9 The review screen shows the question as a student sees it plus the answer key, the explanation and the classification (subject, topic, language, difficulty, source edition when there is one). The reviewer cannot change content there.
- BR10 `questions.review` is a new permission of the module's `PermissionCatalog`. `Curator` receives it through `InitialGrants` (owner, 2026-10-16); `Admin` has it as every permission. Endpoints and screens check the permission, never the role. The review routes need only `questions.review`: a reviewer without `questions.manage` can do the whole review.
- BR11 Two decisions at once (or a decision against a withdrawal) are one winner and one 409 `question.version_conflict`, through the existing concurrency token.
- BR12 The topic delete guard already counts a question of any status: a topic used only by a question in review is refused with `topic.in_use`.
- BR13 Publishing a `Draft` directly with `questions.manage` is unchanged (owner, 2026-10-16). It is refused on an `InReview` question (BR3).
- BR14 Editing a `Published` question stays direct: no new review (owner, 2026-10-16; F-123 captures the idea).

## Screens and API
### API (`/api/v1/question-bank`)
- `questions.manage`: `POST /questions/{id}/submit` (no body; 200 `QuestionResponse`), `POST /questions/{id}/withdraw` (no body; 200 `QuestionResponse`), `GET /questions/{id}/reviews` (the entries of one question, oldest first: `QuestionReviewEntryResponse(Kind, Reason, At)`).
- `questions.review`: `GET /review-queue?page=&pageSize=` (`ReviewQueuePageResponse(Items, Total)` of `ReviewQueueItemResponse(Id, StatementExcerpt, ContentLanguage, Type, TopicName, SubjectName, Difficulty, SubmittedAt)`), `GET /review-queue/{id}` (a `QuestionResponse` of an `InReview` question), `POST /review-queue/{id}/approve` (`ApproveQuestionRequest(Note)`), `POST /review-queue/{id}/reject` (`RejectQuestionRequest(Reason)`); both 200 `QuestionResponse`.
- `QuestionStatus` gains `InReview`; `QuestionResponse` is otherwise unchanged. Typed client methods in `QuestionBankApiClient`.
- Error codes (new): `question.in_review` (409), `question.already_in_review` (409), `question.not_in_review` (409), `question.review_reason_required` (400), `question.review_reason_too_long` (400). Reused: `question.not_found`, `question.already_published`, `question.annulled`, `question.not_published`, `question.version_conflict`, and the completeness codes of publishing.
- Migration in schema `question_bank`: new table `question_review_entries` (`id`, `question_id`, `kind` string, `reason` nullable text, the `TenantEntity` audit columns; index `ix_question_review_entries_question_id_created_at`; column comments), and the comment of `questions.status` updated to list `InReview`. No stored status becomes invalid; no data is changed.

### Screens
- **Question form** (`/admin/questions/{id}`), Publication card [changed]: a `Draft` gets **Send for review** beside Preview and Publish, disabled with the same reasons as Publish (unsaved changes, incomplete checklist). An `InReview` question shows the chip "In review", "Waiting for a reviewer since <date>" and one button **Withdraw from review**; the fields are read-only and an alert above the form says the question is frozen and how to leave it (the F-101 pattern). After a rejection, the draft's card shows an info alert "Rejected on <date>: <reason>" until the next send. Sending is only here: the list has no send action.
- **Review history card** [new], below the Publication card, shown when the question has entries: one row per entry with a chip (Sent, Withdrawn, Approved, Rejected), the date and the reason or note.
- **Review queue** (`/admin/questions/review`) [new]: menu item "Review queue" in the Content section, shown with `questions.review`. `AppDataTable`: statement excerpt, topic with subject, type, language, sent date; one row action **Review**. Empty state "Nothing is waiting for review."
- **Review screen** (`/admin/questions/review/{id}`) [new]: read-only. Classification summary, the question as a student sees it, then the answer key and the explanation; buttons **Approve** (filled; optional note in a small dialog) and **Reject** (outlined; dialog with the required reason) and a link back to the queue. States: loading, load error, not found, no longer in review (alert with the link back), conflict.
- **Question list** [changed]: the status chip says "In review", the status filter offers it, the row menu has no Publish or Unpublish for it and Delete is disabled with the reason said in words.
- **Edition's question list** (`EditionQuestionsSection`) [changed]: shows the same chip through the shared status text.
- Mockup: `/agile:screen F-105` for the queue and the review screen; the rest follows the wireframes below.

### Wireframe: Question form, Publication card
```text
+--------------------------------------------------------+
| Publication                                            |
| [Draft]  Not available outside the back office.        |
| [ Preview ] [ Send for review ] [new]  [ Publish ]     |
+--------------------------------------------------------+

+--------------------------------------------------------+
| (!) This question is in review and cannot be edited.   |
|     Withdraw it to change it.                    [new] |
+--------------------------------------------------------+
| Publication                                            |
| [In review] [new]  Waiting since 16 Oct 2026           |
| [ Preview ] [ Withdraw from review ] [new]             |
+--------------------------------------------------------+

+--------------------------------------------------------+
| (i) Rejected on 17 Oct 2026: The key of option C       |
|     contradicts the explanation.                 [new] |
+--------------------------------------------------------+
```

### Wireframe: Review queue
```text
+------------------------------------------------------------+
| Review queue                                         [new] |
+------------------------------------------------------------+
| Statement          | Topic / Subject | Type  | Lang | Sent |
| Which article...   | Art. 5 / Const. | Single| pt-BR| 16/10| [ Review ]
| Read the text...   | Parsing / Port. | Multi | pt-BR| 16/10| [ Review ]
+------------------------------------------------------------+
| Nothing is waiting for review.        (empty state)  [new] |
+------------------------------------------------------------+
```

### Wireframe: Review screen
```text
+------------------------------------------------------------+
| < Back to the queue                                  [new] |
| Review question                                            |
| Subject / Topic - pt-BR - Difficulty 3 - CEBRASPE 2024 #12 |
+------------------------------------------------------------+
| (base text)                                                |
| Statement ...                                              |
| A) ...   B) ...   C) ... [correct]   D) ...   E) ...       |
| Explanation ...                                            |
+------------------------------------------------------------+
|                          [ Reject ]  [ Approve ]           |
+------------------------------------------------------------+
```

### Wireframe: Reject dialog
```text
+--------------------------------------------+
| Reject this question?                [new]  |
| It goes back to draft with your reason.    |
| Reason *                                   |
| [______________________________]   0/500   |
|                  [ Cancel ]  [ Reject ]    |
+--------------------------------------------+
```

## Docs
- flow: new docs/flows/review-before-publishing.md — states, send, withdraw, approve, reject, the history, the permission and the screens.

## Acceptance criteria
- AC1 Given a complete `Draft` and a user with `questions.manage`, when it is sent for review, then its status is `InReview`, one `Submitted` entry exists, and no version is made. (UC1, BR1, BR2, BR7)
- AC2 Given an incomplete draft, when it is sent, then 400 with the code publishing would answer and nothing changes; given an `InReview`, `Published` or `Annulled` question, then 409 `question.already_in_review`, `question.already_published` or `question.annulled`. (BR2)
- AC3 Given an `InReview` question, when it is saved, published, unpublished or deleted, then 409 `question.in_review` and the stored question, options and entries are unchanged; get, list, versions, preview and annul (`question.not_published`) answer as before. (BR3)
- AC4 Given an `InReview` question, when it is withdrawn, then it is a `Draft` again with the same content and a `Withdrawn` entry; given any other status, then 409 `question.not_in_review`. (UC2, BR4)
- AC5 Given an `InReview` question, when a reviewer approves it with or without a note, then it is `Published`, version 1 holds its content (or the version F-98 makes), and an `Approved` entry holds the trimmed note; a note of 501 characters answers 400 `question.review_reason_too_long` and changes nothing; 500 is accepted. (UC4, BR5)
- AC6 Given an `InReview` question, when a reviewer rejects it with a reason, then it is a `Draft` and a `Rejected` entry holds the trimmed reason; a missing, blank or 501-character reason answers 400 `question.review_reason_required` or `question.review_reason_too_long` and it stays `InReview`; 500 is accepted. (UC5, BR6)
- AC7 Given a question that is not `InReview`, when approve or reject is called, then 409 `question.not_in_review`; given two decisions sent together, then one succeeds and the other answers 409 `question.version_conflict`. (BR11)
- AC8 Given a reviewer who is also the user who sent the question, when they approve it, then it is published and the entries `Submitted` and `Approved` both carry that user's id. (BR5, BR7)
- AC9 Given the permissions, then: with `questions.manage` only, send, withdraw and the history answer and the queue, review detail, approve and reject answer 403; with `questions.review` only, the queue, review detail, approve and reject answer and send, withdraw, save and the history answer 403; with neither, every route answers 403; a new `Curator` and `Admin` hold both; the menu item and the pages follow the permission. (BR10)
- AC10 Given questions in several statuses, a deleted one and two sends of which one was rejected and sent again, when the queue is listed, then only the `InReview` ones appear, oldest latest-send first, paginated, and an empty queue shows the empty state; the review detail of a question that is not `InReview` answers 409 `question.not_in_review`. (UC3, BR8)
- AC11 Given a question that went through send, rejection and send, when its history is read, then three entries come oldest first with their kinds and reasons; entries cannot be changed or deleted by any route. (UC6, BR7)
- AC12 Given a topic used only by an `InReview` question, when it is deleted, then 409 `topic.in_use`. (BR12)
- AC13 Given a question in review, then the list shows the chip and the filter finds it, the row menu has no Publish or Unpublish and Delete is disabled with its reason. (BR1, BR3)
- AC14 Given the question form, then a draft shows **Send for review** with Publish's disabled reasons, an `InReview` question is read-only with only **Withdraw from review**, and a rejected draft shows the rejection alert with the reason. (UC1, UC2, UC6)
- AC15 Given the review screen, then **Approve** publishes and returns to the queue, **Reject** needs a reason in its dialog, and a question decided meanwhile shows the "no longer in review" alert. (UC4, UC5)
- AC16 Given a complete draft, when it is published directly, and given a published question, when it is edited, then both work as in F-95 and F-98 with no review. (UC7, BR13, BR14)
- AC17 Given the migration, then existing rows stay valid, `question_review_entries` exists with its comments and index, and the enum column comments test passes. (migration)
- AC18 All new texts and error code texts appear in every locale of `UI locales:`.

## Criterion → test
<!-- Written by /agile:build at its start, one row per criterion above, in the same order. -->

## Decisions
- 2026-10-16 — The rejection reason is required, 1 to 500 characters, the F-101 rule — one rule for curators to learn — confirmed: F-101.
- 2026-10-16 — Direct publishing by `questions.manage` stays; review is a second way to publish — nothing of F-95 and F-98 breaks, the pilot with testers goes on, and E-9 will create its questions already `InReview` so the AI rule holds there — asked.
- 2026-10-16 — Review permission: who holds `questions.review` by default — owner chose "Admin and Curator" over the recommended "Admin only" (average 8.0 vs 7.5); kept. The brief and the glossary make the curator the person who reviews (`product/brief.md:10`, `docs/glossary.md:74`). "Curator of the tenant" is the `Curator` role: `TenantId` is dormant in v1 — asked.
- 2026-10-16 — The person who sent a question may approve it, with both ids recorded — the AI import has the curator review their own draft and a sole owner needs no second account — asked.
- 2026-10-16 — An `InReview` question is frozen and can be withdrawn — the reviewer judges what was sent, which cannot change under them — asked.
- 2026-10-16 — A rejection returns the question to `Draft` with the reason in the history; there is no `Rejected` status — one state less in every reader of `QuestionStatus` — asked.
- 2026-10-16 — Editing a published question needs no new review — as F-95 and F-98; the idea is F-123 — asked.
- 2026-10-16 — F-105 stays in epic E-4 — E-9 has no epic file yet; this item delivers the state, queue and permission E-9 will consume — asked.
- 2026-10-16 — The reviewer decides on a queue and a read-only review screen of their own, designed with `/agile:screen` — the form is an editor and a reviewer must not see fields they cannot change — asked.
- 2026-10-16 — Future work captured as ideas: F-121 (email the author of a decision), F-122 (assign a reviewer), F-123 (review edits of a published question) — owner chose to capture them over the recommended "only list them" (average 6.6 vs 7.0); kept — asked.
- 2026-10-16 — A send runs the publish completeness check — a reviewer should never receive a question that could not be published — Claude.
- 2026-10-16 — Approve reuses the Publish logic (status and version) and adds the entry in the same transaction — one rule for what "published" means — Claude.
- 2026-10-16 — The review routes live under `/review-queue` and need only `questions.review` — a role holding only the review permission must be able to review, and the least-privileged role is the one to validate with (B-28) — Claude.
- 2026-10-16 — The history is an append-only table, `question_review_entries`, one row per event, with the audit `CreatedBy` as "who" — the interceptor already writes it; no `Rejected` or `Withdrawn` flags on `questions` — Claude.
- 2026-10-16 — No names of people are shown: the module cannot resolve a user id to a name and the version history has none either; a name lookup contract is a separate piece (F-121 needs the author's email and would add it) — Claude, from the code.
- 2026-10-16 — Sending is only in the Publication card; the list shows the chip and the filter — one way to do it, the F-101 rule — Claude.
- 2026-10-16 — No new package (production or tests), setting, job or ADR; the glossary gets the rows `ReviewQueue`, `QuestionReviewEntry`, `Submit for review`, `Approve` and `Reject` before their first use in code — Claude.
- 2026-10-16 — No data is migrated: stored statuses stay valid and the new table starts empty — Claude, from the code.
- 2026-10-16 — The Simulae review source is unreachable on this machine and its status path is unused; nothing is imported — Claude, from the code.

## Out of scope
- Email to the author when a decision is made — F-121.
- Assigning a specific reviewer to a question — F-122.
- Review of edits of a published question — F-123.
- Search, filters and sorting of the review queue; showing people's names in the history (a name lookup contract).
- Creating questions already `InReview` from the AI import — epic E-9.
- A dedicated `Reviewer` role in the seed: the owner can give `questions.review` to any role on the roles screen.
- Review of other content (base texts, exam editions).

## Open questions
- (none)

## Change notes
<!-- Added by /agile:change during build. Increase `version` in the header. -->

## Validation script
<!-- Written at the end of build, in two subsections with the same steps (English, pt-BR). -->

## Events
<!-- Written only by events.js. -->
- 2026-10-16 question — asked 9, from context 1
- 2026-10-16 override — who holds questions.review by default — chose "Admin and Curator" over "Admin only"
- 2026-10-16 override — capture future ideas — chose "capture the three as ideas" over "only list them"

## Delivery
<!-- Filled by /agile:ship. -->
