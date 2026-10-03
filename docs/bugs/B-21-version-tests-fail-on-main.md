---
bug: B-21
feature: -
epic: Foundation and identity
status: idea
board: 108
severity: high
---
# The app version tests fail on main and block every ship

## What happens
1. Run `dotnet test tests/Simulab.ArchitectureTests --filter "FullyQualifiedName~AppVersionTests"` on `main` (`82ba1da`, clean checkout).
2. Two of the four tests fail: `AppVersionTests.ApiProject_CarriesOneSemanticVersion` and `AppVersionTests.ApiAssembly_CarriesTheVersionInItsInformationalVersion` (`Failed: 2, Passed: 2, Total: 4`).
3. Both expect a single `<Version>` in `Simulab.Api.csproj`. Since `5185ec3` (`chore(F-47): bump app version to 0.3.0`) the version `0.3.0` lives in `Directory.Build.props`, and the csproj has none.
4. The full gate (`gate.js ship`) therefore ends `agile gate RED: tests failed (Simulab.slnx): ...` and no item can ship. Found while shipping F-48, whose own tests (Web.Tests 820 of 820) are green.

History: `6e15e20` feat(F-60) gave the app one version on `Simulab.Api`; `baca85b` test(F-46) let the version tests follow the bumped version; `5185ec3` chore(F-47) moved the version.

## Start
- Depends on: nothing.
- Waits on (to start): an owner decision at `/agile:refine` — the tests follow the `Directory.Build.props` convention of the plugin's `version.js`, or the version goes back to `Simulab.Api.csproj` — owner. Not decided.
- Needed to validate: nothing.
- Suggested path: `/agile:refine` → `/agile:build`. It blocks the ship of F-48, so do it first.
- Parallel with: none; F-48 waits on it to ship.

## Expected
`gate.js ship` is green on `main`: the version tests and the place the version lives agree.

## Cause
Cause not verified: measure it at `/agile:refine`. Seen: `Directory.Build.props:4` holds `<Version>0.3.0</Version>` and `Simulab.Api.csproj` holds none, while `AppVersionTests.cs:33` and `:75` read the `Version` element of `Simulab.Api.csproj`.

## Fix
Decided at `/agile:refine`.

## Regression test
- Decided at `/agile:refine`.

## Open questions
- (none)

## Validation script
1. Decided at `/agile:refine`.

## Delivery
- Branch: bug/B-21
- Merge:
