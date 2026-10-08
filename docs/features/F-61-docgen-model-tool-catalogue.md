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

## Decisions
- 2026-10-04 — Closed as already delivered, not built. The premise was false: F-49 (merge `1003885`, 2026-09-28) added `ModelToolAttribute` in `src/BuildingBlocks/Simulab.Ai/`, the catalogue in `tools/Simulab.DocGen/ToolCatalogueDoc.cs` with `ToolCatalogueDocTests`, and committed `docs/architecture/tools.md` (0 tools: the first tools come with the AI coach). `dotnet run --project tools/Simulab.DocGen -- --check` gives `docs/architecture is up to date` today. The sync to 0.0.102 raised this item on 2026-10-02 without knowing about F-49. Issue #100 closed as not planned. Owner's choice at `/agile:refine`.
