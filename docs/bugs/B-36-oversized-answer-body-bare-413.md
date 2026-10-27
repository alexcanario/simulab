---
bug: B-36
feature: F-166
status: validating
board: 244
severity: low
epic: Exam Simulator
version: 1
---
# An oversized answer body answers a bare 413

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: validating (2026-10-27)
- Stopped at: Built; gate and scan green; waiting for the owner's validation
- Next action: Owner runs the validation script, then /agile:ship B-36
- Waiting on: nothing

## What happens
Saving an answer with a body above `AttemptLimits.SaveBodyMaxBytes` answers a bare `Results.StatusCode(413)`: no problem details and no stable `code`. `.claude/rules/agile/api-contracts.md` says errors are RFC 9457 problem details plus a stable code, with its texts in en, pt-BR and pt-PT, so the UI cannot localize this error.

Evidence (review of F-166, 2026-10-26): `src/Modules/ExamEngine/Simulab.ExamEngine.Api/AttemptEndpoints.cs:104` on the branch `feature/F-166`, read by the reviewer and by the session.

## Start
- Depends on: F-166 (delivered).
- Waits on (to start): nothing.
- Needed to validate: nothing (the check is an HTTP request; no screen calls the save route yet).
- Model: sonnet (no signal: one private endpoint branch, one new error code, no schema, no new module)
- Suggested path: `/agile:build B-36`.
- Parallel with: B-37 and F-169 touch `AttemptEndpoints.cs` or the ExamEngine error texts: build one after the other.

## Expected
A save body above `AttemptLimits.SaveBodyMaxBytes` (by `Content-Length` or while read) answers 413 as RFC 9457 problem details with `code` `exam_attempt.answer_too_large` and no `errors` member; nothing is stored; the code has its text in en, pt-BR and pt-PT.

## Rules
- BR1: the status stays 413, passed to `ApiProblem.Problem` as `status`; the `ErrorKind` is `Validation`.
- BR2: the code is new, `exam_attempt.answer_too_large`, a constant in `ExamAttemptErrorCodes`. The body is not parsed, so the answer names no field.
- BR3: both ways `ReadBodyAsync` refuses a body (the declared `Content-Length` and a chunked body that grows past the limit) give the same answer.

## Screens and API
- `PUT /api/v1/exam-engine/attempts/{id}/answers/{questionId}` (route unchanged): a body above the limit answers `413 application/problem+json` with `code: exam_attempt.answer_too_large`.
- Texts (`SharedResources*.resx`, key = code):
  - pt-BR: "A resposta é grande demais para ser salva. Encurte o texto e tente de novo."
  - pt-PT: "A resposta é demasiado grande para ser guardada. Encurte o texto e tente de novo."
  - en: "The answer is too large to be saved. Shorten the text and try again."
- No screen changes.

## Acceptance criteria
- AC1 (BR1, BR2): Given a started attempt, when the student saves a body one byte above `SaveBodyMaxBytes` with its `Content-Length`, then the answer is 413 problem details with `code` `exam_attempt.answer_too_large`, no `errors` member, and nothing is stored.
- AC2 (BR3): Given a started attempt, when the student saves the same body chunked (no `Content-Length`), then the answer is the same as AC1.
- AC3 (localization): the key `exam_attempt.answer_too_large` exists in the neutral file and in each locale of `UI locales:`, and the missing-key test is green.

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
- `ExamAttemptErrorCodes`: add `AnswerTooLarge = "exam_attempt.answer_too_large"`.
- `AttemptEndpoints.SaveAsync`: replace `Results.StatusCode(StatusCodes.Status413PayloadTooLarge)` with `Problem(new Error(ExamAttemptErrorCodes.AnswerTooLarge, ErrorKind.Validation), StatusCodes.Status413PayloadTooLarge)`.
- `SharedResources.resx`, `.pt-BR.resx`, `.pt-PT.resx` (Web host): the key with the texts of `## Screens and API`.

## Docs
- flow: update docs/flows/exam-attempt.md#Rules — the save limit answers 413 `exam_attempt.answer_too_large`; the Errors list gets one new line.

## Regression test

Tests locked: 0955a2392edd0ea5b8cb81facae9b06b469427d4 (2 files); unlocks: 0

- `SaveAnswerEndpointTests.Save_ABodyAboveTheLimitOfTheRoute_Answers4xx_AndStoresNothing` becomes AC1: asserts 413, `code` `exam_attempt.answer_too_large` and no `errors`. Seen failing before the fix (today it answers an empty body, so the `code` read fails).
- A new chunked-body case for AC2, through the real HTTP pipeline (`WebApplicationFactory`, shared `AppJson.Options`).

## Criterion → test
| Criterion | Test |
|---|---|
| AC1 | `SaveAnswerEndpointTests.Save_ABodyAboveTheLimitOfTheRoute_Answers413AnswerTooLarge_AndStoresNothing` |
| AC2 | `SaveAnswerEndpointTests.Save_AChunkedBodyAboveTheLimitOfTheRoute_Answers413AnswerTooLarge_AndStoresNothing` |
| AC3 | `AnswerTooLargeTextTests.TheCodeOfAnOversizedAnswer_HasItsApprovedTextInEachLanguage` (en, pt-BR, pt-PT); `ErrorCodeTextTests.EveryErrorCode_HasATextInEveryLanguage_AndEveryCodeShapedTextAnswersToACode` (missing-key test) |

## Out of scope
- Kestrel's server-wide request limit (30 MB default) still answers a bare 413 on any route; no ordinary route gets near it and the upload routes handle their own. Not registered (owner, 2026-10-27).
- The start route's malformed body: B-37.

## Decisions
- 2026-10-26 — Captured as a bug, severity low, from the F-166 review (finding 3); the owner asked for it to be registered — asked
- 2026-10-27 — No similar item: B-37 touches the same file but another route and another cause — asked
- 2026-10-27 — Status stays 413 with problem details, not 400: it keeps the status the route already answers and the HTTP meaning; `ApiProblem.Problem` takes a `status` override (423 and 429 already use it). The upload routes answer 400 for their own limit; that difference is accepted — asked
- 2026-10-27 — New code `exam_attempt.answer_too_large` with no `errors` entry, not `answer_invalid` + `text_too_long`: the body is not parsed, so no field can be named honestly — asked
- 2026-10-27 — pt-BR text "A resposta é grande demais para ser salva. Encurte o texto e tente de novo."; en and pt-PT carry the same meaning — asked
- 2026-10-27 — Kestrel's server-wide bare 413 is left alone and not captured as an idea — asked
- 2026-10-27 — `ErrorKind.Validation` for the error: the status is set by the override, and the kind matches the upload routes' too-large errors — decided (technical, no product impact)
- 2026-10-27 — Approved by the owner ("aprovo B-36"); `deps.js block`: free (F-166 delivered) — asked

- 2026-10-27 — Build started in parallel with F-169 (both touch `AttemptEndpoints.cs` and the `.resx`; different regions) — asked
- 2026-10-27 — plan — 1 tests first: AC1 rewritten, AC2 chunked case, AC3 key checked in the three `.resx` → seen failing, `test(B-36)` commit, lock; 2 fix: constant `AnswerTooLarge`, `Problem(...413)` in `SaveAsync`, the key in the three `.resx` → affected ExamEngine and Web tests green; 3 docs: `docs/flows/exam-attempt.md` rules and errors lines → `techdocs.js check B-36`; 4 gate, scan, validation script → validating. No design passes (no new project, route, message or schema).

- 2026-10-27 — Reuse: `ApiProblem.Problem` with its `status` override, `Error`/`ErrorKind.Validation`, the existing `ErrorCodeTextTests` (missing-key test) and `UnsizedContent` as the only new test helper
- 2026-10-27 — Shortcuts: none

## Open questions
- (none)

## Validation script
### English
1. In the worktree, run the save tests:
   ```powershell
   dotnet test tests/Modules/ExamEngine/Simulab.ExamEngine.Tests --filter "FullyQualifiedName~SaveAnswerEndpointTests"
   ```
   ```bash
   dotnet test tests/Modules/ExamEngine/Simulab.ExamEngine.Tests --filter "FullyQualifiedName~SaveAnswerEndpointTests"
   ```
   Expected: every test passes, including the two oversized-body cases.
2. Open `src/Hosts/Simulab.Web/Resources/SharedResources.pt-BR.resx` and find `exam_attempt.answer_too_large`. Expected: the text "A resposta é grande demais para ser salva. Encurte o texto e tente de novo."

### pt-BR
1. No worktree, rode os testes de salvar resposta:
   ```powershell
   dotnet test tests/Modules/ExamEngine/Simulab.ExamEngine.Tests --filter "FullyQualifiedName~SaveAnswerEndpointTests"
   ```
   ```bash
   dotnet test tests/Modules/ExamEngine/Simulab.ExamEngine.Tests --filter "FullyQualifiedName~SaveAnswerEndpointTests"
   ```
   Esperado: todos os testes passam, inclusive os dois casos de corpo grande demais.
2. Abra `src/Hosts/Simulab.Web/Resources/SharedResources.pt-BR.resx` e procure `exam_attempt.answer_too_large`. Esperado: o texto "A resposta é grande demais para ser salva. Encurte o texto e tente de novo."

## Events

- 2026-10-27 question — asked 4, from context 0

## Delivery
- Branch: bug/B-36
- Merge: -
