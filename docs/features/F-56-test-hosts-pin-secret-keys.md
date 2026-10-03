---
feature: F-56
epic: Foundation and identity
status: idea
board: 94
version: 1
---
# Check that every Api test host pins the user-secret keys

Technical terms: [glossary](../glossary.md)

## Summary
A test host must pin every configuration key a developer's user secrets can feed. Found while shipping F-38 (2026-10-01): the `Simulab.Api.Tests` `ApiFactory` did not set `Identity:SeedAdmin:Password` to empty, so on a machine with that user secret (F-52) the host tried to seed an administrator against an unreachable database and 10 tests failed. `SimulabApiFactory` already pins it. A project rule (`.claude/rules/agile/project.md`) now says it; this item turns the rule into a check.

## Start
- Depends on: nothing.
- Waits on: nothing.
- Suggested path: `/agile:refine` → `/agile:build`. Direction to confirm at refinement: an architecture test that finds every `WebApplicationFactory` subclass in the test assemblies and fails when one does not set the key, with a rule of presence (it finds at least the two known factories).
- Parallel with: any item outside the test hosts.
- Evidence: `dotnet test tests/Hosts/Simulab.Api.Tests/Simulab.Api.Tests.csproj` on `main` gave `Failed: 10, Passed: 2` with `Failed to connect to 127.0.0.1:1`; with the key set empty in `ApiFactory`, `Passed: 12`.
