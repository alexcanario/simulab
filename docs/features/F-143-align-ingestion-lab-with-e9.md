---
feature: F-143
epic: AI-assisted exam import
status: idea
board: 195
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
# Align the external ingestion lab with E-9

Technical terms: [glossary](../glossary.md)

## Summary
The owner will build a standalone lab project, outside Simulab, for ingesting exam papers and answer keys, written as a tutorial with the `tutorial-make` skill: Python and Docling for extraction, Ollama (an 8B to 14B model for structured JSON, `bge-m3` for embeddings), PostgreSQL with `pg_trgm`, `tsvector` and `pgvector`, a .NET 10 service shaped like `IAiGateway` (an `OllamaAiGateway` plus a fake), and golden exams as evals. The lab is the base for epic E-9. This item compares the lab (tutorial and code) with `docs/epics/ai-assisted-exam-import.md` — data model, pipeline steps, JSON validation, preliminary versus final answer key and annulments, evals, the `OllamaAiGateway` contract — and brings each difference back to E-9 as a change to its candidate approach or as an open question, so the first E-9 item starts from what the lab proved. No Simulab code. Captured on 2026-10-18 so the comparison is not forgotten.

## Start
- Depends on: nothing as an item.
- Waits on (to start): the lab tutorial existing and its path — owner (the tutorial does not exist yet; its location is unknown).
- Needed to validate: the owner reads the differences found and decides each one — owner.
- Model: unknown — settled at /agile:refine
- Suggested path: `/agile:discuss` with the lab path (a comparison that ends in edits to the E-9 epic file, not code)
- Parallel with: any item; it touches only `docs/epics/ai-assisted-exam-import.md`.
