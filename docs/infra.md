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

### Staging start and stop (ADR-0002, BR3)
Staging runs during test windows and is parked outside them. These commands are **declared, not yet run**: no environment exists until F-64 creates it, which will confirm the resource names (`<rg>` is the resource group of the environment, `<server>` its PostgreSQL server).
- Park: `az containerapp update -n api -g <rg> --min-replicas 0`, the same for `web` and `redis`, then `az postgres flexible-server stop -g <rg> -n <server>`. A parked staging keeps the PostgreSQL storage and the registry (about US$ 12/month, estimate); the PostgreSQL server restarts by itself after 7 days, so a long park needs the stop repeated.
- Start: `az postgres flexible-server start -g <rg> -n <server>`, then `az containerapp update -n redis -g <rg> --min-replicas 1`, `-n api --min-replicas 1`, `-n web --min-replicas 0 --max-replicas 1` (the `web` wakes on the first request).
- Production is not created until the first release and is never parked.

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
| `ConnectionStrings:simulab` | Api | local: generated by the app host; cloud: Key Vault |
| `ConnectionStrings:mailpit` | Api | local: generated by the app host from the mapped SMTP endpoint (`smtp://host:port`; the MailPit resource's own connection string carries the container port and is not used); cloud: not used |
| `Identity:VerificationUrl` | Api | local: set by the app host to the Web's `/verify-email`; cloud: the public Web address |
| `Identity:PasswordResetUrl`, `Identity:ForgotPasswordUrl` | Api | local: set by the app host to the Web's `/reset-password` and `/forgot-password` (F-7); cloud: the public Web address |
| `Identity:SignUpUrl` | Api | local: set by the app host to the Web's `/sign-up`; cloud: the public Web address. The farewell email of an erased account points at it (F-10) |
| `ForwardedHeaders:KnownProxies`, `ForwardedHeaders:KnownNetworks` | Web | local: empty (the connection is the visitor); cloud: **must be filled** with the ingress addresses or ranges when the environment is created, or every visitor looks like the ingress and the per-client limits become one bucket for the site again (B-4) |
| `Legal:RootPath` | Api | optional; defaults to the `Content/Legal` folder shipped with the Api |
| `Email:*` (`FromAddress`, `FromName`, `Host`, `Port`, `UseStartTls`, `UserName`, `Password`) | Api | local: `appsettings.json` plus the app host SMTP connection string; cloud: Key Vault |
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
| `Email:SendGrid:ApiKey` | Api | local: not used (Mailpit); cloud: Key Vault |
| `Identity:GoogleSignInEnabled` | Api, Web | the Google sign-in switch (F-20 BR1). Default false on both hosts; local: set to true by the app host only when `Google:ClientId` and `Google:ClientSecret` are in the app host's user secrets (below); cloud: false in v1 |
| `Authentication:Google:ClientId` | Api, Web | the OAuth client id from Google Cloud; the Api checks every ID token's audience against it (F-20 BR2). Local: passed by the app host from `Google:ClientId`; cloud: not a secret, set when the feature is turned on. Required while the switch is on: both hosts refuse to start without it |
| `Authentication:Google:ClientSecret` | Web | local: passed by the app host from `Google:ClientSecret`, as a masked parameter; cloud: Key Vault. Required on the Web while the switch is on |
| `Identity:TotpEnabled` | Api | the two-factor switch (F-11 BR12). Default false (`appsettings.json` has no value); local: true in `appsettings.Development.json`; cloud: false in v1. The Web has no copy: it asks the Api |
| `Identity:TotpEncryptionKey` | Api | base64 of 32 random bytes; required only while `Identity:TotpEnabled` is true, and the Api refuses to start without a valid one (F-11 AC14). Local: a development-only key in `appsettings.Development.json`; cloud: Key Vault. **Losing or changing it invalidates every enrolment and every recovery code**: the secrets are encrypted and the codes hashed with it, so each user would have to turn two-factor off and on again (which needs a code they can no longer produce) |
| `OpenIddict` signing and encryption certificates | Api | local: development certificates (F-5); cloud: Key Vault, planned |

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
| Full build | | 17 s, 0 new warnings (2026-10-06, F-57). Earlier, B-24: 28 s |
| Full test suite | < 5 min | 2307 tests in 92 s, slowest projects 1 m 27 s (Identity) and 1 m 26 s (Catalog), 17 s build (2026-10-06, F-57). Earlier, B-24: 2219 tests in 116 s, slowest project 1 m 48 s (Identity, projects run in parallel, machine shared with other sessions), 28 s build. Earlier, F-54: 2215 tests in 76 s. Earlier, F-51: 2198 tests in 141 s; its first full run failed 24 Identity tests, the same project alone passed 408 of 408 and a second full run was green |

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
