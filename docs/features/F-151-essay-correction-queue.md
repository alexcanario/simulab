---
feature: F-151
epic: Question bank
status: idea
board: 209
version: 1
---
# Essay and open-answer correction queue

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: idea (2026-10-26)
- Stopped at: D-9 decided it waits on F-166, the item that stores a student's answers.
- Next action: Refine F-166 first (owner); refine this item after it ships.
- Waiting on: nothing

## Summary
A screen where a grader sees the essays and open answers waiting for a grade, scores each rubric criterion and saves the grade; the grade reaches the student's result. A grader with no tenant sees all of them, with a tenant only that tenant's. Source: discussion D-5 (2026-10-24). Nothing exists yet that stores a student's answers (no attempt module in `src/`), so the queue has nothing to show until one does.
Added 2026-10-24 (F-109 refinement): performance levels per criterion (ENEM style: each criterion allows only fixed steps, such as 0, 40, 80, 120, 160 or 200, each with a written description) were left out of F-109, which stores only criteria with a name, a description and whole-number maximum points. Decide here, when the correction screen is refined, whether the grader picks a step instead of typing a number; if so, the steps live in the rubric and are versioned with it. No separate item was captured.

## Start
- Depends on: F-109, F-150, F-166.
- Waits on (to start): nothing outside the items above. D-9 (2026-10-26) decided it waits on F-166, which stores a student's answers; no queue, contract or table is built before it.
- Needed to validate: unknown — settled at /agile:refine.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: unknown — settled at /agile:refine.
