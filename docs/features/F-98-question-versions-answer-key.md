---
feature: F-98
epic: Question bank
status: idea
board: 144
version: 1
---
# Question versions and answer key changes

Technical terms: [glossary](../glossary.md)

## Summary
Editing a published question creates a new version and keeps the earlier ones; an answer key change (after an appeal) records its reason and date; the curator sees the history. An attempt of the simulators can name the version that graded it (ADR-0001 #10), so versions must exist before epic E-5 records any attempt. Source: epic Question bank (E-4), brief capability 3 "changed answer keys".

## Start
- Depends on: F-95.
- Waits on (to start): nothing.
- Needed to validate: the local app host — owner.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: F-96 or F-97.
