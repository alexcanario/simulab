---
bug: B-35
feature: F-166
status: idea
board: 243
severity: low
epic: Exam Simulator
version: 1
---
# Starting an attempt without an edition answers with a 404 code

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: idea (2026-10-26)
- Stopped at: Captured from the F-166 review (finding 2); not refined.
- Next action: Owner decides when to refine it (`/agile:refine B-35`).
- Waiting on: F-166 merged, so the code is on the main branch.

## What happens
`POST` that starts an attempt, with a body that has no `examEditionId`, answers 400 with the code `exam_attempt.edition_not_found` and no `errors` entry. The same code means 404 (an edition that does not exist or is not published) in AC4 of F-166, so a client that branches on `code` cannot tell a malformed body from an unknown edition. `.claude/rules/agile/api-contracts.md` asks a 400 validation for one entry per field, each with a code.

Evidence (review of F-166, 2026-10-26): `src/Modules/ExamEngine/Simulab.ExamEngine.Application/Attempts/StartAttemptHandler.cs:22-25` on the branch `feature/F-166`, read by the reviewer and by the session.

## Start
- Depends on: F-166 (the code is on `feature/F-166`, not on the main branch yet).
- Waits on (to start): the merge of F-166.
- Needed to validate: nothing.
- Suggested path: `/agile:refine` → `/agile:build` (or `/agile:autopilot B-35`, cause clear, fix small).
- Parallel with: none that edits the ExamEngine start handler.

## Expected
A start with no `examEditionId` answers 400 with one `errors` entry (`field: examEditionId`, a validation code of its own, with texts in en, pt-BR and pt-PT). `exam_attempt.edition_not_found` stays the 404 of AC4.

## Cause
Not verified on the main branch: the code is not merged yet. Seen on `feature/F-166` at `StartAttemptHandler.cs:22-25`.

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
