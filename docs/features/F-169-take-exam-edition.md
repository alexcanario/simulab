---
feature: F-169
epic: Exam Simulator
status: refining
board: 235
version: 1
---
# Take an exam edition

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: refining (2026-10-27)
- Stopped at: Question round answered and the file filled; the screen has no mockup yet.
- Next action: Owner types `/agile:screen F-169`, approves the mockup, then reads this file and says "aprovo F-169".
- Waiting on: the owner

## Summary
The student's simulator screen: pick a published edition, start an attempt, answer question by question with base texts and images, jump through an answer sheet, see the notice subjects as the sections of the paper in paper order, and submit after a warning about blank questions. No timer here (F-170). It runs on the attempt of F-166.
Added 2026-10-27 (refinement): split in two; this item is part 1 and covers the option types (`SingleChoice`, `TrueFalse`, `MultipleAnswer`). The other five types and the essay with its line limit moved to F-178 (issue 246).

## Start
- Depends on: F-166.
- Waits on (to start): the mockup of `/agile:screen F-169` approved by the owner, before the approval of this item.
- Needed to validate: the seeded Manaus 2026 edition (exists: 60 single-choice questions, question 47 annulled, base texts and two images); a signed-in `Student` account with a verified email (the owner); the guard sample edition of F-110 for a question of another type (exists).
- Model: opus (a door: a new read of question content and images gated by the owner of an attempt; a public API contract in `ExamEngine`).
- Suggested path: `/agile:screen F-169` → approval → `/agile:build`.
- Parallel with: none.

## Goal
Give the student the first screen where a real paper is taken: the questions of the edition as printed, grouped by the notice subjects, with an answer sheet to move around and a submit that warns about blanks. It is what makes the attempt of F-166 usable by a person, and the screen the timer (F-170) and the result (F-171) build on.

## What already exists
Checked in the code on 2026-10-27, on `main` at `1bc9d69`.
- F-166 stores the attempt: `POST/GET /api/v1/exam-engine/attempts`, `PUT/DELETE .../answers/{questionId}`, `POST .../submit`. `AttemptResponse` carries ids, paper numbers, version numbers, types and the student's answers, never a statement, option text or base text (F-166 BR8). An annulled question is left out of the attempt (F-166 BR2).
- No student-facing read of question content exists. `QuestionVersionResponse` (Question bank contracts) holds the statement, base text and options of a version, plus the key and the explanation, which a student must not get. `IPublishedQuestionDirectory` gives answer shapes only.
- Images: the Web route `/question-images/{id}` (`QuestionImageEndpoints`) proxies the Api and requires `questions.manage` or `questions.review`; its comment says the student route arrives with the simulator. The sanitizer (`QuestionHtmlSanitizer`) admits only that address in a question's HTML.
- A question's origin has an optional `NoticeSubjectId` (F-96, F-75); the Manaus 2026 questions all have one.
- `CatalogExam.razor` (`/catalog/exams/{id}`, F-36) lists the published editions of an exam; it is the student's way to an edition.
- The Web host has no Api client for `ExamEngine`; it references `Simulab.ExamEngine.Contracts` for `WebPermissions` only. `QuestionPreviewContent.razor` renders a question from sanitized HTML for the back office and shows the key; it is a reference, not the screen.
- `AnswerSheet` is already in `docs/glossary.md`.

## Users and use cases
- UC1 A `Student` opens a published edition in the catalog and starts the simulation; when an attempt of theirs is in progress on that edition, the same action resumes it.
- UC2 A `Student` reads one question at a time, with its base text and images, and moves to the previous or the next one.
- UC3 A `Student` answers an option question (`SingleChoice`, `TrueFalse`, `MultipleAnswer`), changes the answer or clears it.
- UC4 A `Student` opens the answer sheet, sees which questions are answered or blank, grouped by notice subject, and jumps to one.
- UC5 A `Student` submits the attempt after a warning that lists the blank questions, and sees the page that confirms it was submitted.

## Business rules
- BR1 The start action shows on each published edition of `/catalog/exams/{id}` only for a user holding `attempts.take`. It calls the start of F-166: a new attempt, or the one in progress on that edition (F-166 BR3), and opens it.
- BR2 The screen shows the version of each question the attempt fixed (F-166 BR2), never the current one: statement, base text, option texts and images as stored, in the question's own content language. It never shows an answer key, an explanation or a rubric (F-166 BR8).
- BR3 Sections are the notice subjects of the questions (epic decision of 2026-10-26). Each section holds its questions in paper-number order; sections are ordered by the lowest paper number of their questions. Questions with no notice subject form a last section, "Other". A section shows its notice subject label.
- BR4 The answer sheet lists every question of the attempt by its paper number, grouped as BR3, each marked answered or blank. A paper number left out of the attempt (an annulled question) is not shown: the numbering jumps (Manaus: 46, 48).
- BR5 One question at a time, with previous and next, which cross sections; the first question opens when the attempt opens, or, on a resumed attempt, the first blank one.
- BR6 An answer is saved when the student marks it, with no confirm button: a single choice or true/false saves on click and clicking the marked one again clears it; a multiple answer saves the set on each tick and clearing the last tick clears the answer. The answer sheet updates after the save succeeds.
- BR7 A save that fails keeps the question as it was before the click and says so; a save refused because the attempt is already submitted (409) opens the submitted page.
- BR8 A question of another type (`Matching`, `FillInTheBlanks`, `ShortAnswer`, `OpenAnswer`, `Essay`) shows its statement and base text and a notice that this kind of answer is not yet in the simulator; it stays blank and counts as blank. F-178 replaces the notice.
- BR9 Submit asks for confirmation: the dialog says how many questions are blank and lists their numbers (or says none is blank) and that a submitted attempt cannot be changed. Confirming calls the submit of F-166 and opens the submitted page.
- BR10 The submitted page shows the edition, the submission date and time in the user's time zone, how many questions were answered and how many left blank, and a link back to the edition. Opening the screen of a submitted attempt opens this page. F-171 replaces it with the result.
- BR11 Only the student who owns the attempt opens its screen, its submitted page, its question content and its images; anyone else, an `Admin` included, gets "not found", as for an id that does not exist. The `Admin` reading of F-166 (`attempts.read`, Api only) is unchanged.
- BR12 An image opens through the attempt only when it is referenced by a question version fixed in that attempt; any other image id answers 404. The image keeps the headers of F-103 (`nosniff`, `sandbox`, `private`).
- BR13 Leaving the screen loses nothing: answers are already saved, and starting again from the edition resumes the attempt (BR1). There is no list of attempts (F-173).

## Screens and API
- `/catalog/exams/{id}` [changed] — a start action on each published edition row (BR1).
- `/attempts/{id}` [new] — the simulator: the question, previous and next, the answer sheet and submit (UC2 to UC5).
- `/attempts/{id}/submitted` [new] — the submitted page (BR10).
- Web `/attempts/{id}/images/{imageId}` [new] — streams an image of the attempt from the Api with the signed-in user's token (BR12); the content read gives the question HTML with image addresses pointing here.
- `GET /api/v1/exam-engine/attempts/{id}/content` [new] — the questions of the attempt at their fixed versions, for the owner: paper number, type, content language, statement, base text, options (position and text), notice subject (id, label) and the student's answer, in paper-number order; no key, explanation or rubric (BR2, BR3, BR11).
- `GET /api/v1/exam-engine/attempts/{id}/images/{imageId}` [new] — one image of the attempt, for the owner (BR12).
- Existing and unchanged: start, read, save, clear and submit of F-166.
- Error codes: the existing `exam_attempt.not_found` for both new routes (another student's attempt, an unknown id, an image not in the attempt); no new code.
- Contracts: `Simulab.ExamEngine.Contracts` gains the content response; `Simulab.QuestionBank.Contracts` gains a read of the student-safe content of given question versions (statement, base text, options, notice subject, image ids) and of one image; `Simulab.Catalog.Contracts` is read for the notice subject labels when the question bank does not carry them. The design pass settles the exact shape.
- Web: a typed `ExamEngineApiClient`, as `CatalogApiClient` is.

### Wireframe: simulator
```text
+------------------------------------------------------------+
| Guarda Municipal de Manaus 2026 · Consulplan     [new]     |
| Language and literature (questions 1-15)        [Sheet]    |
+------------------------------------------------------------+
| Question 3 of 59                                           |
| +--------------------------------------------------------+ |
| | Base text (when the question has one)                  | |
| +--------------------------------------------------------+ |
| Statement, with images inline                              |
|  ( ) A  option text                                        |
|  (o) B  option text                                        |
|  ( ) C  option text                                        |
|  ( ) D  option text                                        |
|                                                            |
| [< Previous]                              [Next >]         |
|                                    [Submit exam]           |
+------------------------------------------------------------+
| Answer sheet (side panel; a drawer on a phone)   [new]     |
| Language and literature                                    |
|  [1✓] [2✓] [3 ] [4 ] ...                                   |
| Legislation                                                |
|  ... [46✓] [48 ] ...                                       |
| Other                                                      |
|  12 of 59 answered                                         |
+------------------------------------------------------------+
```

### Wireframe: submit dialog and submitted page
```text
+------------------------------------------+
| Submit the exam?                 [new]   |
| 3 questions are blank: 12, 30, 58.       |
| After submitting you cannot change it.   |
|             [Keep answering] [Submit]    |
+------------------------------------------+

+------------------------------------------+
| Exam submitted                   [new]   |
| Guarda Municipal de Manaus 2026          |
| Submitted on 27/10/2026 at 15:42         |
| Answered: 56 · Blank: 3                  |
| [Back to the edition]                    |
+------------------------------------------+
```

## Security
### Surfaces
- S1 `GET /api/v1/exam-engine/attempts/{id}/content` — a signed-in user holding `attempts.take`; only the owner gets the content.
- S2 `GET /api/v1/exam-engine/attempts/{id}/images/{imageId}` and the Web route `/attempts/{id}/images/{imageId}` — the same.
- S3 The pages `/attempts/{id}` and `/attempts/{id}/submitted` — the same.
- S4 The start action on `/catalog/exams/{id}` — shown to holders of `attempts.take`; the catalog itself is read by holders of `catalog.browse` (students and curators).
### Threats
- T1 (S1, I) Another student, or an `Admin` through `attempts.read`, reads the content of an attempt that is not theirs, or tells an existing id apart from an unknown one → AC13 [sec]
- T2 (S1, I) The content leaks the answer key, the true/false key, the explanation or a rubric → AC14 [sec]
- T3 (S2, I) A student opens an image that is not in a version of their own attempt (a draft question's image, another exam's) by guessing its id → AC15 [sec]
- T4 (S2, T) A stored image is served so the browser runs it as a page of the app's origin → AC15 [sec]
- T5 (S3, I) A student opens another student's simulator or submitted page by its address → AC16 [sec]
- T6 (S1, S2, S) An anonymous caller reads content or an image → AC13 [sec], AC15 [sec]
- T7 (S4, E) A user without `attempts.take` (a `Curator`) sees or uses the start action → AC17 [sec]
### Personal data
No new datum. The screen shows the student's own answers (stored by F-166, whose `## Security` holds the row) to that student only (BR11).

## Docs
- flow: new docs/flows/take-exam-edition.md — the student's path: start from the edition, answer, the answer sheet and sections, submit and the submitted page, with the wireframes.
- flow: update docs/flows/exam-attempt.md#steps — the two new owner-only routes (content and image) and their 404 rule.

## Acceptance criteria
- AC1 Given a `Student` on `/catalog/exams/{id}` of an exam with a published edition, when they use the start action of that edition, then an attempt is started and `/attempts/{id}` opens on the first question; when they use it again while that attempt is in progress, then the same attempt opens on its first blank question.
- AC2 Given an attempt started before a question was edited, when the student opens it, then the statement, base text and options shown are those of the version fixed at the start.
- AC3 Given the Manaus 2026 attempt, when the screen opens, then the sections are the notice subjects with their labels, ordered by their lowest paper number, each with its questions in paper-number order; a question with no notice subject is in a last section "Other".
- AC4 Given the Manaus 2026 attempt, when the answer sheet opens, then it lists 59 numbers grouped by section, with 46 followed by 48, each marked answered or blank, and a click on a number shows that question.
- AC5 Given a question, when the student uses next and previous, then the next and the previous question in paper order are shown, across sections; previous is unavailable on the first question and next on the last.
- AC6 Given a blank single-choice or true/false question, when the student marks an option, then the answer is saved and the answer sheet shows it answered; when they mark another, then it replaces the first; when they click the marked one again, then the answer is cleared and the sheet shows it blank.
- AC7 Given a multiple-answer question, when the student ticks two options, then the saved answer holds both; when they untick both, then the answer is cleared.
- AC8 Given a save that fails, when the Api answers an error, then the question shows the answer it had before the click and a message says the answer was not saved; when the Api answers 409 `exam_attempt.already_submitted`, then the submitted page opens.
- AC9 Given a question of another type (the guard sample edition of F-110), when it is shown, then its statement and base text appear with the notice that this answer is not yet in the simulator, and it stays blank.
- AC10 Given an attempt with blank questions, when the student chooses submit, then the dialog shows how many are blank and their numbers; when they cancel, then nothing changes; when they confirm, then the attempt is submitted and the submitted page opens. With no blank question the dialog says none is blank.
- AC11 Given a submitted attempt, when its submitted page opens, then it shows the edition, the submission date and time in the user's time zone, the answered and blank counts and a link to the edition; opening `/attempts/{id}` of that attempt opens this page.
- AC12 Given a question with an image and a base text (Manaus 2026), when it is shown, then the image loads through `/attempts/{id}/images/{imageId}` and the base text appears above the statement.
- AC13 [sec] (T1, T6) Given an attempt of student A, when student B, an `Admin` or an anonymous caller asks for its content, then B and the `Admin` get 404 `exam_attempt.not_found`, identical to an unknown id, and the anonymous caller gets 401.
- AC14 [sec] (T2) Given questions with an answer key, a true/false key and an explanation, when their attempt's content is read, then the response holds none of them.
- AC15 [sec] (T3, T4, T6) Given an image of a question in student A's attempt and an image of a draft question, when student A asks for the second through the attempt, or student B asks for the first through A's attempt, then each answer is 404; the anonymous caller gets 401 (Api) or the sign-in (Web); the image A may open is served with its stored type, `nosniff`, `sandbox` and `private`.
- AC16 [sec] (T5) Given an attempt of student A, when student B or an `Admin` opens `/attempts/{id}` or `/attempts/{id}/submitted`, then the not-found page is shown and nothing of the attempt appears.
- AC17 [sec] (T7) Given a `Curator`, when they open `/catalog/exams/{id}`, then no start action is shown; a `Student` sees it on each published edition.
- AC18 The architecture tests stay green: `ExamEngine` reads the question bank and the catalog only through their `Contracts`, and `/openapi/v1.json` lists the two new routes.
- AC19 Every new text (start action, simulator, answer sheet, "Other", notice of BR8, submit dialog, submitted page, messages) exists in pt-BR, pt-PT and en, and the missing-key test is green.

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
| AC19 | pending |

## Decisions
- 2026-10-27 — Size L judged; split in two: this item keeps the option types, the content read, images, answer sheet, sections and submit; F-178 (issue 246) takes `Matching`, `FillInTheBlanks`, `ShortAnswer`, `OpenAnswer` and the essay — asked ("Split as proposed")
- 2026-10-27 — No similar item (213 items read) — Claude (technical)
- 2026-10-27 — The sections are the notice subjects in paper order, no `Section` entity, as decided in the epic Exam Simulator. Still true here? Yes — confirmed: docs/epics/exam-simulator.md
- 2026-10-27 — The student starts and resumes from a start action on each published edition of `/catalog/exams/{id}`; no new page to pick an edition — asked
- 2026-10-27 — One question at a time with previous and next, and the answer sheet beside it (a drawer on a phone) — asked
- 2026-10-27 — An answer is saved when it is marked; clicking the marked option again clears it; no confirm button — asked
- 2026-10-27 — An annulled question is not shown and the numbering jumps; F-166 BR2 is unchanged; "annulled counts for everyone" stays with F-174 — asked
- 2026-10-27 — A question of a type that arrives with F-178 shows its statement with a notice and stays blank; the start is not hidden — asked
- 2026-10-27 — Questions with no notice subject go to a last section "Other" — asked
- 2026-10-27 — After submitting, a "submitted" page with date, time and counts and a link to the edition; F-171 replaces it with the result — asked
- 2026-10-27 — An image opens only when it is in a question version of the student's own attempt — asked
- 2026-10-27 — Only the owner opens the simulator; the `Admin` keeps the Api read of F-166 and gets "not found" on the screen and on the content and image routes — asked
- 2026-10-27 — The screen gets a mockup (`/agile:screen F-169`) before approval — asked
- 2026-10-27 — No new package: MudBlazor, the typed Http clients and bUnit 2.10.3 are in `Directory.Packages.props` — asked
- 2026-10-27 — The content and the images are read through `ExamEngine` routes scoped to the attempt, not through the question bank's back office routes: the attempt decides who may read and which version; the image address in the HTML is rewritten from `/question-images/{id}` to `/attempts/{attemptId}/images/{id}` for the student; the design pass settles where the rewrite happens — Claude (technical)
- 2026-10-27 — Sections are ordered by the lowest paper number of their questions, not by the notice subject's display order: the screen follows the paper, and the two agree on Manaus — Claude (technical)
- 2026-10-27 — A resumed attempt opens on its first blank question — Claude (technical)
- 2026-10-27 — No new error code: both new routes answer `exam_attempt.not_found` as F-166 BR7 does — Claude (technical)

## Out of scope
- `Matching`, `FillInTheBlanks`, `ShortAnswer`, `OpenAnswer` and the essay with its line counter (F-178).
- Timer, warning near the end and automatic submit (F-170).
- Score, cut-off and the result screen (F-171); answer review with the key (F-172); the list of attempts (F-173).
- Showing annulled questions in the paper (F-174 decides how they count).
- An `Admin` view of a student's simulator.
- Pause, abandon, anti-fraud and booklet types (epic `## Out of scope`).

## Open questions
- (none)

## Change notes

## Validation script
Written at the end of the build.

## Events

## Delivery
