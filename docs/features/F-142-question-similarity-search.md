---
feature: F-142
epic: Question bank
status: idea
board: 194
version: 1
---
<!--
One file per feature. Save as: docs/features/F-<number>-<slug>.md
With an external ticket (`/agile:idea feature --ticket PRJ-123`): the id is the ticket's, `feature: PRJ-123`, the file docs/features/PRJ-123-<slug>.md, and an optional header line `ticket-url: <URL>` right after `board:`.
Status flow: idea -> refining -> approved -> building -> validating -> done
- cancelled: exit from idea or refining only, as a duplicate; the file stays, with "Duplicate of <id> (<date>): <where the improvement went>" under the header.
- idea: title, summary and start only (/agile:idea, /agile:epic).
- refining: sections below filled during /agile:refine.
- approved: set only after the product owner says "approve F-<number>" and Open questions is empty or deferred.
- building / validating / done: set by /agile:build and /agile:ship.
- blocked: set by `/agile:change <id> --block "<reason>"` from refining, approved, building or validating; the line `Blocked (<date>): <what stops it> — unblocked by <who> — returns to <status>` sits under the header and `--unblock` returns the item to that status.
Remove these comments when the file leaves `idea`.
-->
# Question similarity search

Technical terms: [glossary](../glossary.md)

## Summary
Find questions that are alike: duplicate or reused questions across exam editions (a curator sees them before publishing a new one, and an import of epic E-9 flags them), "similar questions" for a student, and a topic suggestion from the topics of the nearest questions. Today the back-office search is a substring match, `NormalizedStatement.Contains` (`QuestionQueries.cs:77`, F-100). Candidate path, owner's discussion of 2026-10-18: PostgreSQL `pg_trgm` / `tsvector` first, then `pgvector` with a multilingual embedding model (for example `bge-m3` through Ollama). The embedding model is married to the index: changing it means re-embedding every question. F-102 left "finding near-duplicate base texts" out of its scope; this item may cover it.

## Start
- Depends on: nothing as an item; it is useful only once questions exist in volume (F-99 transcription or epic E-9 import).
- Waits on (to start): questions in volume — owner; the embedding model choice, taken in the same ADR as the E-9 import approach (`docs/epics/ai-assisted-exam-import.md`) — owner; the Ollama machine for the vector step — owner.
- Needed to validate: unknown — settled at /agile:refine
- Model: unknown — settled at /agile:refine
- Suggested path: `/agile:discuss` first (full-text only, or vectors too; which of the three uses comes first) → `/agile:refine` → `/agile:build`
- Parallel with: unknown — settled at /agile:refine
