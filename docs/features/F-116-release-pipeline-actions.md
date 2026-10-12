---
feature: F-116
epic: Cloud hosting and operations
status: idea
board: 166
version: 1
---
# A GitHub Actions workflow publishes a release

Technical terms: [glossary](../glossary.md)

## Summary
Install the release pipeline that agile@canary 0.64.0 ships: a `release.yml` workflow, run by hand or by `/agile:publish`, that packages, tags, pushes and creates the GitHub Release on a runner instead of the owner's machine. Raised by `/agile:sync` to 0.65.0 (2026-10-12), which lists it as a missing capability: `.github/workflows/release.yml` and `.github/scripts/agile-release.js` do not exist. Until it is built, `/agile:publish --local` stays the way. `deploy.yml` (F-65) stays as it is (this repository has no `.github/scripts/agile-deploy.js`; F-117 brings it).

## Start
- Depends on: F-65 (done: the `deploy.yml` workflow and its GitHub environments; the release workflow can start the `Deploy` of the new tag).
- Waits on (to start): unknown — settled at /agile:refine. The main branch must accept a push from the GitHub Actions bot; whether it does today is not verified.
- Needed to validate: one run by hand of the workflow (Actions > Release > Run workflow) on a version worth releasing — owner, on GitHub.
- Suggested path: `/agile:refine` → `/agile:build` (the plugin writes the files with `node sync.js pipeline write` from the project root).
- Parallel with: none that touches `.github/workflows/`; F-117 also adds a script under `.github/scripts/`, so do not run them side by side.
