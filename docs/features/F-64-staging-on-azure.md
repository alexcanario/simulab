---
feature: F-64
epic: Cloud hosting and operations
status: building
board: 103
version: 1
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
- AC3 Given two instances of a host sharing the database, when one protects a value, then the other unprotects it, and the keys are stored encrypted with the configured key when one is set (BR4).
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
- 2026-10-04 — D8 Packages approved by the owner (all MIT, versions checked on nuget.org on 2026-10-04): `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore` 10.0.12, `Azure.Extensions.AspNetCore.DataProtection.Keys` 1.6.4, `Aspire.Azure.Security.KeyVault` 13.6.0, `Azure.Identity` 1.21.0. The tests need no new package.
- 2026-10-04 — D9 The Data Protection key ring is stored in PostgreSQL (one table in a schema of its own, `NULLS NOT DISTINCT` not applicable: no tenant column) and encrypted with a Key Vault key in the cloud; each host keeps its own application name — the database already exists and is persisted while parked, and the staging Redis is a container with no volume. The Web gains a reference to the database for this table only; the architect pass at build reviews that dependency (Claude).
- 2026-10-04 — D10 OpenIddict loads its signing and encryption certificates from configuration (Key Vault secrets) outside Development and refuses to start without them; Development keeps the development certificates (Claude) — the alternative, ephemeral keys, signs everybody out on every deploy.
- 2026-10-04 — D11 Staging applies migrations on start through `Database:ApplyMigrationsOnStart`, set by the AppHost for Staging only (Claude) — the switch already exists; a release pipeline is F-65.
- 2026-10-04 — D12 Both hosts add `X-Forwarded-Proto` to the headers they believe, still only from the listed proxies; the Api gets the same `TrustedProxies` rule as the Web (Claude). The ingress range of a Container Apps environment without a custom network is not documented as fixed: the build reads the address the hosts see on staging and records it, and the list is configured from that, never a guess.
- 2026-10-04 — Approved by the owner ("aprovo F-64"); the build waits for F-66 and the first test window.
- 2026-10-04 — D13 The `api` keeps the Container Apps default TCP probe; the health endpoints stay Development-only (Claude) — exposing them is a separate security decision.
- 2026-10-06 — D14 D2 relaxed by the owner: F-66 was in validating when the build was asked for; it is done now (merged, board #105), so the wait is over and the item builds whole.

## Out of scope
- Creating production (ADR-0002 #3: at the first release).
- The deploy workflow in GitHub Actions (F-65).
- The cloud email provider (F-66).
- A custom domain, an allowlist for sign-up, and the AI gateway on staging (D1, D3, D4).
- A second Web instance (its sign-in tickets live in memory, `docs/infra.md`).

## Open questions
- (none)

## Change notes

## Validation script

## Delivery
