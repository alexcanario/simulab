---
feature: F-170
epic: Exam Simulator
status: idea
board: 236
version: 1
---
# Exam timer

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: idea (2026-10-26)
- Stopped at: Captured by the E-06 breakdown; nothing refined yet.
- Next action: Refine it after F-169 is built (owner).
- Waiting on: F-169, F-168

## Summary
An attempt runs against the edition's duration (F-168): the server keeps the countdown, the screen shows it with a warning near the end, the attempt is submitted automatically when time runs out, and a student who closes the browser resumes with the time left. Simulae's `ExamEngine` has a Redis timer, an attempt lock and an auto-submit background service to import (ADR-0001 #21).

## Start
- Depends on: F-169, F-168.
- Waits on (to start): nothing.
- Needed to validate: unknown — settled at /agile:refine.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: F-171.
