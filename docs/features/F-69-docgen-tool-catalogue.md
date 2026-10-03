---
feature: F-69
epic: Foundation and identity
status: idea
board: 111
version: 1
---
# DocGen tool catalogue

Technical terms: [glossary](../glossary.md)

## Summary
The plugin (agile@canary, since 0.0.70) offers a DocGen tool catalogue: one row per tool offered to a model (description, input schema, what it reaches, permissions, writes, confirmation), generated into `docs/architecture/tools.md`. Simulab has `tools/Simulab.DocGen` but not this part. Raised by the sync with agile@canary 0.1.0 on 2026-10-03.

The work: update `tools/Simulab.DocGen` from `templates/dotnet/DocGen`, copy `templates/dotnet/ai/ModelToolAttribute.cs` into the project that owns the model tools, reference it from DocGen, and commit the first `docs/architecture/tools.md`.

## Start
- Depends on: nothing — unknown whether any model tool exists in the code yet; settled at `/agile:refine`.
- Waits on (to start): nothing.
- Needed to validate: nothing.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: unknown — settled at `/agile:refine`.
