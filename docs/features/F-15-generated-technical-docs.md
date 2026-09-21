---
feature: F-15
epic: Foundation and identity
status: approved
board: 728
version: 1
---
# Generated technical docs

## Summary
Adopt the plugin's DocGen tool, which arrived in agile@canary 0.0.21, after this project was bootstrapped: copy `templates/dotnet/DocGen/` to `tools/Simulab.DocGen/`, reference the projects that own a `DbContext`, and generate `docs/architecture/` from the code — entity diagrams and a data dictionary per module (from the EF model), a route map per area (from the OpenAPI document) and a module diagram (from project references), all Mermaid. From then on `/agile:ship` regenerates them and `--check` fails when they are stale, and the definition of done covers it.

## Goal
Keep an always-current technical picture of the data, the API and the module dependencies without writing it by hand, so a reviewer (or a new session) reads `docs/architecture/` instead of the code.

## What already exists
Checked in the code on 2026-09-21, on `main` at `ff22f8f`.
- Two `DbContext` types: `IdentityModuleDbContext` (schema `identity`, `src/Modules/Identity/Simulab.Identity.Infrastructure/Persistence/IdentityModuleDbContext.cs:18`) and `JobsDbContext` (schema `jobs`, `src/BuildingBlocks/Simulab.Jobs/Persistence/JobsDbContext.cs:14`). Both take `ICurrentTenant` besides the options; the tenant query filters capture `this`, so a null tenant does not break building the model (`src/BuildingBlocks/Simulab.Persistence/ModuleDbContext.cs:51`).
- The Api host calls `AddOpenApi()` and serves `/openapi/v1.json` only in Development (`src/Hosts/Simulab.Api/Program.cs:18`, `:65`). **No OpenAPI document exists on disk**, so the template's route map would find nothing.
- Endpoints live under `/api/v1` (`src/Hosts/Simulab.Api/Program.cs:73`): `system` in the host, `identity` in `Simulab.Identity.Api`. The Web host's `/account` endpoints are outside `/api/v1` and outside the OpenAPI document.
- `SolutionLayoutTests` accepts projects only under `src/<group>/`, `tests/<group>/` or the two test-root projects (`tests/Simulab.ArchitectureTests/SolutionLayoutTests.cs:8`). A project under `tools/` fails it today.
- The template references `Microsoft.EntityFrameworkCore.Relational`, which has no `PackageVersion` in `Directory.Packages.props`; `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.3 is there and brings it transitively.
- The template derives the module name from the context type: `IdentityModuleDbContext` would become `IdentityModule`.
- The definition of done (`.claude/rules/agile/definition-of-done.md`) and `docs/agile/workflow.md` already mention `DocGen --check` "when the project has them". `docs/agile/profile.md` left the DocGen sentence out on purpose until this item (`docs/agile/retro-log.md:199`, `:261`).
- No item since the bootstrap touched `docs/architecture/` or `tools/`: neither exists.

## Users and use cases
- UC1 A developer (or Claude in a new session) opens `docs/architecture/README.md` and reaches, per module, the entity diagram, the data dictionary and the route map, plus the module dependency diagram.
- UC2 `/agile:ship` runs the generator after the full suite and commits the refreshed files with the item.
- UC3 `/agile:ship` runs `--check`; when the code changed and the docs did not, it fails and names the stale files.

## Business rules
- BR1 Every file under `docs/architecture/` is generated; none is edited by hand. Each file says so in its header.
- BR2 The generator produces the four documents: `modules.md`; `<Module>/entities.md` and `<Module>/data-dictionary.md` for every `DbContext`; `<Area>/routes.md` for every area under `/api/v1/`.
- BR3 The module name is the context type name without `DbContext`, `Context` and a trailing `Module` (`IdentityModuleDbContext` → `Identity`, `JobsDbContext` → `Jobs`).
- BR4 The route map covers only the API described by the OpenAPI document (`/api/v1/...`); Blazor pages and the Web host's `/account` endpoints are not listed.
- BR5 The OpenAPI document is produced by the Api build into `docs/api/Simulab.Api.json` and committed with the change that produced it (rule `git`, generated files).
- BR6 `--check` exits 1 and lists the stale files when any generated file differs from what the code produces now; exits 0 and writes nothing otherwise. It never needs a database, a container or a running host.
- BR7 The check runs in `/agile:ship` only (definition of done), not in the Stop hook and not in the test suite.
- BR8 A second run with no code change writes nothing (output is deterministic: stable ordering, `\n` line endings).

## Screens and API
No screen and no endpoint change. New files only:
- `tools/Simulab.DocGen/` (console tool, never deployed), listed in `Simulab.slnx` under `/tools/`.
- `docs/api/Simulab.Api.json` (generated at build).
- `docs/architecture/README.md`, `modules.md`, `Identity/{entities,data-dictionary}.md`, `Jobs/{entities,data-dictionary}.md`, `Identity/routes.md`, `System/routes.md`.

## Acceptance criteria
- AC1 (BR2, UC1) Given the solution builds, when the generator runs, then `docs/architecture/` holds `README.md`, `modules.md`, `Identity/entities.md`, `Identity/data-dictionary.md`, `Jobs/entities.md`, `Jobs/data-dictionary.md`, `Identity/routes.md` and `System/routes.md`, and the README links each of them.
- AC2 (BR3) Given `IdentityModuleDbContext` and `JobsDbContext`, when the module name is derived, then it is `Identity` and `Jobs`.
- AC3 (BR2) Given the Identity model, when the data dictionary is rendered, then every table of schema `identity` appears with its columns, keys and indexes; and given a test model with a unique index built by `TenantIndexBuilderExtensions` (no real entity uses it today), the index line shows `NULLS NOT DISTINCT`.
- AC4 (BR4, BR5) Given the Api is built, when the build finishes, then `docs/api/Simulab.Api.json` exists and the route map lists every `/api/v1/identity/...` and `/api/v1/system/...` operation and nothing outside `/api/v1/`.
- AC5 (BR6) Given the docs are up to date, when `--check` runs, then it exits 0 and no file changes.
- AC6 (BR6) Given a generated file differs from the code (e.g., a column added to the model), when `--check` runs, then it exits 1 and names that file.
- AC7 (BR8) Given no code change, when the generator runs twice, then the second run reports 0 files written.
- AC8 (BR2) Given the project references under `src/`, when `modules.md` is rendered, then every project in `src/` appears once, grouped by `Hosts`, `BuildingBlocks` and `Modules`, with an edge per `ProjectReference`.
- AC9 (layout) Given `tools/Simulab.DocGen` is in the solution under `/tools/`, when the architecture tests run, then `SolutionLayoutTests` and the vocabulary test are green.
- AC10 (BR7) Given the rules and the profile, when read after this item, then `docs/agile/profile.md` carries the DocGen sentence and `/agile:ship`'s definition-of-done line applies (the project now has generated docs).
- Localization: no UI text is added or changed; the missing-key test stays green.

## Decisions
- 2026-09-21 - Generate all four documents (entities, data dictionary, routes, modules) - owner; zero marginal cost once the tool exists.
- 2026-09-21 - The hand-written C4 overview (context and containers) comes later, as an idea - owner; with a single business module today it adds little; more useful once the exam and coach modules exist.
- 2026-09-21 - The route map covers only `/api/v1` from the OpenAPI document - owner; the template already does it; Blazor pages would need a second generator to maintain.
- 2026-09-21 - `--check` runs only in `/agile:ship` - owner; the definition of done already asks for it, and building and running the tool would break the 30 s budget of the Stop hook's unit tests.
- 2026-09-21 - The OpenAPI document comes from `Microsoft.Extensions.ApiDescription.Server` 10.0.12 (MIT, latest stable on nuget.org on 2026-09-21), written to `docs/api/Simulab.Api.json` at the Api build and committed - owner; the owner's yes for the package (rule `build-config`) is given here. Risk: build-time generation starts the Api's host builder without Redis or PostgreSQL; if it fails and cannot be fixed without touching production startup, the fallback is the existing `/openapi/v1.json` test writing the file, recorded as a change note.
- 2026-09-21 - Business descriptions per table and column (`HasComment`) stay out, as an idea - owner; it needs a migration and text review; the dictionary is useful without it.
- 2026-09-21 - `tools/` becomes an allowed root folder in `SolutionLayoutTests`, and the tool is listed in `Simulab.slnx` under `/tools/` - technical; the solution build then compiles the tool, so it cannot rot unnoticed.
- 2026-09-21 - Drop the template's explicit `Microsoft.EntityFrameworkCore.Relational` reference - technical; it has no central version and arrives through `Npgsql.EntityFrameworkCore.PostgreSQL`.
- 2026-09-21 - Strip a trailing `Module` from the derived module name (BR3) and skip `bin/` and `obj/` when searching for the OpenAPI document - technical; otherwise the folder is `IdentityModule` and a stale copy under `obj/` could win.
- 2026-09-21 - The tool's own logic (module name, stale detection, deterministic output) is tested in `tests/Simulab.ArchitectureTests` against small in-memory inputs, not by running the tool - technical; keeps the tests inside the unit budget.
- 2026-09-21 - The app manual is not touched - technical; nothing visible to users changes (definition of done).

## Out of scope
- The hand-written C4 overview (idea to capture).
- `HasComment` descriptions on tables and columns (idea to capture).
- Blazor pages and the Web host's `/account` endpoints in the route map.
- Running `--check` in the Stop hook or the test suite.
- Any change to production behavior, screens or the app manual.

## Open questions
