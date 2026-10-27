---
bug: B-35
feature: F-166
status: done
board: 243
severity: low
epic: Exam Simulator
version: 1
---
# Starting an attempt without an edition answers with a 404 code

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: done (2026-10-27)
- Stopped at: Shipped: merged 6843415 on main (2026-10-27)
- Next action: nothing; the item is done
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

Tests locked: 306bf239be2e042d9c935dbf2cb1d6e08d6f8a59 (1 file); unlocks: 0

- `Start_WithNoEdition_Answers400WithEditionRequired` — `tests/Modules/ExamEngine/Simulab.ExamEngine.Tests` (`StartAttemptEndpointTests`, extends the F-166 AC14 test to assert `code` and the `errors` entry; fails today with `exam_attempt.edition_not_found`) — AC1.
- `Start_WithEmptyEditionId_Answers400WithEditionRequired` — same class (fails today: 404) — AC2.
- AC3 is covered by the existing F-166 AC4 test `Start_OnADraftEditionOrAnUnknownOne_Answers404_AndCreatesNothing` (same class), kept unchanged and green.
- AC4 is covered by the missing-key test `EveryErrorCode_HasATextInEveryLanguage_AndEveryCodeShapedTextAnswersToACode` (`tests/Hosts/Simulab.Web.Tests/Localization/ErrorCodeTextTests.cs`), green with the new key.

## Decisions
- 2026-10-26 — Captured as a bug, severity low, from the F-166 review (finding 2); the owner asked for it to be registered — asked
- 2026-10-27 — The 400 code is `exam_attempt.edition_required`, both as `code` and as the one `errors` entry (`field: examEditionId`) — follows the `*_required` codes of the Catalog and the api-contracts rule; one new key — asked
- 2026-10-27 — The empty Guid counts as missing (400), not as an unknown edition (404) — an empty id is never a real edition; it is a malformed body — asked
- 2026-10-27 — A body that does not bind is out of scope, registered as B-37 (board #247) — keeps this bug small; the fix there reads the body by hand, a different change — asked
- 2026-10-27 — The handler returns the field entries and the endpoint writes `errors`, as `SaveAsync` does, instead of a validation filter — one shape for the module's 400s, no new mechanism — Claude
- 2026-10-27 — No validation screen; the check is one HTTP script, as in F-166 — the item has no screen — Claude
- 2026-10-27: plan — 1 regression tests for `{}` and the empty Guid, plus the `EditionRequired` constant as skeleton → both red, locked; 2 handler guard with the field entry and the endpoint writing `errors` → AC1, AC2 and the F-166 AC4 test (AC3) green; 3 the key in the three resource files → `ErrorCodeTextTests` green (AC4); 4 validation scripts `docs/validation/B-35/` → gate GREEN
- 2026-10-27 — The start returns a `StartAttemptOutcome` (attempt, error, field entries) in the shape of `SaveAnswerOutcome`, and both endpoints write `errors` through one helper — the shared `Result` has no field entries; reuses `AnswerFieldError` (its name says "answer"; a rename is out of scope) — Claude
- 2026-10-27 — The build ran the scripts on this worktree's own database (`Database__Name=simulab_b35`) with a test student activated there; the owner's run uses the shared `simulab` database, which the item does not change (no migration, every start refused) — Claude
- Reuse: `SaveAnswerOutcome` shape, `AnswerFieldError`, the `errors` writing of `SaveAsync` (now one helper), the test helpers `ErrorsOf`, `CodeOf`, `BodyAsync`, and the F-166 validation script's form
- Shortcuts: none

## Open questions
- (none)

## Validation script
There is no screen: every check is a call to the API, made by one script (`docs/validation/B-35/validate.sh` and `validate.ps1`, as in F-166) that prints one line per check. Both were run at the build against this worktree's app host and printed `ALL OK`.

### English
1. [ ] At the root of this worktree (`D:\wt\simulab\b-35-start-attempt`), stop any other app host, start this one and leave it open. It runs on the shared local database `simulab`, where your `Student` account lives: the item has no migration and every start it makes is refused, so it writes nothing there. → `https://localhost:7287/api/v1/system/info` answers 200.
   ```powershell
   dotnet run --project src/Hosts/Simulab.AppHost
   ```
   ```bash
   dotnet run --project src/Hosts/Simulab.AppHost
   ```
2. [ ] In a second terminal at the same folder, run the script with your `Student` email and password (replace the two words in capitals) → it prints these four lines; to repeat, run it again (it changes nothing).
   ```powershell
   $env:STUDENT_EMAIL = 'EMAIL'; $env:STUDENT_PASSWORD = 'PASSWORD'; ./docs/validation/B-35/validate.ps1
   ```
   ```bash
   STUDENT_EMAIL='EMAIL' STUDENT_PASSWORD='PASSWORD' bash docs/validation/B-35/validate.sh
   ```
   ```
   OK   body {} -> 400 exam_attempt.edition_required examEditionId/exam_attempt.edition_required
   OK   empty Guid -> 400 exam_attempt.edition_required examEditionId/exam_attempt.edition_required
   OK   random Guid -> 404 exam_attempt.edition_not_found (no errors)
   ALL OK
   ```
3. [ ] Stop the app host (Ctrl+C in the first terminal) → the terminal is back at its prompt.

### pt-BR
1. [ ] Na raiz deste worktree (`D:\wt\simulab\b-35-start-attempt`), pare qualquer outro app host, suba este e deixe aberto. Ele usa o banco local compartilhado `simulab`, onde está a sua conta `Student`: o item não tem migração e todo início que ele faz é recusado, então nada é gravado lá. → `https://localhost:7287/api/v1/system/info` responde 200.
   ```powershell
   dotnet run --project src/Hosts/Simulab.AppHost
   ```
   ```bash
   dotnet run --project src/Hosts/Simulab.AppHost
   ```
2. [ ] Num segundo terminal, na mesma pasta, rode o script com o e-mail e a senha do seu `Student` (troque as duas palavras em maiúsculas) → ele imprime estas quatro linhas; para repetir, rode de novo (não muda nada).
   ```powershell
   $env:STUDENT_EMAIL = 'EMAIL'; $env:STUDENT_PASSWORD = 'PASSWORD'; ./docs/validation/B-35/validate.ps1
   ```
   ```bash
   STUDENT_EMAIL='EMAIL' STUDENT_PASSWORD='PASSWORD' bash docs/validation/B-35/validate.sh
   ```
   ```
   OK   body {} -> 400 exam_attempt.edition_required examEditionId/exam_attempt.edition_required
   OK   empty Guid -> 400 exam_attempt.edition_required examEditionId/exam_attempt.edition_required
   OK   random Guid -> 404 exam_attempt.edition_not_found (no errors)
   ALL OK
   ```
3. [ ] Pare o app host (Ctrl+C no primeiro terminal) → o terminal volta ao prompt.

## Events

- 2026-10-27 question — asked 3, from context 0

## Delivery
- Branch: bug/B-35 (deleted; never pushed)
- Merge: 6843415 on main (2026-10-27), version 1.24.1
- Tests: full suite 5464 passed, 0 failed, in 15 projects (`gate.js ship`, 2 runs: 33 s build + 449 s tests, then after the version bump 57 s build + 680 s tests; over the 5 min budget, F-177)
- Security scan: `agile scan GREEN`, closed by Claude: none
- App manual: unchanged (no screen calls the start yet)
- Validated by the owner in the conversation (2026-10-27, "validado"); no step was ticked in the widget.
- Validation evidence:
  1. [ ] not tested (step 1, start the app host)
  2. [ ] not tested (step 2, run the script)
  3. [ ] not tested (step 3, stop the app host)
  Evidence: 0 of 3 tested
Gate time: 6 passes, 38 min 8 s — turn 2 (3 min 39 s), ship 2 (20 min 19 s), scan 2 (14 min 10 s); 0 red
Usage: 3294.1k weighted (estimate) — refine 13% · build 52% · ship 22% · other 14% — context per turn 127k → 245k — 2 sessions, 146 turns
