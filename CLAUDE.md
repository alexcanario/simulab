# Simulab

A study coach that runs realistic practice exams (public service exams, certifications, university entrance exams, ENEM) and tells each student what to study next.

## Working agreement
- Workflow: agile@canary — see `docs/agile/workflow.md`. One feature in progress at a time.
- Plugins: agile
- Conversation with the product owner in Portuguese (pt-BR); everything in the repository in English.
- Three gates: feature approved → validated on screen → merge authorized.
- Ask before: pushing or merging to `main`, touching shared databases, deleting data, anything outside this repository.
- Worktrees: `D:\dev\_icontrol\wt\simulab` — one folder per item (`f-<n>-<desc>`, `b-<n>-<desc>`), created by `/agile:refine`.
- Simulae (`D:\dev\_icontrol\simulae`) is a read-only source. Never edit it.

## Architecture
- Profile: `modular-monolith` — see `docs/agile/profile.md` (two module shapes; read it before adding a module).
- Decisions: `docs/decisions/` (start with ADR-0001).
- Overview: `docs/architecture-overview.md` (C4 context and containers, hand-written; `ArchitectureOverviewTests` checks it against the app host).
- Keep code simple: add structure only where the profile asks for it.

## Stack
- .NET 10, Blazor Interactive Server + MudBlazor, PostgreSQL 15+ (schema per module), Redis, Aspire. Central Package Management.
- Messaging: in-process. Auth: ASP.NET Identity + OpenIddict (Google and TOTP off in v1). RBAC: yes. Entitlements: yes (`Plans`, `IEntitlementService`). Multi-tenancy: nullable `TenantId`, dormant.
- AI: Claude API behind `IAiGateway`. Files: `IFileStorage` (Azurite in dev). Email: `IEmailSender` (Mailpit in dev).

## Languages
- UI locales: pt-BR, pt-PT, en. No hardcoded UI strings; resource files per module; `IStringLocalizer`.
- Question content is not translated; each exam and question carries its own language.
- App manual: `docs/manual/<locale>/`, updated when each feature ships.

## Commands
- Build: `dotnet build Simulab.slnx`
- Affected tests: run by the agile Stop hook (budget: unit < 30 s, integration < 2 min)
- Full suite (ship only): `dotnet test Simulab.slnx` (budget: < 5 min)
- Run locally: `dotnet run --project src/Hosts/Simulab.AppHost`

## Models
- Independent review (`/agile:review`): strongest available model, passed on the call.
- Design and architecture passes (`/agile:build`, agents `system-design` and `architect`): strongest available model, passed on the call.
- Screen design (`/agile:screen`, agent `ux-designer`): strongest available model, passed on the call.
- Screen implementation (`/agile:build`, agent `frontend`): Sonnet, passed on the call.
- Main session (quiz, refinement, build): the session model, chosen by the owner.
- Code searches and sweeps (Explore): Haiku. Mechanical import and rename from Simulae: Sonnet; the tests verify it.

## Board
Board: GitHub Issues + Projects (`alexcanario/simulab`) via `gh`
Mapping: epic → feature/bug. No tasks per role. Board text in ASCII English.

## Files
- Brief: `product/brief.md`
- Features: `docs/features/F-<n>-<slug>.md`; bugs: `docs/bugs/B-<n>-<slug>.md`
- Glossary: `docs/glossary.md` (business term → English identifier; technical terms → the pt-BR word used with the owner)
- Infra: `docs/infra.md`
- Rules: `.claude/rules/agile/` (project rules in `project.md`)

## Project-specific rules
- Import from Simulae per feature: rename to English, extract UI text to three languages, bring the tests. No code without its tests.
- Every AI call goes through `IAiGateway`, which checks `IEntitlementService` and records usage and cost.
- Modules and pages never read plan columns; they ask `IEntitlementService`.
- Never reference MassTransit, RabbitMQ or FluentAssertions.
- Screens follow the rules `ui` (core) and `ui-project` (Simulab choices): UI kit first, `AppIcons` (Material Outlined), one way to edit an item.
- Redis: always through `Aspire.StackExchange.Redis` (`builder.AddRedisClient(...)`), never a plain `ConnectionMultiplexer.Connect` — the local container uses TLS by default and a plain client cannot trust its dev certificate.
