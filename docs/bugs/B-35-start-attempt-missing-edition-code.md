---
bug: B-35
feature: F-166
status: approved
board: 243
severity: low
epic: Exam Simulator
version: 1
---
# Starting an attempt without an edition answers with a 404 code

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: approved (2026-10-27)
- Stopped at: Approved by the owner (2026-10-27)
- Next action: /agile:build B-35
- Waiting on: nothing

## What happens
`POST` that starts an attempt, with a body that has no `examEditionId`, answers 400 with the code `exam_attempt.edition_not_found` and no `errors` entry. The same code means 404 (an edition that does not exist or is not published) in AC4 of F-166, so a client that branches on `code` cannot tell a malformed body from an unknown edition. `.claude/rules/agile/api-contracts.md` asks a 400 validation for one entry per field, each with a code.

Evidence (review of F-166, 2026-10-26): `src/Modules/ExamEngine/Simulab.ExamEngine.Application/Attempts/StartAttemptHandler.cs:22-25` on the branch `feature/F-166`, read by the reviewer and by the session.

## Start
- Depends on: F-166 (delivered, merged on the main branch).
- Waits on (to start): nothing.
- Needed to validate: nothing.
- Model: opus (changes the error answer of a public API route)
- Suggested path: `/agile:refine` → `/agile:build` (or `/agile:autopilot B-35`, cause clear, fix small).
- Parallel with: none that edits the ExamEngine start handler.

## Expected
- BR1 A start whose body has no `examEditionId`, or has `00000000-0000-0000-0000-000000000000`, answers 400 problem details with `code: exam_attempt.edition_required` and `errors: [{ field: "examEditionId", code: "exam_attempt.edition_required" }]`, and creates nothing. No lookup runs.
- BR2 `exam_attempt.edition_not_found` keeps one meaning: the 404 of F-166 AC4 (an edition that does not exist or is not published). A non-empty id that matches no published edition is still that 404.
- BR3 The new code has its text in en, pt-BR and pt-PT.

### Acceptance criteria
- AC1 (BR1) Given a signed-in student, when they start with a body `{}`, then the answer is 400 with `code` `exam_attempt.edition_required`, one `errors` entry `examEditionId` / `exam_attempt.edition_required`, and no attempt row exists for them.
- AC2 (BR1) Given a signed-in student, when they start with `examEditionId` equal to the empty Guid, then the answer is the same as AC1.
- AC3 (BR2) Given a signed-in student, when they start with a random Guid that is no edition, then the answer is still 404 `exam_attempt.edition_not_found` (F-166 AC4 unchanged).
- AC4 (BR3) Given the three UI locales, when the resources are loaded, then `exam_attempt.edition_required` has a text in en, pt-BR and pt-PT (the missing-key test is green).

### Out of scope
- A body that does not bind (not JSON, or an `examEditionId` that is not a Guid): the binder's bare 400 is B-37 (board #247).
- The bare 413 of an oversized answer body: B-36.
- Any screen: no Web page calls the start on the main branch; F-169 will, and only ever sends a real id.

## Cause
Verified on the main branch (F-166 merged, `1e83f94`), 2026-10-27.
- Cause: `src/Modules/ExamEngine/Simulab.ExamEngine.Application/Attempts/StartAttemptHandler.cs:22-25` returns `new Error(ExamAttemptErrorCodes.EditionNotFound, ErrorKind.Validation)` when `ExamEditionId` is null. `ApiProblem.StatusFor` (`src/BuildingBlocks/Simulab.ApiResults/ApiProblem.cs`) turns `Validation` into 400, so the body is `400` with `code: exam_attempt.edition_not_found` and no `errors` member. The endpoint (`src/Modules/ExamEngine/Simulab.ExamEngine.Api/AttemptEndpoints.cs` `StartAsync`) passes the error to `Problem` without extensions; only `SaveAsync` writes `errors`.
- The same code is the 404 of F-166 AC4 (`StartAttemptHandler.cs:33-36`, `ErrorKind.NotFound`).
- The current test `Start_WithNoEdition_Answers400` (`tests/Modules/ExamEngine/Simulab.ExamEngine.Tests/StartAttemptEndpointTests.cs:184-193`, F-166 AC14) checks only the status, so it stays green with the reused code.
- Duplicates: no duplicate found. The one other `*_not_found` code with `ErrorKind.Validation`, `CatalogErrorCodes.NoticeSubjectMappingTargetNotFound` (`src/Modules/Catalog/Simulab.Catalog.Application/NoticeSubjects/SaveNoticeSubjectHandler.cs:128`), is a code of its own, used only as that 400 and branched on by `NoticeSubjectDialog.razor:465`; it reuses no 404 code.
- Callers: `StartAttemptHandler.HandleAsync` has one caller, `AttemptEndpoints.StartAsync` — not affected by the order of checks. The code `exam_attempt.edition_not_found` is read by no Web page on the main branch; F-169 (refining, `D:/wt/simulab/f-169-take-exam-edition`) will call this start from the edition page (its BR1) and only ever sends an id.

## Fix
- `ExamAttemptErrorCodes`: add `EditionRequired = "exam_attempt.edition_required"` (field code and top-level code), with its summary naming B-35.
- `StartAttemptHandler`: the guard at lines 22-25 becomes "null or `Guid.Empty`" and returns `EditionRequired` with `ErrorKind.Validation`, carrying the field `examEditionId` (the handler result gains the field entries, the way `SaveAnswerHandler` returns `Fields`).
- `AttemptEndpoints.StartAsync`: writes `errors` when the failure has field entries, the same shape as `SaveAsync`.
- `SharedResources.resx`, `.pt-BR.resx`, `.pt-PT.resx` (`src/Hosts/Simulab.Web/Resources/`): one key `exam_attempt.edition_required` each.
- No duplicate to fix (`## Cause`).

## Docs
- none — no technical page describes the start's error codes; the F-166 contract lives in its item file and the OpenAPI document, which this code change updates by itself.

## Regression test
- `Start_WithNoEdition_Answers400WithEditionRequired` — `tests/Modules/ExamEngine/Simulab.ExamEngine.Tests` (`StartAttemptEndpointTests`, extends the F-166 AC14 test to assert `code` and the `errors` entry; fails today with `exam_attempt.edition_not_found`) — AC1.
- `Start_WithEmptyEditionId_Answers400WithEditionRequired` — same class (fails today: 404) — AC2.
- AC3 is covered by the existing F-166 AC4 test, kept unchanged.
- AC4 is covered by the missing-key test of the Web resources.

## Decisions
- 2026-10-26 — Captured as a bug, severity low, from the F-166 review (finding 2); the owner asked for it to be registered — asked
- 2026-10-27 — The 400 code is `exam_attempt.edition_required`, both as `code` and as the one `errors` entry (`field: examEditionId`) — follows the `*_required` codes of the Catalog and the api-contracts rule; one new key — asked
- 2026-10-27 — The empty Guid counts as missing (400), not as an unknown edition (404) — an empty id is never a real edition; it is a malformed body — asked
- 2026-10-27 — A body that does not bind is out of scope, registered as B-37 (board #247) — keeps this bug small; the fix there reads the body by hand, a different change — asked
- 2026-10-27 — The handler returns the field entries and the endpoint writes `errors`, as `SaveAsync` does, instead of a validation filter — one shape for the module's 400s, no new mechanism — Claude
- 2026-10-27 — No validation screen; the check is one HTTP script, as in F-166 — the item has no screen — Claude

## Open questions
- (none)

## Validation script
There is no screen: every check is a call to the API, made by one script (`docs/validation/B-35/validate.sh` and `validate.ps1`, written and run at `/agile:build`, as in F-166) that prints one line per check.

### English
1. [ ] At the root of this worktree (`D:\wt\simulab\b-35-start-attempt`), start the app host and leave it open → `https://localhost:7287/api/v1/system/info` answers 200.
2. [ ] In a second terminal, set your `Student` email and password and run the script → the lines for `{}` and for the empty Guid show `400 exam_attempt.edition_required` with the field `examEditionId`, the random Guid shows `404 exam_attempt.edition_not_found`, and the last line is `ALL OK`.
3. [ ] Stop the app host (Ctrl+C in the first terminal) → the terminal is back at its prompt.

### pt-BR
1. [ ] Na raiz deste worktree (`D:\wt\simulab\b-35-start-attempt`), suba o app host e deixe aberto → `https://localhost:7287/api/v1/system/info` responde 200.
2. [ ] Num segundo terminal, defina o e-mail e a senha do seu `Student` e rode o script → as linhas de `{}` e do Guid vazio mostram `400 exam_attempt.edition_required` com o campo `examEditionId`, o Guid aleatório mostra `404 exam_attempt.edition_not_found`, e a última linha é `ALL OK`.
3. [ ] Pare o app host (Ctrl+C no primeiro terminal) → o terminal volta ao prompt.

## Events

- 2026-10-27 question — asked 3, from context 0

## Delivery
- Branch: bug/B-35
- Merge: -
