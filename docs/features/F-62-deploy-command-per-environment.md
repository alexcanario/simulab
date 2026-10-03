---
feature: F-62
epic: Foundation and identity
status: building
board: 101
version: 2
---
# Each environment declares how it is deployed: Azure as the host

## Summary
Captured as "add the deploy columns to `docs/infra.md`" (raised by `/agile:sync` to 0.0.102, 2026-10-02); the columns landed on `main` in `976db15`, so the owner reframed the item at `/agile:refine` (2026-10-02): decide the host with Azure as the candidate, prove locally that the AppHost publishes to it, declare the deploy commands of staging and production, and add the build-and-test CI. No Azure resource is created and nothing is spent in this item.

## Start
- Depends on: nothing.
- Waits on (to start): nothing.
- Needed to validate: the owner authorizes pushing `feature/F-62` to GitHub, so the CI workflow runs once on GitHub's runners — owner. No Azure subscription use.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: F-66 (cloud email, another area); F-50 (refining, UI only) — but only one of them in `building` per checkout.

## Goal
Know where Simulab will run, at what monthly cost, and with which command, before the first testers need a staging environment — without paying for an environment yet.

## Users and use cases
- UC1 The owner reads one ADR and learns which host was chosen, what it costs per month in each state (running, parked), what the plan B is and when it applies.
- UC2 The owner runs `aspire publish` for Staging or Production on their machine and gets the Azure deployment files, without touching Azure.
- UC3 The owner reads `docs/infra.md` and finds, per environment, the deploy command `/agile:publish` will run and the commands that start and park staging.
- UC4 A developer opens a pull request or pushes to `main`, and GitHub Actions builds the solution and runs the tests.

## Business rules
- BR1 The host is **Azure Container Apps, Brazil South**, with managed PostgreSQL (Flexible Server), Azure Cache for Redis and Key Vault; container images are kept in the Azure Container Registry (Basic) that Aspire creates with the Container Apps environment (change note v2; GHCR is not supported by Aspire 13.6.0 for this environment).
- BR2 Cost ceiling: **US$ 80 per month** for staging and production together. The ADR records the estimate per state from Azure retail prices (source and date); the real cost is measured by F-64. When the measured monthly cost is above the ceiling, plan B applies: one Linux VM running staging and production as two Docker Compose environments (the profile's deploy recipe).
- BR3 **Staging** exists for testers in Brazil: it runs during test windows and is parked outside them (apps at zero replicas, PostgreSQL stopped). `docs/infra.md` declares the start and the stop command.
- BR4 **Production** has its deploy command declared, but it is created only at the first release (ADR-0001 #5); until then it costs nothing.
- BR5 In the cloud the `api` keeps at least one replica (it runs the job worker, ADR-0001 #20: at zero replicas no email leaves), and the `web` runs at most one replica (sign-in tickets live in memory, `docs/infra.md`); the `web` may go to zero replicas.
- BR6 Running locally does not change: `dotnet run --project src/Hosts/Simulab.AppHost` starts the same containers as today (PostgreSQL on 5432 with the local password, Mailpit, Redis) and contacts no Azure service.
- BR7 No secret value is written in a committed file or in the generated deployment files: cloud secrets are AppHost parameters, resolved from Key Vault or from the environment at deploy time (`docs/infra.md`, "Expected secrets").
- BR8 The CI workflow builds `Simulab.slnx` and runs the test suite on every pull request and every push to `main`; it has no deploy step (the deploy workflow is F-65).

## Screens and API
- No screen, route, endpoint or error code changes.
- Files: `docs/decisions/ADR-0002-host.md` (new), `docs/infra.md` (environments table, start and stop commands, secrets, CI row), `src/Hosts/Simulab.AppHost/` (publish model for Azure), `Directory.Packages.props` (Aspire 13.6.0, Azure hosting packages), `.github/workflows/ci.yml` (new), `docs/agile/profile.md` (one line pointing at ADR-0002 for the deploy recipe), `docs/architecture-overview.md` if the containers diagram must name the cloud resources.

## Acceptance criteria
- AC1 Given ADR-0002, when the owner reads it, then it compares Azure Container Apps with one Linux VM running Docker Compose on monthly cost per state (running, parked, not created) with the retail price source and date, operations (backup, updates, start and stop) and the GitHub Actions deploy path (OIDC sign-in with no stored cloud secret, GitHub environments, approval for production), records the choice of BR1, the ceiling and plan B trigger of BR2, and the states of BR3 and BR4. (validation script)
- AC2 Given the AppHost, when `aspire publish` runs for Staging on a machine with no Azure sign-in, then it writes the deployment files for a Container Apps environment, `api`, `web`, a PostgreSQL Flexible Server with the `simulab` database, Azure Cache for Redis and Key Vault, under the ignored `artifacts/` folder, and calls no Azure service. (test on the AppHost publish model + validation script)
- AC3 Given the AppHost run locally, when it starts, then its resources are the same as before the item (PostgreSQL container on 5432 with the named volume, Mailpit, Redis container, `api`, `web`) and no Azure resource is in the model. (test on the AppHost run model)
- AC4 Given the publish model, when it is built, then `api` has a minimum of 1 replica and `web` a maximum of 1 replica and a minimum of 0. (test)
- AC5 Given the generated deployment files, when they are searched, then they contain no secret value: no local PostgreSQL password, no OpenIddict client secret, no API key. (test)
- AC6 Given `Directory.Packages.props` and the AppHost SDK, when they are read, then every `Aspire.*` package, the `Aspire.AppHost.Sdk` and `CommunityToolkit.Aspire.Hosting.MailPit` carry the same version, 13.6.0, the version of the Aspire CLI the deploy command uses. (test)
- AC7 Given `docs/infra.md`, when the owner reads the environments table, then staging and production are `planned` on Azure Container Apps, Brazil South, each with its Deploy command (`aspire deploy` with its environment name), and a section gives the staging start and stop commands. (validation script)
- AC8 Given `.github/workflows/ci.yml`, when `feature/F-62` is pushed, then a GitHub Actions run builds the solution and runs the tests, green, with no deploy step. (validation script)
- AC9 Given the item adds no UI text, when the missing-key test runs, then it stays green; the app manual is unchanged (no visible behavior changes).

## Decisions
- 2026-10-02 — The item is reframed from "add the columns" to "Azure as the host candidate" — the columns already landed in `976db15`; a command can only be declared once there is a host (owner, at `/agile:refine`).
- 2026-10-02 — Scope: an ADR plus a local proof (`aspire publish`), no Azure resource created — ADR-0001 #5: no environment cost before there is something to publish (owner).
- 2026-10-02 — The rival in the ADR is one Linux VM with Docker Compose; both options run Linux containers — the profile already has a measured recipe for it (owner).
- 2026-10-02 — GitHub Actions: the ADR evaluates the deploy pipeline; this item adds only the build-and-test CI (ADR-0001 #33) — with no environment there is nothing a deploy workflow could be tested against (owner). The deploy workflow is F-65.
- 2026-10-02 — Managed PostgreSQL, Redis and Key Vault on Azure — point-in-time backup of student data with no effort (owner).
- 2026-10-02 — Staging and production both get a declared deploy command (owner).
- 2026-10-02 — Ceiling US$ 80 per month for both environments together (owner). Managed services with both environments always on cost about US$ 140-240, so the owner chose (after asking whether GitHub could host the app — it cannot run a server; it can keep the images, GHCR — and whether resources can be switched off — yes, with limits): staging for testers in Brazil, started for test windows and parked outside them; production created only at the first release; plan B the VM when the measured cost is above the ceiling (owner, "ok" to option 1).
- 2026-10-02 — Estimates recorded for the ADR, not verified (Brazil South retail, USD/month): staging running ~60-70, staging parked ~7-10 (PostgreSQL storage; Azure Cache for Redis cannot be stopped, so staging's Redis is a container — next decision), production not created 0. — prices.azure.com, 2026-10-02; the Container Apps free grant is not verified.
- 2026-10-02 — Staging's Redis runs as a container inside the Container Apps environment, not Azure Cache for Redis; production uses Azure Cache for Redis — Azure Cache for Redis cannot be stopped, only deleted (~US$ 16/month even when staging is parked), and staging's Redis holds only sessions: losing them signs the testers out, nothing more (Claude, technical; within the owner's "managed services" answer for the data that matters).
- 2026-10-02 — Packages approved by the owner (all MIT, nuget.org 2026-10-02): every `Aspire.*` package and `Aspire.AppHost.Sdk` 13.4.6/13.5.4 → 13.6.0, `CommunityToolkit.Aspire.Hosting.MailPit` 13.5.0 → 13.6.0 (the profile requires the CLI and the packages on one version; the installed CLI is 13.6.0); new: `Aspire.Hosting.Azure.AppContainers`, `Aspire.Hosting.Azure.PostgreSQL`, `Aspire.Hosting.Azure.Redis`, `Aspire.Hosting.Azure.KeyVault` 13.6.0. Tests: no new package (`Aspire.Hosting.Testing` exists). `Aspire.Hosting.Docker` is not added: plan B is only on paper.
- 2026-10-02 — The Azure resources exist only in publish mode (`builder.ExecutionContext.IsPublishMode`); run mode keeps today's containers — BR6, and no developer needs an Azure sign-in (Claude, technical).
- 2026-10-02 — The deploy command follows the profile's form: `aspire deploy --apphost src/Hosts/Simulab.AppHost/Simulab.AppHost.csproj -e <Environment> -o artifacts/deploy/<environment> --clear-cache --non-interactive --nologo`; the Azure subscription, resource group and location come from environment variables, never from a committed file (Claude, technical).
- 2026-10-02 — ADR-0002 overrides the profile's Docker Compose deploy recipe for Simulab; the profile gets one line pointing at it, the recipe stays as plan B (Claude, technical).
- 2026-10-02 — The CI workflow runs on `ubuntu-latest`, where Docker is available for the test containers; the measured suite (1808 tests, ~90 s) fits the free minutes of a private repository (Claude, technical).
- 2026-10-03 — Approved by the owner ("aprovo f-62"), including the staging Redis as a container.

## Change notes
### v2 — 2026-10-03
- What: BR1 — the images are kept in the Azure Container Registry (Basic) that Aspire creates (`cae-acr`), not in GHCR.
- Why: found while building. With a GHCR registry on the environment, `aspire publish` fails: "The container registry configured for the Azure Container App Environment 'cae' is not an Azure Container Registry. Only Azure Container Registry resources are supported." (Aspire 13.6.0). Option B (hand-edited Bicep) would break `aspire deploy` as the single command; option C (publish without Aspire: Dockerfiles, hand-written Bicep, GHCR) was weighed and deferred to F-65 if cost or drift becomes a problem.
- Affected: BR1 (text above). BR2: the ADR adds the ACR to every cost state (about US$ 5/month, not verified; an ACR cannot be stopped, so it also counts while staging is parked). The decision of 2026-10-02 that names GHCR stays as history. AC1 to AC9 unchanged.
- Re-approved: 2026-10-03 (owner, "A, pode seguir").

## Out of scope
- Creating staging on Azure, the first deploy, persisting Data Protection keys and setting `ForwardedHeaders` in the cloud, measuring the real cost: F-64.
- The GitHub Actions deploy workflow: F-65.
- A cloud email provider (staging testers need it before they sign up): F-66.
- An EU region for Portuguese users: F-67 (still deferred in `docs/infra.md`).
- Plan B (the VM) beyond its description in the ADR.

## Open questions
- (none)

## What exists (checked 2026-10-02)
- **The summary's premise is already false.** Commit `976db15` on `main` added the Deploy command, Check URL and Version (deployed on) columns to `docs/infra.md`; every row says `not declared`. What is left is declaring a real command, which needs a host. The owner reframed the item at `/agile:refine`: Azure as the candidate host.
- `docs/infra.md` lists `staging` and `production` as `planned`: Azure Container Apps, Brazil South, Key Vault for secrets, "Bicep adapted from Simulae". ADR-0001 #5 and #32: Container Apps is the planned target, created only near the first release, to avoid environment cost before there is something to publish. The first release order is still an open question in `product/brief.md`.
- Nothing deployable exists in the repository: no Dockerfile, no Bicep, no `azure.yaml`, no `.github/workflows/`. The AppHost (`src/Hosts/Simulab.AppHost/AppHost.cs`) declares only local resources: PostgreSQL container with a fixed local password, Mailpit, Redis container, `api`, `web` (`WithExternalHttpEndpoints()`); no compose or Azure environment, no `Deploy:HostPort`.
- Package versions: `Aspire.AppHost.Sdk` and `Aspire.Hosting.PostgreSQL` 13.4.6, `Aspire.Hosting.Redis` 13.5.4, MailPit toolkit 13.5.0. The installed Aspire CLI is 13.6.0; the latest stable of every `Aspire.Hosting.Azure.*` package and `Aspire.Hosting.Docker` is 13.6.0 (nuget.org, 2026-10-02). The profile's deploy recipe requires the CLI and every `Aspire.*` package of the AppHost on the same version.
- The profile's deploy recipe (`docs/agile/profile.md`, "Deploy recipe") is Docker Compose on one machine (`aspire deploy` with a compose environment), not Azure.
- Data Protection keys are not persisted anywhere (`PersistKeysToFileSystem` absent); the Web already uses Data Protection for its Redis sessions (`RedisWebSessionStore`, B-3). A redeploy without persisted keys signs everyone out.
- The Web keeps sign-in tickets in memory and must run as **one instance** (`docs/infra.md`, Google sign-in section).
- Simulae (`D:\dev\_icontrol\simulae\infra`, read-only): `main.bicep` plus modules for Container Apps (api and web, 0.5 vCPU / 1 GiB, min 1 replica), PostgreSQL Flexible (B1ms staging, D2ds_v5 prod, v16, 32 GB), Azure Cache for Redis (Basic staging, Standard prod), Key Vault, a shared registry and an OIDC federation script. Database names are Simulae's (`identitydb`, `billingdb`, ...), not Simulab's single `simulab` database.
- Tooling on this machine: `az` logged in to "Azure subscription 1"; `azd` not installed.
- Azure retail prices, Brazil South (prices.azure.com, 2026-10-02, USD, 730 h/month): PostgreSQL Flexible B1ms 0.035/h (~25.6) + storage 0.2185/GB-month (32 GB ~7.0); Azure Cache for Redis C0 Basic 0.022/h (~16.1); Container Apps vCPU 0.000024/s active, 0.000003/s idle, memory 0.000003/GiB-s; Linux VM B2s 0.0672/h (~49.1). Container Apps free monthly grant: not verified.
