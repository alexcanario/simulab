---
feature: F-65
epic: Foundation and identity
status: refining
board: 104
version: 1
---
# A GitHub Actions workflow deploys a release

Technical terms: [glossary](../glossary.md)

## Summary
A `deploy.yml` workflow runs the Deploy command of `docs/infra.md` for an environment, signing in to Azure with OIDC federation (no stored cloud secret) and using GitHub environments, with an approval required for production. F-62's ADR evaluates the approach; this item writes the workflow once there is an environment to deploy to. Raised as out of scope by the F-62 refinement, 2026-10-02.

## Start
- Depends on: F-62; F-64 (an environment to deploy to).
- Waits on (to start): nothing beyond the dependencies.
- Needed to validate: the provisioned staging environment and the GitHub environment `staging` — owner.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: unknown — settled at /agile:refine.
