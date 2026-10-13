---
feature: F-118
epic: Question bank
status: idea
board: 168
version: 1
---
# Sort and extra filters of the question list

Technical terms: [glossary](../glossary.md)

## Summary
The back office question list (`/admin/questions`) gets what F-100 left out on purpose: ordering by column, or by paper number when one edition is picked (F-95 deferred sorting to F-100 in a column comment), and two more filters, content language and origin (authored or from an edition). F-100 keeps "newest first" and its eight filters; this item adds to them without changing their address keys. Source: F-100 refinement, 2026-10-13 (owner left these out of that item).

## Start
- Depends on: F-100.
- Waits on (to start): nothing.
- Needed to validate: the local app host with questions in more than one language, some authored and some from an edition — owner.
- Suggested path: `/agile:refine` → `/agile:build` (or `/agile:autopilot` once refined small).
- Parallel with: unknown — settled at `/agile:refine`.
