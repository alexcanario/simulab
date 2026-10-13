---
feature: F-117
epic: Cloud hosting and operations
status: idea
board: 167
version: 1
---
# The deploy pipeline checks the cloud account before it signs in

Technical terms: [glossary](../glossary.md)

## Summary
Bring in the plugin's `templates/dotnet/workflows/agile-deploy.js` (agile@canary 0.2.0) so the Cloud accounts check runs beside the existing `azure/login` step of `deploy.yml` (F-65). Evidence, checked 2026-10-12: `.github/workflows/deploy.yml:63` uses `azure/login@v3`, and `.github/scripts/` does not exist in this repository (`find . -name "agile-deploy*"` lists nothing), so there is no script to replace: the item adds it and wires it into the workflow. The plan's note (`node sync.js plan`, 0.65.0) says `login step without the check`. The same row was logged as not offered at the 0.63.0 and 0.63.1 syncs; the owner accepted it as an item on 2026-10-12.

## Start
- Depends on: F-65 (done: `deploy.yml` and the current `agile-deploy.js`).
- Waits on (to start): unknown — settled at /agile:refine. `docs/infra.md` must declare a `## Cloud accounts` line for the environment; whether it does is not verified.
- Needed to validate: one deploy run on staging (F-64) — owner, on GitHub.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: none that touches `.github/workflows/` or `.github/scripts/`; F-116 also edits them, so do not run them side by side.
