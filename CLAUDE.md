# Simulab

A study coach that runs realistic practice exams (public service exams, certifications, university entrance exams, ENEM) and tells each student what to study next.

## Working agreement
- Workflow: agile@canary — see `docs/agile/workflow.md`. One feature in progress at a time.
- Conversation with the product owner in Portuguese (pt-BR); everything in the repository in English.
- Three gates: feature approved → validated on screen → merge authorized.
- Ask before: pushing or merging to `main`, touching shared databases, deleting data, anything outside this repository.
- Simulae (`D:\dev\_icontrol\simulae`) is a read-only source. Never edit it.

## Architecture
- Profile: `modular-monolith` — see `docs/agile/profile.md` (two module shapes; read it before adding a module).
- Decisions: `docs/decisions/` (start with ADR-0001).
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
- Run locally: `dotnet run --project src/Simulab.AppHost`

## Models
- Independent review (`/agile:review`): strongest available model, passed on the call.
- Main session (quiz, refinement, build): the session model, chosen by the owner.
- Code searches and sweeps (Explore): Haiku. Mechanical import and rename from Simulae: Sonnet; the tests verify it.

## Board
Board: Azure Boards (`acanariopt/simulab`) via `az boards`
Mapping: epic → feature/bug. No tasks per role. Board text in ASCII English.

## Files
- Brief: `product/brief.md`
- Features: `docs/features/F-<n>-<slug>.md`; bugs: `docs/bugs/B-<n>-<slug>.md`
- Glossary: `docs/glossary.md` (business term → English identifier)
- Infra: `docs/infra.md`
- Rules: `.claude/rules/agile/` (project rules in `project.md`)

## Project-specific rules
- Import from Simulae per feature: rename to English, extract UI text to three languages, bring the tests. No code without its tests.
- Every AI call goes through `IAiGateway`, which checks `IEntitlementService` and records usage and cost.
- Modules and pages never read plan columns; they ask `IEntitlementService`.
- Never reference MassTransit, RabbitMQ or FluentAssertions.
- Screens follow `.claude/rules/agile/ui.md`: UI kit first, `AppIcons` (Material Outlined), one way to edit an item.
