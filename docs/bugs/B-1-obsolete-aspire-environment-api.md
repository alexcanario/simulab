---
bug: B-1
feature: F-4
status: approved
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
`tests/Hosts/Simulab.AppHost.Tests/AppHostModelTests.cs:73` and `:86` call `GetEnvironmentVariableValuesAsync` (added by the F-4 retro, commit `e03cb5c`). Aspire 13.4.6 marks it obsolete in favor of `ExecutionConfigurationBuilder`. No other call to the obsolete method exists in the solution (checked: only this file).

## Fix
Confirmed against the installed `Aspire.Hosting` 13.4.6 assembly (reflection, since the source is not vendored). In both tests, replace:
```csharp
var environment = await api.GetEnvironmentVariableValuesAsync(DistributedApplicationOperation.Publish);
```
with:
```csharp
var result = await ExecutionConfigurationBuilder.Create(api)
    .WithEnvironmentVariablesConfig()
    .BuildAsync(new DistributedApplicationExecutionContext(DistributedApplicationOperation.Publish));
var environment = result.EnvironmentVariables.ToDictionary(kv => kv.Key, kv => kv.Value);
```
`IExecutionConfigurationResult.EnvironmentVariables` is `IEnumerable<KeyValuePair<string, string>>` of the **processed** values (the same shape `GetEnvironmentVariableValuesAsync` returned) — the existing assertions (`ContainKey`, `StartWith`, `Contain`, `NotContain`) do not change. No new package: `ExecutionConfigurationBuilder` is already in `Aspire.Hosting`, referenced transitively through `Aspire.Hosting.Testing` (already in the test project).

## Regression test
- `Api_GetsTheSmtpAddressFromTheMappedEndpoint` and `Api_GetsTheVerificationLinkOfTheWeb` — `Simulab.AppHost.Tests`; both already exist and pass today (the bug is a compiler warning, not a behavior defect, so there is no red test to show — the gate check is the warning count and the baseline entry going back to 0).

## Decisions
- 2026-09-18 — Fix only the obsolete call and remove the baseline entry; no other Aspire API changed — owner, question 1.

## Out of scope
- Any other Aspire package upgrade or API modernization.

## Open questions
- (none)

## Validation script
1. `dotnet build Simulab.slnx` → 0 warnings (no `CS0618`).
2. `dotnet test tests/Hosts/Simulab.AppHost.Tests` → both tests still pass.
3. `.claude/agile/warnings-baseline.json` no longer has an entry for `AppHostModelTests.cs`.

## Delivery
- Branch: `bug/B-1`
- Merge: <commit>
