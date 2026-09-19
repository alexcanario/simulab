---
paths:
  - "**/*.cs"
  - "**/*.razor"
  - "**/*.csproj"
  - "**/*.props"
---
# Simulab project rules

Rules implied by ADR-0001. Core rules in this folder still apply.

## Data
- Entities inherit `TenantEntity` (nullable `TenantId`, audit, soft delete). `User` is the only exception: it inherits `IdentityUser<Guid>` and repeats the fields.
- `TenantId` is dormant in v1: every row is global or B2C (`TenantId` null). Do not build institution features, and do not remove the tenant filters.
- Every unique index that includes `TenantId` is `NULLS NOT DISTINCT` (`.AreNullsDistinct(false)`), with a test that inserts two global duplicates and expects a conflict.
- Soft delete only. Erasure of a person anonymizes personal data and keeps unidentified attempts.
- Published question content is versioned. An attempt stores the question version and answer key version that graded it. An annulment or a changed answer key creates a version; it never rewrites history.
- One schema and one `DbContext` per module. A module never reads another module's tables.
- A query filter that depends on the current tenant or user reads it from the context instance, never from a value captured when the model was built: EF caches the compiled model and the first request would freeze its tenant.
- A new building block or shared test project is listed in `docs/agile/profile.md` in the same item that creates it.

## Access and entitlements
- Pages, menus and endpoints check permissions, never role names.
- Roles decide who may act; plans decide what is included. Never mix them.
- Only the `Plans` module reads plan columns. Everyone else asks `IEntitlementService` (`HasFeature`, `GetLimit`, `GetRemaining`).
- The effective plan is computed at read time from the base plan and the active time-bound grant. No job flips a user back to Free.
- After a trial expires, AI-made data (coach history, study plan) stays readable and frozen. Block new AI calls, never hide existing data.
- An anonymous endpoint answers the same way whether an account exists or not: same status, body and code (sign-up, resend, sign-in, password recovery).
- The OpenIddict password flow is for the first-party Web only. Never register a third-party client for it.
- Google sign-in and TOTP stay in the code, switched off by configuration. Do not delete them, and do not show their UI while off.

## AI
- Every AI call goes through `IAiGateway`. No direct SDK or HTTP call to a provider anywhere else.
- The gateway checks the entitlement and the remaining quota before the call, and records user, purpose, model, tokens and cost after it.
- Nothing extracted by AI is published without a person's review. Import output is always a draft.
- The coach answers from the app's data through tool calls. If the data is missing, it says so; it never states exam facts from model memory.
- Tests use a fake gateway. No real AI call in unit, integration or CI runs.

## Integration
- Cross-module events use the in-process integration events abstraction. Never reference MassTransit or RabbitMQ.
- Long work (import, OCR, extraction) is a job in the job table, run by the worker in the `Api` host. Never inside a request or a Blazor circuit.
- Files go through `IFileStorage`, in private containers. Never a public blob URL; never a path on local disk.
- Email goes through `IEmailSender`, with templates in the three languages, chosen by the user's language.

## Exam sessions
- Every answer is saved on the server as it is given. A lost Blazor circuit must not lose answers or time: the timer lives on the server (Redis), never in the browser.
- The exam timer is exposed accessibly (WCAG 2.2 AA) without interrupting the candidate.

## Content language
- Question content is never translated. Exams and questions carry a language. Only UI text, emails and the manual are localized.

## Importing from Simulae
- Simulae is read-only. Import per feature, never in bulk.
- On the way in: English identifiers and file names, the Simulab layout, UI text moved to resources in three languages, comments in English.
- The tests come with the code, renamed, using one PostgreSQL container per test project. Code without its tests is not imported.
- Do not import migrations. Each module has one fresh initial migration.

## UI tests
- bUnit tests of MudBlazor components inherit `KitTestContext` (async disposal); never `await InvokeAsync` around a call that returns a dialog result.

## Packages
- Versions live in `Directory.Packages.props` only. A `PackageReference` never has `Version=`.
- Assertions: AwesomeAssertions. Never FluentAssertions.
- A cache whose expiry a test must control is not `IMemoryCache` (its clock is not a `TimeProvider` in this stack) - a small `TimeProvider`-backed class instead, like `PermissionCache` (F-6).

## Sessions and retro
- One Claude session per checkout. A second session (refine, retro, ship of another item) runs in its own worktree; never switch branches under a running session.
- A retro changes rules, docs and settings only. A lesson that needs code or tests becomes an item (feature or bug) and goes through build.
