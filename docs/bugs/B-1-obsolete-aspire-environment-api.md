---
bug: B-1
feature: F-4
status: done
board: 718
severity: low
---
# Replace the obsolete Aspire environment API in AppHostModelTests

Technical terms: [glossary](../glossary.md)

## What happens
1. Build `tests/Hosts/Simulab.AppHost.Tests` → 2 warnings `CS0618`: `ResourceExtensions.GetEnvironmentVariableValuesAsync(IResourceWithEnvironment, DistributedApplicationOperation)` is obsolete, "Use ExecutionConfigurationBuilder instead."
2. The 2 warnings were accepted in `.claude/agile/warnings-baseline.json` by the owner on 2026-09-18, during F-12, so the Stop gate does not block other work.

## Expected
The AppHost tests read the Api environment through the supported API, with no `CS0618`, and the baseline entry is removed.

## Cause
`tests/Hosts/Simulab.AppHost.Tests/AppHostModelTests.cs:73` and `:86` call `GetEnvironmentVariableValuesAsync` (added by the F-4 retro, commit `e03cb5c`). Aspire 13.4.6 marks it obsolete in favor of `ExecutionConfigurationBuilder`. No other call to the obsolete method exists in the solution (checked: only this file).

## Fix
Central package management floats `Aspire.Hosting` itself to **13.5.4** for this test project (pulled in transitively by `Aspire.Hosting.Redis` 13.5.4, F-5), even though `Aspire.Hosting.Testing` stays pinned at 13.4.6 — so the real obsolete-vs-current API is 13.5.4's, not 13.4.6's. Confirmed by building: the simple `new DistributedApplicationExecutionContext(DistributedApplicationOperation.Publish)` throws at runtime ("`IServiceProvider` is not available"), and `DistributedApplicationExecutionContextOptions.ServiceProvider` is itself obsolete in 13.5.4 in favor of `Services`. Added a helper in `AppHostModelTests`:
```csharp
private async Task<Dictionary<string, string>> EnvironmentOfAsync(IResource resource)
{
    var options = new DistributedApplicationExecutionContextOptions(DistributedApplicationOperation.Publish)
    {
        Services = _builder!.Services.BuildServiceProvider(),
    };

    var result = await ExecutionConfigurationBuilder.Create(resource)
        .WithEnvironmentVariablesConfig()
        .BuildAsync(new DistributedApplicationExecutionContext(options));

    return result.EnvironmentVariables.ToDictionary(pair => pair.Key, pair => pair.Value);
}
```
`IExecutionConfigurationResult.EnvironmentVariables` is `IEnumerable<KeyValuePair<string, string>>` of the **processed** values (the same shape `GetEnvironmentVariableValuesAsync` returned) — the existing assertions (`ContainKey`, `StartWith`, `Contain`, `NotContain`) do not change. No new package.

## Regression test
- `Api_GetsTheSmtpAddressFromTheMappedEndpoint` and `Api_GetsTheVerificationLinkOfTheWeb` — `Simulab.AppHost.Tests`; both already exist and pass today (the bug is a compiler warning, not a behavior defect, so there is no red test to show — the gate check is the warning count and the baseline entry going back to 0).

## Decisions
- 2026-09-18 — Fix only the obsolete call and remove the baseline entry; no other Aspire API changed — owner, question 1.

## Out of scope
- Any other Aspire package upgrade or API modernization.

## Open questions
- (none)

## Validation script
1. `dotnet build Simulab.slnx` → 0 warnings, no `CS0618`.
2. `dotnet test tests/Hosts/Simulab.AppHost.Tests` → 6 passed, 0 failed.
3. Open `.claude/agile/warnings-baseline.json` → `{}`.

## Delivery
- Branch: `bug/B-1` (deleted after merge)
- Merge: the `--no-ff` merge commit "Merge bug/B-1: replace the obsolete Aspire environment API (AB#718)" on `main`, 2026-09-18 (`7231e41`)
- Validated on screen by the owner: 2026-09-18
- Tests: full suite 268 passed, 0 failed, 27 s; full build 9 s, 0 warnings; `.claude/agile/warnings-baseline.json` back to `{}`
