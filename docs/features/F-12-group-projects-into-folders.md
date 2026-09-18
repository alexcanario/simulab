---
feature: F-12
epic: Foundation and identity
status: refining
board: 716
version: 1
---
# Group projects into folders

## Summary
Technical chore, after F-4 ships. Move the projects into grouped folders on disk (`Hosts`, `BuildingBlocks`, `Modules/<Module>`) under `src/` and `tests/`, with solution folders in `Simulab.slnx` that mirror them. A single `Directory.Packages.props` stays at the root. Updates project references, the architecture tests that read paths, `profile.md` (layout), `CLAUDE.md`, `docs/infra.md` and `.claude/launch.json`. Decided by the owner on 2026-09-18 (move on disk; one packages file; separate item so F-4 merges clean).

## Goal
Make the solution readable by role as it grows (four more modules and two more building blocks are planned), without changing any behavior.

## Users and use cases
- UC1 A developer opens `Simulab.slnx` and finds each project under the solution folder of its group, the same folder it has on disk.
- UC2 A developer adds a new module, building block or host and knows from `profile.md` which folder it goes into; an architecture test fails if the solution folder and the disk folder disagree.

## Business rules
- BR1 Production projects live in:
  - `src/Hosts/`: `Simulab.Api`, `Simulab.Web`, `Simulab.AppHost`, `Simulab.ServiceDefaults`;
  - `src/BuildingBlocks/`: `Simulab.SharedKernel`, `Simulab.Persistence`, `Simulab.Email`, and the future `Simulab.Ai` and `Simulab.Storage`;
  - `src/Modules/<Module>/`: the module projects (Identity as today).
- BR2 Test projects live in:
  - `tests/Hosts/`: `Simulab.Api.Tests`, `Simulab.Web.Tests`, `Simulab.AppHost.Tests`;
  - `tests/BuildingBlocks/`: `Simulab.SharedKernel.Tests`, `Simulab.Persistence.Tests`, `Simulab.Email.Tests`;
  - `tests/Modules/<Module>/`: the module test project (`Simulab.Identity.Tests`);
  - `tests/` root: `Simulab.ArchitectureTests` and `Simulab.Testing`, which serve every group.
- BR3 Every project in `Simulab.slnx` sits in a solution folder equal to its disk folder (for example `/src/Hosts/` for `src/Hosts/Simulab.Api/Simulab.Api.csproj`).
- BR4 `Directory.Build.props` exists only at the root and in `tests/`. A group gets its own file only when it has a first property to share, and that file imports the one above it.
- BR5 `Directory.Packages.props` stays single, at the root.
- BR6 Project names, assembly names and namespaces do not change; no production code changes except paths.

## Screens and API
- No screens, routes or error codes change.

## Acceptance criteria
- AC1 Given the moved layout, when `dotnet build Simulab.slnx` runs, then it succeeds with no new warnings against the committed baseline. (BR1, BR2, BR5, BR6)
- AC2 Given the moved layout, when the full suite runs (`dotnet test Simulab.slnx`), then every test is green, including the updated path checks in `BuildingBlockBoundaryTests` and `IdentityModuleBoundaryTests`. (BR1, BR2)
- AC3 Given `Simulab.slnx`, when the architecture test for the solution layout runs, then every project's solution folder equals its disk folder and every project is under a group folder of BR1/BR2; the test fails on a sample mismatch. (BR3)
- AC4 Given the repository, when the `Directory.Build.props` files are listed, then only the root and `tests/` ones exist, and `Directory.Packages.props` exists only at the root. (BR4, BR5)
- AC5 Given the moved layout, when the app host starts from `src/Hosts/Simulab.AppHost`, then Api and Web come up and `/sign-up` opens. (on screen, validation script)
- AC6 Given a moved file, when `git log --follow` runs on it, then its history before the move is shown. (validation script)
- Localization: not applicable, no UI text is added or changed.

## Decisions
- 2026-09-18 — No `Directory.Build.props` per group for now (owner, question 1) — the groups share no property today; the profile adds structure on the second use.
- 2026-09-18 — `Simulab.ArchitectureTests` and `Simulab.Testing` stay at the `tests/` root (owner, question 2) — they serve every group.
- 2026-09-18 — Future building blocks (`Simulab.Ai`, `Simulab.Storage`) go to `src/BuildingBlocks/`, and `profile.md` says so (owner, question 3) — the next feature does not decide it again.
- 2026-09-18 — Move with `git mv` — keeps the file history (AC6).
- 2026-09-18 — New architecture test that compares `Simulab.slnx` folders with disk folders — keeps BR3 true as projects are added.
- 2026-09-18 — Old paths in the files of F-1 to F-4 and in `docs/agile/retro-log.md` stay as they are — they are history.
- 2026-09-18 — No new packages; the agile Stop gate needs no change (it builds its graph from the `.csproj` files, not from paths).
- 2026-09-18 — F-4 shipped during this refinement (merge `389ea17`); the refinement reached `main` in `a856f94` and the build runs on `feature/F-12`.
- 2026-09-18 — The basic `.editorconfig` is not created by F-12: it already exists at the root (from the bootstrap). The owner replaces it with the final version in board task 717 (child of 716) — owner request; the warnings of the final version are checked by the Stop gate against the baseline.

## Owner tasks
- Board 717 — replace the basic `.editorconfig` with the final version (owner). Not an acceptance criterion of F-12.

## Out of scope
- Renaming projects, assemblies or namespaces.
- Any production code change other than paths.
- Rewriting old paths in the files of shipped items and in the retro log.

## Open questions
- (none)

## Change notes

## Validation script
1. `dotnet build Simulab.slnx` → build succeeds, no new warnings.
2. Open `Simulab.slnx` in the IDE → projects appear under `src/Hosts`, `src/BuildingBlocks`, `src/Modules/Identity` and the matching `tests/` folders.
3. `dotnet run --project src/Hosts/Simulab.AppHost` → the Aspire dashboard shows Api and Web running.
4. Open `/sign-up` on the Web → the sign-up screen opens as before.
5. `git log --follow --oneline src/Hosts/Simulab.Api/Program.cs` → commits from before the move are listed.

## Delivery
- Branch: feature/F-12
- Merge: <commit>
- Tests: <count, duration>
- Manual pages: none (no visible behavior change)
