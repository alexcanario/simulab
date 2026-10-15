---
feature: F-99
epic: Question bank
status: done
board: 145
version: 1
---
# Transcribe the paper of an edition

Technical terms: [glossary](../glossary.md)

## Summary
A transcription mode for one edition: the curator types its questions one after the other, the edition and the current notice subject stay selected and the number in the paper increments by itself, so a whole paper is entered without reopening the form. It gives testers a real volume of questions before the AI import (E-9). Source: epic Question bank (E-4); Simulae `QuestionTranscriptionPage` as the reference.

## Start
- Depends on: F-96, F-97.
- Waits on (to start): nothing.
- Needed to validate: the local app host and one real past paper to transcribe a few questions from — owner.
- Suggested path: `/agile:refine` with `/agile:screen` → `/agile:build`.
- Parallel with: F-100.

## Goal
Let a curator enter a whole past paper in one sitting, instead of reopening the question form for every item: the edition, the notice subject and the content language stay chosen and the number in the paper suggests itself, so transcribing a 60-question paper is sixty saves, not sixty forms.

## What exists (verified 2026-10-14)
- `QuestionBank` (F-95, F-96, F-97, F-98): `Question.Create` always sets `Status = Draft` (`Question.cs`); publishing is a separate, later action (`PublishAsync`). The form has Classification, Origin (edition, paper number 1-500, notice subject — F-96), Statement, base text, options/key and difficulty sections; only `QuestionType.SingleChoice` and `QuestionType.TrueFalse` are offered in the type cards (`QuestionForm.razor:663-666`); every other type (`F-104`, `F-106` to `F-109`) is still `idea` and not in the UI.
- The question form (`/admin/questions/new`, `/admin/questions/{id}`) already opens with an edition preselected from a query string (`/admin/questions/new?examEditionId={id}`), the pattern the edition page's "Add question" button uses (`EditionQuestionsSection.razor:24-29`).
- Paper number is required with an edition, 1 to 500, unique among live questions of the same edition (not of the notice subject): a duplicate is refused with 409 `question.paper_number_taken` (`IQuestionStore.IsPaperNumberTakenAsync` plus a unique index fallback, F-96 BR3). No endpoint or query returns the next free number today; nothing computes it.
- `questions.manage` already guards every question route and form action; there is no narrower permission for transcription.
- Simulae's reference screen (`QuestionTranscriptionPage`) keeps one "Contexto da Prova" card locking organizer/exam/edition, a batch counter, and a manual "Cadastrar outra questão" button between questions; Simulab's answer differs on that last point (BR6).

## Users and use cases
- UC1 A curator opens transcription mode from an edition's Questions card; the screen opens with that edition locked and the next paper number suggested.
- UC2 A curator fills one question, saves it, and the form clears for the next one, keeping the edition, the notice subject and the content language, with the batch counter and the suggested number both updated.
- UC3 A curator overrides the suggested paper number before saving a question.
- UC4 A curator picks or changes the notice subject while transcribing; it stays chosen for the following questions until changed again.
- UC5 A curator chooses between SingleChoice and TrueFalse for each question; no other type is offered here.
- UC6 A curator leaves transcription mode at any point; every question already saved stays, as a `Draft`, exactly as the regular form would have saved it.

## Business rules
- BR1 Transcription mode adds no rule to `Question` itself: it calls the same `POST /questions` the regular form calls, so every existing rule (status always `Draft` on create, paper number range and uniqueness, origin fields, permission) applies unchanged.
- BR2 The edition is fixed for the whole session: there is no field to change it inside transcription mode. To transcribe another edition, the curator leaves and reopens from that edition's own Questions card.
- BR3 Across saved questions in the same session, the edition, the notice subject and the content language stay as they are; the type, statement, base text, difficulty and answer key are cleared after each save.
- BR4 The suggested paper number is `max(PaperNumber) + 1` among the edition's live (not deleted) questions, or `1` when it has none; it is always editable, and the existing uniqueness check (F-96 BR3) is still what refuses a repeated number — the suggestion is a convenience, not a new rule.
- BR5 Only `SingleChoice` and `TrueFalse` are offered, matching what the question form already implements (`QuestionForm.razor` type cards); the other types stay unavailable here until their own items ship.
- BR6 No batch session is kept on the server: the batch counter and which question comes next live only in the browser. Navigating away or reloading loses the counter, never a question already saved — each save is already durable the moment it returns 200, as any other question save.
- BR7 `questions.manage` is the only permission checked, on the entry button and on the screen's own route, exactly as every other question action (F-95 BR14).

## Screens and API

### API (`/api/v1/question-bank`, `questions.manage`)
- `GET /editions/{editionId}/next-paper-number` — `{ paperNumber }`: `max(PaperNumber) + 1` among the edition's live questions, or `1` when it has none. An unknown or deleted edition answers 404 `question.exam_edition_not_found` (reused from F-96).
- No change to `POST /questions`, `PUT /questions/{id}` or any existing contract: transcription mode calls `POST /questions` with `SaveQuestionRequest` exactly as the regular form does.
- Error codes: `question.exam_edition_not_found` (404, reused); no new code besides the one above (the existing paper number and origin codes of F-96 cover every other refusal).

### Screens
- **Edition's Questions card** (`EditionQuestionsSection.razor`) [changed]: a new button "Transcribe" next to "Add question", for a saved edition and `questions.manage`; it opens the transcription screen with the edition's id.
- **Transcription screen** (`/admin/questions/transcribe?examEditionId={id}`) [new]: a header card showing the edition's source line (read-only) and a "Finish" button (back to the edition page); below it, the same Classification, Origin (edition locked, paper number prefilled and editable, notice subject selectable), Statement, base text, options/key and difficulty sections the question form already has, and one "Save and continue" button. After a successful save: a success alert, the batch counter increases by one, and the form clears except edition, notice subject and content language, with the paper number field prefilled to the new suggestion. A refused save behaves as the regular form does (field marked by its code, focus to the error summary) and keeps what the curator typed. States: loading (edition lookup), edition not found (`question.exam_edition_not_found`, "Finish" is the only action), forbidden (route not reachable without `questions.manage`), the usual field-level validation errors.
- Typed client: `QuestionBankApiClient` gets `GetNextPaperNumberAsync(editionId)`.

### Wireframe: Transcription screen
```text
+------------------------------------------------------+
| CEBRASPE . Tribunal XY . 2024                  [new] |
| Question 3 of this session           [ Finish ] [new] |
+------------------------------------------------------+
| (i) Question saved. Keep going.                 [new]|
+------------------------------------------------------+
| Classification                                        |
| [ Subject ______ ] [ Topic ______ ] [ Difficulty __ ] |
+------------------------------------------------------+
| Origin                                                |
| Edition: CEBRASPE . Tribunal XY . 2024 (locked) [new] |
| [ Paper number: 4 ] [ Notice subject ______ ]    [new]|
+------------------------------------------------------+
| Statement                                             |
| [______________________________________________]     |
| Type: ( ) Single choice  ( ) True/False               |
+------------------------------------------------------+
| Options / Answer key                                  |
| [______________________________________________]     |
|                                  [ Save and continue ] |
+------------------------------------------------------+
```

## Docs
- flow: new docs/flows/transcribe-paper.md — transcription mode: entry point, what stays and what clears between questions, the suggested number, no server-side batch state.

## Acceptance criteria
- AC1 (UC1) Given a saved edition, when the curator chooses "Transcribe" on its Questions card, then the transcription screen opens with that edition shown and locked, no type chosen, and the paper number prefilled per AC2. (BR2)
- AC2 (BR4) Given an edition whose live questions reach number 7, when transcription mode opens or a question is saved, then the suggested number is 8; given an edition with no live questions, then it is 1; a deleted question's number is not counted.
- AC3 (UC2, BR1, BR3, BR6) Given a filled, valid question, when the curator saves it, then it is stored exactly as the regular form would store it (`Draft`, the locked edition, the chosen notice subject, the typed number), the batch counter increases by one, and the form clears every field except edition, notice subject and content language.
- AC4 (UC3) Given the suggested number, when the curator changes it to another free one and saves, then it is stored with that number; given a number already used in the edition, then 409 `question.paper_number_taken`, the number field is marked and nothing is cleared. (F-96 BR3)
- AC5 (UC4) Given a chosen notice subject, when the curator saves and continues, then the next question opens with the same subject chosen; given the curator changes or clears it before saving, then the following question keeps that new choice.
- AC6 (UC5, BR5) Given the type cards on the transcription screen, then only SingleChoice and TrueFalse are offered; after a save, the type, statement, base text, difficulty and answer key are blank again.
- AC7 (BR2) Given the transcription screen, then there is no field or action that changes the edition; leaving and reopening from another edition's Questions card is the only way to transcribe a different one.
- AC8 (BR7) Given a user without `questions.manage`, then the "Transcribe" button is not shown on the edition page, the screen's route answers with the app's usual "not found" for a forbidden page, and `GET .../next-paper-number` answers 403.
- AC9 (BR6) Given two questions already saved in a session, when the curator navigates away without finishing and reopens transcription mode on the same edition, then the batch counter restarts at 1, the suggested number is based on the questions already saved (AC2), and both earlier questions are still in the bank as drafts.
- AC10 Given an unknown or deleted edition id in the screen's query string, then `GET .../next-paper-number` answers 404 `question.exam_edition_not_found` and the screen shows the not-found state with only "Finish".
- AC11 All new texts appear in pt-BR, pt-PT and en.

## Criterion → test

Tests locked: 3254ebaeb6da1b87a95e72b861d2fd3ca8f64482 (1 file); unlocks: 0

| Criterion | Test |
|---|---|
| AC1 | QuestionTranscriptionTests.Open_ShowsTheEditionLocked_AndThePaperNumberSuggested, Section_Transcribe_OpensTheTranscriptionScreenWithTheEditionChosen, Section_OfAnUnsavedEdition_HasNoTranscribeButton |
| AC2 | NextPaperNumberEndpointTests.NextPaperNumber_OfAnEditionWithNoQuestions_IsOne, NextPaperNumber_IsTheHighestLiveNumberPlusOne_AndIgnoresDeletedQuestions, NextPaperNumber_FollowsASavedQuestion |
| AC3 | QuestionTranscriptionTests.Save_StoresTheQuestionAndClearsTheForm_KeepingTheEditionTheSubjectAndTheLanguage |
| AC4 | NextPaperNumberEndpointTests.SavingTheSuggestedNumberTwice_IsRefusedTheSecondTime ; QuestionTranscriptionTests.Save_RefusedAsPaperNumberTaken_KeepsWhatWasTyped |
| AC5 | QuestionTranscriptionTests.Save_StoresTheQuestionAndClearsTheForm_KeepingTheEditionTheSubjectAndTheLanguage (subject kept); validation step 5 (changing it) |
| AC6 | QuestionTranscriptionTests.Open_OffersEveryTypeTheFormImplements; Save_StoresTheQuestionAndClearsTheForm_KeepingTheEditionTheSubjectAndTheLanguage (cleared fields) |
| AC7 | QuestionTranscriptionTests.Open_ShowsTheEditionLocked_AndThePaperNumberSuggested (edition field disabled) |
| AC8 | NextPaperNumberEndpointTests.NextPaperNumber_WithoutTheManagePermission_IsForbidden, NextPaperNumber_ForACurator_Answers200 ; ExamEditionQuestionsPermissionTests (button); validation step 6 (route as a user without the permission) |
| AC9 | QuestionTranscriptionTests.Reopen_RestartsTheCounter_AndSuggestsFromWhatIsStored |
| AC10 | NextPaperNumberEndpointTests.NextPaperNumber_OfAnUnknownEdition_Is404 ; QuestionTranscriptionTests.Open_OfAnUnknownEdition_ShowsTheNotFoundStateWithFinishOnly |
| AC11 | ResourceParityTests (every key in en, pt-BR and pt-PT) |

## Decisions
- 2026-10-14 — Entry point: a "Transcribe" button next to "Add question" in the edition's Questions card, opening the screen with the edition preselected — owner, asked (Q1): matches the existing "Add question" pattern, no new navigation to learn.
- 2026-10-14 — Suggested next paper number = max used in the edition + 1 (1 when none), always editable — owner, asked (Q2): the common case is sequential, and an override stays possible for a paper with gaps or a reordered transcription.
- 2026-10-14 — Edition, notice subject and content language stay selected across questions in a session; type, statement, base text, difficulty and answer key are cleared after each save — owner, asked (Q3): these three rarely change question to question in one paper, the rest is specific to each question.
- 2026-10-14 — Only SingleChoice and TrueFalse are offered — owner, asked (Q4), confirmed against `QuestionForm.razor` TypeCards: matches what the UI actually implements today.
- 2026-10-14 — Transcription reuses `questions.manage`; no new permission — owner, asked (Q5): the same curator who adds one question by hand is the one who transcribes a paper.
- 2026-10-14 — No server-persisted batch session; only the on-screen counter is lost on navigation, every saved question is already durable — owner, asked (Q6): simpler, and nothing is lost that matters (BR6).
- 2026-10-14 — After each save: success alert, batch counter, immediate auto-clear with no extra click (diverges from Simulae's manual "Cadastrar outra questão" button) — owner, asked (Q7): one fewer click per question across a whole paper.
- 2026-10-14 — Out of scope: batch review/publish screen, keyboard shortcuts, AI/PDF import of a paper, editing an existing question from this screen — owner, asked (Q8).
- 2026-10-14 — Every created question always starts as `Draft`; publishing stays a separate, later action — confirmed: `Question.cs`, `Create()`.
- 2026-10-14 — New read endpoint `GET /editions/{editionId}/next-paper-number`, `{ paperNumber }`, `max(PaperNumber) + 1` among live questions or `1` — Claude: no such query exists yet (`What exists`), and the suggestion must read real numbers, not be computed only in the browser from a page of questions.
- 2026-10-14 — The transcription screen is its own page and route (`/admin/questions/transcribe`), not a mode flag on `QuestionForm.razor` — Claude: the two differ enough (locked edition, no loading of an existing question, auto-reset after save, batch counter) that a shared component would need a parallel branch of state in most of its sections.
- 2026-10-14 — No new glossary row — Claude: transcription mode adds no persisted concept, entity or column; it is a screen-level flow over the existing `Question` and origin fields (F-96).
- 2026-10-14 — No new package — Claude: nothing here needs one.
- 2026-10-15 — Build: screen as a mode of the question form on `/admin/questions/transcribe` — owner, asked (change note v1).
- 2026-10-15 — Build: no `system-design` or `architect` pass — Claude: the only contract is one read route over an existing query interface and no schema change; a thin slice (step 4b).
- 2026-10-15 — Build: "Finish" opens the questions list filtered by the edition, not the edition page — Claude: the lookup of the question bank carries no exam id, which the edition page route needs, and adding it would change another contract; the list shows exactly the drafts just saved. Owner to confirm at validation.
- 2026-10-15 — Build: the type is back to the form's default (SingleChoice) after a save, since the form has no "no type chosen" state; the topic and subject clear as well, since AC3 lists only the edition, the notice subject and the language as kept — Claude.
- 2026-10-15 — Build: the suggestion past 500 is the plain `max + 1` (501): the form's own 1 to 500 check refuses it — Claude, no new rule.

## Out of scope
- A batch review or publish screen after transcribing — a later idea if the owner wants one.
- Keyboard shortcuts for faster entry.
- AI or PDF import of a paper's questions — epic E-9.
- Editing an existing question from this screen — use `/admin/questions/{id}`.
- Changing the edition mid-session (BR2).
- Question types besides SingleChoice and TrueFalse — F-104, F-106 to F-109, each when it ships.
- A server-stored batch/session, or resuming a batch's counter after navigating away (BR6).

## Open questions
- (none)

## Change notes
- 2026-10-15 (v1) The transcription screen is a mode of `QuestionForm.razor` on its own route (`/admin/questions/transcribe`), not a page of its own — owner, asked at build: the form is 1783 lines and a copy of its sections would double its upkeep. Replaces the decision of 2026-10-14 "own page and route". Criteria unchanged; AC1 to AC11 still hold.

## Validation script
Needed to validate: the local app host and one real past paper to transcribe a few questions from (owner; in place now when the app host starts).

1. Start the app host (from a terminal, with no other app host running): Git Bash `dotnet run --project src/Hosts/Simulab.AppHost`; PowerShell 7 `dotnet run --project src/Hosts/Simulab.AppHost`. This worktree gets its own database volume. Sign in as a curator or the admin.
2. Open an exam edition that is saved (Exams, an exam, an edition). In its Questions card choose "Transcribe". Expect: the edition line on top, "Question 1 of this session", the edition field locked and the number already filled (1 when the edition has no questions).
3. Fill subject, topic, statement and two options with one correct, choose a notice subject, then "Save and continue". Expect: "Question saved. Keep going.", the counter at 2, the number one higher, the notice subject and language kept, everything else blank.
4. Type a number already used in the edition and save. Expect: "Number already used in this edition.", the field marked, nothing cleared. Then type a free number and save: it stores with that number.
5. Change the notice subject, save and continue: the next question opens with the new one.
6. Choose "Finish". Expect: the questions list filtered by the edition with the saved questions as drafts. Sign in as a user without question permission and open `/admin/questions/transcribe`: the page is not reachable.
7. Switch the language to pt-BR and then pt-PT on the transcription screen: the new texts are translated.
8. Keyboard only: reach the form with Tab, fill a True/False question and save with Enter on "Save and continue".

## Events

- 2026-10-14 question — asked 4, from context 0
- 2026-10-14 question — asked 4, from context 1
- 2026-10-15 approved — owner ("aprovo f-99"), version 1

## Delivery
- Branch: `feature/F-99`, merged with `--no-ff` as `f56d6deb` (board #145). App version 0.37.0.
- Tests at the last full run: Web 1741, QuestionBank 305, Catalog 582, Identity 501, ArchitectureTests 220, Api 58, AppHost 80, Jobs 25, Persistence 25, Email 19, ApiResults 13, SharedKernel 12, Ai 10; 0 failed, 0 new warnings. New: 7 Api tests (locked at `3254ebae`) and 10 screen tests.
- Manual: `questions.md` and `exam-editions.md` in en, pt-BR and pt-PT. Technical page: `docs/flows/transcribe-paper.md`.
- Validated by the owner on screen on 2026-10-15; "Finish" opens the questions list filtered by the edition (the lookup carries no exam id).
Gate time: 6 passes, 19 min 59 s — turn 1 (40 s), ship 3 (14 min 49 s), scan 2 (4 min 30 s); 0 red
