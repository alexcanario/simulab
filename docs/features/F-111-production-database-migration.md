---
feature: F-111
epic: Cloud hosting and operations
status: idea
board: 157
version: 1
---
# A release migrates the production database

Technical terms: [glossary](../glossary.md)

## Summary
Decide and build how a release applies the database migrations in Production. Today `Database__ApplyMigrationsOnStart` is on only in Staging (F-64 BR5) and off in Production, so a first production deploy starts on an empty database; `docs/infra.md` says production keeps the default "until F-65 decides how a release migrates", and F-65's rules do not cover it. Raised by the F-65 build, 2026-10-09.

## Start
- Depends on: F-64 (done: staging migrates on start); F-65 (validating: the deploy workflow a migration step would join).
- Waits on (to start): an owner decision on the approach (migrate on start as in Staging, a migration step in the deploy workflow, or a manual command) — unknown — settled at /agile:refine.
- Needed to validate: unknown — settled at /agile:refine.
- Suggested path: `/agile:discuss` first (open direction), then `/agile:refine` → `/agile:build`.
- Parallel with: unknown — settled at /agile:refine; it touches `docs/infra.md` and probably `.github/workflows/deploy.yml`, so not beside F-65.
