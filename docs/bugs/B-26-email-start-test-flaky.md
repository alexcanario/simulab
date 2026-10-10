---
bug: B-26
feature: F-66
status: idea
board: 160
severity: medium
---
# EmailProviderStartTests fails with ObjectDisposedException in the full gate

Technical terms: [glossary](../glossary.md)

## What happens
`Simulab.Api.Tests.EmailProviderStartTests.Start_CloudWithAzureButNoEndpoint_RefusesAndNamesTheKey(environment: "Production")` failed in 2 of 4 runs of `gate.js ship` on `feature/F-94` (2026-10-09), with `System.ObjectDisposedException: Cannot access a disposed object. Object name: 'IServiceProvider'` where the test expects an `OptionsValidationException`. It passed alone in every run: 3 runs of `Simulab.Api.Tests` and 8 concurrent runs of the class. The failures were on code this branch does not touch (`tests/Hosts/Simulab.Api.Tests` is unchanged), so the full suite is not reliably green.

Evidence: the gate logs `C:\Users\alexc\AppData\Local\Temp\agile-canary\gate-logs\20261009-2220*-ship.log` (the 2nd and 3rd runs; the 4th was green). Message line: `Expected a <Microsoft.Extensions.Options.OptionsValidationException> to be thrown, but found <System.ObjectDisposedException>` at `GetRequiredService` in `EmailProviderStartTests`.

Cause not verified: measure it at `/agile:refine`. The suspicion is that the class shares one `ApiFactory` through `IClassFixture<ApiFactory>`, and a host whose start fails disposes what it shares. F-94 hit the same symptom in its own `OpenIddictClientStartTests` and removed it by giving each test its own factory.

## Start
- Depends on: nothing.
- Waits on (to start): nothing.
- Needed to validate: nothing; the proof is N consecutive green full-gate runs (clean build before each, as the B-19 loop).
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: any item that does not touch `tests/Hosts/Simulab.Api.Tests`.
- Related: B-19 (done) fixed other tests that lose races under the parallel load of the full suite; this one has another cause and was not on its list.

## Expected
The start tests of the Api host give the same result alone and in the full suite.

## Cause
<!-- Confirmed in code: file and line. Never a guess. -->

## Fix
<!-- What changes. Keep it minimal. -->

## Regression test
- <Test name> — <project>

## Open questions
- (none)

## Validation script
1. <Step> → <expected result>

## Delivery
- Branch: bug/B-26
- Merge: <commit>
