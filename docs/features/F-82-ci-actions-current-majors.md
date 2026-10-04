---
feature: F-82
epic: Foundation and identity
status: idea
board: 125
version: 1
---
# Update the CI workflow actions to current majors

Technical terms: [glossary](../glossary.md)

## Summary
`.github/workflows/ci.yml` uses `actions/checkout@v4` and `actions/setup-dotnet@v4`; the current releases are `v7.0.1` and `v6.0.0` (GitHub releases, checked 2026-10-04). Move the CI to the current majors and confirm the run stays green. Raised as out of scope by the F-65 refinement, 2026-10-04.

## Start
- Depends on: nothing.
- Waits on (to start): nothing.
- Needed to validate: a CI run on GitHub, which needs the owner's authorization to push the branch — owner.
- Suggested path: `/agile:autopilot F-82` (small and clear).
- Parallel with: unknown — settled at /agile:refine.
