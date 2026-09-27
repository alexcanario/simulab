---
feature: F-49
epic: Foundation and identity
status: idea
board: 774
version: 1
---
# DocGen tool catalogue

## Summary
Since plugin agile@canary 0.0.70, `tools/<App>.DocGen` can also generate `docs/architecture/tools.md`: one row
per tool the app offers to a model, built from a `[ModelTool]` attribute (description, input schema, what it
reaches, permissions, whether it writes, whether it asks first). Simulab does not use this yet — surfaced by
`/agile:sync` (0.0.63 → 0.0.71) as a capability the plugin ships that the project lacks.

## Start
- Depends on: F-15 (`done`, built `tools/Simulab.DocGen`) and `Simulab.Ai` (`IAiGateway`, built). Applies once
  the app actually offers tools to a model — unknown — settled at `/agile:refine`.
- Waits on: an owner decision on whether/when the app exposes tools to a model at all.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: unknown — settled at `/agile:refine`.
