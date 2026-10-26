---
feature: F-166
epic: Exam Simulator
status: building
board: 232
version: 1
---
# Store a student's answers for an attempt

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: building (2026-10-26)
- Stopped at: Building started; fresh-context stop before the design passes (system-design, architect).
- Next action: Run /agile:build F-166 in a clean session (resumes at 4b), or answer segue.
- Waiting on: nothing

## Summary
A student starts an attempt on a published exam edition, saves an answer to each question and submits the attempt, and the system keeps all of it, including the text of an essay or open answer, so that scoring, results and the grader's queue have something to read. Part 1 of a split (2026-10-26): the privacy side (anonymizing attempts on erasure, adding them to "download my data") is F-167. There is no screen: the item is a new module and an API. Nothing in `src/Modules` stores a student's answers today. Source: discussion D-9 (2026-10-26).

## Start
- Depends on: nothing (F-98, F-109 and F-36 are delivered).
- Waits on (to start): nothing.
- Needed to validate: a signed-in `Student` account, an `Admin` account and a published exam edition with published questions of several types (the Manaus 2026 guard seed of F-110 has them) — the owner provides the accounts; a way to call the API (a script of commands, written by the build).
- Model: opus (a door: a new API with personal data, tenant and permissions; a public contract in two existing modules; a new module).
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: none. F-167 starts after this one is built; it must be released together with it (see `## Decisions`).

## Goal
Give the Exam Simulator (epic E-06) and, later, the Question Bank Simulator (E-05) and the grader's queue (F-151) a place where a student's attempt and answers live, with the rules that keep it trustworthy: the questions are fixed at the start, each one names the version the student saw, an answer has the shape its question type asks for, and a submitted attempt never changes.

## What already exists
Checked in the code on 2026-10-26, on `main` at `5fe733b`.
- No module stores attempts: `src/Modules` holds `Catalog`, `Identity`, `Plans` and `QuestionBank`. The glossary already defines `Attempt`; `docs/agile/profile.md` reserves the module name `ExamEngine` (five projects, origin Simulae `ExamEngine`).
- The Simulae module (`D:\dev\_control-alt-systems\simulae\repo\src\Modules\ExamEngine`, read only; the path in `CLAUDE.md` is stale) has the attempt aggregate with a state machine, an opaque `AnswerJson`, a Redis timer and lock, an auto-submit background service and a SignalR hub. Only the attempt and its answers are taken here; the rest waits for the timer item.
- `QuestionBank` exposes no student-facing read: its lists are back office routes and `EditionQuestionListItemResponse` is a back office row. Questions have a status (`Draft`, `Published`, `Annulled`, `InReview`), a paper number inside the edition (`QuestionOriginResponse`) and immutable versions (`QuestionVersion`, F-98).
- `Catalog.Contracts` has `IExamEditionDirectory.FindAsync`, which returns an edition of any status with its `Status` (`Draft` or `Published`); an edition has no time limit, sections or scoring rule.
- Identity: the `Student` role is given at sign-up; a module declares a permission catalog with `InitialGrants` (`CatalogModule` gives `Browse` to `Student` and `Curator`); `UserErased` is published after an erasure (F-59); `User.TenantId` exists since F-150 and `ICurrentTenant` is in `SharedKernel`.
- Rules that apply: `TenantEntity` for every entity, soft delete, one schema and `DbContext` per module, modules talk only through `Contracts` (`.claude/rules/agile/project.md`, `docs/agile/profile.md`).

## Users and use cases
- UC1 A `Student` starts an attempt on a published exam edition and gets the attempt with its list of questions.
- UC2 A `Student` saves, replaces or clears the answer to one question of an in-progress attempt.
- UC3 A `Student` submits an in-progress attempt.
- UC4 A `Student` reads their own attempt: state, questions and the answers saved so far.
- UC5 An `Admin` reads any attempt, for support. An `Admin` never changes one.

## Business rules
- BR1 An attempt starts only on an edition that exists and is `Published`; a draft or unknown edition answers as if it did not exist. An edition with no published question cannot start one.
- BR2 The questions of an attempt are fixed at the start: the `Published` questions of the edition (not `Draft`, `InReview` or `Annulled`), in the order of their paper number. Each one records the question id, its paper number and the number of its current `QuestionVersion`. Editing or annulling a question later never changes an attempt already started.
- BR3 A student has at most one attempt in progress per edition. Starting again while one is in progress returns that attempt and creates nothing. After it is submitted, starting creates a new one; there is no limit on submitted attempts.
- BR4 There is at most one answer per question of the attempt. Saving replaces it; clearing removes it. A question with no answer stays blank, and an attempt may be submitted with blanks.
- BR5 An answer has the shape of its question type, checked against the question's version, and nothing is stored when it is invalid:
  - `SingleChoice`: one option of the version.
  - `TrueFalse`: true or false.
  - `MultipleAnswer`: one or more options of the version.
  - `Matching`: for each left item, at most one right option, all of the version.
  - `FillInTheBlanks`: for each blank number of the version, a text.
  - `ShortAnswer`: a text.
  - `OpenAnswer`: a text of at most the size limit of the question, in characters.
  - `Essay`: a text of at most the size limit of the question, in lines.
  Typed text is kept as written, with line breaks normalized; a text is at most 20,000 characters whatever the type. The minimum number of lines of an essay is not checked when saving or submitting: it is judged at grading.
- BR6 Only an attempt in progress takes answers. Submitting moves it to `Submitted` and records the submission time from the server clock; a submitted attempt never changes again. Submitting a submitted attempt answers 200 with the original time (a retry is safe); saving or clearing an answer on it answers 409.
- BR7 Only the student who owns an attempt reads and changes it; any other student gets 404, as if it did not exist. An `Admin` (permission `attempts.read`) reads any attempt and changes none.
- BR8 The response of an attempt carries the questions' ids, numbers, versions and types and the student's own answers, never an answer key, a rubric's model answer or an explanation.
- BR9 Two saves to the same question at once: the last one wins. No concurrency token.
- BR10 An attempt and its answers are `TenantEntity` rows; `TenantId` comes from the student's tenant (dormant: null for everyone today).
- BR11 The attempt records when it started and when it was submitted, and nothing about time limits, pauses or time per question; those arrive with the timer.

## Screens and API
No screen. The routes (module `ExamEngine`, schema `exam_engine`), all under `/api/v1/exam-engine`:
- `POST /api/v1/exam-engine/attempts` — start (body: the edition id); 201 with the new attempt, 200 with the one already in progress (UC1).
- `GET /api/v1/exam-engine/attempts/{id}` — read an attempt (UC4, UC5).
- `PUT /api/v1/exam-engine/attempts/{id}/answers/{questionId}` — save or replace one answer; the body carries a `type` discriminator (UC2).
- `DELETE /api/v1/exam-engine/attempts/{id}/answers/{questionId}` — clear one answer (UC2).
- `POST /api/v1/exam-engine/attempts/{id}/submit` — submit (UC3).
- Error codes (constants in the module's contracts): `exam_attempt.edition_not_found`, `exam_attempt.edition_without_questions`, `exam_attempt.not_found`, `exam_attempt.question_not_in_attempt`, `exam_attempt.answer_invalid` (400, one entry per field with its code), `exam_attempt.already_submitted`.
- Permissions (catalog of the module): `attempts.take`, granted initially to `Student` (and to `Admin`, as every permission); `attempts.read`, granted to `Admin` only.
- Contracts: a new `Simulab.ExamEngine.Contracts` (requests, responses, error codes, permissions); a new query interface in `Simulab.QuestionBank.Contracts` that gives the published questions of an edition in paper order with their version numbers and what is needed to check an answer (option ids, matching item ids, blank numbers, size limit); `IExamEditionDirectory` of the catalog is used as it is.

## Security
### Surfaces
- S1 `POST /api/v1/exam-engine/attempts` — a signed-in user holding `attempts.take`.
- S2 `GET /api/v1/exam-engine/attempts/{id}` — the owner, or a user holding `attempts.read`.
- S3 `PUT` and `DELETE /api/v1/exam-engine/attempts/{id}/answers/{questionId}` — the owner.
- S4 `POST /api/v1/exam-engine/attempts/{id}/submit` — the owner.
### Threats
- T1 (S1, E) An anonymous caller, or a user without `attempts.take`, starts an attempt → AC14 [sec]
- T2 (S2, I) A student reads another student's attempt, or an id that does not exist is told apart from one that is not theirs → AC15 [sec]
- T3 (S3, T) A student saves, clears or submits in another student's attempt → AC15 [sec]
- T4 (S2, I) The attempt response, for the student or the admin, leaks an answer key, a model answer or an explanation → AC16 [sec]
- T5 (S3, T) An answer names an option, item or blank that is not in the question's version, or a question that is not in the attempt → AC8 [sec]
- T6 (S3, D) An oversized answer fills the database or the request → AC17 [sec]
- T7 (S1, D) Repeated starts pile up attempts in progress → AC2 [sec]
- T8 (S4, T) The submission time is taken from the client → AC18 [sec]
- T9 (S3, T) An answer is changed after the attempt was submitted → AC11 [sec]
- T10 (S2, E) A student holds `attempts.read` through the `Student` role → AC14 [sec]
### Personal data
| Datum | Lives in | Read by | Kept for | Legal basis |
| --- | --- | --- | --- | --- |
| The student's id on an attempt; the text of each answer, essays included; the times | `exam_engine.attempts` and `exam_engine.attempt_answers` | The owner student; an `Admin` holding `attempts.read`; later the grader (F-151) | While the account exists; after an erasure the rows stay with no person attached (ADR-0001 #9, done by F-167) | Performance of the service the student asked for (LGPD art. 7, V; GDPR art. 6(1)(b)) |

The reading of an attempt by an `Admin` is not audited in this item (see `## Decisions`).

## Docs
- flow: new docs/flows/exam-attempt.md — start, answer and submit, the per-type answer shapes, the states and the access rules, as a sequence of API calls.
- architecture: update docs/architecture-overview.md — the new `ExamEngine` module, its schema and its contracts with `Catalog` and `QuestionBank`.

## Acceptance criteria
- AC1 Given a published edition with three published questions, one draft and one annulled, when a `Student` starts an attempt, then the answer is 201 and the attempt is in progress with exactly the three published questions in paper-number order, each with its id, paper number, version number and type, and the start time recorded.
- AC2 [sec] (T7) Given an in-progress attempt of a student on an edition, when the same student starts again on it, then the answer is 200 with the same attempt id and no second row exists, also when two starts arrive at the same time.
- AC3 Given a submitted attempt, when the student starts on the same edition, then a new attempt is created (201) and the submitted one is unchanged.
- AC4 Given a draft edition or an edition id that does not exist, when a student starts, then the answer is 404 `exam_attempt.edition_not_found` and nothing is created.
- AC5 Given a published edition with no published question, when a student starts, then the answer is 422 `exam_attempt.edition_without_questions`.
- AC6 Given an attempt started, when one of its questions is edited, annulled or unpublished afterwards, then reading the attempt still lists the same questions with the same version numbers.
- AC7 Given an in-progress attempt, when a valid answer of each of the eight question types is saved, then reading the attempt returns each answer as saved.
- AC8 [sec] (T5) Given an in-progress attempt, when a save names an option, matching item or blank number that is not in the question's version, a text above the question's limit or above 20,000 characters, a shape of another type, or a question that is not in the attempt, then the answer is 400 `exam_attempt.answer_invalid` (one entry per field with its code) or 404 `exam_attempt.question_not_in_attempt`, and nothing is stored.
- AC9 Given a saved answer, when the student saves another one to the same question, then the second replaces the first; when the student clears it, then the question is blank again; when two saves arrive one after the other, then the last one is what is read.
- AC10 Given an in-progress attempt with blank questions, when the student submits, then the attempt is `Submitted` with the submission time and the blank questions stay blank.
- AC11 [sec] (T9) Given a submitted attempt, when the student saves or clears an answer, then the answer is 409 `exam_attempt.already_submitted` and nothing changes; when the student submits again, then the answer is 200 with the original submission time.
- AC12 Given a student, when they read their own attempt, then the answer is 200 with the state, the questions and the answers; an `Admin` holding `attempts.read` reads any attempt the same way.
- AC13 Given an `Admin`, when they save, clear or submit in an attempt of a student, then the answer is 404 and nothing changes.
- AC14 [sec] (T1, T10) Given an anonymous caller, a `Curator` and a `Student`, when each calls the start route, then the anonymous caller gets 401, the `Curator` gets 403 and the `Student` gets 201; the seeded `Student` role holds `attempts.take` and not `attempts.read`, and every route is tested with the `Student` role, not with the administrator.
- AC15 [sec] (T2, T3) Given an attempt of student A, when student B reads it, saves, clears or submits, then every answer is 404 `exam_attempt.not_found`, identical to the answer for an id that does not exist.
- AC16 [sec] (T4) Given a question with an answer key, an explanation and a model answer, when the attempt is read by the student or by an `Admin`, then the response holds none of them.
- AC17 [sec] (T6) Given a request body above the limit of the route, when it is sent, then the answer is a 4xx and nothing is stored.
- AC18 [sec] (T8) Given a submit with a time in the body or headers, when the attempt is submitted, then the stored submission time is the server's clock.
- AC19 Given attempts of two tenants, when a student of tenant A calls each route on an attempt of tenant B, then it answers 404 and nothing changes; attempt rows carry the student's `TenantId` (null today).
- AC20 Given the migration, when it runs on an empty database and again, then it creates the schema `exam_engine` with `attempts` and `attempt_answers` (column comments for every column and enum, a unique index that allows one in-progress attempt per student and edition, with `NULLS NOT DISTINCT` where `TenantId` is in a unique index, and a test that inserts two global duplicates and expects a conflict).
- AC21 Given the architecture tests, when they run, then `ExamEngine` references other modules only through their `Contracts` projects, and the `/openapi/v1.json` document answers 200 and lists the five routes.
- AC22 This item adds no UI text, so there is no locale file to complete; the error codes are constants with no message text that a client branches on.

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
| AC20 | pending |
| AC21 | pending |
| AC22 | pending |

## Decisions
- 2026-10-26 — The new module is `ExamEngine`, five projects (Clean Architecture), schema `exam_engine` — as decided in `docs/decisions/ADR-0001-foundation.md` #2b and `docs/agile/profile.md`. Still true here? Yes — confirmed: docs/decisions/ADR-0001-foundation.md
- 2026-10-26 — Each question of an attempt stores the number of the version the student saw — as decided in `.claude/rules/agile/project.md` (Data) and ADR-0001 #10. Still true here? Yes — confirmed: .claude/rules/agile/project.md
- 2026-10-26 — This part has no screen and no pause, abandon, timer or list; the student starts, answers, submits and reads — asked
- 2026-10-26 — Several attempts per student and edition, one in progress; starting again returns the one in progress — asked
- 2026-10-26 — The attempt holds the published questions of a published edition, in paper order, fixed at the start with their versions; annulled ones are left out; the scoring item can revisit this when it needs "annulled counts for everyone" — asked
- 2026-10-26 — Answers are typed by question type and checked against the question's version when saved, blank allowed and replaceable until submitted — asked
- 2026-10-26 — Two states, `InProgress` and `Submitted`; pause, abandoned, admin close and automatic submit arrive with the timer — asked
- 2026-10-26 — One permission `attempts.take`, granted to `Student` (and `Admin`) — asked
- 2026-10-26 — Who reads an attempt: the owner student and an `Admin`; the `Admin` only reads — owner chose "Dono e Admin" over the recommended "Só o aluno dono" (average 5.2 vs 8.0); kept — asked. The owner did not say whether those reads are audited; Claude (technical) leaves them unaudited here, because the account audit trail (F-21) covers account events only, and records it for the owner to confirm at the approval.
- 2026-10-26 — The last of two simultaneous saves wins; no concurrency token — asked
- 2026-10-26 — The owner validates by a script of commands calling the API as a `Student` and as an `Admin`; no temporary page — asked
- 2026-10-26 — Timer, score and cut-off and the simulator screen are only listed in `## Out of scope`; `/agile:epic` on E-06 will break them into items — asked
- 2026-10-26 — The item is split in two: this one stores the attempt, F-167 (issue 233) erases and exports it — asked ("Split as proposed")
- 2026-10-26 — F-166 is not released without F-167: until the erasure and the export cover the attempts, a student could store personal text through the API with nothing to anonymize it. The owner publishes both together (`/agile:publish`) — Claude (technical)
- 2026-10-26 — Reading by an `Admin` uses the same `GET` route and a second permission `attempts.read`, granted to `Admin` only, so no role reads attempts by name — Claude (technical)
- 2026-10-26 — Submitting a submitted attempt is idempotent (200 with the original time) and saving on it is 409: a retry of a lost response must not look like a failure — Claude (technical)
- 2026-10-26 — The minimum lines of an essay is not checked on save or submit: a short essay is a poor essay for the grader to score, not an invalid one; the maximum is checked because it is a storage limit — Claude (technical)
- 2026-10-26 — A 20,000-character cap on any text answer, a constant of the module — a storage and request limit, above the largest limit a question can set — Claude (technical)
- 2026-10-26 — An answer is one row per attempt and question (`attempt_answers`), the payload stored as JSON with the question type as discriminator; the module reads questions only through a new `QuestionBank.Contracts` query interface, never its tables — Claude (technical)
- 2026-10-26 — No Redis, no background job and no SignalR in this item; no new package (EF Core, Npgsql and the test fixtures exist) — Claude (technical)
- 2026-10-26 — The build adds the glossary rows `AttemptAnswer` and `AttemptStatus` and the `ExamEngine` row of `docs/agile/profile.md` is already there — Claude (technical)
- 2026-10-26 — `CLAUDE.md` names the Simulae folder as `D:\dev\_icontrol\simulae`; it is `D:\dev\_control-alt-systems\simulae\repo` now. The line is left for the owner to correct, since it is outside this item — Claude (technical)

## Out of scope
- Timer, time limit, pause, abandon, admin close and automatic submit (arrive with the simulator's timer, epic E-06).
- Scoring, penalties, cut-off and any result screen.
- The simulator screen the student uses, and the custom practice of the Question Bank Simulator (E-05).
- Anonymizing attempts on erasure and adding them to "download my data" (F-167).
- The grader's queue and screen (F-151) and AI-suggested grades (F-152).
- Listing a student's attempts and resuming one from a list.
- Auditing the reads of an `Admin`.
- Redis, SignalR and any background job.

## Open questions
- (none)

## Events

- 2026-10-26 question — asked 11, from context 2
- 2026-10-26 override — Who reads an attempt - chose Dono e Admin over Só o aluno dono (5.2 vs 8.0); kept
- 2026-10-26 question — follow-up: asked 1, from context 0
