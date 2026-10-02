---
feature: F-60
epic: Foundation and identity
status: validating
board: 99
version: 1
---
# The app version lives on the Api project

## Summary
The app has no version today: `grep -rn "<Version>"` over `src`, `tools` and `tests` (`.csproj` and `.props`) finds nothing (2026-10-02). agile@canary expects one `<Version>` on `src/Hosts/Simulab.Api/Simulab.Api.csproj` and nowhere else — never `Directory.Build.props`, `SharedKernel`, a module, a `Contracts` project, `ServiceDefaults` or `AppHost` (`docs/agile/profile.md`) — so `/agile:ship` bumps it once per ship (a feature raises MINOR, a bug raises PATCH). Raised by `/agile:sync` to 0.0.97 (capability since 0.0.79); the owner chose to capture it on 2026-10-02.

## Start
- Depends on: nothing.
- Waits on (to start): nothing.
- Needed to validate: nothing (no screen; the owner reads the csproj and the test result).
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: F-46, F-47, F-50 (none touches `Simulab.Api.csproj`); the architecture test project is shared with F-46, so expect a small merge there.

## Goal
Give the app one version that `/agile:ship` raises on every ship, so a release can be named and traced back to the items it carries.

## Users and use cases
- UC1 The owner ships an item with `/agile:ship` and the app version on `Simulab.Api` rises once (MINOR for a feature, PATCH for a bug).
- UC2 A developer adds `<Version>` anywhere else in the solution and the architecture tests fail, naming the file.

## Business rules
- BR1 The solution carries exactly one `<Version>` element, on `src/Hosts/Simulab.Api/Simulab.Api.csproj`; no other `.csproj` and no `.props` file under `src`, `tests` or `tools` carries one.
- BR2 The first version is `0.1.0`.
- BR3 The version is changed only by `/agile:ship` (BR of the profile); this item writes it once and nothing in the code computes or overrides it.

## Screens and API
- No screen, no endpoint, no error code.

## Acceptance criteria
- AC1 Given the solution, when the architecture tests run, then `Simulab.Api.csproj` carries `<Version>0.1.0</Version>` (BR1, BR2).
- AC2 Given the solution, when the architecture tests run, then no other `.csproj` or `.props` under `src`, `tests` or `tools` carries a `<Version>` element, and a failure names each offending file (BR1, UC2).
- AC3 Given the built Api assembly, when its informational version is read, then it starts with `0.1.0` (BR2; proves MSBuild applies it).
- AC4 No UI text is added, so no resource key changes in pt-BR, pt-PT or en (localization criterion: not applicable, checked by the missing-key test staying green).

## Decisions
- 2026-10-02 — `<Version>` only on `Simulab.Api.csproj`, as the profile and `/agile:sync` ask — owner's answer. `/agile:publish` stays blocked after this item: for `modular-monolith`, `scripts/publish.js` matches every `*.Api.csproj` under `src` (`Simulab.Catalog.Api`, `Simulab.Identity.Api` are module class libraries, not deployables) and does not list `Simulab.Web`, the second deployable host (`docs/agile/profile.md`, line 16). This is a plugin defect, recorded as a `plugin` note in `docs/agile/retro-log.md` at this item's retro; the plugin repository is never edited from here.
- 2026-10-02 — Start at `0.1.0` — owner's answer; it is the bootstrap seed and what `/agile:sync` asks, and no history supports another number.
- 2026-10-02 — An architecture test guards BR1 — owner's answer; it keeps the profile rule from depending on discipline.
- 2026-10-02 — The missing `develop` environment and deploy columns of `docs/infra.md` are F-62 (#101, `idea`), already captured at the 0.0.102 sync; no new item — owner's answer was "capture as an idea", and F-62 already is that idea.
- 2026-10-02 — The test reads the project files as XML (`System.Xml.Linq`, as `SolutionLayoutTests` does) and the assembly's `AssemblyInformationalVersionAttribute` for AC3; no package is added — technical choice.

## Out of scope
- Making `/agile:publish` work for this solution (plugin defect, see Decisions).
- Declaring the `develop` environment and the deploy command (F-62).
- A version on `Simulab.Web` or showing the version on a screen or in `/health`.

## Open questions
- (none)

## Change notes

## Validation script
Needed to validate: nothing (no screen, no account, no app host).

1. Open the worktree `D:\dev\_icontrol\wt\simulab\f-60-app-version-on-api` and read `src/Hosts/Simulab.Api/Simulab.Api.csproj`: line 5 is `<Version>0.1.0</Version>`.
2. List every version element in the solution (expected: exactly one line, the Api project).
   - Git Bash: `grep -rn "<Version>" src tools tests --include=*.csproj --include=*.props | grep -v "/obj/\|/bin/"`
   - PowerShell 7: `Get-ChildItem src,tests,tools -Recurse -Include *.csproj,*.props | Where-Object { $_.FullName -notmatch '\\(obj|bin)\\' } | Select-String '<Version>'`
3. Run the guard: `dotnet test tests/Simulab.ArchitectureTests --filter "FullyQualifiedName~AppVersionTests"` (same command in both shells). Expected: `Passed! - Failed: 0, Passed: 4, Skipped: 0, Total: 4`.
4. Prove the guard bites: add `<Version>9.9.9</Version>` to the `PropertyGroup` of `src/Hosts/Simulab.Web/Simulab.Web.csproj`, repeat step 3. Expected: `NoOtherProjectOrPropsFile_CarriesAVersion` fails and names `src/Hosts/Simulab.Web/Simulab.Web.csproj`. Undo the edit (`git checkout src/Hosts/Simulab.Web/Simulab.Web.csproj`).

## Delivery
<!-- Filled by /agile:ship. -->
