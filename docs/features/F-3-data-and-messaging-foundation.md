---
feature: F-3
epic: Foundation and identity
status: done
board: 707
version: 1
---
# Data and messaging foundation

## Summary
Technical slice with no screen: PostgreSQL and Mailpit in the app host; a persistence building block (`Simulab.Persistence`) with the module `DbContext` base, audit and soft-delete interceptors and the global tenant and soft-delete filters; `IEmailSender` over SMTP; the in-process integration events abstraction; and one shared PostgreSQL test fixture per test project.

## Goal
F-4 (Identity) and every later module start from a tested base: a database with isolated schemas, automatic audit and soft delete, tenant isolation that is already enforced, email that can be sent and seen locally, and events between modules without a broker.

## What exists
- `Simulab.SharedKernel`: `Entity` (Guid v7 id), `TenantEntity` (nullable `TenantId`, audit and soft-delete fields), `IAuditableEntity`, `ISoftDeletableEntity`, `Result`/`Error`, `AppJson`. No EF Core dependency.
- `Simulab.AppHost`: projects `api` and `web` only; no containers.
- `Simulab.Api`: `/api/v1/system/info`, OpenAPI, problem details. No module, no `DbContext`.
- No EF Core, Npgsql, Testcontainers, MailKit or Aspire hosting packages.
- Simulae (reference only): `AuditSaveChangesInterceptor`, `ICurrentUserContext`, `ICurrentTenantContext`, `IIntegrationEventPublisher` / `IIntegrationEventConsumer` (MassTransit), per-file PostgreSQL containers in tests.

## Users and use cases
- UC1 A developer (Claude) creates a module `DbContext` by inheriting the base context and gets its own schema, snake_case names, its own migrations history table, audit, soft delete and tenant filters without writing them.
- UC2 A module saves an entity: creation and update time and user are filled in; a delete becomes a soft delete.
- UC3 A module queries: deleted rows and rows of another tenant never come back, unless the query explicitly ignores a named filter.
- UC4 A module sends an email through `IEmailSender`; in development it appears in Mailpit.
- UC5 A module publishes an integration event; every registered handler in other modules receives it in the same process.
- UC6 A test project uses one PostgreSQL container for the whole run and a fresh database per test class.
- UC7 The owner starts the app host and sees PostgreSQL and Mailpit running and healthy in the Aspire dashboard.

## Business rules
- BR1 Every module has its own schema; its tables and its migrations history table (`__ef_migrations_history`) live in that schema. Table and column names are snake_case.
- BR2 On save, an added auditable entity gets `CreatedAt` (UTC, from `TimeProvider`) and `CreatedBy` (current user, null when anonymous); a modified one gets `UpdatedAt` and `UpdatedBy`. Values set by hand are overwritten.
- BR3 Deleting a soft-deletable entity marks it `IsDeleted` with `DeletedAt` and `DeletedBy` and keeps the row. Rows are never physically removed through the context.
- BR4 A tenant entity is visible when its `TenantId` is null (global or B2C) or equals the current tenant. In v1 the current tenant is always null, so only null-tenant rows are visible.
- BR5 Deleted rows are hidden from every query. Each filter is named (`tenant`, `soft_delete`) and can be ignored on its own, only explicitly.
- BR6 The filters apply to every `TenantEntity` of a module context automatically; a module never repeats them.
- BR7 The current user comes from `ICurrentUser`; until sign-in exists (F-5) it is anonymous (`UserId` null). The current tenant comes from `ICurrentTenant`; in v1 it is always null.
- BR8 `IEmailSender` sends one message (to, subject, HTML body, optional text body) over SMTP with the configured sender address. A failure throws; the caller decides what to do.
- BR9 Integration events are records implementing `IIntegrationEvent`, named in the past tense, and live in a module's `Contracts` project. Publishing runs every handler for that event type in sequence, in the same scope, and returns after the last one. A handler failure is logged and propagated to the publisher. No handler is not an error.
- BR10 The app host runs PostgreSQL (with a named data volume, so local data survives restarts) and Mailpit; the Api receives the database connection and the SMTP settings from the app host. The Web receives SMTP settings only if a later feature needs them.
- BR11 The Api health endpoint reports the database.
- BR12 Tests use one PostgreSQL container per test project and a new database per test class; no test starts its own container.

## Screens and API
- No new screen and no new endpoint.
- Aspire dashboard: resources `postgres` (database `simulab`) and `mailpit` (web UI link), both healthy; `api` waits for both.
- Error codes: none.

## Acceptance criteria
- AC1 Given a module context inheriting the base, then its tables and `__ef_migrations_history` are created in the module schema with snake_case names. (BR1)
- AC2 Given a new auditable entity with hand-set audit values, when saved, then `CreatedAt` is the `TimeProvider` time and `CreatedBy` the current user; when modified and saved, then `UpdatedAt` and `UpdatedBy` are set and `Created*` do not change. (BR2, BR7)
- AC3 Given an anonymous current user, when an entity is saved, then `CreatedBy` is null. (BR2, BR7)
- AC4 Given a saved soft-deletable entity, when removed and saved, then the row still exists with `IsDeleted` true, `DeletedAt` and `DeletedBy` set, and a normal query does not return it; a query ignoring `soft_delete` does. (BR3, BR5)
- AC5 Given rows with a null tenant and with tenant A, when the current tenant is null, then only the null-tenant rows are returned; when the current tenant is A, then the null-tenant and tenant-A rows are returned and tenant-B rows are not; a query ignoring `tenant` returns all non-deleted rows. (BR4, BR5)
- AC6 Given a second entity type added to a test context without any filter code, then both filters apply to it. (BR6)
- AC7 Given a unique index over `TenantId` built with the base helper, when two global rows with the same key are inserted, then the database rejects the second. (project rule: NULLS NOT DISTINCT)
- AC8 Given a Mailpit container, when `IEmailSender` sends a message, then Mailpit receives it with the configured sender, recipient, subject and body; given an unreachable SMTP server, then sending throws. (BR8)
- AC9 Given two handlers for an event, when it is published, then both run in registration order in the caller's scope; given a failing handler, then the publisher gets the exception and it is logged; given no handler, then publishing completes. (BR9)
- AC10 Given the app host model, then it contains `postgres` with a data volume, database `simulab`, `mailpit`, and `api` references and waits for both. (BR10)
- AC11 Given the Api with a reachable database, then `/health` is healthy; given an unreachable one, then it is unhealthy. (BR11)
- AC12 Given two test classes in one project, then they share one container and get different databases. (BR12)
- AC13 Architecture: `Simulab.SharedKernel` does not reference EF Core; `Simulab.Persistence` references only `SharedKernel` among the solution projects; no project references MassTransit or RabbitMQ. (Decision 1)
- AC14 Architecture: every module `DbContext` inherits `ModuleDbContext` and declares its own schema, and no two module contexts share a schema. (Decision 1)
- AC15 No new UI text is added (localization unchanged; missing-key test stays green).
- AC16 On screen: the app host starts, the dashboard shows `postgres` and `mailpit` healthy and the Mailpit UI opens — validation script. (BR10)

## Decisions
- 2026-09-17 — Common persistence lives in a new building block `src/Simulab.Persistence` (base `ModuleDbContext`, interceptors, filters, history table, unique-index helper), referencing `SharedKernel` only; `SharedKernel` stays free of EF Core; `profile.md` lists the new building block — owner, question 1. Each module keeps its own `DbContext`, schema and migrations: the base carries only the mechanism (audit, soft delete, filters, schema and history convention, unique-index helper), and an architecture test enforces one distinct schema per module context. Simulae writes the filters per configuration class, which caused its tenant leak (TK #184, worked around with `EnableServiceProviderCaching(false)`); a filter built in the base context does not have that problem — owner, follow-up question 1.
- 2026-09-17 — App host adds PostgreSQL and Mailpit now; Redis arrives with F-5, its first user — owner, question 2.
- 2026-09-17 — `IEmailSender` with SMTP (MailKit) is part of this item, tested against a Mailpit container; email templates in three languages stay in F-4 — owner, question 3.
- 2026-09-17 — Integration events are dispatched in process, synchronously and in sequence after the caller's save; failures propagate. A job-table (outbox) implementation can replace it behind the same interface when F-10 needs guaranteed delivery — owner, question 4.
- 2026-09-17 — Tenant filter shows global (null) rows plus the current tenant's rows — owner, question 5 (ADR-0001 #7, #8).
- 2026-09-17 — PostgreSQL uses a named data volume in development — owner, question 6.
- 2026-09-17 — New packages approved: Npgsql.EntityFrameworkCore.PostgreSQL, Aspire.Npgsql.EntityFrameworkCore.PostgreSQL, Aspire.Hosting.PostgreSQL, CommunityToolkit.Aspire.Hosting.MailPit, EFCore.NamingConventions, MailKit, Testcontainers.PostgreSql; latest stable and license checked when added — owner, question 7.
- 2026-09-17 — Audit times come from `TimeProvider` (UTC), registered once; tests use a fake time provider — `DateTime.Now` is banned and time must be testable.
- 2026-09-17 — Filters use EF Core 10 named query filters (`tenant`, `soft_delete`), applied in the base context to every `TenantEntity` type — each can be ignored alone, and modules cannot forget them.
- 2026-09-17 — Interceptors work on the `IAuditableEntity` / `ISoftDeletableEntity` interfaces, not on `TenantEntity`, so `User` (F-4) is covered too.
- 2026-09-17 — `ICurrentUser` and `ICurrentTenant` live in `SharedKernel`, with anonymous / no-tenant defaults registered by the Api; F-5 replaces the user implementation.
- 2026-09-17 — The integration events abstraction (`IIntegrationEvent`, `IIntegrationEventPublisher`, `IIntegrationEventHandler<T>`) and its in-process publisher live in `SharedKernel` (profile), with no package dependency.
- 2026-09-17 — The shared test fixture lives in `tests/Simulab.Testing` (xUnit assembly fixture: one container per test project, `CREATE DATABASE` per test class) — one place instead of a copy per project.
- 2026-09-17 — Mailpit tests use a generic Testcontainers container of the Mailpit image, reading messages through its HTTP API with the shared JSON options.
- 2026-09-17 — Build: `Aspire.Hosting.Testing` (MIT) added so AC10 is a test and not only a screen step — owner, build question 1.
- 2026-09-17 — Build: the Api reports the database through a small `DatabaseHealthCheck` over the Npgsql data source, not through an EF context: no module context exists yet, and no extra package was needed.
- 2026-09-17 — Build: `IEmailSender` lives in a `Simulab.Email` building block, like `Simulab.Storage` and `Simulab.Ai` in the profile; the Api maps the app host SMTP connection string onto `EmailOptions`.
- 2026-09-17 — Build: the shared test helper is `PostgresServer` (one container per test process, `CREATE DATABASE` per test class), used by the persistence tests and by the Api health test.
- 2026-09-17 — Build: pgAdmin was tried in the app host and removed — it is one more container and the item does not ask for it.
- 2026-09-17 — The Api registers the Npgsql data source and its health check through the Aspire client integration; a module registers its `DbContext` in its own `AddXxxModule()` (F-4 onward).

## Out of scope
- Redis (F-5).
- Job table and background worker (first import feature).
- `IFileStorage` and Azurite; `IAiGateway`.
- The real current user from the sign-in (F-5) and any tenant resolution.
- Email templates and any email sent by a feature (F-4).
- Any screen, endpoint or module.
- An outbox or broker for integration events.

## Open questions
- (none)

## Change notes

## Validation script
A container runtime (Docker Desktop or Podman) must be running. Close any app host you have open first.
1. Start `dotnet run --project src/Simulab.AppHost` and open the dashboard URL printed on start.
2. Check the resources: `postgres`, `simulab`, `mailpit`, `api` and `web` all reach Running. There is no Redis and no pgAdmin.
3. Open the `api` URL and add `/health`: it reads `Healthy` (the database is part of that answer).
4. Open the `mailpit` URL from the dashboard: the inbox opens and is empty. No feature sends email yet (F-4 does).
5. Open the `web` URL: the app shell from F-2 still works (menu, theme, language).
6. Stop the app host (Ctrl+C), start it again and check `postgres` reaches Running once more: the named volume survived.
7. Optional, to prove the data volume: with the app host running, `docker volume ls` lists `simulab-postgres-data`.
8. Nothing in this item shows UI text or needs a permission: no language switch and no permission check to do here.

## Delivery
- Branch: `feature/F-3` (deleted after merge)
- Merge: the `--no-ff` merge commit "Merge feature/F-3: data and messaging foundation (AB#707)" on `main`, 2026-09-17
- Validated on screen by the owner: 2026-09-17
- Tests: full suite 156 passed, 0 failed (SharedKernel 12, Architecture 15, AppHost 4, Web 104, Api 5, Persistence 14, Email 2), 9 s; full build 9 s, 0 warnings
- Manual pages: none (no end-user visible behavior; the F-2 page still applies)
