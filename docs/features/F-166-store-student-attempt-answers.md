---
feature: F-166
epic: Exam Simulator
status: validating
board: 232
version: 2
---
# Store a student's answers for an attempt

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: validating (2026-10-26)
- Stopped at: Review fixes applied and validation script written; both scripts ran green against the real app host (2026-10-26).
- Next action: Owner follows the validation script (steps 1-5) and says validado; then /agile:ship F-166.
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
- BR10 An attempt and its answers are `TenantEntity` rows; `TenantId` is taken from `ICurrentTenant` (null today), so the rows stay global until F-162 wires it to the `tenant_id` claim. No query ignores the tenant filter and none reads the claim.
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
- AC19 Given an attempt of student B, when student A calls each route on it, then it answers 404 `exam_attempt.not_found` and nothing changes, because the ownership check by `student_id` decides; attempt and answer rows carry the `TenantId` of `ICurrentTenant` (null today).
- AC20 Given the migration, when it runs on an empty database and again, then it creates the schema `exam_engine` with `attempts` and `attempt_answers` (column comments for every column and enum, a unique index that allows one in-progress attempt per student and edition, with `NULLS NOT DISTINCT` where `TenantId` is in a unique index, and a test that inserts two global duplicates and expects a conflict).
- AC21 Given the architecture tests, when they run, then `ExamEngine` references other modules only through their `Contracts` projects, and the `/openapi/v1.json` document answers 200 and lists the five routes.
- AC22 `WebPermissions` lists the `ExamEngine` catalog. In en, pt-BR and pt-PT: name and description of `attempts.take` and `attempts.read`, the group `attempts`, and a text for each constant of `ExamAttemptErrorCodes` (field codes included). `RoleResourcesTests` and `ErrorCodeTextTests` are green.

## Criterion → test

Tests locked: 4a2b362e03905ab8947e18631276b1eb3e8d16b8 (10 files); unlocks: 1

| Criterion | Test |
| --- | --- |
| AC1 | `StartAttemptEndpointTests.Start_PublishedEdition_Answers201WithThePublishedQuestionsInPaperNumberOrder` |
| AC2 | `StartAttemptEndpointTests.Start_WhileOneIsInProgress_Answers200WithTheSameAttempt_AndCreatesNoSecondRow`, `StartAttemptEndpointTests.Start_ManyAtTheSameTime_CreatesOneAttemptAndAnswersTheOthersWithIt` |
| AC3 | `StartAttemptEndpointTests.Start_AfterTheFirstWasSubmitted_CreatesANewAttempt_AndLeavesTheSubmittedOneAlone` |
| AC4 | `StartAttemptEndpointTests.Start_OnADraftEditionOrAnUnknownOne_Answers404_AndCreatesNothing` |
| AC5 | `StartAttemptEndpointTests.Start_OnAPublishedEditionWithNoPublishedQuestion_Answers422` |
| AC6 | `AttemptReadEndpointTests.Read_AfterTheQuestionsWereEditedAnnulledOrUnpublished_StillListsTheSameQuestionsAndVersions` |
| AC7 | `SaveAnswerEndpointTests.Save_AValidAnswerOfEachOfTheEightTypes_IsReadBackAsSaved`, `SaveAnswerEndpointTests.Save_Answers200WithTheQuestionAndItsAnswer`, `SaveAnswerEndpointTests.Save_AnEssayWithWindowsLineBreaks_IsKeptWithOneKindOfLineBreakAndNothingElseChanged` |
| AC8 | `AnswerCheckerTests` (repeated item and blank, an option named twice, the essay lines, the characters of an open answer), `SaveAnswerEndpointTests.Save_AnAnswerThatDoesNotFitTheQuestion_Answers400WithOneEntryPerField_AndStoresNothing`, `SaveAnswerEndpointTests.Save_ABodyWithAMissingOrUnknownType_Answers400AnswerInvalid_AndStoresNothing`, `SaveAnswerEndpointTests.Save_AQuestionThatIsNotInTheAttempt_Answers404_AndStoresNothing` |
| AC9 | `SaveAnswerEndpointTests.Save_AnotherAnswerToTheSameQuestion_ReplacesTheFirst_AndTheLastOfTwoInARowIsWhatIsRead`, `SaveAnswerEndpointTests.Clear_MakesTheQuestionBlankAgain_AndClearingABlankOneAnswers204Too`, `SaveAnswerEndpointTests.Save_TwoAtTheSameTime_LeavesOneOfThemAndOneRow` |
| AC10 | `SubmitAttemptEndpointTests.Submit_AnAttemptWithBlanks_IsSubmittedWithTheServersTime_AndTheBlanksStayBlank` |
| AC11 | `SubmitAttemptEndpointTests.Save_OrClear_OnASubmittedAttempt_Answers409_AndNothingChanges`, `SubmitAttemptEndpointTests.Submit_AgainAnAttemptAlreadySubmitted_Answers200WithTheOriginalTime`, `SubmitAttemptEndpointTests.Save_RacingASubmit_IsEitherStoredBeforeItOrRefusedAfterIt_NeverStoredAfterIt` |
| AC12 | `AttemptReadEndpointTests.Read_TheOwnerSeesStateQuestionsAndAnswers_AndAnAdminHoldingReadSeesTheSame` |
| AC13 | `SubmitAttemptEndpointTests.AnAdmin_ReadsButNeverSavesClearsOrSubmits_AnAttemptOfAStudent` |
| AC14 | `StartAttemptEndpointTests.Start_AnAnonymousCallerIs401_ACuratorIs403_AndAStudentIs201`, `StartAttemptEndpointTests.TheSeededRoles_StudentHoldsTakeAndNotRead_AndAdminHoldsBoth`, `StartAttemptEndpointTests.Start_WithNoEdition_Answers400`, `AttemptReadEndpointTests.Read_Anonymous_Answers401`, `SubmitAttemptEndpointTests.TheWriteRoutes_AnAnonymousCallerIs401_AndACuratorIs403` |
| AC15 | `AttemptReadEndpointTests.Read_AnotherStudentsAttempt_AnswersLikeAnIdThatDoesNotExist`, `SubmitAttemptEndpointTests.AnotherStudent_CannotSaveClearOrSubmit_AndGetsTheAnswerOfAnIdThatDoesNotExist` |
| AC16 | `AttemptReadEndpointTests.Read_NeverCarriesAnAnswerKeyAnExplanationAModelAnswerOrAStatement` |
| AC17 | `SaveAnswerEndpointTests.Save_ABodyAboveTheLimitOfTheRoute_Answers4xx_AndStoresNothing`, `SaveAnswerEndpointTests.Save_ATextOfTheLargestSizeWithAccents_IsAccepted` |
| AC18 | `SubmitAttemptEndpointTests.Submit_WithATimeInTheBodyOrTheHeaders_StoresTheServersClock` |
| AC19 | `AttemptTenantTests.EveryRowOfAnAttempt_CarriesTheTenantOfTheHost_AndTheStudentStillReadsAndChangesIt`, `AttemptTenantTests.AnAttemptOfAnotherTenant_Answers404ToEveryRoute_AndNothingChanges`, `StartAttemptEndpointTests.Start_InV1_TheRowsOfTheAttemptAreGlobal` |
| AC20 | `InitialExamEngineMigrationTests` (up and down and a second run, two in-progress duplicates, the state and time check, two answer rows, column comments) |
| AC21 | `ExamEngineContractTests.TheOpenApiDocument_Answers200_AndListsTheFiveAttemptRoutes`, `ModuleBoundaryTests` (the `ExamEngine` module is in its lists) |
| AC22 | `RoleResourcesTests.EveryPermissionSystemRoleAndActionName_HasAText`, `ErrorCodeTextTests.EveryErrorCode_HasATextInEveryLanguage_AndEveryCodeShapedTextAnswersToACode` (existing tests that read the module's permissions and error codes) |

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
- 2026-10-26: design passes (system-design, architect, both opus) — accepted: five projects, module copy `AttemptQuestionType`, one `attempt_answers` row per question written at start (clear sets `answer` to null), partial unique index with `NULLS NOT DISTINCT` (also on `attempt_answers`), catch the unique violation and re-read on a double start, conditional submit with the server clock, write routes also need `attempts.take`, option and item positions from 0 (versions hold no ids), field codes `exam_attempt.answer_type_mismatch|option_unknown|item_unknown|blank_unknown|text_too_long|answer_empty` as `errors: [{ field, code }]`, an essay line is a line break after CRLF/CR become LF, matching may repeat a right option, line breaks normalized and no trim, a port in Application with its adapter in Infrastructure (the module boundary test), the answer body bound as `JsonElement` and read in the endpoint so a bad `type` answers `exam_attempt.answer_invalid`, the PUT body limit derived from the constants as route metadata (AC17 proves it), a published question with no version fails the start. Dropped: `IgnoreQueryFilters` with an own tenant rule (breaks profile.md:76 and builds F-162 inside this item); the index on `attempts.student_id` (no query here uses it; F-167 adds it if needed); skipping a question with no version. Moved to step 1: the architecture-test module lists, project references and DocGen entries (the tests cannot load the solution otherwise).
- 2026-10-26: design coverage — scope, rules, layout, contracts, data, simplicity checked by the architect; read: profile.md, project.md, api-contracts.md, ModuleBoundaryTests, SolutionAssemblies, ErrorCodeTextTests, WebPermissions, ModuleDbContext, F-162.
- 2026-10-26: traps — T1 reuse `QuestionType` in the Contracts: a Contracts-to-Contracts reference breaks the profile; T2 clear by soft delete: rows pile up and the unique index turns partial; T3 reading BR9 as covering save against submit: an answer could land after the submission (a row lock prevents it).
- 2026-10-26: premise stopped, waiting for the owner — (1) BR10/AC19: `ICurrentTenant` is always `NoTenant` and the filter hides a row stamped with a tenant from its own student; (2) AC22: the new permissions and error codes need texts in three locales (`RoleResourcesTests`, `ErrorCodeTextTests`) and a `WebPermissions` entry once the module joins the architecture lists.
- 2026-10-26: design trade-off 13 Save against submit — owner chose as proposed (`SELECT ... FOR SHARE` on the attempt in the save transaction)
- 2026-10-26: design trade-off 14 Where the answer shape is checked — owner chose as proposed (one `QuestionBank.Contracts` query per save)
- 2026-10-26: premise answers (owner) — BR10/AC19: option A, stamp `TenantId` from `ICurrentTenant` (null today), rows stay global until F-162, the 404 of AC19 comes from the ownership check; AC22: change it, the item adds the texts of the two permissions, the group `attempts` and every `ExamAttemptErrorCodes` constant in en, pt-BR and pt-PT plus the `WebPermissions` entry. Waiting for `/agile:change F-166` to rewrite BR10, AC19 and AC22 and re-approve them; no plan and no code before it.
- 2026-10-26: size — one chain (start, answer, submit) behind one API; the plan has 7 steps, inside the file refined; doubt keeps the item whole.
- 2026-10-26: plan — 1 module skeleton (five projects, solution, host wiring, architecture-test and DocGen lists, `WebPermissions` and the three-locale texts), the `QuestionBank` query contract, migration `InitialExamEngine` and start an attempt → AC1, AC4, AC5, AC14, AC20, AC21, AC22; 2 double start returns the attempt in progress, also at the same time → AC2; 3 read an attempt as owner or `attempts.read`, 404 for anyone else, no answer key, questions fixed → AC6, AC12, AC15 (read), AC16; 4 save and clear the option types (`SingleChoice`, `TrueFalse`, `MultipleAnswer`) with the shape check against the fixed version → AC7 (3 types), AC8 (options, type, question not in attempt), AC9; 5 the other five types, the question limit, the 20,000 cap, line-break normalization and the body limit of the PUT route → AC7, AC8, AC17; 6 submit with the server clock, idempotent retry, 409 on a submitted attempt, `FOR SHARE` against a racing save, a new attempt after a submit, ownership and `TenantId` on every write → AC3, AC10, AC11, AC13, AC15, AC18, AC19; 7 docs: `docs/flows/exam-attempt.md`, `docs/architecture-overview.md`, glossary rows, `DocGen --check` → `ArchitectureOverviewTests`, `techdocs.js check F-166`.
- 2026-10-26: tests unlocked (the AC10, AC11 and AC18 tests of `SubmitAttemptEndpointTests` were wrong: the fake clock moved 42 and 60 minutes past the 15-minute access token, and an anonymous type had two members of one JSON name; rewritten with 10 minutes and a raw body) — owner chose "Destravar e corrigir"
- 2026-10-26: the answer column is `json`, not `jsonb`: `jsonb` reorders the members of an object ("text" sorts before "type" at four letters) and the polymorphic read needs the discriminator first; the design said `jsonb`, the code proved it wrong — Claude (technical)
- 2026-10-26: the answer is kept as the contract's own `AnswerPayload` records (no second set of domain value objects), the 400 field codes gained `exam_attempt.entry_repeated` for a matching item or blank number named twice (BR5 says one answer for each), and the body of the save is read in the endpoint up to `AttemptLimits.SaveBodyMaxBytes` instead of route metadata — Claude (technical)
- 2026-10-26: the `architecture:` line of `## Docs` was dropped: `techdocs.js check` only accepts pages inside the folder of their kind, and `docs/architecture-overview.md` lives outside `docs/architecture/` (F-23 BR1); the overview was updated by hand all the same (the module, its schema, its contracts) and `ArchitectureOverviewTests` checks it — Claude (technical)
- 2026-10-26: review finding 1 (major) — fixed: `AppJson.Configure` sets `AllowOutOfOrderMetadataProperties`, so a save body with `type` after the other members is read; new test file `SaveAnswerMemberOrderTests` (the locked files stay untouched). Not seen failing before the fix.
- 2026-10-26: review finding 2 (minor) — start without `examEditionId` answers 400 with the code that means 404 in AC4 — new idea, not built here.
- 2026-10-26: review finding 3 (minor) — an oversized save body answers a bare 413 with no problem details and no code — new idea, not built here.
- 2026-10-26: review finding 4 (minor) — fixed: the `FOR SHARE` and `FOR UPDATE` reads take `studentId` and lock only the student's own row (`AND student_id = ...`); no test added, the existing cross-student tests (AC15, AC19) stay green.
- 2026-10-26: review finding 5 (minor) — fixed: `RowLockTransactions` lives once in `Simulab.Persistence`, both module copies deleted, the three QuestionBank stores gained `using Simulab.Persistence`.
- 2026-10-26: review finding 6 (minor) — fixed: one private `LockAndLoadAsync` in `AttemptStore` serves both lock reads.
- 2026-10-26: review finding 7 (minor) — fixed: the Portuguese words in a comment of `AnswerPayload` removed.
- 2026-10-26: review coverage — read the diff of 84 files against base 5fe733b, the five handlers, the `Attempt` and `AttemptAnswer` entities, `AnswerChecker`, the migration, the 10 locked test files, profile.md and the rules; checked and fine: correctness (BR1-BR11, AC1-AC13, start race, idempotent submit, save against submit), data (additive migration, unique indexes, constraints, column comments), localization (13 error codes and 2 permissions in 3 locales), locks (diff against the lock commit empty besides the recorded unlock), tests (every AC has a test that can fail), rules (module boundaries, profile). Not checked: none.
- 2026-10-26: validation scripts — the owner follows two scripts that call the API (`docs/validation/F-166/validate.sh` for Git Bash, `validate.ps1` for PowerShell 7); both were run against the real app host from this worktree and ended in `ALL OK`. The student account of that run (`aluno.f166@simulab.local`) stays in the local database; the owner uses an account of their own — Claude (technical)
- 2026-10-26: found while running the script — the first submit answered `submittedAt` with 7 fractional digits and every later read with 6 (PostgreSQL keeps microseconds), so the "same time" of AC11 differed as text. Fixed: `Attempt.Start` and `Attempt.Submit` keep microseconds; new test file `AttemptTimeTests` (not seen failing before the fix) — Claude (technical)
- 2026-10-26: traps — T1 `QuestionVersion` loaded whole on every save, two connections per save: cost under load; T2 `json` column depends on byte order: breaks if a writer rewrites through `jsonb` (F-167 anonymization, F-151 queue); T3 save body buffered up to 1.5 MB before the ownership check: GC pressure on frequent essay autosaves.

## Change notes
### v2 — 2026-10-26
- What: BR10, AC19 and AC22 rewritten. `TenantId` comes from `ICurrentTenant` (null today), no filter is bypassed and the claim is not read; AC19 is decided by the ownership check on `student_id`; AC22 now covers the `WebPermissions` entry and the en, pt-BR and pt-PT texts of the two permissions, the group `attempts` and every `ExamAttemptErrorCodes` constant.
- Why: the design pass found that `ICurrentTenant` is always `NoTenant`, so a row stamped with a tenant would be hidden from its own student, and that the Web tests require texts for every new permission and error code.
- Affected: BR10, AC19, AC22. Not affected: AC1 to AC18, AC20, AC21, the other rules, the routes, `Model: opus`.
- Re-approved by the owner: 2026-10-26.

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

## Validation script
There is no screen in this item: every check is a call to the API, made by two scripts that print one line per check (`docs/validation/F-166/validate.sh` for Git Bash, `validate.ps1` for PowerShell 7). Both were run before this text was written and ended in `ALL OK`.

### English
Needed to validate: the app host running from this worktree (you start it in step 1), one `Student` account with a verified email (step 2 creates one; the owner provides the password), the seeded `admin@simulab.local` (its development password is in `src/Hosts/Simulab.Api/appsettings.Development.json`) and a published edition with published questions (the edition `0198f370-0004-7000-8000-000000000003` is in the local database now). All in place.

1. [ ] In a terminal, at the root of this worktree (`D:\wt\simulab\f-166-store-student`), start the app host and leave it open; wait until `https://localhost:7287/api/v1/system/info` answers 200 → the Aspire dashboard (the page that lists the app's parts and their logs) address is printed.
   ```bash
   dotnet run --project src/Hosts/Simulab.AppHost
   ```
   ```powershell
   dotnet run --project src/Hosts/Simulab.AppHost
   ```
2. Skip this step if you already have a verified `Student` account on this machine.
   - [ ] 2.1 Open `https://localhost:7125/sign-up`, fill in an email, a password and the name, tick "I am 18 or older", the terms and the privacy policy, and submit → the page says the account was created and a confirmation email was sent.
   - [ ] 2.2 Open Mailpit (the local mailbox; its address is the `mailpit` entry in the Aspire dashboard resources list), open the message to that email and click the link inside → the page `verify-email` says the email is confirmed.
3. In a second terminal at the same folder, set the three variables (use your own `Student` email and password) and run the script → the last line is `ALL OK`.
   ```bash
   export ADMIN_PASSWORD='<admin password>' STUDENT_EMAIL='<student email>' STUDENT_PASSWORD='<student password>'
   bash docs/validation/F-166/validate.sh
   ```
   ```powershell
   $env:ADMIN_PASSWORD='<admin password>'; $env:STUDENT_EMAIL='<student email>'; $env:STUDENT_PASSWORD='<student password>'
   pwsh -NoProfile -File docs/validation/F-166/validate.ps1
   ```
   Expected output (the ids and times differ on each run):
   ```text
   == Step 2: start
   OK   anonymous start -> 401
   OK   student start -> 201
        attempt <id>, questions: 1:TrueFalse 2:MultipleAnswer 17:SingleChoice
   OK   start again returns the same attempt -> 200
   OK   same attempt id
   OK   unknown edition -> 404
   == Step 3: save and read back
   OK   save TrueFalse -> 200
   OK   save MultipleAnswer -> 200
   OK   save SingleChoice -> 200
   OK   read the attempt -> 200
        answers read back: [{"type":"TrueFalse","value":true},{"type":"MultipleAnswer","optionPositions":[0,1]},{"type":"SingleChoice","optionPosition":1}]
   == Step 4: invalid, other order, replace, clear
   OK   answer of another type -> 400
        code: exam_attempt.answer_invalid
   OK   type member after the others (true/false) -> 200
   OK   question not in the attempt -> 404
   OK   clear the answer -> 204
   == Step 5: who may read and change
   OK   admin reads the student's attempt -> 200
   OK   admin saves in it -> 404
   OK   admin submits it -> 404
   OK   anonymous reads -> 401
   OK   no key, explanation or model answer in the response
   == Step 6: submit
   OK   submit -> 200
        status Submitted, submittedAt <time>
   OK   submit again -> 200
   OK   same submission time
   OK   save after submit -> 409
        code: exam_attempt.already_submitted
   OK   clear after submit -> 409
   ALL OK
   ```
4. Read the output of step 3 against the rules:
   - [ ] 4.1 Start: the anonymous caller gets 401, the student gets 201 (200 when an earlier run left an attempt in progress), starting again gives the same attempt id, an unknown edition gives 404 → AC1, AC2, AC4, AC14.
   - [ ] 4.2 Answers: each of the three types is saved and the attempt reads them back as saved → AC7.
   - [ ] 4.3 Invalid and clear: an answer of another type is 400 `exam_attempt.answer_invalid`, an answer written with `type` after the other members is accepted, a question that is not in the attempt is 404, a cleared answer gives 204 → AC8, AC9.
   - [ ] 4.4 Access: the `Admin` reads the student's attempt (200) but cannot save or submit in it (404), an anonymous caller gets 401, and the response holds no key, explanation or model answer → AC12, AC13, AC14, AC16.
   - [ ] 4.5 Submit: the attempt becomes `Submitted`, submitting again gives the same time, saving or clearing after it gives 409 `exam_attempt.already_submitted` → AC10, AC11.
5. [ ] Run step 3 again → `ALL OK` again, and the line `student start` says 201 with a new attempt id (the earlier attempt is submitted, so a new one starts; AC3).

### pt-BR
Necessário para validar: o app host rodando a partir deste worktree (você o inicia no passo 1), uma conta `Student` com e-mail verificado (o passo 2 cria uma; a senha é sua), o `admin@simulab.local` do seed (a senha de desenvolvimento está em `src/Hosts/Simulab.Api/appsettings.Development.json`) e uma edição publicada com questões publicadas (a edição `0198f370-0004-7000-8000-000000000003` está no banco local agora). Tudo no lugar.

1. [ ] Em um terminal, na raiz deste worktree (`D:\wt\simulab\f-166-store-student`), inicie o app host e deixe aberto; espere `https://localhost:7287/api/v1/system/info` responder 200 → o endereço do dashboard do Aspire (a página que lista as partes do app e seus logs) aparece impresso.
   ```bash
   dotnet run --project src/Hosts/Simulab.AppHost
   ```
   ```powershell
   dotnet run --project src/Hosts/Simulab.AppHost
   ```
2. Pule este passo se você já tem uma conta `Student` verificada nesta máquina.
   - [ ] 2.1 Abra `https://localhost:7125/sign-up`, preencha e-mail, senha e nome, marque "tenho 18 anos ou mais", os termos e a política de privacidade, e envie → a página diz que a conta foi criada e que um e-mail de confirmação foi enviado.
   - [ ] 2.2 Abra o Mailpit (a caixa de e-mail local; o endereço é a entrada `mailpit` na lista de recursos do dashboard do Aspire), abra a mensagem desse e-mail e clique no link dentro dela → a página `verify-email` diz que o e-mail foi confirmado.
3. Em um segundo terminal, na mesma pasta, defina as três variáveis (use o e-mail e a senha do seu `Student`) e rode o script → a última linha é `ALL OK`.
   ```bash
   export ADMIN_PASSWORD='<admin password>' STUDENT_EMAIL='<student email>' STUDENT_PASSWORD='<student password>'
   bash docs/validation/F-166/validate.sh
   ```
   ```powershell
   $env:ADMIN_PASSWORD='<admin password>'; $env:STUDENT_EMAIL='<student email>'; $env:STUDENT_PASSWORD='<student password>'
   pwsh -NoProfile -File docs/validation/F-166/validate.ps1
   ```
   Saída esperada (os ids e os horários mudam a cada execução):
   ```text
   == Step 2: start
   OK   anonymous start -> 401
   OK   student start -> 201
        attempt <id>, questions: 1:TrueFalse 2:MultipleAnswer 17:SingleChoice
   OK   start again returns the same attempt -> 200
   OK   same attempt id
   OK   unknown edition -> 404
   == Step 3: save and read back
   OK   save TrueFalse -> 200
   OK   save MultipleAnswer -> 200
   OK   save SingleChoice -> 200
   OK   read the attempt -> 200
        answers read back: [{"type":"TrueFalse","value":true},{"type":"MultipleAnswer","optionPositions":[0,1]},{"type":"SingleChoice","optionPosition":1}]
   == Step 4: invalid, other order, replace, clear
   OK   answer of another type -> 400
        code: exam_attempt.answer_invalid
   OK   type member after the others (true/false) -> 200
   OK   question not in the attempt -> 404
   OK   clear the answer -> 204
   == Step 5: who may read and change
   OK   admin reads the student's attempt -> 200
   OK   admin saves in it -> 404
   OK   admin submits it -> 404
   OK   anonymous reads -> 401
   OK   no key, explanation or model answer in the response
   == Step 6: submit
   OK   submit -> 200
        status Submitted, submittedAt <time>
   OK   submit again -> 200
   OK   same submission time
   OK   save after submit -> 409
        code: exam_attempt.already_submitted
   OK   clear after submit -> 409
   ALL OK
   ```
4. Leia a saída do passo 3 contra as regras:
   - [ ] 4.1 Início: o chamador anônimo recebe 401, o aluno recebe 201 (200 quando uma execução anterior deixou uma tentativa em andamento), iniciar de novo devolve o mesmo id de tentativa, uma edição desconhecida dá 404 → AC1, AC2, AC4, AC14.
   - [ ] 4.2 Respostas: cada um dos três tipos é salvo e a tentativa devolve as respostas como salvas → AC7.
   - [ ] 4.3 Inválida e apagar: uma resposta de outro tipo dá 400 `exam_attempt.answer_invalid`, uma resposta escrita com `type` depois dos outros membros é aceita, uma questão que não é da tentativa dá 404, uma resposta apagada dá 204 → AC8, AC9.
   - [ ] 4.4 Acesso: o `Admin` lê a tentativa do aluno (200) mas não consegue salvar nem enviar nela (404), um chamador anônimo recebe 401, e a resposta não traz gabarito, explicação nem resposta-modelo → AC12, AC13, AC14, AC16.
   - [ ] 4.5 Envio: a tentativa vira `Submitted`, enviar de novo devolve o mesmo horário, salvar ou apagar depois dá 409 `exam_attempt.already_submitted` → AC10, AC11.
5. [ ] Rode o passo 3 de novo → `ALL OK` outra vez, e a linha `student start` diz 201 com um id de tentativa novo (a tentativa anterior foi enviada, então começa uma nova; AC3).

## Events

- 2026-10-26 question — asked 11, from context 2
- 2026-10-26 override — Who reads an attempt - chose Dono e Admin over Só o aluno dono (5.2 vs 8.0); kept
- 2026-10-26 question — follow-up: asked 1, from context 0
- 2026-10-26 change — v2: tenant from ICurrentTenant (BR10, AC19); UI texts and WebPermissions in AC22
- 2026-10-26 blocked — test wrong: AC10, AC11 and AC18 tests were wrong: the clock moved past the 15 min access token, and an anonymous type had two members of one JSON name
- 2026-10-26 test-fail — tests failed (Simulab.slnx): Simulab.Identity.Tests.SystemRoleSeedTests.Seed_RoleCreatedBeforeF9_IsMarkedAsSystemAndKeep, 1 red
- 2026-10-26 rework — review finding 1 AppJson reads type member out of order
- 2026-10-26 rework — review finding 4 lock reads only the students own row
- 2026-10-26 rework — review finding 5 RowLockTransactions moved to Persistence
- 2026-10-26 rework — review finding 6 one LockAndLoadAsync for both lock reads
- 2026-10-26 rework — review finding 7 comment in English
