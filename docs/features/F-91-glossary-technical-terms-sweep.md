---
feature: F-91
epic: Foundation and identity
status: idea
board: 134
version: 1
---
# Glossary sweep of technical terms

Technical terms: [glossary](../glossary.md)

## Summary
Read `docs/infra.md`, `docs/decisions/`, `docs/features/`, `docs/bugs/`, `docs/discussions/`, `docs/epics/` and `docs/releases/` (never `docs/manual/`) and add to the "Technical terms" table of `docs/glossary.md` every product, service, acronym, protocol, pattern or library used there and missing from the table, with its pt-BR word and both meanings. Raised by the sync with agile@canary 0.7.0 (step 5g b) on 2026-10-04; the owner chose to defer it. The sync to 0.4.0 added 15 rows from a sweep that was not exhaustive.

## Start
- Depends on: nothing.
- Waits on (to start): nothing.
- Needed to validate: the owner reads the new rows — the owner.
- Suggested path: `/agile:autopilot F-91` (docs only, small and clear).
- Parallel with: any item that does not edit `docs/glossary.md`; unknown which open worktrees do — settled at `/agile:refine`.

## Decisions
- 2026-10-09 — Closed as already delivered, not built. The sync to agile@canary 0.42.0 ran the sweep (step 5g) and added 15 rows to the "Technical terms" table of `docs/glossary.md` (commit `c702fb7`): ADR, NULLS NOT DISTINCT, TTL, MFA, CDN, TLS, replica, slug, Docker Desktop, Aspire CLI, point-in-time restore, pull request, cold start, bounce / complaint, idempotency key. The `Meaning (pt-BR)` column and the `Technical terms` link lines were already complete. The sweep searched acronym counts and a term list; it did not read every document, so a term can still be missing. Owner's choice.
