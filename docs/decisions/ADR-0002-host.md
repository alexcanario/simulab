---
adr: 0002
status: accepted
date: 2026-10-03
---
# ADR-0002: Host — Azure Container Apps, Brazil South

Technical terms: [glossary](../glossary.md)

> **Region replaced by [ADR-0004](ADR-0004-data-region-central-us.md) (2026-10-09):** decision 1 names Brazil South; the region of record is Azure Central US for staging and production. The rest of this ADR (host, parking, cost ceiling, deploy command) stands. The text below is the record of 2026-10-03 and is not rewritten.

## Context
ADR-0001 #5 and #32 name Azure Container Apps as the planned target, created only near the first release so that no environment costs money before there is something to publish. `docs/infra.md` lists `staging` and `production` as `planned` with no deploy command. Before the first testers need a staging environment the project must know where it runs, what it costs per month and with which command (F-62).

Facts checked on 2026-10-03:
- Nothing deployable existed: no Dockerfile, no Bicep, no workflow. The AppHost declared only local resources.
- The `api` runs the job worker (ADR-0001 #20): at zero replicas no email leaves. The `web` keeps its sign-in tickets in memory (`docs/infra.md`): it runs as one instance.
- Aspire 13.6.0 can publish an AppHost to Azure Container Apps (`Aspire.Hosting.Azure.*`). Its Container Apps environment accepts only an Azure Container Registry for the images; a GHCR registry fails at publish with "Only Azure Container Registry resources are supported" (tried on 2026-10-03). So the images go to the registry that Aspire creates, `cae-acr` (change note v2 of F-62).
- Azure retail prices, Brazil South (prices.azure.com, 2026-10-02 and 2026-10-03, USD, 730 h/month, 30.4 days): PostgreSQL Flexible B1ms 0.035/h (about 25.6) plus storage 0.2185/GB-month (32 GB about 7.0); Azure Cache for Redis C0 Basic 0.022/h (about 16.1); Azure Container Registry Basic 0.1666/day (about 5.1); Container Apps vCPU 0.000024/s active, 0.000003/s idle, memory 0.000003/GiB-s; Linux VM B2s 0.0672/h (about 49.1). The Container Apps free monthly grant is not verified.

## Options
**A. Azure Container Apps with managed services** (chosen). `api` and `web` as container apps, PostgreSQL Flexible Server, Redis, Key Vault, ACR Basic.

**B. One Linux VM running Docker Compose.** Staging and production as two compose environments, the profile's deploy recipe (`docs/agile/profile.md`, "Deploy recipe"). Both options run Linux containers.

| | A. Container Apps | B. One VM with Compose |
|---|---|---|
| Staging running (estimate) | about 65-75/month: PostgreSQL about 33, ACR about 5, containers and Redis container the rest | about 49 (B2s) shared with production |
| Staging parked (estimate) | about 12/month: PostgreSQL storage about 7 plus ACR about 5; apps at zero replicas, PostgreSQL stopped | about 49 if the VM runs, about 5 for the disk if it is deallocated (everything on it stops, production included) |
| Production not created | 0 | 0 |
| Production running (estimate) | about 70-75/month: PostgreSQL about 33, Azure Cache for Redis about 16, ACR about 5, containers the rest | the same VM, no extra cost until it is too small |
| Backup of student data | point-in-time restore of PostgreSQL, no effort | a script and a place to keep the dumps, to be written and tested |
| Updates | the platform's | the operating system, Docker and PostgreSQL, by us |
| Start and stop | a command per environment (below) | start and stop the VM, or the compose project |
| Deploy path | GitHub Actions with OIDC sign-in, no stored cloud secret; GitHub environments, approval for production (F-65) | GitHub Actions over SSH with a stored key; the same environments and approval |

All figures are estimates from retail prices, not measured; F-64 measures the real monthly cost.

## Decision
1. **The host is Azure Container Apps, Brazil South**, with managed PostgreSQL (Flexible Server), Azure Cache for Redis for production and Key Vault. Container images are kept in the Azure Container Registry (Basic) that the Aspire environment creates.
2. **Staging** exists for testers in Brazil. It runs during test windows and is parked outside them: apps at zero replicas, PostgreSQL stopped. Azure Cache for Redis can only be deleted, not stopped, and staging's Redis holds only sessions (losing them signs testers out), so staging runs Redis as a container inside the Container Apps environment; production uses Azure Cache for Redis.
3. **Production** has its deploy command declared and is created only at the first release (ADR-0001 #5). Until then it costs nothing.
4. **Cost ceiling: US$ 80 per month** for staging and production together. With production created and staging parked the estimate is about 85 (70-75 plus about 12), slightly above the ceiling; with both running it is about 140, well above it. The first release therefore starts with the ceiling already at risk, and F-64's measurement decides whether it holds. **Plan B applies when the monthly cost measured by F-64 is above the ceiling**: one Linux VM running staging and production as two Docker Compose environments (option B).
5. In the cloud the `api` keeps at least one replica and the `web` runs at most one replica and may go to zero.
6. The deploy command is Aspire's: `aspire deploy --apphost src/Hosts/Simulab.AppHost/Simulab.AppHost.csproj -e <Environment> -o artifacts/deploy/<environment> --clear-cache --non-interactive --nologo`. The Azure subscription, resource group and location come from environment variables, never from a committed file. (Note, F-68, 2026-10-09: the tenant and subscription ids are identifiers, not secrets, and are recorded in the `## Cloud accounts` table of `docs/infra.md`, which `/agile:publish` reads; the deploy command still takes them from the environment, and no credential is committed.) The AppHost declares the Azure resources only when it publishes; a local run is unchanged and needs no Azure sign-in.
7. This ADR overrides the profile's Docker Compose deploy recipe for Simulab; the recipe stays as plan B.
8. The CI workflow (`.github/workflows/ci.yml`) builds and tests on every pull request and push to `main`, with no deploy step. The deploy workflow is F-65: GitHub Actions signs in to Azure with OIDC (a federated credential, so no cloud secret is stored in GitHub), uses one GitHub environment per target, and asks for approval before production.

## Consequences
- The AppHost has two shapes (run and publish); tests read both from the model and the publish files from the manifest publisher, with no Azure sign-in.
- Every `Aspire.*` package and the AppHost SDK share one version (13.6.0, the CLI's); a test fails when they drift.
- Not decided here and left to later items: creating the environment, persisting Data Protection keys and `ForwardedHeaders` in the cloud, measuring the cost (F-64); the deploy workflow (F-65); a cloud email provider (F-66); an EU region (F-67).
- A VM-based plan B, or publishing without Aspire (Dockerfiles and hand-written Bicep, with GHCR for the images), remains possible if the registry cost or the two-shape AppHost becomes a problem; revisit at F-65.
