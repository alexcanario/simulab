# Profile: modular-monolith

One deployable, several business modules with clear boundaries. Each module is **one project plus a contracts project** — not a set of layers.

## Simulab specifics (ADR-0001)
`<App>` is `Simulab`. The solution is `Simulab.slnx` at the repository root, with Central Package Management (`Directory.Packages.props`) and shared properties (`Directory.Build.props`).

| Module | Shape | Origin |
|---|---|---|
| Identity | five projects | Simulae `Identity` |
| Catalog | five projects | Simulae `ContentCatalog` (organizers, exams, editions, taxonomy, question bank) |
| ExamEngine | five projects | Simulae `ExamEngine` (simulators, scoring, timer) |
| Plans | five projects | Simulae `Billing` (plans, assignments, promo codes, usage meter, `IEntitlementService`) |
| New modules (import, analytics, recommendations, coach) | one project + `Contracts`, unless refinement shows a rich domain | new |

- Two deployable hosts: `Simulab.Api` (also runs the background job worker) and `Simulab.Web`.
- Building blocks besides `SharedKernel`: `Simulab.Persistence` (F-3: `ModuleDbContext` base, audit and soft-delete interceptor, tenant and soft-delete filters, unique-index helper, database health check), `Simulab.Email` (F-3: `IEmailSender` over SMTP), `Simulab.Jobs` (F-13: the `jobs` schema, `IJobQueue`, `JobRunner` and the hosted `JobWorker` of ADR-0001 #20), `Simulab.Ai` (`IAiGateway`, usage and cost records), `Simulab.Storage` (`IFileStorage`), and the in-process integration events in `SharedKernel`. Created by the first feature that needs them, not before.
- `Simulab.Persistence` references `SharedKernel` only; `SharedKernel` never references EF Core or ASP.NET. Each module keeps its own `DbContext`, schema and migrations and inherits `ModuleDbContext`; architecture tests check both.
- Test helpers shared by test projects live in `tests/Simulab.Testing` (one PostgreSQL container per test project, a database per test class).
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
```
Simulab (F-12): the solution folders in `Simulab.slnx` mirror these disk folders, and `SolutionLayoutTests` checks it. `Directory.Build.props` exists only at the root and in `tests/`; a group gets its own file only when it has a first property to share. `Directory.Packages.props` stays single, at the root.
Every solution root also has `Directory.Build.props`, `Directory.Packages.props` (central package versions), `.editorconfig`, `BannedSymbols.txt` and `global.json`, copied from the plugin at bootstrap; rules the build can check live there, and the Stop gate fails on new warnings.
Code reaches an Aspire resource (Redis, PostgreSQL, a broker) through its client integration package (`builder.Add<X>Client(...)`), never a driver connection built from the raw connection string: Aspire runs local resources with TLS and a password by default, and a plain driver hangs on the dev certificate.
The `ServiceDefaults` project the bootstrap generates calls `AddStandardResilienceHandler(options => options.Retry.DisableForUnsafeHttpMethods())`: by default the handler retries a POST, which replays single-use tokens and sends emails twice.
With Blazor Interactive Server, state that changes during a session (rotating tokens, permissions) lives in a server-side store keyed by an id the cookie carries, never in the cookie itself: a circuit cannot rewrite the cookie. The cookie is checked in `OnValidatePrincipal`, and open circuits revalidate through `RevalidatingServerAuthenticationStateProvider`.
With Blazor Interactive Server, data from the first HTTP request that a circuit needs later (the visitor's address, a header) is read in `App` (static render, `HttpContext` available), passed to the interactive root component as a parameter and kept in a scoped service: a circuit has no `HttpContext`.

## Where business rules live
- A rule about one entity lives in that entity (`Domain/`): methods that return `Result`, no public setters on ruled state.
- A rule that needs data or several entities lives in the feature handler.
- CRUD without rules is a thin slice: endpoint → `DbContext`. No repository, no mediator, no mapping library, no interface with a single implementation.
- Add structure on the second use, not the first: a domain service when two features share a rule, an abstraction when a second implementation exists.
- Modules talk only through `Contracts`: a query interface or an in-process integration event. Never another module's `DbContext`, tables or entities.
- A row a request must not lose (a job, an outbox message) is staged on the caller's own `DbContext`: the shared table is mapped into it with `ExcludeFromMigrations()` while the owning context keeps the migration, so one `SaveChanges` commits the row and the data that justifies it. Two contexts over one connection is neither needed nor available with Npgsql (F-13).
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
- bUnit: after a click whose handler awaits (an Api call, `Task.Yield`), assert with `WaitForAssertion`: `Click()` returns when the handler first yields, not when it finishes.
- A Razor attribute mistake compiles: a string attribute without `@` is literal text (`Value="_name"` sets the two words), and a generic type parameter under the wrong name fails only when the component renders. Every new page and dialog gets a bUnit test that renders it with its real parameters.
- xUnit + AwesomeAssertions. FluentAssertions 8+ and MassTransit 9+ have commercial licenses: do not add them.
- A bug starts with a test that fails. A budget overrun is a retro finding, not something to ignore.

## Architecture tests
- A module references another module only through its `Contracts` project.
- `Contracts` projects reference only `SharedKernel`.
- `Domain/` types do not depend on EF Core, ASP.NET or `Data/`.
- `Web` references `Contracts` projects, never a module project.
- `Api` contains no entities and no handlers.
- Vocabulary: identifiers in **every** assembly (modules, Api and Web) are English; forbidden terms come from `docs/glossary.md`.
- Every rule of absence is paired with a rule of presence (the test asserts it matched at least one type). An empty assembly must fail, not pass.
- Screens follow the `ui` rule: a UI kit and a dev-only gallery come before the first screen; pages use the kit, icons go through semantic names, and architecture tests forbid raw icons and raw tables outside the kit.
- Layout: every project sits in the folder the `## Layout` assigns to its kind, and the solution folders mirror the disk folders. The test lists the solution and fails on a project outside its group (rule of presence: it saw at least one project per group).
