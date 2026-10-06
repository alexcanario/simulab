---
feature: F-81
epic: Foundation and identity
status: idea
board: 124
version: 1
---
# Park and start staging from a GitHub Actions workflow

Technical terms: [glossary](../glossary.md)

## Summary
Manual GitHub Actions actions that run the staging park and start commands of `docs/infra.md` ("Staging start and stop"), so a test window is opened and closed from GitHub instead of a local `az` session. The commands are only confirmed once F-64 creates the environment and its resource names. Raised as out of scope by the F-65 refinement, 2026-10-04.

## Start
- Depends on: F-64 (the environment and its confirmed resource names); F-65 (the OIDC sign-in and the GitHub environment `staging`).
- Waits on (to start): nothing beyond the dependencies.
- Needed to validate: the provisioned staging environment — owner.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: unknown — settled at /agile:refine.
