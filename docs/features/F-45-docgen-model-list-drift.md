---
feature: F-45
epic: Foundation and identity
status: idea
board: 768
version: 1
---
# The DocGen tests see every context DocGen documents

## Summary
`EntityModelsTests.RealModels()` lists the contexts by hand, and it drifted: it never loaded `AiDbContext`, although DocGen has generated `docs/architecture/Ai/` since F-41, because DocGen discovers contexts from the assemblies next to it (`EntityModels.Load`). For four weeks the generator documented a table that every test built on that list was blind to. F-24 found it only because its new rule walks the same list and would have passed without ever looking at `ai_calls`. Make the two agree by construction: a test that compares what DocGen discovers with what the tests load, so a context added later cannot be documented and untested at the same time. Raised by the retro of F-24 (2026-09-25).

## Start
- Depends on: nothing. F-15 (DocGen) and F-24 (the description rules) are done, and both are on `main`.
- Waits on: nothing.
- Suggested path: `/agile:autopilot F-45` — small and well understood; the shape of the check is the only open choice.
- Parallel with: anything. It touches `tests/Simulab.ArchitectureTests/DocGen/` only.
