---
feature: F-105
epic: Question bank
status: idea
board: 151
version: 1
---
# Review before publishing

Technical terms: [glossary](../glossary.md)

## Summary
Questions gain the `InReview` status: a curator sends a draft for review, a reviewer approves it (it is published) or rejects it with a reason, from a review queue, under a review permission. F-95 has only `Draft` and `Published` (owner, 2026-10-09); this item may move to epic E-9, where every AI-extracted item must be reviewed by a person. Source: epic Question bank (E-4), brief capability 3 "Workflow"; Simulae `FilaRevisaoPage` and `QuestionReviewDecision` as the reference.

## Start
- Depends on: F-95.
- Waits on (to start): nothing.
- Needed to validate: the local app host and two users (an author and a reviewer) — owner.
- Suggested path: `/agile:refine` with `/agile:screen` → `/agile:build`.
- Parallel with: unknown — settled at /agile:refine.
