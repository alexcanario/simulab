---
bug: B-1
feature: F-4
status: idea
board: 718
severity: low
---
# Replace the obsolete Aspire environment API in AppHostModelTests

## What happens
1. Build `tests/Hosts/Simulab.AppHost.Tests` → 2 warnings `CS0618`: `ResourceExtensions.GetEnvironmentVariableValuesAsync(IResourceWithEnvironment, DistributedApplicationOperation)` is obsolete, "Use ExecutionConfigurationBuilder instead."
2. The 2 warnings were accepted in `.claude/agile/warnings-baseline.json` by the owner on 2026-09-18, during F-12, so the Stop gate does not block other work.

## Expected
The AppHost tests read the Api environment through the supported API, with no `CS0618`, and the baseline entry is removed.

## Cause
`tests/Hosts/Simulab.AppHost.Tests/AppHostModelTests.cs:73` and `:86` call `GetEnvironmentVariableValuesAsync` (added by the F-4 retro, commit `e03cb5c`). Aspire 13.4.6 marks it obsolete.

## Fix
To refine: use `ExecutionConfigurationBuilder.Create(resource).WithEnvironmentVariablesConfig().BuildAsync(...)` and read `EnvironmentVariables`. Not verified: whether publish-mode values still carry the expressions the tests check (`mailpit.bindings.smtp`, `web.bindings.https`).

## Regression test
- `Api_GetsTheSmtpAddressFromTheMappedEndpoint` and `Api_GetsTheVerificationLinkOfTheWeb` — Simulab.AppHost.Tests; the gate check is the baseline entry going back to 0.
