---
bug: B-26
feature: F-66
status: refining
board: 160
severity: medium
---
# EmailProviderStartTests fails with ObjectDisposedException in the full gate

Technical terms: [glossary](../glossary.md)

## What happens
`Simulab.Api.Tests.EmailProviderStartTests.Start_CloudWithAzureButNoEndpoint_RefusesAndNamesTheKey(environment: "Production")` failed in 2 of 4 runs of `gate.js ship` on `feature/F-94` (2026-10-09), with `System.ObjectDisposedException: Cannot access a disposed object. Object name: 'IServiceProvider'` where the test expects an `OptionsValidationException`. It passed alone in every run: 3 runs of `Simulab.Api.Tests` and 8 concurrent runs of the class. The failures were on code this branch does not touch (`tests/Hosts/Simulab.Api.Tests` is unchanged), so the full suite is not reliably green.

Evidence: the gate logs `C:\Users\alexc\AppData\Local\Temp\agile-canary\gate-logs\20261009-2220*-ship.log` (the 2nd and 3rd runs; the 4th was green). Message line: `Expected a <Microsoft.Extensions.Options.OptionsValidationException> to be thrown, but found <System.ObjectDisposedException>` at `GetRequiredService` in `EmailProviderStartTests`.

Cause not confirmed for this class: measure it at `/agile:refine`. What F-94 learned (2026-10-10): the same `ObjectDisposedException` hit its own `OpenIddictClientStartTests` in a fixture-free version too, with the stack inside `WebApplicationFactory.DeferredHost.StartAsync`. So the suspicion that the shared `IClassFixture<ApiFactory>` is the cause is wrong or incomplete: a start that fails (here `ValidateOnStart`) sometimes surfaces as `ObjectDisposedException` from the framework instead of the validation error. F-94 stopped asserting on a failed host start and checks the registration with `IStartupValidator`. F-93 saw the same failure on this test (`docs/infra.md`, Measured times).

## Start
- Depends on: nothing.
- Waits on (to start): nothing.
- Needed to validate: nothing from a person or an environment; the proof is 5 consecutive green full-gate runs (clean build before each, as the B-19 loop), run by the build.
- Suggested path: `/agile:build B-26` (test-only fix, the cause is confirmed). With F-95 in `validating` a second item in `building` needs `--worktree`.
- Parallel with: any item that touches neither `tests/Hosts/Simulab.Api.Tests` nor `tests/BuildingBlocks/Simulab.Email.Tests`.
- Related: B-19 (done) fixed other tests that lose races under the parallel load of the full suite; this one has another cause and was not on its list. F-94 (done) applied the same fix to `OpenIddictClientStartTests` in the Web.

## Expected
The email start tests of the Api host give the same result alone and in the full suite, and nothing in them depends on how a failed host start surfaces.

- AC1 Given a `Staging` or a `Production` Api host that started with every cloud setting, when the `IValidateOptions<EmailOptions>` it registered validates options with provider `Smtp`, then the result is a failure and its message names `Email:Provider` (F-66 AC2, BR4: the host passes `cloudEnvironment: !IsDevelopment()`).
- AC2 Given the same host, when the registered validator validates options with provider `AzureCommunicationServices` and an empty endpoint, then the failure names `Email:AzureCommunicationServices:Endpoint` (F-66 AC4).
- AC3 Given a `Development` Api host, when the registered validator validates options with provider `Smtp` and a sender address, then the result is a success (the rule is off outside the cloud).
- AC4 Given the email registration of a cloud environment, when `IStartupValidator.Validate()` runs for provider `Smtp`, and for provider `AzureCommunicationServices` with an empty endpoint, then each throws an `OptionsValidationException` naming `Email:Provider` and `Email:AzureCommunicationServices:Endpoint` respectively: the refusal is registered to run at start (`ValidateOnStart`), which `IOptions<T>.Value` does not prove.
- AC5 Given the same registration with every cloud key, when `IStartupValidator.Validate()` runs, then it does not throw.
- AC6 Given the whole solution, when `gate.js ship` runs 5 times in a row with a clean build before each, then all 5 are green (baseline: 2 red in 4 on `feature/F-94`, 2026-10-09).
- AC7 No production code changes: the diff touches only files under `tests/` and this item's file. No new UI text: the localization criterion does not apply.

## Cause
Confirmed (2026-10-11) in the code and in the framework source; the race itself was not reproduced (see "Measured").

- The test asserts on the failed start of a `WebApplicationFactory`: `tests/Hosts/Simulab.Api.Tests/EmailProviderStartTests.cs:28-37` and `:44-54` read `host.Services`, which starts the Api host, and expect the `OptionsValidationException` of `ValidateOnStart` (`src/BuildingBlocks/Simulab.Email/EmailServiceCollectionExtensions.cs:26-40`, rules in `EmailOptionsValidator.cs`).
- The framework races on that start. In `Microsoft.AspNetCore.Mvc.Testing` 10.0.12 (`Directory.Packages.props:22`) the factory wraps the host in `DeferredHostBuilder.DeferredHost`; its `StartAsync` runs on the test thread and does `_host.Services.GetRequiredService<IHostApplicationLifetime>()` (dotnet/aspnetcore `release/10.0`, `src/Mvc/Mvc.Testing/src/DeferredHostBuilder.cs:152`), while the application's own start runs on the entry-point thread. When the validation fails there, that thread throws and disposes the host; if the dispose lands before line 152, the test thread gets `ObjectDisposedException` ('IServiceProvider') instead of the validation error, which only reaches it through `EntryPointCompleted` (same file, `:100-111`).
- The gate logs match that stack frame by frame: `ThrowObjectDisposedException` ← `ServiceProvider.GetService` ← `GetRequiredService<T>` ← `DeferredHost.StartAsync` ← `WebApplicationFactory.CreateHost` ← `StartServer` ← `get_Services` ← `EmailProviderStartTests.cs:51` (`gate-logs/20261009-221951-860-ship.log` and `20261009-222348-957-ship.log`).
- It depends on thread timing, so it shows only when the full suite loads the machine: it passes alone, in 8 concurrent runs of the class and in 3 runs of the project.

### Measured (2026-10-11, scratch test, removed)
- 96 failed starts of the same host (`Production`, endpoint empty) in 12-way parallel inside one process: 96 × `OptionsValidationException`, 0 × `ObjectDisposedException`. The race did not show without the load of the whole gate.
- A heavier run (600 starts, 48-way, with busy threads on every core) did not finish in 540 s and was stopped; no result. The cause above rests on the stack and the source, not on a reproduction.

### Duplicates
- The rule (BR4 of F-66) is implemented once, in `EmailOptionsValidator`. The same refusals are already asserted without a host in `tests/BuildingBlocks/Simulab.Email.Tests/EmailProviderRegistrationTests.cs` (`Validate_CloudWithSmtp_NamesTheProvider`, `Validate_CloudWithoutEndpointSenderAndIdentity_NamesEveryMissingKeyAtOnce`, `Validate_EndpointWithoutHttps_IsRefused`), but through `IOptions<EmailOptions>.Value`, which does not prove that `ValidateOnStart` is registered.
- What only the Api tests cover: that `src/Hosts/Simulab.Api/Program.cs:42-45` passes `cloudEnvironment: !builder.Environment.IsDevelopment()` and reads the setting keys the deploy sets.

### Same pattern elsewhere
- `OpenIddictCertificateStartTests` (5 asserts): by reading `IdentityModule.cs:216-255` the certificates are loaded while the services are registered, before `Build()`, so no host exists to dispose and the factory surfaces the entry point's own exception. Not exposed by that mechanism; never seen failing; not measured. Left out of this item.
- `TrustedProxiesTests` (Web) and `DataProtectionWiringTests` (Api) assert `InvalidOperationException` on the extension methods directly, not on a started host: not affected.

## Fix
Test-only; no file under `src/` changes.
- `tests/Hosts/Simulab.Api.Tests/EmailProviderStartTests.cs`: the two theories that start a host that must fail (`Start_CloudWithSmtp_RefusesAndNamesTheProvider`, `Start_CloudWithAzureButNoEndpoint_RefusesAndNamesTheKey`) become tests that start the host successfully and ask its registered `IValidateOptions<EmailOptions>` (AC1, AC2, and AC3 for Development). The two other tests of the class stay as they are.
- `tests/BuildingBlocks/Simulab.Email.Tests/EmailProviderRegistrationTests.cs`: three tests run the registration through `IStartupValidator` (AC4, AC5), the way F-94 did for the Web (`OpenIddictClientStartTests`).
- `OpenIddictCertificateStartTests` stays as it is (decision of 2026-10-11 below).
- Duplicates: none to fix; the rule is implemented once and its other tests already live in `Simulab.Email.Tests`.

## Regression test
- `Simulab.Api.Tests.EmailProviderStartTests` (AC1 to AC3, the replaced tests) and `Simulab.Email.Tests.EmailProviderRegistrationTests` (AC4, AC5).
- "Seen failing before the fix": the race cannot be forced alone (96 of 96 starts gave the right exception, see `## Cause`), so the evidence is the baseline of 2 red in 4 gate runs of 2026-10-09 (stack in `## Cause`) and the AC6 loop. The build says so in `## Delivery` instead of pretending a forced failure.

## Decisions
- 2026-10-11 — Scope: only the two email tests; the five `OpenIddictCertificateStartTests` stay. By `IdentityModule.cs:216-255` the certificates are loaded while the services are registered, before the host exists, so the race cannot reach them; they were never seen failing; moving them would open an `internal` class of `Simulab.Identity.Infrastructure` (no `InternalsVisibleTo` today) to a test with no measured failure. Owner chose the recommended option.
- 2026-10-11 — Coverage of the Api wiring: the tests start the host successfully and call the validator the host registered, instead of provoking a failed start. It keeps F-66 AC2/AC4 proven at the host without a framework race and without a production change. Owner chose the recommended option.
- 2026-10-11 — Proof: 5 consecutive green full-gate runs with a clean build before each, as B-19. No architecture test that forbids asserting on a failed start: the project rule in `.claude/rules/agile/project.md` exists, the sweep found only these two tests, and such a test would pull in the five certificate tests. Owner chose the recommended option.
- 2026-10-11 — Technical (no product impact): no new package; `ApiFactory` and its `IClassFixture` stay as they are (F-94 showed the fixture is not the cause); no new permission, screen or message.

## Out of scope
- The five `OpenIddictCertificateStartTests` and any other test not named above.
- A guard (architecture test) against asserting on a failed factory start; if the pattern comes back it is a new item.
- Any change to `src/` or to how the Api host fails at start.

## Open questions
- (none)

## Validation script
No screen is involved; the owner runs the loop (or reads the build's report of it).
1. In the item's worktree, build clean and run the whole suite, five times in a row → each of the 5 runs ends with 0 failed tests.

```powershell
dotnet build Simulab.slnx --no-incremental
dotnet test Simulab.slnx --no-build
```

```bash
dotnet build Simulab.slnx --no-incremental
dotnet test Simulab.slnx --no-build
```

2. Look at the diff against the main branch → only files under `tests/` and `docs/bugs/B-26-email-start-test-flaky.md` appear, and no file under `src/`.

```powershell
git diff main --stat
```

```bash
git diff main --stat
```

## Events

- 2026-10-11 question — asked 3, from context 0

## Delivery
- Branch: bug/B-26
- Merge: <commit>
