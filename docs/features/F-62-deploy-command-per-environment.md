---
feature: F-62
epic: Foundation and identity
status: refining
board: 101
version: 1
---
# Each environment declares how it is deployed

## Summary
`docs/infra.md` has no Deploy command, Check URL or Version (deployed on) column in its environments table; `templates/infra.md` of agile@canary 0.0.101 has all three, and `/agile:publish <environment>` reads them. The item adds the columns, with the command each environment is deployed by or `not declared`. Raised by `/agile:sync` to 0.0.102; the owner chose to capture it on 2026-10-02.

## Start
- Depends on: nothing.
- Waits on (to start): the owner says which environments exist beyond local and how each is deployed — unknown, settled at `/agile:refine`.
- Needed to validate: unknown — settled at `/agile:refine`.
- Suggested path: `/agile:refine` first — unknown, settled at `/agile:refine`.
- Parallel with: unknown — settled at `/agile:refine`.

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
