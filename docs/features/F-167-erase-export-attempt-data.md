---
feature: F-167
epic: Exam Simulator
status: idea
board: 233
version: 1
---
# Erase and export a student's attempt data

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: idea (2026-10-26)
- Stopped at: Split from F-166 at its refinement; nothing refined yet.
- Next action: Refine it after F-166 is built (owner).
- Waiting on: nothing

## Summary
Split from F-166 (2026-10-26). When a student erases the account, the attempts and the text of the answers are anonymized, and "download my data" (F-16) includes the student's attempts and answers. F-166 stores them; this item makes the privacy rules of ADR-0001 #9 (erasure keeps unidentified attempts) and F-16 (each module adds its own section to the export) hold for them. It must be done before any student uses the simulator screen.

## Start
- Depends on: F-166.
- Waits on (to start): nothing outside the items above.
- Needed to validate: unknown — settled at /agile:refine.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: unknown — settled at /agile:refine.
