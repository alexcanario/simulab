---
bug: B-36
feature: F-166
status: refining
board: 244
severity: low
epic: Exam Simulator
version: 1
---
# An oversized answer body answers a bare 413

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: refining (2026-10-27)
- Stopped at: Refinement started; cause reproduced on main (AttemptEndpoints.cs:104).
- Next action: Owner answers the question round.
- Waiting on: nothing

## What happens
Saving an answer with a body above `AttemptLimits.SaveBodyMaxBytes` answers a bare `Results.StatusCode(413)`: no problem details and no stable `code`. `.claude/rules/agile/api-contracts.md` says errors are RFC 9457 problem details plus a stable code, with its texts in en, pt-BR and pt-PT, so the UI cannot localize this error.

Evidence (review of F-166, 2026-10-26): `src/Modules/ExamEngine/Simulab.ExamEngine.Api/AttemptEndpoints.cs:104` on the branch `feature/F-166`, read by the reviewer and by the session.

## Start
- Depends on: F-166 (the code is on `feature/F-166`, not on the main branch yet).
- Waits on (to start): the merge of F-166.
- Needed to validate: nothing.
- Suggested path: `/agile:refine` → `/agile:build` (or `/agile:autopilot B-36`, cause clear, fix small).
- Parallel with: B-35 (both touch the ExamEngine error codes and their texts: build one after the other).

## Expected
An oversized save body answers 413 as problem details with a stable code (a new one, or `exam_attempt.answer_invalid` with the field `text_too_long`), and the texts of the code exist in en, pt-BR and pt-PT.

## Cause
Verified on the main branch (`bacc7d5`, F-166 and B-35 merged), 2026-10-27.

- `src/Modules/ExamEngine/Simulab.ExamEngine.Api/AttemptEndpoints.cs:101-105`: `SaveAsync` calls `ReadBodyAsync` and, when it returns null, answers `Results.StatusCode(StatusCodes.Status413PayloadTooLarge)`, an empty 413 with no problem details and no `code`. Every other failure of the route goes through `ApiProblem.Problem` (`src/BuildingBlocks/Simulab.ApiResults/ApiProblem.cs:25`).
- `AttemptEndpoints.cs:157-178`: `ReadBodyAsync` returns null in two places: a `Content-Length` above `AttemptLimits.SaveBodyMaxBytes` (1,500,000, `Simulab.ExamEngine.Contracts/AttemptLimits.cs:13`), and a chunked body that grows past it while read.
- `ExamAttemptErrorCodes` (`Simulab.ExamEngine.Contracts/ExamAttemptErrorCodes.cs`) has no code for an oversized body, so the UI has no resource key for it.
- The test of the limit (`tests/Modules/ExamEngine/Simulab.ExamEngine.Tests/SaveAnswerEndpointTests.cs:221-231`, AC17 of F-166) checks only a 4xx status and that nothing is stored: it does not fail today.

Duplicates (the same rule, "a body above the limit of a route answers a problem details with a code"):
- `src/Modules/QuestionBank/Simulab.QuestionBank.Api/QuestionImageEndpoints.cs:63-66`: already right, answers 400 `QuestionImageTooLarge`.
- `src/Modules/Catalog/Simulab.Catalog.Api/NoticeDocumentEndpoints.cs:64`: already right, answers 400 `ExamEditionNoticeDocumentTooLarge` (asserted at `tests/Modules/Catalog/Simulab.Catalog.Tests/NoticeDocumentEndpointTests.cs:207-211`).
- No other route of the solution answers a bare `Status413PayloadTooLarge` (grep of `src`). Kestrel's own server-wide limit (30 MB default, not changed in `src`) still answers a bare 413 on any route; that is outside the save route and out of scope.

Callers: `ReadBodyAsync` is private and called only by `SaveAsync` (`AttemptEndpoints.cs:101`); no shared function changes.

## Fix
To be written at `/agile:refine`.

## Docs
- none — to be settled at `/agile:refine`.

## Regression test
- To be written at `/agile:refine`.

## Decisions
- 2026-10-26 — Captured as a bug, severity low, from the F-166 review (finding 3); the owner asked for it to be registered — asked

## Open questions
- (none)

## Validation script
### English
1. To be written at `/agile:refine`.

### pt-BR
1. A ser escrito em `/agile:refine`.

## Events

## Delivery
- Branch: bug/B-36
- Merge: -
