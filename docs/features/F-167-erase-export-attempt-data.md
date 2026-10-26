---
feature: F-167
epic: Exam Simulator
status: approved
board: 233
version: 1
---
# Erase and export a student's attempt data

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: approved (2026-10-26)
- Stopped at: Approved by the owner (2026-10-26).
- Next action: /agile:build F-167 (owner).
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
- BR1 When `UserErased` arrives, the ExamEngine erases the free texts of every attempt whose `StudentId` is the erased user, whatever the attempt's status. The text of a `ShortAnswer`, an `OpenAnswer`, an `Essay` and of each blank of a `FillInTheBlanks` answer becomes empty (`null`). Everything else stays as it was: the answer's type, the blank numbers, the answers of the other four shapes (`SingleChoice`, `TrueFalse`, `MultipleAnswer`, `Matching`), the student id (the pseudonym, ADR-0001 #9), the edition, the status, the times, the question ids, paper numbers and version numbers, and the audit fields.
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
- AC7 (UC1, BR1, BR2, BR3) Given a student with a submitted attempt and an attempt in progress, each with a text in a `ShortAnswer`, an `OpenAnswer`, an `Essay` and a `FillInTheBlanks` answer and with a `SingleChoice`, a `TrueFalse`, a `MultipleAnswer` and a `Matching` answer, when the account is erased and the jobs have run, then the four texts (each blank's text included) are `null` with their types and blank numbers kept, the four other answers are unchanged, the attempts' student id, edition, status, times, question ids, paper numbers, version numbers and audit fields are unchanged, and the attempt in progress is still in progress.
- AC8 [sec] (T4, BR5) Given students A and B with attempts that hold texts, when A is erased, then every row of B's attempts is unchanged.
- AC9 (BR4) Given the erasure already applied, when `UserErased` is delivered again, then it succeeds and nothing changes; and given an erased student with no attempt, it succeeds too.
- AC10 [sec] (T5, BR4) Given three attempts of the erased student and a store that fails while the second is written, when the job attempt runs, then none of the three is changed, the job is due again after the backoff, and the next attempt erases the texts of all three.
- AC11 (UC3) Given an attempt of an erased student after the erasure, when an `Admin` holding `attempts.read` reads it, then the answer is 200 with the same questions and versions, the choice answers as saved and the texts `null`.
- AC12 (BR6) Given the migration, when it runs on an empty database and again, then `exam_engine.attempts` has the index on `student_id`, the migration is additive and the down removes only it.
- AC13 Given the architecture tests, when they run, then `ExamEngine` still references other modules only through their `Contracts` projects and `Identity.Contracts` references only `SharedKernel`.
- AC14 (BR11) Given the `/account` page, when the "Your data" card is read, then its explanation names the attempts and answers in pt-BR, pt-PT and en, and the missing-key test stays green.

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

## Events

- 2026-10-26 question — asked 2, from context 3
