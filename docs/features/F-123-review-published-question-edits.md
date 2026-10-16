---
feature: F-123
epic: Question bank
status: idea
board: 174
version: 1
---
# Review edits of a published question

Technical terms: [glossary](../glossary.md)

## Summary
An edit of a `Published` question that changes its content goes through review before it replaces the published version; the published version keeps being served meanwhile. This needs a pending version beside the published one (F-98 versions). F-105 reviews only the first publication: editing a published question stays direct, as in F-95 and F-98 (owner, 2026-10-16). Source: epic Question bank (E-4).

## Start
- Depends on: F-105; F-98 (done).
- Waits on (to start): nothing.
- Needed to validate: the local app host and two users (an author and a reviewer) — owner.
- Suggested path: `/agile:discuss` first (a pending version beside the published one touches the simulators of E-5 and E-6), then `/agile:refine` → `/agile:build`.
- Parallel with: unknown — settled at /agile:refine.
