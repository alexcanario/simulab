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
- Building blocks besides `SharedKernel`: `Simulab.Ai` (`IAiGateway`, usage and cost records), `Simulab.Storage` (`IFileStorage`), and the in-process integration events in `SharedKernel`. Created by the first feature that needs them, not before.
- `User` inherits `IdentityUser<Guid>` and repeats the `TenantEntity` fields: a known exception in the architecture tests.
- Forbidden references, checked by the architecture tests: MassTransit, RabbitMQ, FluentAssertions.
- Importing from Simulae: per feature, renamed to this layout (`Modules.<Module>.<Layer>` becomes `Simulab.<Module>.<Layer>`, `Simulae.Common` becomes `Simulab.SharedKernel`), UI text extracted to the three languages, tests included and moved to the shared PostgreSQL fixture.
- The AI gateway is faked in tests. Golden-exam extraction checks run on demand, never in CI.

## Layout
```
src/
  <App>.AppHost/                 local orchestration (Aspire)
  <App>.ServiceDefaults/         telemetry, health, resilience
  <App>.Api/                     host only: composition, auth, middleware, OpenAPI. No business code.
  <App>.Web/                     Blazor UI. Talks to the API through typed clients. No business rules.
  <App>.SharedKernel/            Entity, TenantEntity, Result/Error, clock, AppJson.Options
  Modules/<Module>/
    <App>.<Module>/
      Features/<Feature>/        vertical slice: endpoint, request/response, handler, validator
      Domain/                    entities and value objects that carry rules
      Data/                      DbContext (own schema), Configurations/, Migrations/
      Resources/                 <Module>.resx + pt-BR, pt-PT, en
      <Module>Module.cs          AddXxxModule() / MapXxxEndpoints()
    <App>.<Module>.Contracts/    what other modules may see: DTOs, query interfaces, integration events
tests/
  <App>.<Module>.Tests/          unit + integration for the module
  <App>.Web.Tests/               bUnit
  <App>.ArchitectureTests/
```

## Where business rules live
- A rule about one entity lives in that entity (`Domain/`): methods that return `Result`, no public setters on ruled state.
- A rule that needs data or several entities lives in the feature handler.
- CRUD without rules is a thin slice: endpoint → `DbContext`. No repository, no mediator, no mapping library, no interface with a single implementation.
- Add structure on the second use, not the first: a domain service when two features share a rule, an abstraction when a second implementation exists.
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
- Modules talk only through `Contracts`: a query interface or an in-process integration event. Never another module's `DbContext`, tables or entities.
- EF mappings are `IEntityTypeConfiguration<T>` classes in `Data/Configurations/`. Tenant and soft-delete filters are global, never repeated per query.
- The UI validates for comfort only. The API is the authority.

## Test strategy and time budget
| Level | What | Budget |
|---|---|---|
| Unit | Domain rules and handlers without I/O | < 30 s per project |
| Integration | Slice through HTTP (`WebApplicationFactory`) on real PostgreSQL | < 2 min per project |
| UI | bUnit, only for screens with logic | < 30 s |
| Full suite | Everything, only on ship | < 5 min |

- **One PostgreSQL container per test project** (Testcontainers, assembly-level fixture). Isolate with a database per test class or a reset between tests — never a container per class.
- Dispose every host, factory and Aspire builder (`await using`). One leaked builder turned 143 ms of tests into 7 minutes.
- No `Thread.Sleep` and no fixed delays: wait on a condition with a timeout.
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
