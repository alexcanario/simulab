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
- Building blocks besides `SharedKernel`: `Simulab.Persistence` (F-3: `ModuleDbContext` base, audit and soft-delete interceptor, tenant and soft-delete filters, unique-index helper, database health check), `Simulab.Email` (F-3: `IEmailSender` over SMTP), `Simulab.Jobs` (F-13: the `jobs` schema, `IJobQueue`, `JobRunner` and the hosted `JobWorker` of ADR-0001 #20), `Simulab.Ai` (`IAiGateway`, usage and cost records), `Simulab.ApiResults` (F-39: `ApiProblem`, the one mapping from an `Error` to an HTTP answer; the only building block that carries ASP.NET), `Simulab.Storage` (`IFileStorage`), and the in-process integration events in `SharedKernel`. Created by the first feature that needs them, not before.
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
