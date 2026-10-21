---
bug: B-33
feature: F-49
epic: Foundation and identity
status: idea
board: 201
severity: medium
---
# DocGen with `tools: false` deletes the tool catalogue page that a test still reads

Technical terms: [glossary](../glossary.md)

## What happens
Found at the ship of F-89 (2026-10-21). Since F-49 (`959b821`, "turn off the DocGen tool catalogue until the app offers a tool") `tools/Simulab.DocGen/docgen.json` has `"tools": false`, but `docs/architecture/tools.md` and its link in `docs/architecture/README.md` are still committed on `main`, and `tests/Simulab.ArchitectureTests/DocGen/ToolCatalogueDocTests.cs` still reads the committed page.
1. Run the declared docs command (`gate.js docs`, which runs `dotnet run --project tools/Simulab.DocGen`) on a tree that matches `main`.
2. DocGen deletes `docs/architecture/tools.md` and the `tools.md` line of `docs/architecture/README.md`, and reports `agile docs GREEN: 2 file(s) changed under docs/architecture/`.
3. Commit those two files and run `gate.js ship`: the suite is RED on `ToolCatalogueDocTests.CommittedCatalogue_MatchesTheToolsInTheProductionAssemblies`.

Evidence: the gate log of the F-89 ship (`agile gate RED: tests failed (Simulab.slnx): Simulab.ArchitectureTests.DocGen.ToolCatalogueDocTests.CommittedCatalogue_MatchesTheToolsInTheProductionAssemblies`). The F-89 ship restored both files from `main` and the next full run was green (4382 tests), so `main` still carries the inconsistency and the next item whose ship runs the docs command meets it again.

Added 2026-10-21 (retro of F-84): it did. At the ship of F-84, `gate.js docs` ran `dotnet run --project tools/Simulab.DocGen` and printed `agile docs GREEN: 2 file(s) changed under docs/architecture/`; `git status` showed ` M docs/architecture/README.md` and ` D docs/architecture/tools.md` (the same two files). They were restored with `git checkout --` and not committed, and the next full run was green (4409 tests). Evidence: the output of that docs run.

## Start
- Depends on: nothing (F-49 is done; the catalogue code stays).
- Waits on (to start): nothing.
- Needed to validate: nothing; `gate.js docs` followed by `gate.js ship` is the check.
- Model: unknown — settled at /agile:refine.
- Suggested path: `/agile:refine` → `/agile:build`. Fix one side: drop the committed page and its README link and make the test accept a catalogue that is off, or turn the catalogue on again. Cause of the choice not verified: read the test and F-49's decisions at refinement.
- Parallel with: none known.

## Expected
Running the docs command on `main` changes nothing, and the architecture tests are green with the committed pages exactly as DocGen leaves them.

## Cause
Not confirmed in code yet. Symptom: `docgen.json` says `"tools": false` while a committed page and an architecture test expect the catalogue. Cause not verified: read `ToolCatalogueDocTests.CommittedCatalogue_MatchesTheToolsInTheProductionAssemblies` and the DocGen switch at refinement.

## Fix
To be written at refinement.

## Docs
- none — to be decided at refinement.

## Regression test
- To be written at refinement.
