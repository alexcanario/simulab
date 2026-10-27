# Profile: modular-monolith

One deployable, several business modules with clear boundaries. Each module is **one project plus a contracts project** — not a set of layers. A section whose only line is a `Shared section:` pointer has its text in that file: read it only when its `Read when` fits the item, and always at bootstrap.

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
    <App>.<Block>/               every other technical block (Persistence, Email, Storage, Ai): created by the first feature that needs it
  Modules/<Module>/
    <App>.<Module>/
      Features/<Feature>/        vertical slice: endpoint, request/response, handler, validator
      Domain/                    entities and value objects that carry rules
      Data/                      DbContext (own schema), Configurations/, Migrations/
      Resources/                 <Module>.resx + the locales of UI locales:
      <Module>Module.cs          AddXxxModule() / MapXxxEndpoints()
    <App>.<Module>.Contracts/    what other modules may see: DTOs, query interfaces, integration events
tests/
  Hosts/<App>.Web.Tests/         bUnit; Api and AppHost tests sit next to it
  BuildingBlocks/<App>.<Block>.Tests/
  Modules/<Module>/<App>.<Module>.Tests/   unit + integration for the module
  <App>.ArchitectureTests/       serves every group
  <App>.Testing/                 shared test helpers (container fixture, factories)
```
Contracts, whose change stops a resolve run (`resolve-check.js contract`): `**/*.Contracts/**`
The solution folders in the `.slnx` mirror these disk folders (`Hosts`, `BuildingBlocks`, `Modules/<Module>`), and a layout test checks it. `Directory.Build.props` exists at the root and in `tests/`; a group gets its own only when it has a first property to share. `Directory.Packages.props` stays single, at the root.
Every solution root also has `Directory.Build.props`, `Directory.Packages.props` (central package versions), `.editorconfig`, `BannedSymbols.txt` and `global.json`, copied from the plugin at bootstrap; rules the build can check live there, and the Stop gate fails on new warnings. With technical docs chosen in the quiz (round 8), `tools/<App>.DocGen/` generates `docs/architecture/` from the EF model, the OpenAPI document and the project references, and `--check` runs at ship. When the app offers tools to a model (question 37), `ModelToolAttribute.cs` goes into the `<App>.Ai` building block, DocGen references that project, and `docs/architecture/tools.md` lists every tool with its permissions and what it reaches.
The app version is one `<Version>` in `Directory.Build.props`, which every project inherits, so every DLL reports the release and the commit (`0.1.0+<commit>`); no `.csproj` carries `<Version>`, `VersionPrefix`, `AssemblyVersion`, `FileVersion` or `InformationalVersion`. `/agile:publish` packages `<App>.Api`. Bootstrap seeds `<Version>0.1.0</Version>`; `/agile:ship` adds it when it is missing (`0.1.0`, and that item ships as it), moves it from a `.csproj` when it sits there, and bumps it once per ship: MINOR for a feature (PATCH resets to 0), PATCH for a bug, or MAJOR when the item's `## Decisions` records a breaking change (MINOR and PATCH reset to 0).
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
- **SQLite** (question 5): no container; each test gets its own temporary database file, deleted on dispose. The test connection string sets `Pooling=False`: a pooled connection keeps the file locked on Windows and the delete fails (`IOException`, measured on Microsoft.Data.Sqlite 10.0.12), and `SqliteConnection.ClearAllPools()` would free it for the whole process, every test running in parallel included (#93).
- Dispose every host, factory and Aspire builder (`await using`). One leaked builder turned 143 ms of tests into 7 minutes.
- No `Thread.Sleep` and no fixed delays: wait on a condition with a timeout. The gate runs tests with a hang timeout (120 s per test), so a hung test fails with its name.
- bUnit with a component library that owns async services (MudBlazor): one shared test context with async disposal; never `await InvokeAsync` around a call that waits for a dialog result.
- A test that replaces the `TimeProvider` with `FakeTimeProvider` (`Microsoft.Extensions.TimeProvider.Testing`) also freezes MudBlazor's debounce (`MudAutocomplete`, any input with `DebounceInterval`): the search never fires. After typing, `Advance` the fake clock past the debounce, then assert with `WaitForAssertion` (measured on MudBlazor 9.11.0 and bUnit 2.11.3, with a real-clock control, #93).
- Long text (`ui` rule): a component that shows user or Core text gets a test with a message longer than its width. bUnit cannot measure layout, so it asserts the text sits in the kit element that wraps (no fixed width, no `nowrap`); the gallery shows the real wrap.
- bUnit: after a click whose handler awaits (an Api call, `Task.Yield`), assert with `WaitForAssertion`: `Click()` returns when the handler first yields, not when it finishes. Anything that follows a click (a snackbar, a dialog closing, JS interop) is asserted the same way, in the tests written today too.
- A Razor attribute mistake compiles: a string attribute without `@` is literal text (`Value="_name"` sets the two words), and a generic type parameter under the wrong name fails only when the component renders. Every new page and dialog gets a bUnit test that renders it with its real parameters.
- Endpoints answer their own statuses: the Blazor template's `UseStatusCodePagesWithReExecute("/not-found")` re-executes the `404` or `401` of an endpoint on the not-found page, and a re-executed `POST` answers `400` (no antiforgery token). A middleware right after it sets `IStatusCodePagesFeature.Enabled = false` when the matched endpoint is not a page (`context.GetEndpoint()` with no `ComponentTypeMetadata`): `/api/v1` and every minimal endpoint keep their status, and a URL that matches nothing still gets the not-found page. A test asserts the `404` of a minimal `POST` (measured on .NET SDK 10.0.401: `400` without the middleware, `404` with it, #93).
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
- Screens follow the `ui` rule: a UI kit and a dev-only gallery come before the first screen; pages use the kit, icons go through semantic names, and architecture tests forbid raw icons and raw tables outside the kit. The kit item writes the MudBlazor theme (`MudTheme` palette for light and dark, typography, default border radius) from `docs/design/identity.tokens.json`, and a test loads the tokens and asserts every palette value equals its token. The run-time theme module (preferred theme per OS mode, user customizations, font) follows `## Run-time themes` when `docs/design/themes/` exists.
- Layout: every project sits in the folder the `## Layout` assigns to its kind, and the solution folders mirror the disk folders. The test lists the solution and fails on a project outside its group (rule of presence: it saw at least one project per group).

## Run-time themes
Shared section: `docs/agile/profile-sections/runtime-themes-mudblazor.md`. Read when: `CLAUDE.md` has no `- UI library: adminlte` line and the item builds or changes the run-time theme module (`Theming/`, `/appearance`, `docs/design/themes/`), the import and generation of themes, or adds the module to an existing kit.

## Local services (fixed ports and passwords)
Shared section: `docs/agile/profile-sections/local-services.md`. Read when: the item adds or changes a container resource of the AppHost, its port, password or data volume, or `## Local services` of `docs/infra.md`.

## Deploy recipe (containers + Aspire, with an AppHost)
Shared section: `docs/agile/profile-sections/deploy-aspire.md`. Read when: the item changes how an environment is deployed with `aspire deploy`, the AppHost's publish mode, or a Deploy command of `docs/infra.md`.

## Deploy recipe (Azure Container Apps + Aspire)
Shared section: `docs/agile/profile-sections/deploy-aca.md`. Read when: the item touches an environment whose recipe in `docs/infra.md` is `aca`, its Azure resources, custom domain or cost.

## Deploy recipe (containers without Aspire)
Shared section: `docs/agile/profile-sections/deploy-compose.md`. Read when: the item touches an environment whose Deploy command is not an `aspire deploy` line, its compose file or images.

## Autonomy
- Reserved: `new-module` — a new module or service boundary
- Delegated: `layout-in-kit` — a layout inside the UI kit's patterns
- Delegated: `field-order` — the order of the fields of a form or a list
- Delegated: `ui-text` — error and empty texts in the kit's tone
