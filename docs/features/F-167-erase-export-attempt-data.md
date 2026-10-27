---
feature: F-167
epic: Exam Simulator
status: done
board: 233
version: 2
---
# Erase and export a student's attempt data

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: done (2026-10-27)
- Stopped at: Shipped on 2026-10-27: merge 2f9670a, app version 1.24.0, board #233 closed.
- Next action: nothing: done.
- Waiting on: nothing

## Summary
Split from F-166 (2026-10-26). When a student erases the account, the free texts of the answers in their attempts are erased and the rest of the attempts stays without a person attached, and "download my data" (F-16) includes the student's attempts and answers. F-166 stores them; this item makes the privacy rules of ADR-0001 #9 (erasure keeps unidentified attempts) and F-16 (each module adds its own section to the export) hold for them. It must be done before any student uses the simulator screen, and F-166 is released together with it.

## Start
- Depends on: F-166 (delivered, merged as 1e83f94).
- Waits on (to start): nothing outside the items above.
- Needed to validate: the app host running from this worktree and one `Student` account with a verified email and a known password (the owner provides it; the build writes a script of commands that creates attempts through the API, as F-166's did). The account is erased at the end, so use one you do not need.
- Model: opus (a door: the data export route returns personal data; a public contract in `Identity.Contracts` that a second module implements; the first consumer of `UserErased`).
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: none. F-166 is not released without this item: they are published together (`/agile:publish`).

## Goal
Make the two promises the app already gives about personal data true for a student's attempts: erasing the account removes what can name the person from the attempts (the free texts) and keeps the rest as unidentified statistics, and "download my data" hands the person every attempt and answer they gave, in the same file as the rest.

## What already exists
Checked in the code on 2026-10-26, on `main` at `424606b`.
- The attempts: `Attempt` and `AttemptAnswer` (`src/Modules/ExamEngine/Simulab.ExamEngine.Domain/Entities`), tables `exam_engine.attempts` and `exam_engine.attempt_answers`. The student is held by id (`StudentId`, no foreign key). One answer row per question, written at the start; the answer is the contract's `AnswerPayload` stored as `json`, with eight shapes. The text lives in `ShortAnswerPayload.Text`, `OpenAnswerPayload.Text`, `EssayPayload.Text` and each `BlankTextPayload.Text` of `FillInTheBlanksPayload`; the other four shapes hold positions and a boolean only.
- `attempts` has one index on the student: the partial unique `ux_attempts_tenant_student_edition_in_progress` (in-progress rows only). Nothing serves a lookup of all attempts of a student (F-166 left the index to this item "if needed").
- The erasure (F-10, F-59): `EraseAccountHandler` stages the job `account.erased` in its transaction; `AccountErasedJobHandler` (`Identity.Infrastructure/Account`) revokes the sessions and publishes `UserErased(UserId, ErasedAt)` through the in-process publisher, at least once, retried by the F-13 policy (5 attempts, waits of 1, 2, 4, 8 minutes). The publisher is synchronous in the caller's scope: a consumer failure fails the job attempt. `UserErased` has no consumer in `src/` (`IIntegrationEventConsumer<UserErased>` appears only in a test helper of Identity).
- The export (F-16, F-32): `ExportDataHandler` (`Identity.Application/Account`) checks the password and the lockout, then builds `DataExportResponse(Format, Version, ExportedAt, Identity)` from `IDataExportQueries.GetIdentityDataAsync`; the route is `POST /api/v1/identity/data-exports`. Nothing lets another module add a section: F-16 decided "no cross-module abstraction yet, add structure on the second use" (decision 2026-09-21). This is the second use.
- `ExamEngine` already references `Simulab.Identity.Contracts` (`IdentityRoles`, in `ExamEngineModule`); `Identity.Contracts` references only `SharedKernel`. A module that implements a port defined in another module's `Contracts` is already the pattern (`ITopicUsageCheck` of Catalog, implemented by QuestionBank).
- The texts: `Account.Data.Explanation` (card "Your data" of `/account`) lists what the file holds and does not name attempts; `Account.Erase.Explanation` says "we keep only records that no longer identify you", which stays true.
- Privacy documents: the privacy policy `2026-v3` (pt-BR lines 3, 8 and 14) already says the answers in the simulations are kept and become unnamed on erasure; en and pt-PT carry the same text, so no new policy version is needed. `docs/privacy/data-processing-register.md` has no row for exam attempts; F-166 introduced the category without one.
- Rules that apply: modules talk only through `Contracts`; an entity rule lives in the entity; `TenantEntity` rows with query filters (`docs/agile/profile.md`, `.claude/rules/agile/project.md`).

## Users and use cases
- UC1 A student erases their own account (F-10); the free texts of every answer in their attempts are erased, and nothing else about the attempts changes.
- UC2 A student downloads their data (F-16); the file gains a section with every attempt they started and every answer they gave.
- UC3 An `Admin` holding `attempts.read` opens an attempt of an erased student and sees the questions and the answers with the texts empty.

## Business rules
- BR1 When `UserErased` arrives, the ExamEngine erases the free texts of every attempt whose `StudentId` is the erased user, whatever the attempt's status. The text of a `ShortAnswer`, an `OpenAnswer`, an `Essay` and of each blank of a `FillInTheBlanks` answer becomes empty (`null`). Everything else stays as it was: the answer's type, the blank numbers, the answers of the other four shapes (`SingleChoice`, `TrueFalse`, `MultipleAnswer`, `Matching`), the student id (the pseudonym, ADR-0001 #9), the edition, the status, the times, the question ids, paper numbers and version numbers, and the audit fields of the attempt. An answer row whose text is erased carries the erasure's update stamp (`updated_at`, `updated_by`, which is null for this system action); its creation fields are unchanged, and an answer row with nothing to erase is not written.
- BR2 This is a system action, not a student action: it is allowed on a submitted attempt, and F-166 BR6 ("a submitted attempt never changes") keeps binding every route a student or an `Admin` can call.
- BR3 An attempt in progress of an erased student is erased the same way and stays in progress: no submission time is invented and no state is added.
- BR4 The erasure of the texts is atomic and repeatable. All the attempts of the student are changed in one transaction, so a failure leaves none changed and the retry of the job (F-59 BR5) starts from the same point. A second run, or an event delivered twice, changes nothing and succeeds; a student with no attempt succeeds too (the consumer must accept the same event twice, as F-59 BR4 wrote for `UserErased`).
- BR5 The erasure never touches an attempt of another student. The lookup is by `StudentId` alone.
- BR6 The attempts of a student are found by an index on `StudentId`. A job that gives up stays as a `Failed` row with an error log, as every job does (F-59 BR5, confirmed 2026-10-26); there is no alert and no sweep in this item.
- BR7 The export gains a top-level section for the module, `examEngine`, beside `identity` (F-16 BR3: one section per module, named after it). It holds `attempts`: every attempt of the requesting user, in progress and submitted, oldest first by start time then id. Each has its id, its edition id, its status, `startedAt`, `submittedAt` (null while in progress) and its `questions` in paper-number order, each with the question id, the paper number, the version number, the type and the `answer` exactly as saved (the polymorphic shape with its `type` member; `null` while blank). It holds no statement, option text, answer key, model answer, explanation or tenant id.
- BR8 A user with no attempt gets `examEngine.attempts` as an empty list, not a missing or null member. The file's `format` and `version` do not change (additive, rule `api-contracts`), and the `identity` section is as it is.
- BR9 The section is built only from the id of the requesting user, the token subject (F-16 BR1). A module adds its section through a port declared in `Simulab.Identity.Contracts`; `ExportDataHandler` asks every registered section for its content and puts each under the name the section gives. A module that is not registered adds nothing.
- BR10 A section that cannot be built fails the whole export: the answer is a server error problem with no detail of the cause, no file is returned and no notice email is sent; the failure is logged. An incomplete portability file is worse than none. The password check, the failure count and the lockout are unchanged (F-16 BR2).
- BR11 `Account.Data.Explanation` names the study attempts and answers among what the file contains, in pt-BR, pt-PT and en (as F-32 did for the security events).
- BR12 `docs/privacy/data-processing-register.md` gets the row "Exam attempts" (what is held, purpose, legal basis, storage and region, retention, and how the person exercises their rights: the export and the erasure of this item).

## Screens and API
No new screen and no new route.
- `POST /api/v1/identity/data-exports` (F-16): same request and answers (200, 422, 423, 401); the 200 body gains the top-level member `examEngine`. Additive inside version 1.
- Contracts (the build's design pass decides the exact shape): in `Simulab.Identity.Contracts`, a port for a module's export section (its name and a method that returns the section for a user id); in `Simulab.ExamEngine.Contracts`, the response records of the section (attempt, question). `ExamEngine.Infrastructure` registers its implementation, and `UserErased` is consumed through `AddIntegrationEventConsumer<UserErased, ...>` in `ExamEngineModule`.
- Data: one additive migration of `ExamEngine`, an index on `exam_engine.attempts (student_id)`.
- Text: the key `Account.Data.Explanation` changes value in the three resource files (no new key).

## Security
### Surfaces
- S1 `POST /api/v1/identity/data-exports` — a signed-in user with the current password; the route is F-16's, the surface that grows is the file it returns.
- S2 The consumer of `UserErased` — called by the job worker through the in-process publisher; no HTTP surface and no caller outside the process.
### Threats
- T1 (S1, I) The file of student A holds an attempt, answer or id of student B → AC2 [sec]
- T2 (S1, I) The section leaks an answer key, a model answer, an explanation or a tenant id → AC3 [sec]
- T3 (S1, I) A failing section hides its cause in the answer, or the export returns a file without the section the person is owed → AC5 [sec]
- T4 (S2, T) The erasure of student A changes the attempts of student B → AC8 [sec]
- T5 (S2, I) A failure half-way leaves the texts of some attempts of an erased student in the database as if the erasure had finished → AC10 [sec]
### Personal data
| Datum | Lives in | Read by | Kept for | Legal basis |
| --- | --- | --- | --- | --- |
| The student's id on an attempt; the text of each free answer, essays included; the times | `exam_engine.attempts` and `exam_engine.attempt_answers` | The owner student (own attempts, and the export); an `Admin` holding `attempts.read`; later the grader (F-151) | While the account exists; after an erasure the texts are erased and the rest stays with no person attached (ADR-0001 #9) | Performance of the service the student asked for (LGPD art. 7, V; GDPR art. 6(1)(b)); the export answers LGPD art. 18 and GDPR art. 20 |

Struck threat: a student with very many attempts makes the file large (S1, D). F-16 BR6 already accepted a file with no limit because the password and the lockout protect the route and an attempt is a few kilobytes; recorded in `## Decisions`, no requirement.

## Docs
- flow: update docs/flows/exam-attempt.md — add the erasure (the consumer of `UserErased` and what it clears) and the export section, as sequences.

## Acceptance criteria
- AC1 (UC2, BR7) Given a student with a submitted attempt that holds an answer of each of the eight types and an attempt in progress with a blank question, when they export their data with the right password, then the 200 file has `examEngine.attempts` with both attempts, oldest first, each with id, edition id, status, `startedAt`, `submittedAt` and its questions in paper-number order with question id, paper number, version number, type and the answer as saved (`null` for the blank one).
- AC2 [sec] (T1, BR9) Given attempts of students A and B, when A exports, then no attempt, question row, answer or id of B appears in the file, and the section source receives only the id of the caller.
- AC3 [sec] (T2, BR7) Given a question with an answer key, an explanation and a model answer, when the student exports, then `examEngine` holds none of them, no statement or option text and no `tenantId` member.
- AC4 (BR8) Given a student with no attempt, when they export, then `examEngine.attempts` is an empty list; and `format`, `version` and the whole `identity` section are as before.
- AC5 [sec] (T3, BR10) Given a registered export section that throws, when the user exports with the right password, then the answer is a server error problem without the exception text, no file and no notice email, and the failure is logged.
- AC6 (BR9) Given a host with no registered section, when the user exports, then the file has no `examEngine` member and nothing else differs from F-32's file.
- AC7 (UC1, BR1, BR2, BR3) Given a student with a submitted attempt and an attempt in progress, each with a text in a `ShortAnswer`, an `OpenAnswer`, an `Essay` and a `FillInTheBlanks` answer and with a `SingleChoice`, a `TrueFalse`, a `MultipleAnswer` and a `Matching` answer, when the account is erased and the jobs have run, then the four texts (each blank's text included) are `null` with their types and blank numbers kept, the four other answers are unchanged, the attempts' student id, edition, status, times, question ids, paper numbers, version numbers and audit fields are unchanged; each answer row whose text was erased has a new `updated_at` and its `created_at`/`created_by` unchanged, and every other answer row is unchanged; and the attempt in progress is still in progress.
- AC8 [sec] (T4, BR5) Given students A and B with attempts that hold texts, when A is erased, then every row of B's attempts is unchanged.
- AC9 (BR4) Given the erasure already applied, when `UserErased` is delivered again, then it succeeds and nothing changes; and given an erased student with no attempt, it succeeds too.
- AC10 [sec] (T5, BR4) Given three attempts of the erased student and a store that fails while the second is written, when the job attempt runs, then none of the three is changed, the job is due again after the backoff, and the next attempt erases the texts of all three.
- AC11 (UC3) Given an attempt of an erased student after the erasure, when an `Admin` holding `attempts.read` reads it, then the answer is 200 with the same questions and versions, the choice answers as saved and the texts `null`.
- AC12 (BR6) Given the migration, when it runs on an empty database and again, then `exam_engine.attempts` has the index on `student_id`, the migration is additive and the down removes only it.
- AC13 Given the architecture tests, when they run, then `ExamEngine` still references other modules only through their `Contracts` projects and `Identity.Contracts` references only `SharedKernel`.
- AC14 (BR11) Given the `/account` page, when the "Your data" card is read, then its explanation names the attempts and answers in pt-BR, pt-PT and en, and the missing-key test stays green.

## Criterion → test

Tests locked: fb380939072daaed68b803ad5c25fa7f6480993e (6 files); unlocks: 0

| Criterion | Test |
| --- | --- |
| AC1 | `AttemptDataExportTests.Export_HasEveryAttemptAndAnswerOfTheStudent_OldestFirst_InPaperOrder` |
| AC2 | `AttemptDataExportTests.Export_HoldsNothingOfAnotherStudent_AndTheSectionIsAskedOnlyForTheCaller` |
| AC3 | `AttemptDataExportTests.Export_SectionCarriesNoKeyExplanationModelAnswerStatementOrTenant` |
| AC4 | `AttemptDataExportTests.Export_StudentWithNoAttempt_HasAnEmptyList_AndTheRestOfTheFileAsBefore` |
| AC5 | `DataExportFailingSectionTests.Export_WhenASectionThrows_AnswersAServerErrorWithoutDetail_NoFile_NoNotice_AndLogsIt` |
| AC6 | `DataExportWithoutSectionsTests.Export_WithNoSection_HasOnlyTheMembersOfBefore` (a guard: green before the code too) |
| AC7 | `AttemptErasureTests.Erase_ClearsTheFourTextsOfEveryAttempt_AndKeepsEverythingElse`; `AttemptEraseTextsRuleTests.EraseTexts_ClearsEveryText_KeepsEveryOtherValue_OnAnyStatus`, `.Samples_CoverEveryShapeOfTheAnswer`; `AttemptErasureStampTests.Erase_StampsTheErasedRowsWithANewTimeAndNoUser_AndLeavesTheOthersStamps` (added after the lock, review finding 1) |
| AC8 | `AttemptErasureTests.Erase_LeavesEveryRowOfAnotherStudentAsItWas` |
| AC9 | `AttemptErasureTests.UserErased_DeliveredAgain_ChangesNothing_AndAStudentWithNoAttemptSucceeds`; `AttemptEraseTextsRuleTests.EraseTexts_Twice_TheSecondChangesNothing` |
| AC10 | `AttemptErasureTests.Erase_FailingHalfWay_ChangesNoAttempt_AndTheRetryErasesAllThree` |
| AC11 | `AttemptErasureTests.Read_ByAnAdmin_AfterTheErasure_ShowsTheQuestionsTheChoicesAndNoText` |
| AC12 | `AddAttemptsStudentIndexMigrationTests.Up_AddsTheStudentIndex_KeepsTheRows_SecondRunChangesNothing_AndDownDropsOnlyIt` |
| AC13 | `ModuleBoundaryTests.EveryLayer_ReachesAnotherModuleOnlyThroughItsContracts`, `.Contracts_ReferencesNoSolutionProjectBesidesSharedKernel`, `.Application_ReferencesItsOwnDomainAndContractsOnly` (existing) |
| AC14 | `DataExplanationResourcesTests.Explanation_NamesTheAttemptsAndAnswers`; `ResourceParityTests` (missing key, existing) |

## Decisions
- 2026-10-26 — The erasure keeps the user id as the pseudonym and keeps attempts without a person attached, as decided in ADR-0001 #9 and F-10. Still true here? Yes — confirmed: docs/decisions/ADR-0001-foundation.md
- 2026-10-26 — A job that gives up is handled like a failed email job (`Failed` row, error log, no alert), as decided in F-59. Still true here? Yes — confirmed: F-59
- 2026-10-26 — The export stays one JSON file, confirmed with the current password, no download limit, one top-level section per module, as decided in F-16 (BR3, BR6). Still true here? Yes — confirmed: F-16
- 2026-10-26 — On erasure only the free texts of the answers are erased; choice answers, times and versions stay for the statistics — asked (owner chose the recommended option)
- 2026-10-26 — The export carries what the student produced (attempts, questions with ids and versions, answers as saved, positions for choices), never the statement, options or key — asked (owner chose the recommended option)
- 2026-10-26 — The mechanism for a module's section is a port in `Simulab.Identity.Contracts`, implemented by the ExamEngine, as `ITopicUsageCheck` is by QuestionBank; `SharedKernel` stays free of it because the file is Identity's — Claude (technical). The F-16 decision "no abstraction on the first use" ends here: this is the second use.
- 2026-10-26 — The section travels as a raw JSON value under its name, so Identity needs no reference to a module's records; the exact type is the build's design pass — Claude (technical).
- 2026-10-26 — A section that fails fails the export (BR10) — Claude (technical): a portability request answered with a silently incomplete file is a legal defect; the person retries.
- 2026-10-26 — The ExamEngine consumes `UserErased` in `ExamEngine.Infrastructure`; the rule "erase the texts" lives in `Attempt` (a method that returns a `Result`, allowed on any status), driven by a port of `ExamEngine.Application` — Claude (technical): profile "Where business rules live"; the consumer is a delivery mechanism, not a rule.
- 2026-10-26 — An attempt in progress of an erased student stays in progress, with its texts erased — Claude (technical): a new state or an invented submission time would be a rule of the timer item, and the student cannot sign in again, so nothing continues it.
- 2026-10-26 — The anonymization must reach every attempt of the student whatever the tenant. Today every row is global (`TenantId` null, F-166 BR10) and the criteria use global rows; how the consumer reaches rows of a tenant waits for F-162, which wires the tenant claim, and the build's design pass records the trap — Claude (technical).
- 2026-10-26 — The privacy policy `2026-v3` is not versioned again: it already says answers are kept and become unnamed on erasure (read in the three locales); only the processing register gets a row — Claude (technical). The project rule on a change of personal-data category is met for what is true: F-166 introduced the category and the policy text already covered it.
- 2026-10-26 — The farewell email and `Account.Erase.Explanation` stay as they are: they already say that only records that no longer identify the person are kept — Claude (technical).
- 2026-10-26 — The app manual pages `my-account.md` ("Baixar meus dados") and `erase-account.md` get a line each in pt-BR at the ship; pt-PT and en are frozen (F-124) — Claude (technical).
- 2026-10-26 — No new package, for code or tests: EF Core, Npgsql, the test host and the job runner exist — Claude (technical).
- 2026-10-26 — Approved by the owner ("aprovo F-167").
- 2026-10-26 — Struck threat, a student with very many attempts makes the file large — Claude (technical): accepted by F-16 BR6; revisit with the "job plus emailed link" change F-16 already names when study history is large.
- 2026-10-27 — Design passes (system-design, architect; opus). Accepted: `IDataExportSection { Name; GetAsync(userId) → JsonElement }` in `Identity.Contracts`, asked after `GetIdentityDataAsync` and before the notice, logged and rethrown on failure, names that repeat or collide with `format`/`version`/`exportedAt`/`identity` refused; `DataExportResponse` carries the sections as extension data, or the endpoint composes a `JsonObject` if CA2227 fires (checked by building `Identity.Contracts` right after); `Attempt.EraseTexts()` (any status) with an internal `AttemptAnswer.EraseText()` that assigns only when a text is non-null; `EraseStudentTextsHandler(Guid)` in Application; `UserErasedConsumer` in Infrastructure; the lookup locks the student's rows `FOR UPDATE` in one transaction (`using`) and ignores only the soft-delete filter (architect finding 3, precedent `TopicStore.cs:89`), keeping the tenant filter; `[JsonIgnore(Condition = Never)]` on `SubmittedAt` and `Answer` of the export records so BR7's `null` holds under `AppJson`'s `WhenWritingNull` (architect finding 2); a 500 with no new error code (`UseExceptionHandler`, no new resource key); "Exam attempts" added to `RequiredCategories`; a classification test over every `JsonDerivedType` of `AnswerPayload`; AC13 checked at the step that adds the last cross-module reference (architect finding 5); no exception message carries answer text (it lands in `jobs.last_error`). Dropped: none. Stopped for the owner: the audit stamp on the erased answer rows against BR1 (architect finding 1) — Claude (technical), verified against the files.
- 2026-10-27 — Design coverage: architect checked simplicity, scope, contracts, data, rules, layout against the profile, `CLAUDE.md`, `.claude/rules/agile/`, the item file and the code it cites; not checked: none.
- 2026-10-27 — Traps — T1 ignoring the tenant filter to reach every row: opens a cross-tenant read in the export and settles F-162's question here (avoided; F-162 must revisit the erasure lookup); T2 one `ExecuteUpdate` or raw SQL over the `json` column: the rule leaves the entity and a ninth shape is missed in silence (avoided); T3 reusing `AttemptResponse` as the export section: a member added for the screen enters the legal file unasked (avoided, pending trade-off 6).
- 2026-10-27 — Design trade-off 6 Shape of the export records — owner chose as proposed (own `Exported*` records in `ExamEngine.Contracts`).
- 2026-10-27 — Audit stamp on the erased answer rows — owner chose the recommended change note on BR1/AC7 (attempt rows keep their audit fields; changed answer rows carry the erasure's update stamp); the build waits for `/agile:change F-167` before writing AC7's test.
- 2026-10-27: plan — 1 erasure path (index + migration, locked lookup by student, `Attempt.EraseTexts`, handler, `UserErasedConsumer`) → AC7, AC12, AC13 (module boundaries after the consumer); 2 erasure guarantees (other student, repeat, failure half-way, payload classification) → AC8, AC9, AC10; 3 admin read after erasure → AC11; 4 export port in Identity (`IDataExportSection`, sections in the file, name guard) → AC6, AC4 (identity half); 5 ExamEngine export section (`Exported*` records, read by student) → AC1, AC2, AC3, AC4, AC13; 6 failing section → AC5; 7 text, register row, flow page → AC14, `DataProcessingRegisterTests`, `techdocs.js check`.
- 2026-10-27 — AC6's test is a guard: it was green before the code too, since it proves that nothing else in the file changed — Claude (technical).
- 2026-10-27 — Review finding 1 (minor, confirmed): AC7's test only checked that `updated_at` was set, which every saved row already was; fixed with a new test file, `AttemptErasureStampTests`, comparing the stamp before and after and requiring a null `updated_by`; the lock stays intact — owner chose "Teste novo".
- 2026-10-27 — Review coverage: correctness, security, data, contracts, localization, locks, simplicity, rules, none with a finding; read: the 30 files of the diff from `424606b`, the item file, `.claude/rules/agile/api-contracts.md` and `project.md`; the reviewer ran no test.
- 2026-10-27 — Traps — T1 the export reads through the soft-delete filter while the erasure ignores it: once a route soft-deletes an attempt, it leaves the portability file but keeps its texts until the erasure (no delete route today); T2 the export loads whole graphs and serializes twice, growing with the study history (accepted by F-16 BR6); T3 the erasure keeps the tenant filter, so after F-162 tenant rows would be locked but not erased while the job succeeds (deferred to F-162, design trap T1).
- 2026-10-27 — Reuse: the lock-then-load pattern of `AttemptStore.LockAndLoadAsync`, `IgnoreQueryFilters([SoftDeleteFilter])` as in `TopicStore.cs:89`, the in-process `AddIntegrationEventConsumer`, the port-in-Contracts pattern of `ITopicUsageCheck`, `RecordingLoggerProvider` and the shared Api test host.
- 2026-10-27 — Shortcuts: none.

## Out of scope
- Reading or exporting an attempt on behalf of another person (an `Admin` exporting a student's data on request).
- Scores, results, the grader's queue, grades (F-151, F-152) and the grader's feedback text: the item that stores them adds its own erasure and its own export section.
- Alerts or a sweep for a job that gives up (F-59 decision).
- Deleting attempts, or any change of what the student sees while the account exists.
- Listing a student's attempts, and the simulator screen.
- A new version of the privacy policy.

## Open questions
- (none)

## Change notes
### v2 — 2026-10-27
- What: BR1 and AC7 keep the attempt's audit fields unchanged, and an answer row whose text is erased carries the erasure's update stamp (`updated_at`, `updated_by` null); creation fields unchanged; a row with nothing to erase is not written.
- Why: the audit interceptor (`AuditAndSoftDeleteInterceptor.cs:61-63`) stamps every modified row, so "the audit fields stay" could not hold without skipping it (trap T2); an update with no trail is worse than the stamp (design pass, architect finding 1).
- Affected: BR1, AC7.
- Re-approved by the owner on 2026-10-27 ("aprovo").

## Validation script
The screen part is the "Your data" card of `/account` and the file it downloads; the erasure is checked by two scripts that print one line per check (`docs/validation/F-167/validate.sh` for Git Bash, `validate.ps1` for PowerShell 7). Both were run by Claude against the app host of this worktree on 2026-10-27 and ended in `ALL OK`.

### English
Needed to validate: the app host running from this worktree on its own database `simulab_f167` (you start it in step 1), one `Student` account with a verified email in that database (step 2 creates one; the password is yours; the account is ERASED in step 5, so use one you do not need) and the seeded `admin@simulab.local` (its development password is in `src/Hosts/Simulab.Api/appsettings.Development.json`). The script makes its own published edition. All in place except the Student, which you create.

1. [ ] Stop any app host you have open. In a terminal at the root of this worktree (`D:\wt\simulab\f-167-erase-export-attempt`), start the app host on the worktree's own database and leave it open; wait until `https://localhost:7287/api/v1/system/info` answers 200 → the Aspire dashboard (the page that lists the app's parts and their logs) address is printed. The item's migration lands only in `simulab_f167`, never in the shared `simulab`.
   ```bash
   Database__Name=simulab_f167 dotnet run --project src/Hosts/Simulab.AppHost
   ```
   ```powershell
   $env:Database__Name = "simulab_f167"; dotnet run --project src/Hosts/Simulab.AppHost
   ```
2. Create the Student of this check.
   - [ ] 2.1 Open `https://localhost:7125/sign-up`, fill in an email, a password and the name, tick "I am 18 or older", the terms and the privacy policy, and submit → the page says the account was created and a confirmation email was sent.
   - [ ] 2.2 Open Mailpit (the local mailbox; its address is the `mailpit` entry in the Aspire dashboard resources list), open the message to that email and click the link inside → the page `verify-email` says the email is confirmed.
3. The "Your data" card, with the keyboard only (no mouse in this step).
   - [ ] 3.1 Sign in at `https://localhost:7125` with the Student, open the user menu in the app bar (the strip at the top of every page, with the menu, the brand and the theme, language and user buttons) and choose "Minha conta" ("My account" in English) → the `/account` page opens; the card "Seus dados" says the file holds "todas as tentativas de simulado que você iniciou, com as respostas que deu" (AC14).
   - [ ] 3.2 Press the language switch (the globe in the app bar) and choose English → the card "Your data" says "all the study attempts you started, with the answers you gave"; choose Português (Portugal) → "todas as tentativas de simulado que iniciou, com as respostas que deu" (AC14). Switch back to Português (Brasil).
   - [ ] 3.3 With Tab only, reach "Baixar meus dados" and press Enter, type the password, press Enter → a file `simulab-my-data-<date>.json` downloads.
   - [ ] 3.4 Open the file → the last member is `"examEngine": { "attempts": [] }` after `identity`, and `format` and `version` are `simulab.data-export` and `1` as before (AC4).
4. In a second terminal at the same folder, set the three variables (your Student's email and password) and run the script → the last line is `ALL OK`. It makes an edition, answers two attempts as the Student, downloads the data, ERASES the account and reads the attempts as the Admin.
   ```bash
   export ADMIN_PASSWORD='<admin password>' STUDENT_EMAIL='<student email>' STUDENT_PASSWORD='<student password>'
   bash docs/validation/F-167/validate.sh
   ```
   ```powershell
   $env:ADMIN_PASSWORD='<admin password>'; $env:STUDENT_EMAIL='<student email>'; $env:STUDENT_PASSWORD='<student password>'
   pwsh -NoProfile -File docs/validation/F-167/validate.ps1
   ```
   Expected output (ids, times and the mark differ on each run):
   ```text
   == Step 3: a published edition with a short answer, a fill-in-the-blanks and a single choice question
   OK   edition <id> published with 3 questions
   == Step 4: two attempts with texts
   OK   start the first attempt -> 201
   OK   save a short answer -> 200
   OK   save the blanks -> 200
   OK   save a single choice -> 200
   OK   submit the first attempt -> 200
   OK   start the second attempt -> 201
   OK   save a short answer in the second -> 200
   == Step 5: download my data
   OK   export with the right password -> 200
        members: format version exportedAt identity examEngine
   OK   the two attempts, oldest first
        second attempt: status InProgress, submittedAt null
        first attempt answers: [{"type":"ShortAnswer","text":"TEXT-<mark>-SHORT"},{"type":"FillInTheBlanks","blanks":[{"number":1,"text":"TEXT-<mark>-B1"},{"number":2,"text":"TEXT-<mark>-B2"}]},{"type":"SingleChoice","optionPosition":1}]
   OK   no statement, explanation or option text in the section
   == Step 6: erase the account and wait for the job
   OK   erase the account -> 204
   OK   the job erased the texts (waited <n> s at most)
   == Step 7: an Admin reads the attempt after the erasure
   OK   admin reads the first attempt -> 200
        status Submitted, answers: [{"type":"ShortAnswer"},{"type":"FillInTheBlanks","blanks":[{"number":1},{"number":2}]},{"type":"SingleChoice","optionPosition":1}]
   OK   admin reads the second attempt -> 200
        status InProgress, answers: [{"type":"ShortAnswer"},null,null]
   OK   the erased student's old token -> 401
   ALL OK
   ```
5. Read the output of step 4 against the rules:
   - [ ] 5.1 Export: the file has `examEngine` after `identity`, both attempts oldest first, the one in progress with `submittedAt null`, the answers exactly as saved, and no statement, explanation or option text → AC1, AC3.
   - [ ] 5.2 Erasure: after the job ran, the short answer and each blank have no text, the blank numbers and the single choice are as saved, the first attempt is still `Submitted` and the second still `InProgress` → AC7, AC11.
   - [ ] 5.3 Access: the Admin (who holds `attempts.read`) reads both attempts with 200, and the erased Student's old token is refused with 401 → AC11.
6. [ ] Sign in again at `https://localhost:7125` with the Student of step 2 → the sign-in is refused: the account no longer exists. Stop the app host (Ctrl+C in the first terminal) when you finish.

### pt-BR
Necessário para validar: o app host rodando a partir deste worktree no banco próprio `simulab_f167` (você o inicia no passo 1), uma conta `Student` com e-mail verificado nesse banco (o passo 2 cria uma; a senha é sua; a conta é APAGADA no passo 5, então use uma de que não precise) e o `admin@simulab.local` do seed (a senha de desenvolvimento está em `src/Hosts/Simulab.Api/appsettings.Development.json`). O script cria a própria edição publicada. Tudo no lugar, exceto o Student, que você cria.

1. [ ] Pare qualquer app host que esteja aberto. Em um terminal na raiz deste worktree (`D:\wt\simulab\f-167-erase-export-attempt`), inicie o app host no banco próprio do worktree e deixe aberto; espere `https://localhost:7287/api/v1/system/info` responder 200 → o endereço do dashboard do Aspire (a página que lista as partes do app e seus logs) aparece impresso. A migração do item cai só em `simulab_f167`, nunca no `simulab` compartilhado.
   ```bash
   Database__Name=simulab_f167 dotnet run --project src/Hosts/Simulab.AppHost
   ```
   ```powershell
   $env:Database__Name = "simulab_f167"; dotnet run --project src/Hosts/Simulab.AppHost
   ```
2. Crie o Student desta verificação.
   - [ ] 2.1 Abra `https://localhost:7125/sign-up`, preencha e-mail, senha e nome, marque "tenho 18 anos ou mais", os termos e a política de privacidade, e envie → a página diz que a conta foi criada e que um e-mail de confirmação foi enviado.
   - [ ] 2.2 Abra o Mailpit (a caixa de e-mail local; o endereço é a entrada `mailpit` na lista de recursos do dashboard do Aspire), abra a mensagem desse e-mail e clique no link dentro dela → a página `verify-email` diz que o e-mail foi confirmado.
3. O cartão "Seus dados", só com o teclado (nada de mouse neste passo).
   - [ ] 3.1 Entre em `https://localhost:7125` com o Student, abra o menu do usuário na barra superior (a faixa no alto de todas as páginas, com o menu, a marca e os botões de tema, idioma e usuário) e escolha "Minha conta" ("My account" em inglês) → a página `/account` abre; o cartão "Seus dados" diz que o arquivo traz "todas as tentativas de simulado que você iniciou, com as respostas que deu" (AC14).
   - [ ] 3.2 Aperte o seletor de idioma (o globo na barra superior) e escolha English → o cartão "Your data" diz "all the study attempts you started, with the answers you gave"; escolha Português (Portugal) → "todas as tentativas de simulado que iniciou, com as respostas que deu" (AC14). Volte para Português (Brasil).
   - [ ] 3.3 Só com Tab, chegue em "Baixar meus dados" e aperte Enter, digite a senha, aperte Enter → um arquivo `simulab-my-data-<data>.json` é baixado.
   - [ ] 3.4 Abra o arquivo → o último membro é `"examEngine": { "attempts": [] }` depois de `identity`, e `format` e `version` são `simulab.data-export` e `1` como antes (AC4).
4. Em um segundo terminal, na mesma pasta, defina as três variáveis (e-mail e senha do seu Student) e rode o script → a última linha é `ALL OK`. Ele cria uma edição, responde duas tentativas como o Student, baixa os dados, APAGA a conta e lê as tentativas como o Admin.
   ```bash
   export ADMIN_PASSWORD='<admin password>' STUDENT_EMAIL='<student email>' STUDENT_PASSWORD='<student password>'
   bash docs/validation/F-167/validate.sh
   ```
   ```powershell
   $env:ADMIN_PASSWORD='<admin password>'; $env:STUDENT_EMAIL='<student email>'; $env:STUDENT_PASSWORD='<student password>'
   pwsh -NoProfile -File docs/validation/F-167/validate.ps1
   ```
   Saída esperada (ids, horários e a marca mudam a cada execução):
   ```text
   == Step 3: a published edition with a short answer, a fill-in-the-blanks and a single choice question
   OK   edition <id> published with 3 questions
   == Step 4: two attempts with texts
   OK   start the first attempt -> 201
   OK   save a short answer -> 200
   OK   save the blanks -> 200
   OK   save a single choice -> 200
   OK   submit the first attempt -> 200
   OK   start the second attempt -> 201
   OK   save a short answer in the second -> 200
   == Step 5: download my data
   OK   export with the right password -> 200
        members: format version exportedAt identity examEngine
   OK   the two attempts, oldest first
        second attempt: status InProgress, submittedAt null
        first attempt answers: [{"type":"ShortAnswer","text":"TEXT-<mark>-SHORT"},{"type":"FillInTheBlanks","blanks":[{"number":1,"text":"TEXT-<mark>-B1"},{"number":2,"text":"TEXT-<mark>-B2"}]},{"type":"SingleChoice","optionPosition":1}]
   OK   no statement, explanation or option text in the section
   == Step 6: erase the account and wait for the job
   OK   erase the account -> 204
   OK   the job erased the texts (waited <n> s at most)
   == Step 7: an Admin reads the attempt after the erasure
   OK   admin reads the first attempt -> 200
        status Submitted, answers: [{"type":"ShortAnswer"},{"type":"FillInTheBlanks","blanks":[{"number":1},{"number":2}]},{"type":"SingleChoice","optionPosition":1}]
   OK   admin reads the second attempt -> 200
        status InProgress, answers: [{"type":"ShortAnswer"},null,null]
   OK   the erased student's old token -> 401
   ALL OK
   ```
5. Leia a saída do passo 4 contra as regras:
   - [ ] 5.1 Exportação: o arquivo tem `examEngine` depois de `identity`, as duas tentativas da mais antiga para a mais nova, a em andamento com `submittedAt null`, as respostas exatamente como salvas, e nenhum enunciado, explicação ou texto de alternativa → AC1, AC3.
   - [ ] 5.2 Apagamento: depois que o job rodou, a resposta curta e cada lacuna estão sem texto, os números das lacunas e a escolha única estão como salvos, a primeira tentativa continua `Submitted` e a segunda continua `InProgress` → AC7, AC11.
   - [ ] 5.3 Acesso: o Admin (que tem `attempts.read`) lê as duas tentativas com 200, e o token antigo do Student apagado é recusado com 401 → AC11.
6. [ ] Entre de novo em `https://localhost:7125` com o Student do passo 2 → a entrada é recusada: a conta não existe mais. Pare o app host (Ctrl+C no primeiro terminal) quando terminar.

## Events

- 2026-10-26 question — asked 2, from context 3
- 2026-10-27 change — v2: BR1 and AC7 keep the attempt audit fields; erased answer rows carry the erasure update stamp
- 2026-10-27 rework — review 1: AC7 test did not prove the erasure stamp; new test file AttemptErasureStampTests

## Delivery
- Branch: feature/F-167 (removed; never pushed)
- Merge: 2f9670a (board #233), app version 1.24.0
- Tests: full suite 5463 tests in 15 projects, 0 failures, 36 s build and 388 s test run (the ship's last run, after bringing main in); over the 5 min budget by 88 s of test time (F-177)
- Manual pages: docs/manual/pt-BR/my-account.md, docs/manual/pt-BR/erase-account.md (pt-PT and en frozen, F-124)
- Validated by the owner on 2026-10-27 ("validado F-167"); both validation scripts ran green in the build (ALL OK in Git Bash and PowerShell 7)
- Validation evidence: 0 of 11 boxes ticked in the widget; every path recorded as not tested

Gate time: 7 passes, 31 min 35 s — turn 2 (1 min 48 s), ship 3 (21 min 26 s), scan 2 (8 min 21 s); 0 red
Usage: 9588.7k weighted (estimate) — build 19% · review 34% · ship 13% · other 34% — agents 12% — context per turn 161k → 469k — 1 session, 200 turns
