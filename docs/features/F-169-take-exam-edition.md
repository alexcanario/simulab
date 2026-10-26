---
feature: F-169
epic: Exam Simulator
status: idea
board: 235
version: 1
---
# Take an exam edition

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: idea (2026-10-26)
- Stopped at: Captured by the E-06 breakdown; nothing refined yet.
- Next action: Refine it with `/agile:screen` after F-166 is built (owner).
- Waiting on: F-166

## Summary
The student's simulator screen: pick a published edition, start an attempt, answer question by question with base texts and images, jump through an answer sheet, see the notice subjects as the sections of the paper in paper order, write the essay within its line limit, and submit after a warning about blank questions. No timer here (F-170). It runs on the attempt of F-166.

## Start
- Depends on: F-166.
- Waits on (to start): nothing.
- Needed to validate: the seeded Manaus 2026 edition (exists).
- Suggested path: `/agile:refine` with `/agile:screen` → `/agile:build`.
- Parallel with: none.
