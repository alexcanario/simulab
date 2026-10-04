---
feature: F-79
epic: Subject taxonomy
status: idea
board: 117
version: 1
---
# Subjects and topics back office

Technical terms: [glossary](../glossary.md)

## Summary
An admin registers the canonical taxonomy in two levels (ADR-0001 #43): `Subject`, with an optional `Area` from a fixed seeded list, and `Topic` under its subject. A subject or topic in use cannot be deleted. Lives in the `Catalog` module (`catalog` schema) and is edited under `catalog.manage` (epic decisions, 2026-10-04).

## Start
- Depends on: F-33 to F-37 (the `Catalog` module, done).
- Waits on (to start): nothing.
- Needed to validate: unknown — settled at /agile:refine.
- Suggested path: `/agile:refine` with `/agile:screen` → `/agile:build`.
- Parallel with: none (every other feature of the epic depends on it).
