---
feature: F-151
epic: Question bank
status: idea
board: 209
version: 1
---
# Essay and open-answer correction queue

Technical terms: [glossary](../glossary.md)

## Summary
A screen where a grader sees the essays and open answers waiting for a grade, scores each rubric criterion and saves the grade; the grade reaches the student's result. A grader with no tenant sees all of them, with a tenant only that tenant's. Source: discussion D-5 (2026-10-24). Nothing exists yet that stores a student's answers (no attempt module in `src/`), so the queue has nothing to show until one does.

## Start
- Depends on: F-109, F-150.
- Waits on (to start): the item that stores a student's answers in the simulators (epics Question Bank Simulator and Exam Simulator have no item yet) — owner.
- Needed to validate: unknown — settled at /agile:refine.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: unknown — settled at /agile:refine.
