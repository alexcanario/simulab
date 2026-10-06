---
feature: F-56
epic: Foundation and identity
status: validating
board: 94
version: 1
---
# Check that every Api test host pins the user-secret keys

Technical terms: [glossary](../glossary.md)

## Summary
A test host must pin every configuration key a developer's local settings can feed. Found while shipping F-38 (2026-10-01): the `Simulab.Api.Tests` `ApiFactory` did not set `Identity:SeedAdmin:Password` to empty, so on a machine with that user secret (F-52) the host tried to seed an administrator against an unreachable database and 10 tests failed. `SimulabApiFactory` already pins it. A project rule (`.claude/rules/agile/project.md`) now says it; this item turns the rule into a check, and adds `Ai:ApiKey` to the pinned keys.

## Start
- Depends on: nothing.
- Waits on (to start): nothing.
- Needed to validate: nothing (the owner runs one test command, see `## Validation script`).
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: any item outside the test hosts and `docs/infra.md`.
- Evidence: `dotnet test tests/Hosts/Simulab.Api.Tests/Simulab.Api.Tests.csproj` on `main` gave `Failed: 10, Passed: 2` with `Failed to connect to 127.0.0.1:1`; with the key set empty in `ApiFactory`, `Passed: 12`.

## Goal
A new Api test host that forgets to pin a developer-fed key fails the architecture tests on every machine, instead of failing only on the machine of whoever has that key set.

## What already exists (verified 2026-10-04)
- Two Api test hosts, both subclasses of `WebApplicationFactory<Program>` (the Api's `Program`): `tests/Hosts/Simulab.Api.Tests/ApiFactory.cs` and `tests/Simulab.Testing.ApiHost/SimulabApiFactory.cs`. Both set `Identity:SeedAdmin:Password` to empty (F-38 commit `5806d71`, F-52). Neither sets `Ai:ApiKey`.
- Premise update: since F-44 (2026-10-02) the seed admin password is also committed in `src/Hosts/Simulab.Api/appsettings.Development.json`. Test hosts run in the `Development` environment, so an unpinned host now fails on every machine, not only one with the user secret. The check still matters for a new host and for keys that only user secrets carry.
- Only `Simulab.Api` (and the Aspire app host) has a `UserSecretsId`. `Simulab.Web` has none: the Web test hosts (`WebApplicationFactory<Program>` of the Web, used directly in `tests/Hosts/Simulab.Web.Tests`) load no user secrets.
- `Ai:ApiKey` (`AiOptions.ApiKey`) is what the gateway reads; the app host passes it to the Api as `Ai__ApiKey` from its own user secrets (F-41). A developer can also put it in the Api's user secrets. No Api host test exercises a model call.
- The architecture tests look at production assemblies only (`SolutionAssemblies`); `ArchitectureOverviewTests` already checks a project (the app host) from its source.
- `docs/infra.md` line 84 lists `Ai:Anthropic:ApiKey`, a key no code reads; the real key is `Ai:ApiKey` (line 80).

## Users and use cases
- UC1 A developer adds a new Api test host and forgets a pinned key; the architecture tests fail and name the file and the missing key.
- UC2 A developer writes an Api test with `WebApplicationFactory<Program>` directly; the architecture tests fail and point to `ApiFactory` or `SimulabApiFactory`.

## Business rules
- BR1 The pinned keys are `Identity:SeedAdmin:Password` and `Ai:ApiKey`, declared once in the test with the reason for each.
- BR2 An Api test host is a class, in a test project that references `Simulab.Api`, that derives from `WebApplicationFactory<Program>`. Each one sets every pinned key to empty, in its own source.
- BR3 In a test project that references `Simulab.Api`, using `WebApplicationFactory<Program>` without a subclass (a fixture, a field, a parameter, `new`) is refused.
- BR4 Rule of presence: the check finds at least the two known hosts (`ApiFactory`, `SimulabApiFactory`); finding fewer fails, so a scan that silently matches nothing cannot pass.
- BR5 Both known hosts pin `Ai:ApiKey` to empty, next to the seed admin password.
- BR6 Test projects that reference only `Simulab.Web` are not checked (no user secrets there).

## Screens and API
- No screen, no endpoint. Architecture test in `tests/Simulab.ArchitectureTests`, reading test sources.

## Acceptance criteria
- AC1 (BR2, UC1) Given an Api test host that does not set one pinned key, when the architecture tests run, then they fail naming the file and the key.
- AC2 (BR3, UC2) Given a test in an Api test project that uses `WebApplicationFactory<Program>` directly, when the architecture tests run, then they fail naming the file and the two hosts to use instead.
- AC3 (BR4) Given the scan finds fewer than the two known hosts, when the architecture tests run, then they fail.
- AC4 (BR1, BR5) Given today's code with this item, when the architecture tests run, then they pass, and both hosts set `Identity:SeedAdmin:Password` and `Ai:ApiKey` to empty.
- AC5 (BR6) Given a Web test project using `WebApplicationFactory<Program>` of the Web, when the architecture tests run, then it is not reported.
- AC6 `docs/infra.md` no longer lists `Ai:Anthropic:ApiKey`; `Ai:ApiKey` is the only Claude key row, and the rule in `.claude/rules/agile/project.md` names both pinned keys.
- AC7 No UI text is added (localization not applicable); the missing-key test stays green.

## Decisions
- 2026-10-04 — Approved by the owner ("aprovo F-56") — gate 1.
- 2026-10-04 — A source-scanning architecture test over the test projects that reference `Simulab.Api` — owner's choice; small, and leaves the existing tests untouched (a shared helper or a `Testing` environment were heavier).
- 2026-10-04 — Pin `Ai:ApiKey` too — owner's choice; a real Claude key in a test host would spend money, and empty makes the gateway answer `ai.not_configured`.
- 2026-10-04 — Refuse direct `WebApplicationFactory<Program>` use in Api test projects — owner's choice; without a subclass there is nowhere to pin the keys.
- 2026-10-04 — Fix `docs/infra.md` line 84 in this item — owner's choice; one line about the same key this item pins.
- 2026-10-04 — Api test projects are found by their `ProjectReference` to `Simulab.Api.csproj`, not by name — Claude; the Web's `Program` has the same name, and the reference is what decides which one a file means.
- 2026-10-04 — The detection rules (BR2, BR3) get unit tests on sample source text, so AC1–AC3 are shown failing without breaking a real host — Claude; a check that only ever passes proves nothing.
- 2026-10-04 — No new package — Claude; the scan reads text with the BCL.
- 2026-10-06 — The scan skips `Simulab.ArchitectureTests` itself, although it references the Api — Claude; that project holds the sample sources of the check, not a host.
- 2026-10-06 — The two tests that scan real sources (`ApiTestHosts_PinEveryKey`, `ApiTestProjects_NeverUseTheFactoryDirectly`) were seen failing before the hosts pinned `Ai:ApiKey` (2 failed, 12 passed), naming both files and the key — Claude.

## Out of scope
- Switching test hosts to a non-`Development` environment.
- The Aspire app host tests (`Simulab.AppHost.Tests`): they pass their own parameters (`--Ai:ApiKey=`).
- Web test hosts (no user secrets).

## Open questions
- (none)

## Change notes

## Validation script
Needed to validate: nothing. No screen, so no app host and no sign-in.

1. In the worktree `D:\wt\simulab\f-56-test-hosts-pin`, run the check. Git Bash and PowerShell 7 take the same command:
   `dotnet test tests/Simulab.ArchitectureTests/Simulab.ArchitectureTests.csproj --filter "FullyQualifiedName~ApiTestHostPinTests"`
   Expected: `Passed!  - Failed: 0, Passed: 14, Skipped: 0, Total: 14`.
2. Make it fail on purpose: in `tests/Simulab.Testing.ApiHost/SimulabApiFactory.cs`, change `["Ai:ApiKey"] = string.Empty,` to `["Ai:ApiKey"] = "x",` and repeat step 1. Expected: `Failed: 2` (two tests see the same gap) and a message naming `Simulab.Testing.ApiHost\SimulabApiFactory.cs` and `'Ai:ApiKey'`.
3. Undo the edit (`git checkout tests/Simulab.Testing.ApiHost/SimulabApiFactory.cs`) and repeat step 1: green again.
4. Check the docs: `docs/infra.md` has no `Ai:Anthropic:ApiKey` row, and `.claude/rules/agile/project.md` names `Identity:SeedAdmin:Password` and `Ai:ApiKey`.

## Delivery
<!-- Filled by /agile:ship. -->
