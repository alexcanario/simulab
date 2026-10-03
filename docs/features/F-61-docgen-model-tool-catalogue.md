---
feature: F-61
epic: Foundation and identity
status: idea
board: 100
version: 1
---
# DocGen lists every tool offered to a model

Technical terms: [glossary](../glossary.md)

## Summary
agile@canary 0.0.70 ships a DocGen part that writes `docs/architecture/tools.md`: one row per tool offered to a model (description, input schema, what it reaches, permissions, writes, confirmation). The project's `tools/Simulab.DocGen` predates it. The item updates DocGen from `templates/dotnet/DocGen`, copies `templates/dotnet/ai/ModelToolAttribute.cs` into the project that owns the model tools, references it from DocGen and commits the first `docs/architecture/tools.md`. Raised by `/agile:sync` to 0.0.102; the owner chose to capture it on 2026-10-02.

## Start
- Depends on: nothing.
- Waits on (to start): nothing.
- Needed to validate: nothing.
- Suggested path: `/agile:refine` first — unknown, settled at `/agile:refine` (which project owns the model tools, and whether any exist yet behind `IAiGateway`).
- Parallel with: unknown — settled at `/agile:refine`.
