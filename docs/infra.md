---
updated: 2026-10-03
---
# Infra

Technical terms: [glossary](glossary.md)

Written at bootstrap from ADR-0001 round 6. What is true today; anything that does not exist yet is `planned`.

## Run locally
- Prerequisites: .NET 10 SDK, a container runtime (Docker Desktop or Podman), Aspire tooling.
- Start: `dotnet run --project src/Hosts/Simulab.AppHost` → the Aspire dashboard URL is printed on start. Web: https://localhost:7125 (UI kit gallery at `/dev/ui`, Development only; close the app host before building, it locks the Web DLLs). Api: https://localhost:7287 (OpenAPI at `/openapi/v1.json`; the OpenIddict token endpoint is `/connect/token`, outside `/api/v1` — see F-5). Create an account at `/sign-up`; the verification email arrives in Mailpit and its link opens `/verify-email` on the Web. Sign in at `/sign-in` with that account.
- Local containers started by the app host: PostgreSQL (database `simulab`, named volume `simulab-postgres-data`, fixed local-only credentials `postgres`/`postgres` on host port 5432 — reach it by hand with `psql`/DataGrip at `Host=127.0.0.1;Port=5432;Database=simulab;Username=postgres;Password=postgres`; the port is exposed by Aspire's own `dcp` proxy, so it may not match the random port `docker ps` shows for the container), Mailpit (SMTP capture; its web UI is linked from the dashboard) and Redis (refresh-token sessions and access-token revocation, F-5; the Identity per-client rate-limit counters `identity:rate-limit:*`, F-54 — shared by every Api replica, and when Redis is down those limits allow the call and log an error; the Web's server-side sessions `web:session:*`, Data-Protection-encrypted, B-3; TLS and a password by default — the `Api` and `Web` hosts trust it through the `Aspire.StackExchange.Redis` client integration, not a plain connection string). Planned, added by the first feature that needs it: Azurite (blob storage).
- Reset local data: stop the app host and remove the volume (`docker volume rm simulab-postgres-data`); it is recreated empty on the next start. The first start applies every migration with no `fail` line; each module creates its `__ef_migrations_history` table before migrating (F-19), so a `fail` on start is a real failure.
- Background work (F-13): the `Api` host runs the job worker of ADR-0001 #20. It polls `jobs.jobs` every 5 s, retries a failed job 5 times (1-2-4-8-16 minutes) and deletes it as soon as it succeeds. Every identity email leaves through it, so a Mailpit that is stopped delays an email instead of losing it. To watch the queue by hand: `select status, attempts, last_error from jobs.jobs;`.
- Retention of failed jobs (F-27): a job given up on stays as a `Failed` row — the evidence of what was lost — for **90 days**, counted from `created_at`, and is then deleted. The cleanup runs inside the same worker poll, at most once an hour and always once when the process starts, and logs one line with the count only when it removed something. The number is `JobPolicy.FailedRetention`, a constant and not configuration, so changing it is a code change. `Pending` and `Running` rows are never deleted however old they are.

### Useful commands
The same command works in Git Bash and in PowerShell 7 unless two forms are shown.

- **A worktree's own database** (rule: worktrees): `AppHost.cs` reads `Database:Name`, so an item's migration never lands in the shared `simulab` database. The Postgres server is the same; Aspire creates the database on first start.
  - Git Bash: `Database__Name=simulab_f42 dotnet run --project src/Hosts/Simulab.AppHost`
  - PowerShell 7: `$env:Database__Name = "simulab_f42"; dotnet run --project src/Hosts/Simulab.AppHost`
  - Drop it when the item ships (server up, nobody connected to it): `DROP DATABASE simulab_f42;`
- **Only the database server, through the app host**: there is no flag to start one resource. Start the app host, then in the dashboard's Resources page choose **Stop** on `api` and `web` (and `redis`, `mailpit` when not needed). Postgres stays on `127.0.0.1:5432`.
- **Only the database server, without the app host**: a container on the app's volume. The Postgres version must be the one that created the volume, and the data path depends on it.
  - Which layout (read only). In Git Bash set `export MSYS_NO_PATHCONV=1` first, or `/d` is rewritten to `D:/` and the command finds nothing: `docker run --rm -v simulab-postgres-data:/d:ro alpine ls -la /d`. A folder `18` means a PostgreSQL 18 volume (data in `18/docker`); `PG_VERSION` at the root means 17 or older.
  - Verified 2026-10-02: the local volume is PostgreSQL 18 (`18/docker/PG_VERSION` holds `18`). The 18 image keeps data under `/var/lib/postgresql`, so mounting the volume on `/var/lib/postgresql/data` makes `initdb` run on a non-empty folder and refuse (`directory "/var/lib/postgresql/data" exists but is not empty`); nothing is lost, only the container fails.
  - PostgreSQL 18: `docker run -d --name simulab-pg -e POSTGRES_PASSWORD=postgres -p 5432:5432 -v simulab-postgres-data:/var/lib/postgresql postgres:18`
  - PostgreSQL 17 or older: the same with `-v simulab-postgres-data:/var/lib/postgresql/data` and the matching image (`postgres:17`).
  - Read through the container, with no client installed (the server asks for the password even on its socket): `docker exec -e PGPASSWORD=postgres simulab-pg psql -U postgres -d simulab -At -c "select count(*) from catalog.exams"`
  - Stop it, keeping the data: `docker rm -f simulab-pg`
  - Never run it together with the app host: both want port 5432 and the same data directory.
- **Repeat one migration by hand** (a data migration being checked on screen): `DELETE FROM catalog.__ef_migrations_history WHERE migration_id = '<migration id>';` in that database, then start the app host again. Use it only on a worktree's own database. The history table of a module is `<schema>.__ef_migrations_history`, with `migration_id`.
- **Connect by hand**: `Host=127.0.0.1;Port=5432;Database=simulab;Username=postgres;Password=postgres` (psql or DataGrip).

## Environments
| Environment | Status (`provisioned` / `planned`) | URL | How it is deployed | Configuration and secrets live in | Deploy command | Check URL | Version (deployed on) |
|---|---|---|---|---|---|---|---|
| local | provisioned | printed by the app host | app host | user secrets | not declared | | |
| staging | planned | — | Azure Container Apps, Brazil South (ADR-0002); parked outside test windows | Azure Key Vault | `aspire deploy --apphost src/Hosts/Simulab.AppHost/Simulab.AppHost.csproj -e Staging -o artifacts/deploy/staging --clear-cache --non-interactive --nologo` | | |
| production | planned | — | Azure Container Apps, Brazil South (ADR-0002); created at the first release; approval required | Azure Key Vault | `aspire deploy --apphost src/Hosts/Simulab.AppHost/Simulab.AppHost.csproj -e Production -o artifacts/deploy/production --clear-cache --non-interactive --nologo` | | |

The deploy command needs the Aspire CLI on the same version as the packages (13.6.0), an Azure sign-in, and `AZURE__SUBSCRIPTIONID`, `AZURE__LOCATION` (`brazilsouth`) and `AZURE__RESOURCEGROUP` in the environment; nothing about the subscription is committed. The secret parameters it asks for are listed in "Expected secrets". To see what it would create without Azure: `aspire publish --apphost src/Hosts/Simulab.AppHost/Simulab.AppHost.csproj -e Staging -o artifacts/publish/staging --non-interactive --nologo` (the Bicep files land under the ignored `artifacts/`).

### First deploy of staging (F-64)
The owner runs every command below in the owner's own subscription (F-64 BR10); nothing here is run by Claude, and each one that creates a resource is paid. Command shapes were checked with `az <command> --help` only. Names in `<angle brackets>` are read from step 4. The same command works in Git Bash and PowerShell 7 unless two forms are shown. The sign-in and the subscription are the owner's: `az login`, then `az account set --subscription <subscription id>`.

The Api refuses to start until its certificates exist (F-64 BR4), so the first deploy ends with a healthy `web` and an `api` that restarts in a loop. That is expected: steps 5 to 8 give it what it needs, and the second deploy (step 10) starts it.

1. Settings for the deploy command (never committed):
   - Git Bash: `export AZURE__SUBSCRIPTIONID=<subscription id> AZURE__LOCATION=brazilsouth AZURE__RESOURCEGROUP=simulab-staging`
   - PowerShell 7: `$env:AZURE__SUBSCRIPTIONID = "<subscription id>"; $env:AZURE__LOCATION = "brazilsouth"; $env:AZURE__RESOURCEGROUP = "simulab-staging"`
   - The OpenIddict client secret is asked as the parameter `openiddict-client-secret` and must be **the same value on every deploy** (both hosts share it): generate it once, keep it in a password manager, and pass it as the environment variable `Parameters__openiddict-client-secret` in front of the command of step 3 (a name with a hyphen cannot be exported in Git Bash, so use `env 'Parameters__openiddict-client-secret=<value>' aspire deploy ...`; in PowerShell 7 `${env:Parameters__openiddict-client-secret} = "<value>"`).
2. The resource group: `az group create -n simulab-staging -l brazilsouth`.
3. Deploy: the staging command of the table above. The Aspire CLI must be 13.6.0.
4. Learn the names: `az resource list -g simulab-staging -o table`. Note the Key Vault (`keyvault...`) as `<vault>`, the PostgreSQL server (`postgres-...`) as `<server>` and the two storage accounts (`storageapi...`, `storageweb...`, one per host).
5. Give yourself access to the vault's secrets (the owner of a subscription has no data access to a vault by default). In Git Bash and PowerShell 7: `az role assignment create --role "Key Vault Administrator" --assignee-object-id "$(az ad signed-in-user show --query id -o tsv)" --assignee-principal-type User --scope "$(az keyvault show -n <vault> -g simulab-staging --query id -o tsv)"` (PowerShell 7 accepts the same line). The role takes a minute or two to reach the vault.
6. The key that encrypts the Data Protection key ring (never delete or rotate it: every key in the ring becomes unreadable and every tester is signed out, every sent link dies; soft delete keeps a deleted one for 90 days): `az keyvault key create --vault-name <vault> -n dataprotection --kty RSA --size 2048 --ops wrapKey unwrapKey`.
7. The two OpenIddict certificates, created by the vault itself so the private key never touches a disk (the default policy is exportable, valid 12 months, with the key usages OpenIddict checks; the names are the configuration keys `OpenIddict:SigningCertificate` and `OpenIddict:EncryptionCertificate` with `--` for `:`):
   - `az keyvault certificate get-default-policy > policy.json`
   - `az keyvault certificate create --vault-name <vault> -n OpenIddict--SigningCertificate -p @policy.json`
   - `az keyvault certificate create --vault-name <vault> -n OpenIddict--EncryptionCertificate -p @policy.json`
   - Delete `policy.json` afterwards. A certificate renewed by the vault is picked up at the next restart and signs everybody out once; rolling certificates is F-73's subject.
8. The password of the seeded administrator `admin@simulab.local` (12+ characters, upper case, digit, symbol), from a hidden prompt so it never reaches the shell history:
   - Git Bash: `read -rs -p "Admin password: " P; echo; az keyvault secret set --vault-name <vault> -n Identity--SeedAdmin--Password --value "$P" -o none; unset P`
   - PowerShell 7: `$p = Read-Host -AsSecureString "Admin password"; az keyvault secret set --vault-name <vault> -n Identity--SeedAdmin--Password --value (ConvertFrom-SecureString $p -AsPlainText) -o none`
9. Read the address of the ingress (F-64 BR6, D12): open the staging address once in a browser, then `az containerapp logs show -n web -g simulab-staging --type console --tail 100` and look for the warning `A forwarded header arrived from <address> and is ignored`. Do the same for `api` after step 10 (`-n api`): the two addresses may differ. Write them in `src/Hosts/Simulab.AppHost/appsettings.Staging.json` under `ForwardedHeaders:KnownProxies` (one address) or `ForwardedHeaders:KnownNetworks` (a range, only if the addresses change between restarts) and record what you measured in `## Delivery` of F-64. Never a guess.
10. Deploy again (step 3). The apps restart with the certificates, the key and the proxy list. Check `https://<staging address>/api/v1/system/info`: it answers 200 with `"environment": "Staging"`.

The Api migrates the module schemas, roles and permissions and the OpenIddict client on start in Staging only (`Database__ApplyMigrationsOnStart=true`, F-64 BR5). **Production keeps the default (off) until F-65 decides how a release migrates; a production deploy without a migration step breaks nothing in the key ring** (it lives in a blob) **but starts on an empty database.**

### Budget and cost of staging (F-64 BR8, BR9)
- A subscription budget of US$ 80 a month that emails the owner at 50 % and 100 % of the spend. Save this as `budget.json` (replace the date by the first day of the current month and the address by the owner's; Azure takes the amount in the subscription's billing currency, so US$ assumes that currency):
  ```json
  {
    "properties": {
      "category": "Cost",
      "amount": 80,
      "timeGrain": "Monthly",
      "timePeriod": { "startDate": "2026-10-01T00:00:00Z" },
      "notifications": {
        "actual_50": { "enabled": true, "operator": "GreaterThanOrEqualTo", "threshold": 50, "thresholdType": "Actual", "contactEmails": ["owner@example.com"] },
        "actual_100": { "enabled": true, "operator": "GreaterThanOrEqualTo", "threshold": 100, "thresholdType": "Actual", "contactEmails": ["owner@example.com"] }
      }
    }
  }
  ```
  `az rest --method put --url "/subscriptions/<subscription id>/providers/Microsoft.Consumption/budgets/simulab-monthly?api-version=2023-11-01" --body @budget.json`, then read it back with `az rest --method get --url` on the same address: both notifications must be there. The budget only warns; it stops nothing. `/agile:publish` writes its own budget on the resource group at 80 % and 100 % (a second, different one): deploy staging with the command of the table, not with `/agile:publish`, or accept both.
- The measured cost (BR8) is read after one full test window followed by 7 parked days, in two periods: the days it ran and the parked days. Save as `cost.json` (dates are the period): `{ "type": "ActualCost", "timeframe": "Custom", "timePeriod": { "from": "2026-10-10T00:00:00Z", "to": "2026-10-16T23:59:59Z" }, "dataset": { "granularity": "None", "aggregation": { "totalCost": { "name": "Cost", "function": "Sum" } } } }`, then `az rest --method post --url "/subscriptions/<subscription id>/resourceGroups/simulab-staging/providers/Microsoft.CostManagement/query?api-version=2023-11-01" --body @cost.json`. The figures lag 8 to 24 hours behind the spend; the service answers 429 after a handful of queries in a minute. The monthly figure is running days × running cost per day + parked days × parked cost per day, with the number of test days a month the owner expects; plan B of ADR-0002 applies when it, plus the ADR's production estimate, is above US$ 80.

### Staging start and stop (ADR-0002, BR3)
Staging runs during test windows and is parked outside them. These commands are **declared, not yet run**: step 4 above gives the resource names (`<rg>` is the resource group of the environment, `<server>` its PostgreSQL server). A parked staging keeps its data: the key ring is a blob and the Redis container holds only sessions (they are lost on a park and testers sign in again).
- Park: `az containerapp update -n api -g <rg> --min-replicas 0`, the same for `web` and `redis`, then `az postgres flexible-server stop -g <rg> -n <server>`. A parked staging keeps the PostgreSQL storage and the registry (about US$ 12/month, estimate); the PostgreSQL server restarts by itself after 7 days, so a long park needs the stop repeated.
- Start: `az postgres flexible-server start -g <rg> -n <server>`, then `az containerapp update -n redis -g <rg> --min-replicas 1`, `-n api --min-replicas 1`, `-n web --min-replicas 0 --max-replicas 1` (the `web` wakes on the first request).
- Production is not created until the first release and is never parked.

### What the deploy creates for email (F-66)
`aspire deploy` creates, from `src/Hosts/Simulab.AppHost/Bicep/email.bicep`: an Email Communication Service and its Azure-managed domain (sender `DoNotReply@<id>.azurecomm.net`, display name `Simulab`), a Communication Service linked to that domain, both with data location Brazil (ADR-0003), and one role assignment, "Communication and Email Service Owner" on the Communication Service alone, for the Api's managed identity alone (a custom send-only role would need `roleDefinitions/write`, which the deploying account does not have). No manual step and no secret. Staging and production send through it over its HTTP API; any recipient is allowed. The Azure-managed domain has low sending limits, which Simulab's own domain (F-87) lifts; bounces and spam reports are F-88. The processor of the emails is Microsoft (Azure Communication Services).

Data region: every user's data, Portuguese users included, is hosted in Brazil South; Brazil is adequate under Art. 45 GDPR, so no EU region is planned (ADR-0003, with the triggers that reopen it).

## Code hosting, CI and board
| What | Status | Where |
|---|---|---|
| Git remote | provisioned | GitHub, repository `alexcanario/simulab` |
| CI (build + tests on pull requests and `main`, no deploy stages; F-62) | provisioned | GitHub Actions, `.github/workflows/ci.yml` |
| Deploy workflow (OIDC sign-in, GitHub environments, approval for production) | planned | GitHub Actions, F-65 |
| Board | provisioned | GitHub Issues + Projects, repository `alexcanario/simulab` |

## Expected secrets
Names only. None exists yet; each arrives with the feature that needs it.
| Name | Used by | Kept in (per environment) |
|---|---|---|
| `ConnectionStrings:simulab` | Api | local: generated by the app host; cloud: the PostgreSQL server's generated login and password (`WithPasswordAuthentication`, F-64 D15: the hosts use plain Npgsql, so the server's Entra-ID-only default does not work), kept by the deploy as a Key Vault secret and handed to the container app by reference. The Web gets no database connection (F-64) |
| `ConnectionStrings:mailpit` | Api | local: generated by the app host from the mapped SMTP endpoint (`smtp://host:port`; the MailPit resource's own connection string carries the container port and is not used); cloud: not used |
| `Identity:VerificationUrl` | Api | local: set by the app host to the Web's `/verify-email`; cloud: the public Web address |
| `Identity:PasswordResetUrl`, `Identity:ForgotPasswordUrl` | Api | local: set by the app host to the Web's `/reset-password` and `/forgot-password` (F-7); cloud: the public Web address |
| `Identity:SignUpUrl` | Api | local: set by the app host to the Web's `/sign-up`; cloud: the public Web address. The farewell email of an erased account points at it (F-10) |
| `ForwardedHeaders:KnownProxies`, `ForwardedHeaders:KnownNetworks` | Api, Web | not secrets. Both hosts believe `X-Forwarded-For` and `X-Forwarded-Proto` only from the addresses listed here (B-4, F-64 BR6). Local: empty (the connection is the visitor). Cloud: **must be filled** with the ingress address measured on the first staging (step 9 of "First deploy of staging"), in the app host's `appsettings.<Environment>.json`, which passes them to both hosts; with none listed the Api refuses every token request (it sees plain http) and every visitor looks like the ingress, so the per-client limits become one bucket for the site again. Never set `ASPNETCORE_FORWARDEDHEADERS_ENABLED`: it makes a host believe every sender (the app host removes the one the publish adds) |
| `DataProtection:KeyVaultKeyId` | Api, Web | not a secret. The Key Vault key that encrypts each host's key ring, `https://<vault>.vault.azure.net/keys/dataprotection`, set by the app host (F-64 D9). Without it the keys stay as the framework writes them |
| `ConnectionStrings:keys` | Api, Web | the blob container `keys` of each host's own storage account, holding its Data Protection key ring (`simulab-api.xml`, `simulab-web.xml`), set by the app host in the cloud; **absent locally**, where the framework's own store is used. The hosts reach it with their managed identity, no key (F-64) |
| `ConnectionStrings:keyvault` | Api | the address of the Key Vault, set by the app host in the cloud: the Api reads the vault's secrets as configuration (`Identity--SeedAdmin--Password` is `Identity:SeedAdmin:Password`, `OpenIddict--SigningCertificate` is `OpenIddict:SigningCertificate`). Absent locally (F-64 BR7). The vault is added to the configuration **last**: a secret with the same key as an app host setting or an environment variable wins over it (an emergency override set on the container app is ignored while such a secret exists; the generated `connectionstrings--*` secrets are in the vault too) |
| `Database:ApplyMigrationsOnStart` | Api | not a secret. Development: true by default. Staging: set to true by the app host (F-64 BR5). Production: off until F-65 decides how a release migrates |
| `Legal:RootPath` | Api | optional; defaults to the `Content/Legal` folder shipped with the Api |
| `Email:Provider` | Api | not a secret (F-66 BR3). `Smtp` (the default; local: `appsettings.Development.json`) or `AzureCommunicationServices` (cloud: set by the app host). In Staging and Production the Api refuses to start unless it is `AzureCommunicationServices` |
| `Email:AzureCommunicationServices:Endpoint` | Api | not a secret. The address of the Communication Service, set by the app host from the Bicep output (`Email__AzureCommunicationServices__Endpoint`); required when the provider is Azure |
| `Email:FromAddress`, `Email:FromName` | Api | not secrets. Local: `appsettings.Development.json` (`no-reply@simulab.app`); cloud: the Azure-managed sender `DoNotReply@<id>.azurecomm.net`, set by the app host, until F-87 brings Simulab's own domain. `FromName` is `Simulab` |
| `Email:Host`, `Email:Port`, `Email:UseStartTls`, `Email:UserName`, `Email:Password` | Api | the SMTP provider only: local Mailpit (`appsettings.Development.json` plus the app host SMTP connection string); not used in the cloud |
| `AZURE_CLIENT_ID` | Api | not a secret. Client id of the Api's managed identity; Aspire sets it on the container app. The Azure sender signs in with that identity: no email key, connection string or client secret exists in any environment (F-66 BR1) |
| `Jobs:WorkerEnabled` | Api | optional, defaults to true. The test host sets it to false and runs the jobs itself (F-13 BR13); a second Api replica may keep it true — the claim uses `FOR UPDATE SKIP LOCKED`, so no message is sent twice |
| `ConnectionStrings:redis` | Api, Web | local: generated by the app host (TLS, password); cloud: Key Vault |
| `Authentication:OpenIddict:ClientId` | Web | local: `appsettings.Development.json` (`simulab-web`); cloud: same value, not a secret |
| `Authentication:OpenIddict:ClientSecret` | Api, Web | local: `appsettings.Development.json`, same value on both hosts; cloud: the AppHost parameter `openiddict-client-secret`, supplied at deploy time as the environment variable `Parameters__openiddict-client-secret` (F-62); the same value goes to both hosts |
| `Identity:SeedAdmin:Password` | Api | the password of `admin@simulab.local` (F-52). Optional: without it nothing is seeded and the app starts normally. Local: the Api project's user secrets, or the development password the owner keeps in `appsettings.Development.json` (an accepted exception since F-44, 2026-10-02; the base `appsettings.json` carries none); cloud: Key Vault, as the environment variable `Identity__SeedAdmin__Password`. Never in a committed file other than that one; it must satisfy the password policy (12+ characters, upper case, digit, symbol) or the start fails |
| `Identity:SeedAdmin:RequirePasswordChange` | Api | not a secret (F-53). Whether the seed marks `admin@simulab.local` "must change password" when it creates the account: the first sign-in then asks for a new password before anything else. Optional, defaults to true; `appsettings.Development.json` sets it to false so the development password keeps working. An account that already exists is never marked |
| `ConnectionStrings:blobs` | Api | local: Azurite, generated by the app host; cloud: Key Vault |
| `Ai:ApiKey` | Api | the Claude API key (F-41). Local: the app host's user secrets under `Ai:ApiKey`, passed to the Api as a masked parameter; cloud: Key Vault. **Optional**: without it the app starts normally and every model call answers `ai.not_configured`, so nobody needs a key to work on the rest of the app |
| `Ai:DefaultModel`, `Ai:MaxTokens`, `Ai:Models:*`, `Ai:Prices:*` | Api | not secrets: `appsettings.json` carries `claude-opus-5`, 16000 tokens and the price per million for the models in use. `Ai:Models:<purpose>` sends one purpose to another model; `Ai:Prices:<model>` is what the recorded cost is computed from, so a price that changes is updated there (F-41, BR6, BR7) |
| `Entitlements:Limits:*`, `Entitlements:Features` | Api | not secrets: the stand-in for the `Plans` module (F-41). `Entitlements:Limits:ai.calls_per_month` is the ceiling per user per calendar month; absent means no ceiling. The real module replaces the implementation, and these keys go with it |
| `Database:Name` | AppHost | optional, defaults to `simulab`. Changes only the physical database the app host creates; every connection string stays `simulab`. A worktree sets it (`Database__Name=simulab_f41`) so an item's migration never lands in the shared local database (F-41) |
| `Identity:GoogleSignInEnabled` | Api, Web | the Google sign-in switch (F-20 BR1). Default false on both hosts; local: set to true by the app host only when `Google:ClientId` and `Google:ClientSecret` are in the app host's user secrets (below); cloud: false in v1 |
| `Authentication:Google:ClientId` | Api, Web | the OAuth client id from Google Cloud; the Api checks every ID token's audience against it (F-20 BR2). Local: passed by the app host from `Google:ClientId`; cloud: not a secret, set when the feature is turned on. Required while the switch is on: both hosts refuse to start without it |
| `Authentication:Google:ClientSecret` | Web | local: passed by the app host from `Google:ClientSecret`, as a masked parameter; cloud: Key Vault. Required on the Web while the switch is on |
| `Identity:TotpEnabled` | Api | the two-factor switch (F-11 BR12). Default false (`appsettings.json` has no value); local: true in `appsettings.Development.json`; cloud: false in v1. The Web has no copy: it asks the Api |
| `Identity:TotpEncryptionKey` | Api | base64 of 32 random bytes; required only while `Identity:TotpEnabled` is true, and the Api refuses to start without a valid one (F-11 AC14). Local: a development-only key in `appsettings.Development.json`; cloud: Key Vault. **Losing or changing it invalidates every enrolment and every recovery code**: the secrets are encrypted and the codes hashed with it, so each user would have to turn two-factor off and on again (which needs a code they can no longer produce) |
| `OpenIddict:SigningCertificate`, `OpenIddict:EncryptionCertificate` | Api | the certificates OpenIddict signs and encrypts tokens with. Local (Development): development certificates (F-5), nothing to set. Every other environment: the base64 of a PKCS#12 file without a password, read from Key Vault (certificates `OpenIddict--SigningCertificate` and `OpenIddict--EncryptionCertificate`, step 7 of "First deploy of staging"); the Api **refuses to start** without them and the message names the missing key (F-64 BR4). Changing them signs everybody out |

## Google sign-in locally (F-20)
Off unless the app host finds an OAuth client in its user secrets. To turn it on:
1. In the Google Cloud console, open *APIs & Services → OAuth consent screen*: user type *External*, publishing status *Testing*, and add the Google accounts that will sign in as *Test users* (only they can, while the app is in testing).
2. *APIs & Services → Credentials → Create credentials → OAuth client ID*: type *Web application*. Under *Authorized redirect URIs*, paste exactly `https://localhost:7125/signin-google` — no trailing slash; Google compares it letter by letter and answers `redirect_uri_mismatch` otherwise. Leave *Authorized JavaScript origins* empty.
   - `https://localhost:7125` is the Web's local address (the https port fixed in `src/Hosts/Simulab.Web/Properties/launchSettings.json`).
   - `/signin-google` is where Google sends the visitor back after the sign-in (`GoogleSignInSettings.CallbackPath` in the code).
3. Keep the client id and secret in the app host's user secrets (never in a file of the repository):
   ```
   dotnet user-secrets set "Google:ClientId" "<client id>" --project src/Hosts/Simulab.AppHost
   dotnet user-secrets set "Google:ClientSecret" "<client secret>" --project src/Hosts/Simulab.AppHost
   ```
4. Start the app host. The dashboard shows `Identity__GoogleSignInEnabled=true` on `api` and `web`, and the sign-in and sign-up pages show "Continue with Google".

To turn it off again: `dotnet user-secrets remove "Google:ClientId" --project src/Hosts/Simulab.AppHost`. The public address's redirect URI and the production client are release work (F-20, out of scope).

The Web keeps its short-lived sign-in tickets in memory (`SignInTicketStore`, `GoogleSignUpTickets`, `CodeStepTickets`, all on `SingleUseTickets<T>`): it runs as **one instance**. A second Web instance needs them moved to Redis first, or a sign-in that lands on the other instance finds no ticket.

The Api is capped at **one replica** in the publish model (`minReplicas: 1`, `maxReplicas: 1`, F-54 BR7): OpenIddict signs tokens with development certificates per machine, so a token from one replica is rejected by another (F-73 shares the keys and lifts the cap). The rate limits already count in Redis and hold at any replica count. The setting `Identity:RateLimit:Namespace` exists for tests only (it gives each test host its own counters); it must stay unset in every deployed environment, or each replica would count alone again.

## The first Admin (F-9, BR11)
Every new account gets `Student` automatically (F-6), and roles are given on the back office screen `/admin/users` (F-9) by someone who already manages roles. The very first Admin of an installation has nobody to give it, so it is inserted directly against the module's database, once:
```sql
insert into identity.user_roles (user_id, role_id)
select u.id, r.id
from identity.users u, identity.roles r
where u.email = '<email>' and r.name = 'Admin';
```
The Web picks up the change on the next page load, at most a minute after its last check of the session (B-3, BR4); no new sign-in is needed. The Api's own check (F-6 BR3) reflects it within 10 seconds regardless. From then on, use `/admin/users`; the back office refuses any change that would leave no active account able to manage roles (F-9, BR8).

## Release steps
1. No release process yet.

## Measured times
| What | Budget | Last measured (date) |
|---|---|---|
| Full build | | 23 s, 0 new warnings (2026-10-06, F-66). Earlier, F-63: 17 s; F-59: 23 s; F-57: 17 s; B-24: B-24: 28 s |
| Full test suite | < 5 min | 2350 tests in 89 s, slowest projects 1 m 24 s (Identity) and 1 m 21 s (Catalog), 23 s build (2026-10-06, F-66). Earlier, F-63: 2323 tests in 88 s, slowest projects 1 m 23 s (Identity) and 1 m 22 s (Catalog), 17 s build. Earlier, F-59: 2312 tests in 105 s, slowest projects 1 m 40 s (Identity) and 1 m 32 s (Catalog), 23 s build. Earlier, F-57: 2307 tests in 92 s, slowest projects 1 m 27 s (Identity) and 1 m 26 s (Catalog), 17 s build (2026-10-06, F-57). Earlier, B-24: 2219 tests in 116 s, slowest project 1 m 48 s (Identity, projects run in parallel, machine shared with other sessions), 28 s build. Earlier, F-54: 2215 tests in 76 s. Earlier, F-51: 2198 tests in 141 s; its first full run failed 24 Identity tests, the same project alone passed 408 of 408 and a second full run was green |

Until B-19 the suite was not reliably green under its own parallel load: 2 of 3 full runs failed on a test the
change had nothing to do with. B-19 did not close it: on 2026-10-05 (F-51) one full run failed 24 Identity tests
that passed alone, and B-24 reproduced it under load (about 1 process run in 11) as a starved thread pool: 24 Api
hosts start at once, each blocking a pool thread, and Npgsql's 15 s open timeout expires. Since B-24 every test
project raises `System.Threading.ThreadPool.MinThreads` to 256 (`tests/Directory.Build.props`, guarded by
`ThreadPoolMinimumTests` in each project that starts a host). With it, 10 load rounds in a row (full suite plus 3
extra Identity processes, 40 Identity process runs) were all green. A red full run is a real failure from here on,
as long as that setting stays. Timings are noisy when other sessions share the machine (alternating runs on the
same binaries, 2026-10-06: 113 s and 85 s with the setting, 98 s and 101 s without it).

## Technical docs
`docs/architecture/` is generated by `tools/Simulab.DocGen` (F-15) and never edited by hand. The route map reads `docs/api/Simulab.Api.json`, which the Api host test `OpenApi_document_is_written_to_docs_api` rewrites when the served document changes, so run the suite first.
- Regenerate: `dotnet run --project tools/Simulab.DocGen`
- Check (exit 1 when stale, used by `/agile:ship`): `dotnet run --project tools/Simulab.DocGen -- --check`
- Entity diagrams are DBML (`<Module>/schema.dbml`, F-26): open them with the dbdiagram VS Code extension ("DBML: Open Preview to the Side").
- The descriptions in the data dictionaries and the DBML notes come from `HasComment` in the EF configurations (F-24), so they are also real `COMMENT ON` in PostgreSQL: psql (`\d+ identity.users`) and DataGrip show them. A column of a table Simulab defines cannot be added without one — `TableDescriptionTests` fails the build and names it.
- The modules the generator documents and the modules the architecture tests load are the same set by construction (F-45): the tests
  load the closure of DocGen's own project references, and `DocGenModelDriftTests` fails when a folder under `docs/architecture/`
  has no model loaded, or a loaded model has no folder. Before F-45 the list was written by hand and stood four weeks behind the
  generator, so `ai_calls` was documented and covered by no rule.
- The hand-written C4 overview (context and containers, F-23) lives outside that folder, because DocGen deletes every file it does not generate: [`docs/architecture-overview.md`](architecture-overview.md). `ArchitectureOverviewTests` fails when an app host resource is missing from the containers diagram, or when a node still marked `planned` has been built.
