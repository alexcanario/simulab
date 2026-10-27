---
bug: B-35
feature: F-166
status: refining
board: 243
severity: low
epic: Exam Simulator
version: 1
---
# Starting an attempt without an edition answers with a 404 code

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: refining (2026-10-27)
- Stopped at: Refinement started; cause verified on main (2026-10-27)
- Next action: Owner answers the question round
- Waiting on: nothing

## What happens
`POST` that starts an attempt, with a body that has no `examEditionId`, answers 400 with the code `exam_attempt.edition_not_found` and no `errors` entry. The same code means 404 (an edition that does not exist or is not published) in AC4 of F-166, so a client that branches on `code` cannot tell a malformed body from an unknown edition. `.claude/rules/agile/api-contracts.md` asks a 400 validation for one entry per field, each with a code.

Evidence (review of F-166, 2026-10-26): `src/Modules/ExamEngine/Simulab.ExamEngine.Application/Attempts/StartAttemptHandler.cs:22-25` on the branch `feature/F-166`, read by the reviewer and by the session.

## Start
- Depends on: F-166 (delivered, merged on the main branch).
- Waits on (to start): nothing.
- Needed to validate: nothing.
- Suggested path: `/agile:refine` → `/agile:build` (or `/agile:autopilot B-35`, cause clear, fix small).
- Parallel with: none that edits the ExamEngine start handler.

## Expected
A start with no `examEditionId` answers 400 with one `errors` entry (`field: examEditionId`, a validation code of its own, with texts in en, pt-BR and pt-PT). `exam_attempt.edition_not_found` stays the 404 of AC4.

## Cause
Verified on the main branch (F-166 merged, `1e83f94`), 2026-10-27.
- Cause: `src/Modules/ExamEngine/Simulab.ExamEngine.Application/Attempts/StartAttemptHandler.cs:22-25` returns `new Error(ExamAttemptErrorCodes.EditionNotFound, ErrorKind.Validation)` when `ExamEditionId` is null. `ApiProblem.StatusFor` (`src/BuildingBlocks/Simulab.ApiResults/ApiProblem.cs`) turns `Validation` into 400, so the body is `400` with `code: exam_attempt.edition_not_found` and no `errors` member. The endpoint (`src/Modules/ExamEngine/Simulab.ExamEngine.Api/AttemptEndpoints.cs` `StartAsync`) passes the error to `Problem` without extensions; only `SaveAsync` writes `errors`.
- The same code is the 404 of F-166 AC4 (`StartAttemptHandler.cs:33-36`, `ErrorKind.NotFound`).
- The current test `Start_WithNoEdition_Answers400` (`tests/Modules/ExamEngine/Simulab.ExamEngine.Tests/StartAttemptEndpointTests.cs:184-193`, F-166 AC14) checks only the status, so it stays green with the reused code.
- Duplicates: no duplicate found. The one other `*_not_found` code with `ErrorKind.Validation`, `CatalogErrorCodes.NoticeSubjectMappingTargetNotFound` (`src/Modules/Catalog/Simulab.Catalog.Application/NoticeSubjects/SaveNoticeSubjectHandler.cs:128`), is a code of its own, used only as that 400 and branched on by `NoticeSubjectDialog.razor:465`; it reuses no 404 code.
- Callers: `StartAttemptHandler.HandleAsync` has one caller, `AttemptEndpoints.StartAsync` — not affected by the order of checks. The code `exam_attempt.edition_not_found` is read by no Web page on the main branch; F-169 (refining, `D:/wt/simulab/f-169-take-exam-edition`) will call this start from the edition page (its BR1) and only ever sends an id.

## Fix
To be written at `/agile:refine`.

## Docs
- none — to be settled at `/agile:refine`.

## Regression test
- To be written at `/agile:refine`.

## Decisions
- 2026-10-26 — Captured as a bug, severity low, from the F-166 review (finding 2); the owner asked for it to be registered — asked

## Open questions
- (none)

## Validation script
### English
1. To be written at `/agile:refine`.

### pt-BR
1. A ser escrito em `/agile:refine`.

## Events

## Delivery
- Branch: bug/B-35
- Merge: -
