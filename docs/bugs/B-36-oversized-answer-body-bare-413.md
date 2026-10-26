---
bug: B-36
feature: F-166
status: idea
board: 244
severity: low
epic: Exam Simulator
version: 1
---
# An oversized answer body answers a bare 413

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: idea (2026-10-26)
- Stopped at: Captured from the F-166 review (finding 3); not refined.
- Next action: Owner decides when to refine it (`/agile:refine B-36`).
- Waiting on: F-166 merged, so the code is on the main branch.

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
Not verified on the main branch: the code is not merged yet. Seen on `feature/F-166` at `AttemptEndpoints.cs:104`.

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
