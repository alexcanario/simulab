---
bug: B-37
feature: F-166
status: idea
board: 247
severity: low
epic: Exam Simulator
version: 1
---
# Starting an attempt with a malformed body answers a 400 with no code

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: idea (2026-10-27)
- Stopped at: Split out of the B-35 refinement; not refined.
- Next action: Owner decides when to refine it (`/agile:refine B-37`).
- Waiting on: nothing.

## What happens
`POST /api/v1/exam-engine/attempts` with a body that is not valid JSON, or with an `examEditionId` that is not a Guid, is expected to answer the ASP.NET binder's bare 400: no stable `code` and no `errors` entry, because `AttemptEndpoints.StartAsync` binds `StartAttemptRequest` before the handler runs. `.claude/rules/agile/api-contracts.md` asks every error to carry a stable code and a 400 validation to carry one entry per field.

B-35 fixes only the missing or empty `examEditionId` (a body that binds); this item is what is left: a body that does not bind.

## Start
- Depends on: F-166 (delivered).
- Waits on (to start): nothing. Cause not verified: measure it at `/agile:refine` (symptom from reading `AttemptEndpoints.StartAsync`; no request sent yet).
- Needed to validate: nothing.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: none that edits `AttemptEndpoints.cs` — B-35 and B-36 touch the same file and the ExamEngine error codes: build one after the other.

## Expected
unknown — settled at `/agile:refine`.

## Cause
unknown — settled at `/agile:refine`.

## Fix
To be written at `/agile:refine`.

## Docs
- none — to be settled at `/agile:refine`.

## Regression test
- To be written at `/agile:refine`.

## Decisions
- 2026-10-27 — Split out of B-35 as its own bug, severity low; the owner chose to register it instead of growing B-35 — asked

## Open questions
- (none)

## Validation script
### English
1. To be written at `/agile:refine`.

### pt-BR
1. A ser escrito em `/agile:refine`.

## Events

## Delivery
- Branch: bug/B-37
- Merge: -
