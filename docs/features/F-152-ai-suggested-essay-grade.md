---
feature: F-152
epic: Question bank
status: idea
board: 210
version: 1
---
# AI-suggested essay grade

Technical terms: [glossary](../glossary.md)

## Summary
The AI proposes a score for each criterion of an essay's rubric and the grader confirms or changes it; no grade reaches the student without a person confirming it (brief: a person reviews every AI output). The call goes through `IAiGateway`, which checks the plan and records usage and cost. Source: discussion D-5 (2026-10-24).

## Start
- Depends on: F-151.
- Waits on (to start): the AI cost per active student and the per-plan limit for this use (brief open note "AI") — owner.
- Needed to validate: unknown — settled at /agile:refine.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: unknown — settled at /agile:refine.
