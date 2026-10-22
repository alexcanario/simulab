---
feature: F-147
epic: Cloud hosting and operations
status: idea
board: 205
version: 1
---
# Scheduled re-park of staging every 6 days

Technical terms: [glossary](../glossary.md)

## Summary
Azure starts a stopped PostgreSQL server again by itself 7 days after the stop (`docs/infra.md`, "Staging start and stop"), so a staging that was parked and forgotten silently costs money again. Run a scheduled park (a re-park every 6 days while staging is parked on purpose) from GitHub Actions. Raised as out of scope by the F-81 refinement (2026-10-22): F-81 adds the manual `staging-power.yml` workflow and only documents the 7-day limit. The plugin's `park.yml` and `agile-park.js` (agile@canary templates) do this from a `## Schedule` table of `docs/infra.md`, but `agile-park.js` requires `.github/scripts/agile-deploy.js`, which this repository does not have until F-117.

## Start
- Depends on: F-117 (brings `.github/scripts/agile-deploy.js`); F-81 (the manual park and start workflow and the documented commands).
- Waits on (to start): unknown — settled at /agile:refine. How the schedule tells "parked on purpose" from "running for a test window" is not decided.
- Needed to validate: the provisioned staging environment, parked, and a scheduled run (or a manual run of the schedule's job) — owner.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: unknown — settled at /agile:refine.
