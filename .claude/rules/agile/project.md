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
- On an anonymous endpoint every input limit (column width, format) is checked before the account lookup: a failure only one path can hit, even a 500, reveals whether the account exists (B-7).
- The OpenIddict password flow is for the first-party Web only. Never register a third-party client for it.
- In a sign-in with more than one step, only the last step clears the failure count: a step that clears it gives unlimited tries to the next one (F-11).
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
- When an effect moves out of the request (an email, an event, long work), re-read every test that asserted it instead of only making it compile: an assertion that "nothing was sent" passes for free once the effect is deferred (F-13).

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
- After a click whose handler awaits (an Api call, `Task.Yield`), assert what follows with `WaitForAssertion`, never on the line after `Click()` (F-8).
- A colour token a screen relies on has its contrast ratio asserted over the theme (`ThemeContrastTests`), not only measured on screen once (F-10).
- A contrast failure of a theme token is fixed in the palette, with `ThemeContrastTests` holding the numbers; patching the screen that showed it is debt, not a fix (B-8).
- Read a rendered colour only after the theme transition settles (0.25 s in MudBlazor): a value read right after the theme switch is still the old colour (B-8). With the browser pane hidden, transitions never finish: inject `* { transition: none !important }` before measuring (F-17).
- A contrast fix on a theme token covers the whole family in the same item (the colour and its contrast text, every severity): F-10, B-8 and B-9 chased the same defect one token at a time.
- Measuring a rendered colour whose background has alpha: composite it over the first opaque ancestor before computing the ratio; the raw `rgba` reads as a false failure (B-9).
- Driving a Blazor Server dialog from the browser pane: do the whole flow (open, fill, confirm) in one call - the pane recreates the circuit between calls and the dialog is gone (F-10).
- A kit CSS rule that overrides a MudBlazor class carries that class too (`.mud-tooltip-root.app-nav-tooltip`): MudBlazor's one-class rules load later and win; pin it with a test that reads `app.css` (F-14).

## Packages
- Versions live in `Directory.Packages.props` only. A `PackageReference` never has `Version=`.
- Assertions: AwesomeAssertions. Never FluentAssertions.
- A cache whose expiry a test must control is not `IMemoryCache` (its clock is not a `TimeProvider` in this stack) - a small `TimeProvider`-backed class instead, like `PermissionCache` (F-6).

## Talking to the owner
- A technical term used with the owner for the first time gets a row in `## Technical terms` of `docs/glossary.md` (term, pt-BR word, meaning) in the same step (F-8).
- A validation script step that needs a terminal gives the command for Git Bash and for PowerShell 7, each run by Claude before handing over, with the expected output (B-4).

## Sessions and retro
- One Claude session per checkout. A second session (refine, retro, ship of another item) runs in its own worktree; never switch branches under a running session.
- A retro changes rules, docs and settings only. A lesson that needs code or tests becomes an item (feature or bug) and goes through build.
- Run the gate with its whole output saved to a file (scratchpad) and quote from that file; never pipe it through a filter that can drop the failure (B-7).
- A new item found during another item's work is captured with `/agile:idea` from the template, never written by hand (B-7).
- Never change state (sign-ups, requests that count, data) in an app host Claude did not start; ask first, or use data no one else uses and say which (B-4).
