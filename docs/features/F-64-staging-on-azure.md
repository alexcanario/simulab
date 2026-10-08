---
feature: F-64
epic: Cloud hosting and operations
status: done
board: 103
version: 2
---
# Staging runs on Azure for testers in Brazil

Technical terms: [glossary](../glossary.md)

## Summary
Provision the staging environment on Azure Container Apps (Brazil South) with the topology of ADR-0002, and run the first deploy, so testers in Brazil can use the app. Staging is started for test windows and parked outside them with the start and stop commands of `docs/infra.md`. Measure the real monthly cost against the US$ 80 ceiling of ADR-0002; above it, plan B (one Linux VM with Docker Compose) applies. To make a deployed host survive a restart, this item also persists the Data Protection keys, gives OpenIddict real certificates, applies the migrations on start, wires the cloud secrets through Key Vault and sets `ForwardedHeaders` for the cloud ingress. Raised as out of scope by the F-62 refinement, 2026-10-02.

## Start
- Depends on: F-62 (done: ADR-0002, AppHost publish shape, declared commands); F-66 (idea: cloud email; testers must receive the sign-up verification email). The build waits for F-66 (D2).
- Waits on (to start): F-66 done; the owner decides the first test window — owner.
- Needed to validate: an Azure subscription with billing, signed in with `az` on the owner's machine (owner); at least one tester in Brazil (owner).
- Suggested path: `/agile:refine` → `/agile:build` (after F-66).
- Parallel with: any item that does not touch the hosts' `Program.cs`, `AppHost`, `AzureDeployment.cs` or `docs/infra.md` (for example F-79). F-65 depends on this item.

## Goal
Testers in Brazil use a running copy of the app on the internet, and the project learns what that really costs per month before the first release.

## What exists (checked 2026-10-04)
- `AzureDeployment.cs` (F-62) declares, in publish mode only: the Container Apps environment `cae` with its registry, Key Vault `keyvault`, PostgreSQL Flexible Server `postgres` with database `simulab`, and a Redis container for staging. `api` keeps at least one replica, `web` runs zero to one. Only `openiddict-client-secret` is passed as a parameter.
- Migrations, roles and permissions, and the OpenIddict client are applied on start only when `Database:ApplyMigrationsOnStart` is true, which defaults to Development (`src/Hosts/Simulab.Api/Program.cs:116`). A staging deploy today starts on an empty database.
- OpenIddict uses development certificates in every environment (`IdentityModule.cs:220`). A container generates new ones on every start, so every issued token stops working after a restart or a deploy.
- No host calls `AddDataProtection`. The key ring lives in the container's file system and is lost when it restarts: the Web's Redis sessions are encrypted with it (B-3), and so are the Api's ASP.NET Identity tokens (email verification, password reset).
- The Web trusts only `X-Forwarded-For` from listed proxies (`TrustedProxies.cs`, B-4); the Api has no forwarded headers. Container Apps ends TLS at its ingress, so both hosts see `http` and OpenIddict refuses non-HTTPS requests outside Development (`IdentityModule.cs`, `DisableTransportSecurityRequirement` only in Development). How Container Apps' internal ingress reaches the Api is not verified.
- The seeded admin password, the AI key and the email settings are not passed in publish mode (F-62 BR7); `docs/infra.md` lists them as "cloud: Key Vault".
- The health endpoints are mapped only in Development (`ServiceDefaults/Extensions.cs`); Container Apps uses its default TCP probes.
- The Catalog seed (municipal guard catalog) arrives with the migrations.

## Users and use cases
- UC1 The owner provisions staging and runs the first deploy by following the commands in `docs/infra.md`, signed in to the owner's own subscription.
- UC2 A tester in Brazil opens the staging address, creates an account, receives the verification email, signs in and uses the app.
- UC3 The owner starts staging for a test window and parks it afterwards with the declared commands; a parked staging keeps its data.
- UC4 The owner gets a budget alert when the subscription's monthly spend reaches 50 % or 100 % of US$ 80.
- UC5 The owner reads the measured monthly cost in `docs/infra.md` and ADR-0002, and knows whether plan B applies.

## Business rules
- BR1 Sign-up on staging is open, as everywhere else; the address is shared only with the testers. No allowlist.
- BR2 Staging uses the default Azure address (`*.azurecontainerapps.io`) with the platform's TLS; no custom domain.
- BR3 The AI gateway is off on staging: no `Ai:ApiKey`, every model call answers `ai.not_configured` (F-41 BR4).
- BR4 A deployed host keeps working across a restart, a deploy, a park and a start: a signed-in tester stays signed in (within the token lifetimes), a verification or reset link sent before the restart still works, and data stays.
- BR5 Staging applies the module migrations, roles and permissions and the OpenIddict client on start (`Database:ApplyMigrationsOnStart=true` for Staging); production keeps the default until F-65 decides how a release migrates.
- BR6 Both hosts believe `X-Forwarded-For` and `X-Forwarded-Proto` only from the ingress addresses configured in `ForwardedHeaders:KnownProxies` / `KnownNetworks`; with none configured, nothing changes (B-4 BR5 unchanged).
- BR7 No secret value is committed or written into the deployment files: secrets reach the hosts from Key Vault or from deploy-time parameters (F-62 BR7 unchanged).
- BR8 The cost is measured with Azure Cost Management after one full test window followed by 7 parked days, and extrapolated to 30 days as running days × running cost per day + parked days × parked cost per day, with the number of test days a month the owner expects. Plan B applies when that monthly figure, plus the ADR-0002 estimate for production, is above US$ 80.
- BR9 A subscription budget of US$ 80 per month alerts the owner by email at 50 % and 100 %.
- BR10 Every command that creates or changes a paid Azure resource is run by the owner from `docs/infra.md`; Claude writes the commands and reads the pasted output, and never signs in to the subscription.

## Screens and API
- No new screen, route or error code. `GET /api/v1/system/info` on staging answers `"environment": "Staging"` and is the check URL in `docs/infra.md`.

## Acceptance criteria
- AC1 Given the AppHost in publish mode for Staging, when the manifest is generated, then the `api` has `Database__ApplyMigrationsOnStart=true` and production does not (BR5).
- AC2 Given a host started outside Development, when it signs a token and then restarts with the same configured certificates, then a token issued before the restart is still accepted; with no certificates configured outside Development the host refuses to start with a message naming the missing setting (BR4).
- AC3 Given a host with `ConnectionStrings:keys` set, when it starts, then its Data Protection key repository is the Azure Blob one, and with `DataProtection:KeyVaultKeyId` set the keys are encrypted with that Key Vault key; with neither set, behavior is unchanged (BR4; cross-instance proof in AC8).
- AC4 Given the Api outside Development behind a listed proxy, when a request arrives over http with `X-Forwarded-Proto: https` from that proxy, then OpenIddict treats it as HTTPS; from an unlisted address the header is ignored (BR6).
- AC5 Given the Web behind a listed proxy, when a request arrives with `X-Forwarded-Proto: https`, then the request scheme is https; with no proxy listed, behavior is unchanged (BR6, B-4).
- AC6 Given the AppHost in publish mode, when the manifest is generated, then no secret value appears in it, the seeded admin password reaches the Api from Key Vault and no AI key is passed for Staging (BR3, BR7).
- AC7 Given the owner follows `docs/infra.md`, when staging is deployed, then `GET /api/v1/system/info` on the staging address answers 200 with `Staging`, and a tester creates an account, receives the verification email and signs in (UC1, UC2; validation script).
- AC8 Given a tester signed in on staging, when the owner parks staging and starts it again, then the tester's data is there and a verification link sent before the park still works (BR4, UC3; validation script).
- AC9 Given the subscription, when the owner runs the budget command, then the budget shows alerts at 50 % and 100 % of US$ 80 (BR9; validation script).
- AC10 Given one test window and 7 parked days, when the owner pastes the Cost Management figures, then `docs/infra.md` and ADR-0002 record the measured monthly cost and whether plan B applies (BR8; validation script).
- AC11 This item adds no UI text; any text it adds anyway exists in pt-BR, pt-PT and en and the missing-key test is green.

## Decisions
- 2026-10-04 — D1 Sign-up open on staging, no allowlist (owner) — no new code; the data is test data.
- 2026-10-04 — D2 The build waits for F-66 (owner) — without email a tester cannot verify the account; the item validates whole.
- 2026-10-04 — D3 AI off on staging (owner) — no student screen uses the model yet; no model cost against the ceiling.
- 2026-10-04 — D4 Default Azure address, no custom domain (owner) — free TLS, no DNS; a domain is release work.
- 2026-10-04 — D5 Cost measured after one test window plus 7 parked days, extrapolated (owner) — done in weeks, not a month.
- 2026-10-04 — D6 Azure Budget at US$ 80 with alerts at 50 % and 100 % (owner) — no cost, catches a resource left running.
- 2026-10-04 — D7 The owner runs every Azure command from a script in `docs/infra.md`; Claude never signs in (owner) — the subscription and its bill are the owner's.
- 2026-10-04 — D8 Packages approved by the owner (all MIT, versions checked on nuget.org on 2026-10-04): `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore` 10.0.12, `Azure.Extensions.AspNetCore.DataProtection.Keys` 1.6.4, `Aspire.Azure.Security.KeyVault` 13.6.0, `Azure.Identity` 1.21.0. The tests need no new package. Superseded in part by change note v2: `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore` is dropped; `Aspire.Hosting.Azure.Storage` 13.6.0, `Aspire.Azure.Storage.Blobs` 13.6.0 and `Azure.Extensions.AspNetCore.DataProtection.Blobs` 1.5.4 are added (all MIT, checked on nuget.org on 2026-10-07).
- 2026-10-04 — D9 The Data Protection key ring is stored in PostgreSQL (one table in a schema of its own, `NULLS NOT DISTINCT` not applicable: no tenant column) and encrypted with a Key Vault key in the cloud; each host keeps its own application name — the database already exists and is persisted while parked, and the staging Redis is a container with no volume. The Web gains a reference to the database for this table only; the architect pass at build reviews that dependency (Claude). **Superseded by change note v2 (2026-10-07):** the key ring is stored in a blob container of an Azure Storage account (`Standard_LRS`, access by managed identity), as the profile recipe does; encrypted with a Key Vault key in the cloud; each host keeps its own application name; the repository is wired only when `ConnectionStrings:keys` is set, so a local run is unchanged. No new building block, no table, no migration. The Web gets a role on that container only, never on the database (architect review: with one generated administrator login a database reference would make the Web an administrator).
- 2026-10-04 — D10 OpenIddict loads its signing and encryption certificates from configuration (Key Vault secrets) outside Development and refuses to start without them; Development keeps the development certificates (Claude) — the alternative, ephemeral keys, signs everybody out on every deploy.
- 2026-10-04 — D11 Staging applies migrations on start through `Database:ApplyMigrationsOnStart`, set by the AppHost for Staging only (Claude) — the switch already exists; a release pipeline is F-65.
- 2026-10-04 — D12 Both hosts add `X-Forwarded-Proto` to the headers they believe, still only from the listed proxies; the Api gets the same `TrustedProxies` rule as the Web (Claude). The ingress range of a Container Apps environment without a custom network is not documented as fixed: the build reads the address the hosts see on staging and records it, and the list is configured from that, never a guess.
- 2026-10-04 — Approved by the owner ("aprovo F-64"); the build waits for F-66 and the first test window.
- 2026-10-04 — D13 The `api` keeps the Container Apps default TCP probe; the health endpoints stay Development-only (Claude) — exposing them is a separate security decision.
- 2026-10-06 — D14 D2 relaxed by the owner: F-66 was in validating when the build was asked for; it is done now (merged, board #105), so the wait is over and the item builds whole.
- 2026-10-06 — D15 The deployed hosts sign in to PostgreSQL with a generated password kept in Key Vault (owner, option A): `aspire publish -e Staging` showed `passwordAuth: Disabled` and a connection string with no user, which the hosts' plain Npgsql cannot use; with `WithPasswordAuthentication(keyVault)` the same publish shows `passwordAuth: 'Enabled'`, the login and password generated by Aspire, and the connection string stored as a Key Vault secret that the `api` container app reads by `secretUri`. No new package; hosts and contexts unchanged.
- 2026-10-06 — D16 The F-66 assertions "web has no `AZURE_CLIENT_ID`" and "only `api-identity`" are narrowed to the email settings (owner): this item gives the Web an identity for Key Vault; the F-66 BR9 rule (the Web sends no email) stays tested.
- 2026-10-06 — D17 The publish output sets `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` on the `api` (seen in `api.bicep`): it makes the host believe every proxy, which contradicts BR6. The build removes it for both hosts and a test pins its absence (Claude).

- 2026-10-07 — D18 One storage account per host for the key ring (review of F-64, major): Aspire gives a referencing host its Blob, Table and Queue roles on the whole account, so the shared account of change note v2 let the Web read and overwrite the Api's `simulab-api.xml`. Two `Standard_LRS` accounts (`storage-api`, `storage-web`, a few cents) each with a container `keys`; the publish shows each host's role file naming its own account only, and a test pins it (Claude).
- 2026-10-07 — D19 Accepted for staging, recorded not fixed (review of F-64, minor): the Web's Key Vault role (Crypto Service Encryption User) is vault-wide, so it could also wrap and unwrap with the key behind `OpenIddict--EncryptionCertificate`. Scoping a role to one key needs a hand-written Bicep role assignment; revisit at the production release (Claude). Also documented in `docs/infra.md`: a vault secret has the same key as an app host setting and wins over it, because the Key Vault source is added last.
- 2026-10-08 — D20 The PostgreSQL login and password are deploy parameters (`postgres-admin-user`, `postgres-admin-password`), kept in the vault as `deploy--PostgresAdminUser` and `deploy--PostgresAdminPassword` (owner, after the first staging): values Aspire generates again at each `--clear-cache` deploy no longer matched the server (`28P01`).
- 2026-10-08 — D21 The Redis container's password is the deploy parameter `redis-password` (vault `deploy--RedisPassword`), for the same reason as D20 (`NOAUTH` on both hosts). The platform does not restart a running Redis when its secret changes, so a deploy that changes it is followed by a restart of the revision.
- 2026-10-08 — D22 Staging trusts the ingress network `100.100.0.0/16` (`ForwardedHeaders:KnownNetworks` in `appsettings.Staging.json`), not one address: the address of the ingress changes between restarts. Measured on the first staging; with none listed the Api answered 400 `ID2083`.
- 2026-10-08 — D23 The cloud Web is told `Authentication__OpenIddict__ClientId=simulab-web` by the app host (Claude, found on the first staging): the Web has no `appsettings.Development.json` in the cloud, so it sent no `client_id` and every sign-in got a 401 `invalid_client` ("The mandatory 'client_id' parameter is missing"). Found by the Web logging the OAuth error code and description when the token endpoint answers 401. Regression test: `AzurePublishModelTests.Publish_TheWebIsToldTheClientIdTheApiSeeds`.

## Out of scope
- Creating production (ADR-0002 #3: at the first release).
- The deploy workflow in GitHub Actions (F-65).
- The cloud email provider (F-66).
- A custom domain, an allowlist for sign-up, and the AI gateway on staging (D1, D3, D4).
- A second Web instance (its sign-in tickets live in memory, `docs/infra.md`).

## Open questions
- (none)

## Change notes

### v2 — 2026-10-07
- What: the Data Protection key ring moves from PostgreSQL to an Azure Blob container (D9 superseded, D8 packages changed, AC3 rewritten).
- Why: the Web would otherwise receive the database administrator connection string (password sign-in has one generated login, D15) and could read and change every schema; the profile's own recipe keeps the keys in a blob and needs no building block, table or migration.
- Affected: D8, D9, AC3. Not affected: BR4, AC1, AC2, AC4-AC11, D10-D17.
- AC3 is proven by the wiring (`KeyManagementOptions`), without a storage emulator; the cross-instance proof is AC8 on the real staging.
- Re-approved by the owner on 2026-10-07 ("sim").

## Validation script
Needed to validate: an Azure subscription with billing, signed in with `az` on the owner's machine, and the Aspire CLI 13.6.0 (owner, in place only if you confirm it); at least one tester in Brazil (owner); F-66 done (it is, board #105). This item has no screen, so there is no keyboard-only pass and no language switch of its own; step 1 uses the app's screens only to prove that a local run did not change. Every command that creates a paid resource is run by you (BR10); the full commands are in `docs/infra.md`, "First deploy of staging" and "Budget and cost of staging".

1. Local run unchanged. Start the app host (`dotnet run --project src/Hosts/Simulab.AppHost`, stop any other app host first), sign up at `/sign-up`, open the verification email in Mailpit, sign in, switch the language to English and back in the profile. Expected: all works as before; the Api log has no line about Key Vault, blob or certificates.
2. Preview the deployment files without Azure (no sign-in, nothing created):
   - Git Bash: `aspire publish --apphost src/Hosts/Simulab.AppHost/Simulab.AppHost.csproj -e Staging -o artifacts/publish/staging --non-interactive --nologo; grep -c ASPNETCORE_FORWARDEDHEADERS_ENABLED artifacts/publish/staging/api/api.bicep artifacts/publish/staging/web/web.bicep; grep -c Database__ApplyMigrationsOnStart artifacts/publish/staging/api/api.bicep; grep -c ConnectionStrings__simulab artifacts/publish/staging/web/web.bicep`
   - PowerShell 7: the same `aspire publish ...`, then `(Select-String -Path artifacts\publish\staging\api\api.bicep,artifacts\publish\staging\web\web.bicep -Pattern ASPNETCORE_FORWARDEDHEADERS_ENABLED | Measure-Object).Count` and likewise for the other two files.
   - Expected, run by Claude in both shells on 2026-10-07: `Pipeline succeeded`, then `0`, `0` (one per file), `1` (the Api applies migrations on start in Staging) and `0` (the Web has no database). Repeat: delete `artifacts/publish/staging` and run again.
3. First deploy (infra.md steps 1 to 8, then 3 again as step 10 after step 9): create the resource group, deploy, learn the names, give yourself the vault role, create the key, the two certificates and the admin password secret. Expected after the first deploy: the `web` runs and the `api` restarts in a loop (it refuses to start without its certificates); after the second deploy `https://<staging address>/api/v1/system/info` answers 200 with `"environment": "Staging"`. Paste me the ingress address you measured in step 9 (AC7, D12).
4. A tester in Brazil (or you on a phone) opens the staging address, creates an account, receives the verification email, opens the link, signs in and uses the app. Also sign in as `admin@simulab.local` with the password of step 8: it must ask for a new password before anything else (AC7, UC2). If the sign-in fails, first check that the proxy list of step 3 is filled and deployed: the Web calls the Api at `https://api.internal.<environment domain>` (seen in the publish output), the internal ingress ends TLS and forwards plain http with `X-Forwarded-Proto`, and the Api refuses that request until it believes the ingress. If it still fails, stop and paste me the Api log (`az containerapp logs show -n api -g simulab-staging --type console --tail 50`); the address the Api sees for that internal ingress cannot be known without Azure.
5. Persistence (AC8, BR4, UC3): sign up a second account but do **not** open its verification link; sign in with the first one and keep the page open. Park staging and start it again with the commands of "Staging start and stop". Expected: the first account is still signed in or signs in again with the same password, its data is there, and the second account's verification link, sent before the park, still works.
6. Budget (AC9, BR9): run the budget command of "Budget and cost of staging" and read it back. Expected: two notifications, 50 % and 100 % of 80.
7. Cost (AC10, BR8): after one full test window and 7 parked days, run the cost query for the two periods and paste me the two figures and the number of test days a month you expect; I record the monthly figure and whether plan B applies in `docs/infra.md` and ADR-0002.
8. Park staging when the window ends (UC3) and tell me when it is done.

## Delivery
Built 2026-10-07 on `feature/F-64`, validated on the real staging by the owner (AC7 to AC10, 2026-10-08) and shipped 2026-10-08 as merge `143d7d6` (app version 0.19.0, board #103). Full suite at the ship: 2395 tests, 0 failures, 115 s (slowest Identity and Catalog, 1 m 49 s); build 24 s, 0 new warnings; `docs` gate green. Security scan: 13 critical findings triaged as false positive by the owner (test passwords, dev-only values, a public Azure role id), 2 medium in `tools/Simulab.DocGen` left open (not in this item). No manual page: the item changes no screen or text (AC11). The pt-BR runbook `artifacts/staging-commands.md` is committed on the owner's request (it is a Portuguese file in a repository that is otherwise English).

### Criterion → test
| Criterion | Test(s) |
|---|---|
| AC1 | `AzurePublishFilesTests.Publish_StagingAppliesTheMigrationsOnStartAndProductionDoesNot` |
| AC2 | `OpenIddictCertificateRestartTests` (same certificates accept the token after a restart; other certificates refuse it) and `OpenIddictCertificateStartTests` (no certificates, one missing, not a PKCS#12 file: the start refuses and names the setting; both present: OpenIddict holds them) |
| AC3 | `DataProtectionWiringTests` (blob repository, Key Vault encryptor, application name per host, bad key address, local run unchanged); `AzurePublishFilesTests.Publish_GivesBothHostsTheirKeyRingAndTheWebNoDatabase` and `Publish_KeepsEachHostsKeyRingApartAndGivesOnlyTheApiTheSecrets` |
| AC4 | `ForwardedHeadersHostTests` (listed proxy: OpenIddict treats http as https; no header, unlisted sender, no proxy listed: `ID2083`; the one-time log with the sender's address) |
| AC5 | `TrustedProxiesTests` (`ListedProxy_ForwardedProto_TheSchemeIsHttps`, `UnlistedSender_...`, `NothingListed_...`, plus the B-4 address cases) |
| AC6 | `AzurePublishFilesTests.Publish_PassesNoSecretAndNoBlanketForwardedHeaders` (Staging and Production), `Publish_WritesNoSecretValue`, `Publish_KeepsEachHostsKeyRingApart...` (Secrets User role on the Api only) and `Publish_PassesTheListedProxiesToBothHosts` |
| AC7, AC8, AC9, AC10 | Validation script steps 3 to 7 (they need the Azure subscription) |
| AC11 | No UI text added; no resource file touched |

### What is proven and what is not
- The Key Vault source in the Api's configuration (`Program.cs`) has no automated test: reaching a vault needs Azure. It is proven on the first staging (a certificate or the admin password read from the vault). The publish tests prove the Api has the vault's address and the Secrets User role, and the Web has neither.
- Not verified without Azure: the address the Api sees for the internal ingress (the Web calls `https://api.internal.<domain>`; the ingress ends TLS and forwards http), whether the ACA setting `autoConfigureDataProtection: true` in the publish output interacts with the explicit key ring, and that Key Vault's default-policy certificates pass OpenIddict's key-usage checks at runtime (tests use certificates with the same key usages).
- Review: independent fresh-context review ran; its major finding (the Web's storage roles, D18) is fixed with a test, its minor findings are D19 and the documented precedence of the vault source; its missing validation script is written.

### Results (2026-10-07, worktree)
- Gate (`gate.js stop`): `agile gate GREEN`, build 21 s, 0 new warnings.
- Api.Tests 39, AppHost.Tests 31, Web.Tests 1086, Identity.Tests 472 (1 m 21 s), ArchitectureTests 181: all passed. The full suite runs at `/agile:ship`.

### Changes to existing files worth knowing
- `TrustedProxies` moved from the Web to `Simulab.ServiceDefaults` and now also believes `X-Forwarded-Proto`; the Api uses it too.
- `ASPNETCORE_FORWARDEDHEADERS_ENABLED`, which the Aspire publish adds, is removed from both hosts (D17).
- The two F-66 publish assertions about identities were narrowed to the email settings (D16).
- `docs/infra.md`: first-deploy, budget, cost, expected secrets; `docs/agile/profile.md`: one line naming the three deviations from the Azure recipe; `docs/glossary.md`: three technical terms.
- Packages added (D8, change note v2): `Aspire.Hosting.Azure.Storage`, `Aspire.Azure.Storage.Blobs`, `Aspire.Azure.Security.KeyVault`, `Azure.Extensions.AspNetCore.DataProtection.Blobs`, `Azure.Extensions.AspNetCore.DataProtection.Keys`.
- Precondition for F-65: a production deploy applies no migration (BR5); the Api writes its keys to a blob regardless.

### First staging (2026-10-08, region `centralus`, group `rg-simulab-staging`)
- Measured: the ingress network is `100.100.0.0/16` (D22). Cadastro and the confirmation e-mail work; the admin signs in after D23 (owner, on screen).
- Found and fixed on the first staging: D20 (PostgreSQL login and password), D21 (Redis password), D22 (network), D23 (client id). Commits `0169200`, `9d81225`, `22e0230`, `9b55013`; `b66539e` makes the Web log the OAuth error on a 401.
- Wrong diagnoses along the way, recorded so they are not repeated: "old hash in the database" and "the user's password" (both refuted by the log line of `b66539e`).
- Still to do before `/agile:ship`: validation steps 4 to 8 (testers, persistence across stop/start, budget, cost) and the full suite.
