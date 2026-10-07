# Profile: modular-monolith

One deployable, several business modules with clear boundaries. Each module is **one project plus a contracts project** — not a set of layers.

## Simulab specifics (ADR-0001)
`<App>` is `Simulab`. The solution is `Simulab.slnx` at the repository root, with Central Package Management (`Directory.Packages.props`) and shared properties (`Directory.Build.props`).

| Module | Shape | Origin |
|---|---|---|
| Identity | five projects | Simulae `Identity` |
| Catalog | five projects | Simulae `ContentCatalog` (organizers, exams, editions, taxonomy, question bank). One module for the three epics that fill it — Assessment catalog (691), Subject taxonomy (692) and Question bank (693): they share invariants and transactions, and splitting them would put `Contracts` between things that change together (owner, 2026-09-20) |
| ExamEngine | five projects | Simulae `ExamEngine` (simulators, scoring, timer) |
| Plans | five projects | Simulae `Billing` (plans, assignments, promo codes, usage meter, `IEntitlementService`) |
| New modules (import, analytics, recommendations, coach) | one project + `Contracts`, unless refinement shows a rich domain | new |

- Two deployable hosts: `Simulab.Api` (also runs the background job worker) and `Simulab.Web`.
- Building blocks besides `SharedKernel`: `Simulab.Persistence` (F-3: `ModuleDbContext` base, audit and soft-delete interceptor, tenant and soft-delete filters, unique-index helper, database health check), `Simulab.Email` (F-3: `IEmailSender`; F-66: over SMTP or Azure Communication Services, by `Email:Provider`), `Simulab.Jobs` (F-13: the `jobs` schema, `IJobQueue`, `JobRunner` and the hosted `JobWorker` of ADR-0001 #20), `Simulab.Ai` (`IAiGateway`, usage and cost records), `Simulab.ApiResults` (F-39: `ApiProblem`, the one mapping from an `Error` to an HTTP answer; the only building block that carries ASP.NET), `Simulab.Storage` (`IFileStorage`), and the in-process integration events in `SharedKernel`. Created by the first feature that needs them, not before.
- `Simulab.Persistence` references `SharedKernel` only; `SharedKernel` never references EF Core or ASP.NET. Each module keeps its own `DbContext`, schema and migrations and inherits `ModuleDbContext`; architecture tests check both.
- Test helpers shared by test projects live in `tests/Simulab.Testing` (one PostgreSQL container per test project, a database per test class) and, since F-33, in `tests/Simulab.Testing.ApiHost`: the Api host every module's integration tests run against (`SimulabApiFactory`, `ApiHostTests`, `TestAccounts`, `TokenClient`). The two stay separate on purpose — a project that only needs a container, and `Simulab.Web.Tests`, whose own host has a top-level `Program` that would clash with the Api's, must not reference the Api host.
- A module declares its permission names as a `PermissionCatalog` in the container (F-33); Identity owns the `permissions` table and seeds the union, granting every name to `Admin`. A catalog may also declare `InitialGrants` (permission to role names, never `Admin`): Identity applies them once, in the start that creates the permission row, so the roles back office stays the only place that changes them afterwards (F-36). The Web lists the same catalogs in `WebPermissions` to build one policy per permission.
- `User` inherits `IdentityUser<Guid>` and repeats the `TenantEntity` fields: a known exception in the architecture tests.
- Forbidden references, checked by the architecture tests: MassTransit, RabbitMQ, FluentAssertions.
- Importing from Simulae: per feature, renamed to this layout (`Modules.<Module>.<Layer>` becomes `Simulab.<Module>.<Layer>`, `Simulae.Common` becomes `Simulab.SharedKernel`), UI text extracted to the three languages, tests included and moved to the shared PostgreSQL fixture.
- The AI gateway is faked in tests. Golden-exam extraction checks run on demand, never in CI.

## Layout
```
src/
  Hosts/
    <App>.AppHost/               local orchestration (Aspire)
    <App>.ServiceDefaults/       telemetry, health, resilience
    <App>.Api/                   host only: composition, auth, middleware, OpenAPI. No business code.
    <App>.Web/                   Blazor UI. Talks to the API through typed clients. No business rules.
  BuildingBlocks/
    <App>.SharedKernel/          Entity, TenantEntity, Result/Error, clock, AppJson.Options
    <App>.Persistence/ ...       every building block (Email, Ai, Storage) goes here
  Modules/<Module>/
    <App>.<Module>/
      Features/<Feature>/        vertical slice: endpoint, request/response, handler, validator
      Domain/                    entities and value objects that carry rules
      Data/                      DbContext (own schema), Configurations/, Migrations/
      Resources/                 <Module>.resx + pt-BR, pt-PT, en
      <Module>Module.cs          AddXxxModule() / MapXxxEndpoints()
    <App>.<Module>.Contracts/    what other modules may see: DTOs, query interfaces, integration events
tests/
  Hosts/<App>.Web.Tests/         bUnit (Api and AppHost tests sit next to it)
  BuildingBlocks/<App>.<Block>.Tests/
  Modules/<Module>/<App>.<Module>.Tests/   unit + integration for the module
  <App>.ArchitectureTests/       serves every group
  <App>.Testing/                 shared test helpers
tools/
  <App>.DocGen/                  generates docs/architecture/ (F-15); never deployed
```
Technical docs (F-15): `tools/Simulab.DocGen/` generates `docs/architecture/` from the EF model (built through each context's design-time factory, as the migrations), the OpenAPI document in `docs/api/` (written by the Api host test that fetches `/openapi/v1.json`) and the project references; `/agile:ship` regenerates it after the full suite and `--check` fails when it is stale. Every file there is generated; a hand-written page goes elsewhere. Tools offered to a model (F-49; none yet, the AI coach adds the first): `ModelToolAttribute` lives in `Simulab.Ai`, DocGen finds it by name and writes `docs/architecture/tools.md` (an empty catalogue while there is no tool) listing every tool with its permissions and what it reaches; `--check` fails on a tool with no description, permissions or reaches, or a repeated name.
Simulab (F-12): the solution folders in `Simulab.slnx` mirror these disk folders, and `SolutionLayoutTests` checks it. `Directory.Build.props` exists only at the root and in `tests/`; a group gets its own file only when it has a first property to share. `Directory.Packages.props` stays single, at the root.
Every solution root also has `Directory.Build.props`, `Directory.Packages.props` (central package versions), `.editorconfig`, `BannedSymbols.txt` and `global.json`, copied from the plugin at bootstrap; rules the build can check live there, and the Stop gate fails on new warnings.
The app version is one `<Version>` on `<App>.Api.csproj` alone — never on `Directory.Build.props`, and never on `SharedKernel`, a module, a `Contracts` project, `ServiceDefaults` or `AppHost`. Bootstrap seeds `<Version>0.1.0</Version>`; `/agile:ship` bumps it once per ship: MINOR for a feature (PATCH resets to 0), PATCH for a bug, or MAJOR when the item's `## Decisions` records a breaking change (MINOR and PATCH reset to 0).
Code reaches an Aspire resource (Redis, PostgreSQL, a broker) through its client integration package (`builder.Add<X>Client(...)`), never a driver connection built from the raw connection string: Aspire runs local resources with TLS and a password by default, and a plain driver hangs on the dev certificate.
The `ServiceDefaults` project the bootstrap generates calls `AddStandardResilienceHandler(options => options.Retry.DisableForUnsafeHttpMethods())`: by default the handler retries a POST, which replays single-use tokens and sends emails twice.
With Blazor Interactive Server, state that changes during a session (rotating tokens, permissions) lives in a server-side store keyed by an id the cookie carries, never in the cookie itself: a circuit cannot rewrite the cookie. The cookie is checked in `OnValidatePrincipal`, and open circuits revalidate through `RevalidatingServerAuthenticationStateProvider`.
With Blazor Interactive Server, data from the first HTTP request that a circuit needs later (the visitor's address, a header) is read in `App` (static render, `HttpContext` available), passed to the interactive root component as a parameter and kept in a scoped service: a circuit has no `HttpContext`.
With Blazor Interactive Server, a page that peeks a single-use ticket (an invite, a confirmation link) in `OnInitialized` sees it twice — prerender runs the component before the interactive circuit does — so it reads the ticket there but spends it only on success, in the handler that acts on it; a ticket that must not be replayed from a copied link is bound to the browser with an HttpOnly cookie, never carried by the URL alone.
A failed `SaveChanges` keeps the entry state you set: after `Remove` the entry stays `Deleted`, so a later save repeats the DELETE instead of writing what the error handler just set. The delete that closes a unit of work goes outside the `try` that handles that unit of work's own failure.
With ASP.NET Core Identity, a sign-in with more than one step (password, then a second factor) clears the failure count (`ResetAccessFailedCountAsync`) only in its last step: a step that clears it gives the next one unlimited tries.

## Where business rules live
- A rule about one entity lives in that entity (`Domain/`): methods that return `Result`, no public setters on ruled state.
- A rule that needs data or several entities lives in the feature handler.
- CRUD without rules is a thin slice: endpoint → `DbContext`. No repository, no mediator, no mapping library, no interface with a single implementation.
- Add structure on the second use, not the first: a domain service when two features share a rule, an abstraction when a second implementation exists.
- Modules talk only through `Contracts`: a query interface or an in-process integration event. Never another module's `DbContext`, tables or entities.
- A row a request must not lose (a queued job, an outbox message) is written by the same `SaveChanges` as the data that justifies it: the shared table is mapped into the caller's own `DbContext` with `ExcludeFromMigrations()`, and the context that owns the table keeps the migration. Two contexts sharing one connection is neither needed nor available with Npgsql.
- EF mappings are `IEntityTypeConfiguration<T>` classes in `Data/Configurations/`. Tenant and soft-delete filters are global, never repeated per query.
- The UI validates for comfort only. The API is the authority.
- The default is per module, not per solution: a module may use the Clean Architecture variant below while its neighbours stay with one project.

## Variant: Clean Architecture module (five projects)
Suggest it, with the reason, when a module has a rich domain (many invariants, state machines, calculations), more than one delivery mechanism (API + worker + import), or infrastructure that is likely to be swapped. The owner decides; record it in an ADR. Never apply it to a CRUD module.
```
Modules/<Module>/
  <App>.<Module>.Domain/           entities, value objects, domain events, domain services. References SharedKernel only.
  <App>.<Module>.Application/      use cases (handlers), validators, ports (interfaces) the use cases need. References Domain + Contracts.
  <App>.<Module>.Infrastructure/   DbContext, Configurations/, Migrations/, adapters for the ports. References Application.
  <App>.<Module>.Api/              endpoints, request/response mapping, <Module>Module.cs. References Application (+ Infrastructure for DI only).
  <App>.<Module>.Contracts/        what other modules may see.
```
- The dependency rule points inward: `Api → Application → Domain`, `Infrastructure → Application`. `Domain` and `Application` never reference EF Core or ASP.NET.
- A port exists only when `Application` needs something from outside (persistence with rules, clock, external service). Simple reads may use a query interface over the `DbContext`.
- The architecture tests add these directions for the module, and the Stop gate treats the five projects as one unit through their references.

## Test strategy and time budget
| Level | What | Budget |
|---|---|---|
| Unit | Domain rules and handlers without I/O | < 30 s per project |
| Integration | Slice through HTTP (`WebApplicationFactory`) on real PostgreSQL | < 2 min per project |
| UI | bUnit, only for screens with logic | < 30 s |
| Full suite | Everything, only on ship | < 5 min |

- **One PostgreSQL container per test project** (Testcontainers, assembly-level fixture). Isolate with a database per test class or a reset between tests — never a container per class.
- A test host (`WebApplicationFactory`) created per test class clears its Npgsql pool on dispose (`NpgsqlConnection.ClearPool`): idle pooled connections of disposed hosts exhaust the container's `max_connections` (`53300: too many clients`).
- Dispose every host, factory and Aspire builder (`await using`). One leaked builder turned 143 ms of tests into 7 minutes.
- No `Thread.Sleep` and no fixed delays: wait on a condition with a timeout. The gate runs tests with a hang timeout (120 s per test), so a hung test fails with its name.
- bUnit with a component library that owns async services (MudBlazor): one shared test context with async disposal; never `await InvokeAsync` around a call that waits for a dialog result.
- bUnit: after a click whose handler awaits (an Api call, `Task.Yield`), assert with `WaitForAssertion`: `Click()` returns when the handler first yields, not when it finishes. Anything that follows a click (a snackbar, a dialog closing, JS interop) is asserted the same way, in the tests written today too.
- A Razor attribute mistake compiles: a string attribute without `@` is literal text (`Value="_name"` sets the two words), and a generic type parameter under the wrong name fails only when the component renders. Every new page and dialog gets a bUnit test that renders it with its real parameters.
- xUnit + AwesomeAssertions. FluentAssertions 8+ and MassTransit 9+ have commercial licenses: do not add them. The SDK's `dotnet new xunit` still generates xUnit 2.x: `TestContext.Current.CancellationToken` is v3 only, so a v2 test passes no token (or `CancellationToken.None`). Adopting v3 is a decision, pinned in `Directory.Packages.props` and written in the item that makes it.
- A bug starts with a test that fails. A budget overrun is a retro finding, not something to ignore.

## Architecture tests
- A module references another module only through its `Contracts` project.
- `Contracts` projects reference only `SharedKernel`.
- `Domain/` types do not depend on EF Core, ASP.NET or `Data/`.
- `Web` references `Contracts` projects, never a module project.
- `Api` contains no entities and no handlers.
- Vocabulary: identifiers in **every** assembly (modules, Api and Web) are English; forbidden terms come from `docs/glossary.md`.
- Every rule of absence is paired with a rule of presence (the test asserts it matched at least one type). An empty assembly must fail, not pass.
- Screens follow the `ui` rule: a UI kit and a dev-only gallery come before the first screen; pages use the kit, icons go through semantic names, and architecture tests forbid raw icons and raw tables outside the kit. The kit item writes the MudBlazor theme (`MudTheme` palette for light and dark, typography, default border radius) from `docs/design/identity.tokens.json`, and a test loads the tokens and asserts every palette value equals its token.
- Layout: every project sits in the folder the `## Layout` assigns to its kind, and the solution folders mirror the disk folders. The test lists the solution and fails on a project outside its group (rule of presence: it saw at least one project per group).

## Deploy recipe (containers + Aspire, with an AppHost)
> Simulab: the host is Azure Container Apps (ADR-0002); the compose recipe below is its plan B, not the current path.
`docs/infra.md` declares each environment's "Deploy command"; with an AppHost it is `aspire deploy --apphost src/<App>.AppHost/<App>.AppHost.csproj -e <Environment> -o artifacts/deploy/<environment> --clear-cache --non-interactive --nologo`, and `/agile:publish <environment>` runs it (`-o` keeps the `.env.<Environment>` that holds the secrets in plain text under the ignored `artifacts/`; `--clear-cache` keeps no saved value that would win over a new one). The AppHost is written for it once, at bootstrap (measured on Aspire 13.6.0):
- The Aspire CLI, `Aspire.AppHost.Sdk`, `Aspire.Hosting.Docker` and every other `Aspire.*` package of the AppHost are the latest stable and carry the same version (13.6.0 packages with a 13.5.3 CLI fail with `Run completed without returning a backchannel`). `Aspire.Hosting.AppHost` is never a `PackageVersion` in `Directory.Packages.props` (the SDK adds it: NU1009). `<AspireUseCliBundle>true</AspireUseCliBundle>` on the AppHost removes ASPIRE010.
- One compose environment per environment name (else staging replaces production), no dashboard (it publishes a random port on every address of the host), and one named volume per environment for the Data Protection keys, mounted at `/home/app` (another folder is not writable by the `app` user): declared on the service and at the top level, or compose fails with `refers to undefined volume`.
- Each web or API project resource sets `ASPNETCORE_ENVIRONMENT` to the environment name (else staging logs `Production`), marks only its `http` endpoint external with a port pinned in the AppHost's `appsettings.<Environment>.json` (`{ "Deploy": { "HostPort": "<port>" } }`, the port of the URL in `docs/infra.md`; local runs have no such file and get port 0, which is fine; a deployed environment without it gets a random port that changes on every deploy; a second external resource needs its own key, never the same port; never `WithExternalHttpEndpoints()`, which also publishes the https endpoint on a random port) and parses the port with `CultureInfo.InvariantCulture` (CA1305).
- The app calls `AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(path)).SetApplicationName("<App>")` only when `DataProtection:KeysPath` is set: a redeploy then keeps cookies and antiforgery tokens valid, and local development is unchanged.
- A secret is `builder.AddParameter("<name>", secret: true)`, supplied by the environment variable `Parameters__<name>`, and listed in `docs/infra.md` "Expected secrets" with that environment variable as where it is kept; its value is never written down.
- `docker compose down -v` is never run: it deletes the keys volume and, with it, every signed-in session.
```csharp
// <App>.AppHost/Program.cs: using System.Globalization; using Aspire.Hosting.Docker.Resources.ServiceNodes;
var environmentName = builder.Environment.EnvironmentName;
var hostPort = int.Parse(builder.Configuration["Deploy:HostPort"] ?? "0", CultureInfo.InvariantCulture);
var keys = $"keys-{environmentName.ToLowerInvariant()}";
builder.AddDockerComposeEnvironment($"compose-{environmentName.ToLowerInvariant()}")
    .WithDashboard(false)
    .ConfigureComposeFile(file => file.AddVolume(new Volume { Name = keys, Driver = "local" }));
builder.AddProject<Projects.<App>_Api>("api")   // each web or API project resource
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", environmentName)
    .WithEnvironment("DataProtection__KeysPath", "/home/app/keys")
    .WithEndpoint("http", e => { e.Port = hostPort; e.IsExternal = true; })
    .PublishAsDockerComposeService((_, service) => service.AddVolume(new Volume { Name = keys, Source = keys, Target = "/home/app", Type = "volume" }));
```

## Deploy recipe (Azure Container Apps + Aspire)
An environment that runs in the client's Azure subscription (its row in `## Cloud accounts` of `docs/infra.md` says `azure`, with Resource group and Region filled) uses the same `aspire deploy` command as the compose recipe above, and the AppHost then provisions Azure Container Apps instead of a compose environment: `Deploy:Target` (`compose`, the default, or `aca`, any case) in the AppHost's own `appsettings.<Environment>.json` chooses per environment, so staging may stay on a host while production goes to the client's Azure. `/agile:publish <environment>` (`--plan` included) and the pipeline stop an `aca` environment whose row is not `azure` or leaves Resource group or Region blank (they are `Azure__ResourceGroup` and `Azure__Location`) or has no `Monthly budget` (a whole number, the monthly ceiling, or `none`), and the pipeline installs `Aspire.Cli` at the version of `Aspire.AppHost.Sdk` of the AppHost before the command. The AppHost is written for it at bootstrap (measured on Aspire 13.6.0 with `aspire publish -e Staging`, 11 of 11 steps, and a local run):
- Both shapes live in one `Program.cs`; Azure resources are declared only in publish mode with `Deploy:Target` `aca`, so a local run and a `compose` environment need no Azure sign-in, and the compose block of the recipe above moves under `if (compose)` (both declared, the publish fails with `Compute resource(s) 'api', 'web' are not assigned to a compute environment, but the model contains multiple compute environments`). A local run keeps the database as a container with its password.
- The packages are the compose recipe's plus `Aspire.Hosting.Azure.AppContainers`, `Aspire.Hosting.Azure.Storage` and `Aspire.Hosting.Azure.PostgreSQL` or `Aspire.Hosting.Azure.Sql` (and `Aspire.Hosting.PostgreSQL` or `Aspire.Hosting.SqlServer` for the local container), all at the version of the CLI; each web or API project adds `Aspire.Azure.Storage.Blobs` and `Azure.Extensions.AspNetCore.DataProtection.Blobs` (its latest stable: only the `Aspire.*` packages follow the CLI's version), and a PostgreSQL project `Aspire.Azure.Npgsql.EntityFrameworkCore.PostgreSQL` (`AddNpgsqlDbContext<T>("appdb")`: the Entra ID token in Azure, the container's password locally).
- `AddAzureContainerAppEnvironment("cae").WithDashboard(false)`: it creates its own Azure Container Registry (Basic) and a Log Analytics workspace, images go only to that registry, and the Aspire dashboard component is off (0 matches in `cae.bicep`).
- The database is managed, never a container in Container Apps: `AddAzurePostgresFlexibleServer("postgres")` is `Standard_B1ms` Burstable, 32 GB, version 16, Entra ID sign-in only (`passwordAuth: Disabled`); `AddAzureSqlServer("sql")` is serverless `GP_S_Gen5_2` with `useFreeLimit: true` and `AutoPause`, Entra ID only, and an `AllowAllAzureIps` firewall rule. Each app's managed identity becomes administrator of the server, so migrations run with it.
- Each web or API project resource sets `ASPNETCORE_ENVIRONMENT` to the environment name and `ASPNETCORE_FORWARDEDHEADERS_ENABLED` to `true` (https behind the ingress; a compose environment gets it from Aspire), calls `WithExternalHttpEndpoints()` only when it is public (the other one is `external: false`), and runs `MinReplicas = 0`, `MaxReplicas = 1` through `PublishAsAzureContainerApp`; a host with background work (a queue, e-mail, scheduled jobs) sets `MinReplicas = 1`.
- Data Protection keys go to a blob: `AddAzureStorage("storage")` with `Standard_LRS` set in `ConfigureInfrastructure` (the default is `Standard_GRS`; shared key access is off, the apps reach it by their managed identity), `.AddBlobs("keys")` and `WithReference(keys)` on every web or API project (that reference supplies `ConnectionStrings:keys`; an `aca` project never gets `DataProtection__KeysPath`); the app calls `AddAzureBlobServiceClient("keys")` and `PersistKeysToAzureBlobStorage` only when `ConnectionStrings:keys` is set, else it keeps the compose rule (`DataProtection:KeysPath`), so local development is unchanged.
- A secret is `AddParameter("<name>", secret: true)` as in the compose recipe (`Parameters__<name>`, "Expected secrets"); it becomes a secret of the container app (no Key Vault); a local run needs `Parameters__<name>` set, or the project that uses it stays `Waiting` (`ValueMissing`) and `aspire run` does not fail.
- The recipe creates a user-assigned managed identity per app and role assignments for it, so the client's administrator grants the deploying account Contributor and Role Based Access Control Administrator on the subscription. The address of the app is generated and known after the first deploy: fill URL and Check URL of `docs/infra.md` by hand from the command's output, or write the client's own domain as the URL (the next line).
- A client's own domain on the public app, with a free managed certificate and no step in the Azure portal: write it as the URL of the environment in `## Environments` of `docs/infra.md` (`https://app.client.com` or `https://client.com`; a blank URL, an IP address or a generated `azurecontainerapps.io` host means none; one domain per environment, on the one resource that calls `WithExternalHttpEndpoints()`). Before the command, `/agile:publish <environment>` (`--plan` included) and the pipeline read the app's `customDomainVerificationId`, its generated address and the environment's static IP with `az`, and resolve the records over public DNS; apex or subdomain is decided by the host's own SOA record, never by counting labels (`client.com.br` is an apex). A subdomain needs `CNAME <sub>` pointing straight at the app's generated address (a proxy in between, such as Cloudflare's, blocks issuance and renewal) and `TXT asuid.<sub>` with that code, validation CNAME; an apex needs `A @` pointing at the static IP and `TXT asuid` with the code, validation HTTP; a CAA record on the root must allow `0 issue "digicert.com"`. With a record missing or wrong the deploy runs without the domain and lists each record (type, host, expected value, what was found): the client creates them at their DNS provider and the next deploy goes on; on a first deploy the list comes after the command, with the real values. With the records right the command gets `Deploy__CustomDomain` (the AppHost reads `Deploy:CustomDomain` the way it reads `Deploy:Target`, and only then declares the Aspire parameters `customDomain` and `certificateName` and calls `ConfigureCustomDomain` on the public resource inside its `PublishAsAzureContainerApp` (experimental, `ASPIREACADOMAINS001`: the code block disables the warning around that call); `Deploy:CertificateName` without `Deploy:CustomDomain` is ignored; a `Parameters__` value would be saved by Aspire in its deployment state and read back, so a domain once passed would never leave, measured on 13.6.0), and after the command the plugin has Azure issue the managed certificate (`az containerapp env certificate create`, named `mc-` and the host with hyphens, at most 60 characters), waits up to 10 min and binds it (`az containerapp hostname bind`); every later deploy finds it `Succeeded` and passes `Deploy__CertificateName`, so Aspire keeps the binding `SniEnabled` (without a name the binding is `Disabled`: Aspire never creates the certificate). A certificate still pending ends the run as `deploy ok; certificate pending: the next deploy binds it` (exit 0); one Azure refuses ends it as `deploy ok; domain not bound: <reason>` (a failed run; the deploy is not undone). The Check URL may be the domain: while it is not bound the generated address with the same path is polled instead, and the run says so. The generated address keeps answering and any redirect to the domain is the app's business. The app must stay reachable while Azure issues and renews the certificate: park (minimum 0) keeps the ingress answering, Azure's stop action does not. The plugin binds on the group's one container app with external ingress; none, or more than one, stops the domain step naming the apps and the deploy's own result stands. When the state of the domain cannot be read (public DNS does not answer, `az` fails or lacks its `containerapp` extension, the host has no DNS zone, a certificate of that name belongs to another host) the run stops before the command, because a deploy without the domain would take a live one off the app; blank the URL to deploy without it. An AppHost without the `Deploy:CustomDomain` code (one bootstrapped before this) skips the step and says so. A certificate that failed earlier is deleted and created again by the next deploy that finds the records right. The same code runs in the pipeline with its own login.
- After the deploy command, whatever its exit code while the resource group exists, the plugin writes one monthly budget on that group with `az rest` (`Microsoft.Consumption/budgets` `agile-monthly-ceiling`, api-version `2023-11-01`; a budget declared in the AppHost would fail a month later, measured: the start date of a budget cannot change and cannot be before the current month): the amount is the whole number of the `Monthly budget` cell of `## Cloud accounts` (the billing currency of the subscription), the start date is the first day of the month it was created and every later write keeps it, and Owner and Contributor of the group get Azure's e-mail at 80 % and 100 % of the spend and at 100 % of the forecast. `none` writes nothing and deletes nothing, and a failed write ends the run as `deploy ok; budget not written: <reason>` (the deploy is not undone). The alert only warns: nothing is stopped or scaled.
- `/agile:publish <environment> --park` stops what costs while nobody uses an `aca` environment and `--resume` brings it back; both show their plan (`--plan`) and wait for the owner's yes. Park sets every container app to minimum 0 (its minimum is kept in the tag `agile-min-replicas` of the app) and stops the group's PostgreSQL server after marking it `agile-parked=<UTC date>`, apps first; resume starts the server and removes the mark, then restores each app's minimum. The registry, the Log Analytics workspace, the storage account and the database's disk keep costing (measured 2026-10-06, eastus2, USD: a parked environment costs about 8.7 a month plus what Log Analytics ingests; parking stops the PostgreSQL compute, about 12.4, and a background host at minimum 1, about 11.8 before the monthly free grant); an Azure SQL database pauses itself and is never stopped. Azure starts a stopped PostgreSQL server by itself after 7 days: park again (the mark stays, so a later park knows it was parked). A deploy of a parked environment, local or by the pipeline, starts the PostgreSQL server first and removes the mark, because deploying over a stopped server half deploys (`UpsertServerManagementOperationComputeOnlySupportForStoppedServer`); the apps then get the AppHost's minimums. The deploying account's Contributor role covers all of it.
- `/agile:publish <environment> --cost` reads what an `aca` environment has cost this month: one Azure Cost Management query (`az rest` on the resource group's `Microsoft.CostManagement/query`, `ActualCost`, `MonthToDate`, the total, in the subscription's billing currency) printed as `Spend this month: <amount> <currency> of <ceiling> (<n> %)` against the row's Monthly budget (`(no ceiling declared)` for `none`), and as the `Spend:` line after `Budget:` in `--plan`, where a failed reading never stops the plan or a deploy. The figures lag 8 to 24 hours behind the spend, so a young environment reads 0.00; the service answers 429 after a handful of queries in a minute (retried three times, then the command says to try again in a minute); an account whose role cannot read cost gets Azure's own message. A deploy and `/agile:status` never read it.
- Deleting an environment is deleting its resource group and then its budget, both by hand in the client's subscription (the budget outlives its group): `az rest --method delete --url "/subscriptions/<subscription>/resourceGroups/<group>/providers/Microsoft.Consumption/budgets/agile-monthly-ceiling?api-version=2023-11-01"`; no plugin command does either, and the `docker compose down -v` warning does not apply.
```csharp
// <App>.AppHost/Program.cs, beside the compose block's usings, `builder` and `environmentName`, plus: using Azure.Provisioning.Storage;
var aca = builder.ExecutionContext.IsPublishMode && string.Equals(builder.Configuration["Deploy:Target"], "aca", StringComparison.OrdinalIgnoreCase);
var compose = builder.ExecutionContext.IsPublishMode && !aca;
// Custom domain: the plugin passes Deploy__CustomDomain (and Deploy__CertificateName once the managed certificate exists) only when the URL of the environment in docs/infra.md is the client's own domain.
var customDomain = aca && !string.IsNullOrEmpty(builder.Configuration["Deploy:CustomDomain"]) ? builder.AddParameter("customDomain", builder.Configuration["Deploy:CustomDomain"]!) : null;
var certificateName = customDomain is null ? null : builder.AddParameter("certificateName", builder.Configuration["Deploy:CertificateName"] ?? "");
IResourceBuilder<IResourceWithConnectionString> database = aca
    ? builder.AddAzurePostgresFlexibleServer("postgres").AddDatabase("appdb")
    : builder.AddPostgres("postgres").AddDatabase("appdb");
var apiKey = builder.AddParameter("apikey", secret: true);   // each secret, as in the compose recipe
var api = builder.AddProject<Projects.<App>_Api>("api").WithReference(database).WaitFor(database)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", environmentName).WithEnvironment("ApiKey", apiKey);
var web = builder.AddProject<Projects.<App>_Web>("web").WithReference(api).WithEnvironment("ASPNETCORE_ENVIRONMENT", environmentName);
if (aca)
{
    builder.AddAzureContainerAppEnvironment("cae").WithDashboard(false);
    var keys = builder.AddAzureStorage("storage")
        .ConfigureInfrastructure(infra => { foreach (var a in infra.GetProvisionableResources().OfType<StorageAccount>()) a.Sku = new StorageSku { Name = StorageSkuName.StandardLrs }; })
        .AddBlobs("keys");
    api.WithReference(keys).WithEnvironment("ASPNETCORE_FORWARDEDHEADERS_ENABLED", "true")
        .PublishAsAzureContainerApp((_, app) => { app.Template.Scale.MinReplicas = 1; app.Template.Scale.MaxReplicas = 1; });   // background work: stays on
    web.WithReference(keys).WithEnvironment("ASPNETCORE_FORWARDEDHEADERS_ENABLED", "true").WithExternalHttpEndpoints()
        .PublishAsAzureContainerApp((_, app) => { app.Template.Scale.MinReplicas = 0; app.Template.Scale.MaxReplicas = 1;
#pragma warning disable ASPIREACADOMAINS001
            if (customDomain is not null && certificateName is not null) app.ConfigureCustomDomain(customDomain, certificateName);
#pragma warning restore ASPIREACADOMAINS001
        });
}
else if (compose) { /* the compose block of the recipe above, for each project resource */ }
```
```csharp
// <App>.Api/Program.cs and every other web or API project (the PostgreSQL client only where the project has a DbContext): using Azure.Storage.Blobs; using Microsoft.AspNetCore.DataProtection;
builder.AddNpgsqlDbContext<AppDb>("appdb");
var dataProtection = builder.Services.AddDataProtection().SetApplicationName("<App>");
if (!string.IsNullOrEmpty(builder.Configuration.GetConnectionString("keys")))
{
    builder.AddAzureBlobServiceClient("keys");
    dataProtection.PersistKeysToAzureBlobStorage(sp =>
    {
        var container = sp.GetRequiredService<BlobServiceClient>().GetBlobContainerClient("dataprotection");
        container.CreateIfNotExists();
        return container.GetBlobClient("keys.xml");
    });
}
else if (builder.Configuration["DataProtection:KeysPath"] is { Length: > 0 } path)
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(path));
```
