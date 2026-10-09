---
feature: F-94
epic: Cloud hosting and operations
status: building
board: 140
version: 1
---
# Every setting a host requires has a source in the publish

Technical terms: [glossary](../glossary.md)

## Summary
A test, in the app host tests, that lists the settings the Web and the Api require outside Development and asserts that each has a value in the publish model of Staging and of Production, or in the host's own committed `appsettings.json` / `appsettings.<Environment>.json`, so the next missing one fails the build and not the staging. Raised by the retro of F-64 (2026-10-08): the Web had `Authentication:OpenIddict:ClientId` only in `appsettings.Development.json`, `docs/infra.md` said "cloud: same value" but nothing set it, and every sign-in got a 401 `invalid_client` ("The mandatory 'client_id' parameter is missing"). Evidence: the Web log line added in `b66539e` on the first staging. F-64 D23 fixed that one key with `AzurePublishModelTests.Publish_TheWebIsToldTheClientIdTheApiSeeds`; this item generalizes it.

## Start
- Depends on: F-64 (done).
- Waits on (to start): nothing.
- Needed to validate: nothing; the test itself is the proof (the owner runs one test command, see `## Validation script`).
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: any item that does not edit `tests/Hosts/Simulab.AppHost.Tests` or the Web's `OpenIddictClientOptions`. F-73 (validating) edits `AzureDeployment.cs`; this item edits it only if the new test finds a real gap, and then by one line.

## Goal
A key a host needs outside Development that no publish value and no committed host setting provides fails `dotnet test` on every machine, naming the host, the environment and the key, instead of failing as a 401 on the first staging.

## What already exists (verified 2026-10-09, today's code)
- There is no list of required settings. Each host reads them in its own way: `[Required]` data annotations on `EmailOptions` and `AiOptions` (both `ValidateOnStart`); `IValidateOptions` for email, TOTP and Google; raw `configuration[key] ?? throw` for `Authentication:OpenIddict:ClientSecret` (`IdentityModule.cs:315`, `ClientRateLimiter.cs:125`); `GetConnectionString(...) ?? throw` for `simulab` in `Program.cs` of the Api; OpenIddict certificates in `OpenIddictCertificates.cs`.
- The Web binds `OpenIddictClientOptions` (`Program.cs:47`) with no `[Required]` and no validation at start: that is why the F-64 gap became a 401 at sign-in and not a refusal to start.
- The publish model is built in `AzureDeployment.cs`: `ConfigureHosts` sets the cloud environment variables of each host; `AddResources` and `WithReference` add the connection strings; `AppHost.cs` sets `Identity__VerificationUrl`, `PasswordResetUrl`, `ForgotPasswordUrl`, `SignUpUrl` on the Api for every mode.
- The AppHost's own `appsettings.<Environment>.json` is not a source for the hosts' settings: it only feeds `ForwardedHeaders` (`AddForwardedHeaders`). The sources a deployed host really has are the publish variables and its own `appsettings.json` / `appsettings.<Environment>.json` (the cloud sets `ASPNETCORE_ENVIRONMENT`).
- `VerificationEmailOptions.VerificationUrl` defaults to `https://localhost:7125/verify-email`: a cloud Api that missed `Identity__VerificationUrl` would send a localhost link with no error. Same class of gap as F-64, so these four keys are on the explicit list (BR2).
- The existing test builds the publish model with `DistributedApplicationTestingBuilder` and reads a host's variables with `ExecutionConfigurationBuilder` (`AzurePublishModelTests.cs`, the F-64 D23 test). `Simulab.AppHost.Tests` references only the AppHost project; the architecture tests reference the hosts but not the AppHost (`SolutionAssemblies.ExemptProjects`).
- Items that touched this code since F-64: F-66 (email settings), F-73 (replicas, certificates), F-112 (privacy text only).
- Not verified: whether the publish model exposes the `ConnectionStrings__*` keys that `WithReference` adds (they may be secret references in the Bicep). First step of the build; a key the model cannot show goes to the list with source `Infrastructure` and a reason.

## Users and use cases
- UC1 A developer adds a `[Required]` option with no default (or an explicit-list key) and forgets to set it in the publish; `dotnet test` fails naming host, environment and key.
- UC2 A developer adds a new options class with a `[Required]` property and registers it in no list; the test fails asking to name its host or exempt it with a reason.
- UC3 A host started in Staging or Production without a required option refuses to start with the key named (the Web, for `OpenIddictClientOptions`), instead of failing later at sign-in.

## Business rules
- BR1 The check runs on the publish model of Staging and of Production (both are built today by the tests; their settings differ, for example Redis).
- BR2 A setting is required in the cloud when: (a) it is a `[Required]` property of an options class a host binds and its default is null or empty (a non-empty code default is its own source); or (b) it is on the explicit list of that host, because it is read outside an options class or because its development default is wrong in the cloud. Initial explicit list, each entry verified against the code at build:
  - Api: `ConnectionStrings:simulab`, `ConnectionStrings:redis`, `ConnectionStrings:keys`, `ConnectionStrings:keyvault`, `DataProtection:KeyVaultKeyId`, `Authentication:OpenIddict:ClientSecret`, `Identity:VerificationUrl`, `Identity:PasswordResetUrl`, `Identity:ForgotPasswordUrl`, `Identity:SignUpUrl`, `Email:Provider`, `Email:AzureCommunicationServices:Endpoint`.
  - Web: `ConnectionStrings:redis`, `ConnectionStrings:keys`, `DataProtection:KeyVaultKeyId`, `Authentication:OpenIddict:ClientSecret`, and the service-discovery address of the Api.
  - Key Vault, listed and not verified (BR4): `OpenIddict:SigningCertificate`, `OpenIddict:EncryptionCertificate` (Api).
- BR3 Which host binds which options class is named in the test (a short list of types per host). A completeness rule scans the assemblies the Api and the Web reference: an options class (it has `SectionName`) with a `[Required]` property that is on no host's list, and is not exempt with a written reason, fails the test (UC2).
- BR4 A required key has a source when one of these holds, checked in this order: the host's variables in the publish model have it with a value; the host's committed `appsettings.json` or `appsettings.<Environment>.json` has it non-empty; or it is listed with source `KeyVault` (the model cannot see the vault, so it is not verified; the Api already refuses to start without the certificates, F-64 BR7 and F-73 BR3).
- BR5 The failure message names the host, the environment, the key and the sources looked at, and lists every missing key of that run at once, not the first.
- BR6 The Web's `OpenIddictClientOptions` (`ClientId`, `ClientSecret`) is `[Required]`, validated with data annotations and `ValidateOnStart`: outside Development the Web refuses to start with the key named. Development keeps working: `appsettings.Development.json` has both.
- BR7 Settings required only while a switch is on (Google sign-in, TOTP) are out: both switches are off in v1 and their own validators check them at start (`IdentityModule.cs:171`, `:191`).
- BR8 Rule of presence: the scan must find at least `OpenIddictClientOptions` and `EmailOptions` and at least one key per host; finding fewer fails, so a scan that silently matches nothing cannot pass.
- BR9 The detection and the source rules have unit tests on a small fake model and sample settings, so each failure is shown without breaking the real model.

## Screens and API
- No screen, no endpoint. Tests in `tests/Hosts/Simulab.AppHost.Tests`; the Web options class gets data annotations and `ValidateOnStart` (BR6).

## Acceptance criteria
- AC1 (BR2, BR4, BR5, UC1) Given a required key with no value in the publish model and none in the host's committed settings, when the test runs, then it fails naming host, environment and key.
- AC2 (BR2, BR4) Given today's code with this item, when the test runs on Staging and Production, then it passes, and `Authentication:OpenIddict:ClientId` is found in the Web's publish variables.
- AC3 (BR3, UC2) Given an options class with a `[Required]` property that no host list names, when the architecture rule runs, then it fails naming the class and the property.
- AC4 (BR2) Given a `[Required]` property with a non-empty default, when the test runs, then it needs no source.
- AC5 (BR6, UC3) Given the Web started in Staging with no `Authentication:OpenIddict:ClientId`, when the host starts, then it refuses with a message naming the key.
- AC6 (BR6) Given the Web in Development, when it starts, then it starts (both keys come from `appsettings.Development.json`).
- AC7 (BR4) Given a key listed with source `KeyVault`, when the test runs, then it is reported as not verified and does not fail.
- AC8 (BR8) Given a scan that finds fewer than the known options classes, when the test runs, then it fails.
- AC9 (BR9) The unit tests of the detection rules were seen failing on a fake model with a missing key before the real check was wired.
- AC10 Localization: no UI text is added; the missing-key test stays green.

## Criterion → test
| Criterion | Test |
|---|---|
| AC1 | pending |
| AC2 | pending |
| AC3 | pending |
| AC4 | pending |
| AC5 | pending |
| AC6 | pending |
| AC7 | pending |
| AC8 | pending |
| AC9 | pending |
| AC10 | pending |

## Decisions
- 2026-10-09 — Approved by the owner ("aprovo F-94") — gate 1.
- 2026-10-09 — Declare the requirement on the options classes (`[Required]`, `ValidateOnStart`) plus a short explicit list for what is read outside options — owner's choice (recommended); a new required option fails the build alone, and the host also refuses to start. Rejected: a list only (a key nobody adds escapes), and booting the real hosts (needs database and Redis, over the 2 min budget).
- 2026-10-09 — Key Vault secrets are listed and not verified — owner's choice (recommended); the model cannot see the vault and the Api already refuses to start without the certificates.
- 2026-10-09 — The check lives in `Simulab.AppHost.Tests`, which gains project references to the Api and the Web to read their options types — Claude; the publish model is only reachable there, and the architecture tests are forbidden to reference the AppHost. Fallback if the references clash with Aspire's generated `Projects` types: load the two assemblies by name from the build output, as `SolutionAssemblies` does.
- 2026-10-09 — Environments Staging and Production, not only Staging — Claude; Production's publish model already builds in the tests and differs (managed Redis).
- 2026-10-09 — The host's `appsettings` files are read from the project folder through Aspire's project metadata — Claude; the summary's "app host's `appsettings.<Environment>.json`" is not a source for the hosts (see What already exists).
- 2026-10-09 — The four `Identity:*Url` keys are on the explicit list although they have a development default — Claude; the default is a localhost link, silently wrong in the cloud.
- 2026-10-09 — No new package — Claude; the test uses the packages `Simulab.AppHost.Tests` already has.

## Out of scope
- Settings required only while a switch is on (Google, TOTP), BR7.
- Checking the value of a setting beyond "has a value" (the certificates' validity and key usage stay with `OpenIddictCertificates`).
- Verifying the Key Vault's contents (needs a deployed vault; `docs/infra.md` and F-64 own it).
- Rewriting the raw `configuration[...] ?? throw` reads as options classes; this item lists them instead.
- Updating `docs/infra.md`'s settings table to be generated from the list.

## Open questions
- (none)

## Change notes

## Validation script
Needed to validate: nothing. No screen, so no app host and no sign-in.

1. In the item's worktree, run the check. Git Bash and PowerShell 7 take the same command:
   `dotnet test tests/Hosts/Simulab.AppHost.Tests/Simulab.AppHost.Tests.csproj --filter "FullyQualifiedName~RequiredSettings"`
   Expected: `Failed: 0`, with the count reported by the build.
2. Make it fail on purpose: in `src/Hosts/Simulab.AppHost/AzureDeployment.cs`, comment out the line `.WithEnvironment("Authentication__OpenIddict__ClientId", "simulab-web")` and repeat step 1. Expected: a failure naming the Web, Staging and `Authentication:OpenIddict:ClientId`.
3. Undo the edit (`git checkout src/Hosts/Simulab.AppHost/AzureDeployment.cs`) and repeat step 1: green again.
