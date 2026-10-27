---
feature: F-168
epic: Exam Simulator
status: refining
board: 234
version: 1
---
# Exam rules of an edition

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: refining (2026-10-27)
- Stopped at: Refinement started in D:/wt/simulab/f-168-edition-exam-rules.
- Next action: Claude asks the round of questions.
- Waiting on: nothing

## Summary
An admin records, on an exam edition, the rules the notice sets for the paper: its duration, the points per question of each notice subject, the cut-off of the objective paper and of the essay. The Manaus 2026 edition gets its rules seeded (5 hours; 1.50 or 2.00 points per question; pass at 60 of 100 points; essay of 20 points, pass at 10). Today `ExamEdition` and `NoticeSubject` carry none of this (epic E-06, "What exists").

## Start
- Depends on: nothing.
- Waits on (to start): nothing.
- Needed to validate: nothing; the Manaus 2026 notice is in the owner's folder.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: F-166, F-167.
