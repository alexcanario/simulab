---
feature: F-41
epic: Foundation and identity
status: building
board: 762
version: 2
---
# AI gateway

## Summary
Every AI call in Simulab goes through `IAiGateway`, which checks `IEntitlementService` and records usage and cost (`CLAUDE.md`, Stack; ADR-0001 #22). Neither interface exists in `src/` today, so no feature can call a model yet. This feature adds the `Ai` building block with the gateway, one real call to the Claude API, the entitlement check and the usage and cost record, plus a development-only diagnostics page to see it work. It unblocks F-40 (eval suite for model calls), which has nothing to evaluate until a real call exists.

## Start
- Depends on: nothing (no item in `docs/features/` provides `IAiGateway`; confirmed by `grep -rn "IAiGateway" src/ tests/` returning no match)
- Waits on: nothing — the owner answered every question on 2026-09-24
- Suggested path: `/agile:build F-41` (no screen design: the diagnostics page copies `/dev/ui`)
- Parallel with: F-34 (exam back office) — different files, its own worktree

## Goal
Give Simulab one door to the Claude API that no feature can go around: it knows who is calling, refuses a call the plan does not allow, and writes down what every call cost. Until it exists, the AI capabilities of the brief (import with OCR, the coach) and the eval suite of F-40 have nothing to build on.

## What already exists
- No `IAiGateway`, no `Ai` building block, no `Plans` module, no `IEntitlementService` anywhere in `src/` or `tests/`.
- `Simulab.Email` is the pattern for an external provider behind an interface: options class with `SectionName`, an `AddX(configuration)` extension, `ValidateDataAnnotations().ValidateOnStart()`.
- `Simulab.Jobs` is the pattern for a building block that owns data: its own `jobs` schema, `JobsDbContext`, configurations and migrations.
- `ICurrentUser` (`Simulab.SharedKernel.Security`) exposes a nullable `UserId`; it is anonymous until a user signs in.
- `Result` / `Result<T>` with an `Error` carrying a `Code` (`Simulab.SharedKernel.Results`).
- `/dev/ui` (`Components/Pages/Dev/UiGallery.razor`) is the precedent for a development-only page: `_allowed = Environment.IsDevelopment()` in the page and `DevelopmentOnly: true` on its `NavigationItem`.
- Nothing to import from Simulae: it has no Claude or Anthropic code.

## Users and use cases
- UC1 A feature (import, coach, eval suite) asks `IAiGateway` for a completion and gets back the text, the tokens used and the cost, or a failure it can show.
- UC2 A developer opens `/dev/ai` in Development, sends a prompt, and sees the answer, the model, the tokens, the cost and the row written to `ai_calls`.
- UC3 The owner reads what the app spent per user and period from `ai_calls`, without a screen (SQL for now).

## Business rules
- BR1 Every call to a model goes through `IAiGateway`. No other code talks to the Claude API or references the `Anthropic` package; the architecture test enforces it.
- BR2 A call needs a signed-in user. With `ICurrentUser.UserId` null the gateway refuses with `ai.no_user` and calls nothing.
- BR3 Before every call the gateway asks `IEntitlementService.GetLimitAsync(userId, AiFeatures.CallsPerMonth)`. When a limit exists and the user's successful calls in the current calendar month (UTC) already reached it, the gateway refuses with `ai.quota_exceeded` and calls nothing. No limit configured means no ceiling.
- BR4 Without an API key the app still starts; every call fails with `ai.not_configured` and nothing is recorded.
- BR5 Every call that reached the API is recorded in `ai_calls`, whether it succeeded or failed: user, tenant, purpose, model, input and output tokens, the unit prices applied, the cost, the duration, and the error code when it failed. A call refused by BR2, BR3 or BR4 is not recorded: nothing was spent.
- BR6 Cost is computed from the tokens the response reports and the prices in configuration for that model ($/M input and $/M output), and stored on the row together with those prices, so a later price change never rewrites the past.
- BR7 The default model is `claude-opus-5`. Each purpose may name another model in configuration; code never hardcodes a model id.
- BR8 A failure of the API (network, 4xx, 5xx) becomes a `Result` failure with `ai.call_failed`; the gateway never throws at the caller.
- BR9 `/dev/ai` exists only in Development: the page renders nothing outside it and the menu entry is hidden, exactly as `/dev/ui` does.
- BR10 The gateway emits the OpenTelemetry counters for tokens and cost (ADR-0001 #31) on every recorded call.

## Screens and API
- `/dev/ai` — development-only diagnostics page: prompt box, purpose selector, Send; result panel with the answer, model, input and output tokens, cost and duration; the failure code when it fails. Menu entry `Nav.Dev.Ai` under the Development section, `DevelopmentOnly: true`.
- `POST /api/v1/ai/diagnostics` — Development only, and absent outside it: takes the purpose and the prompt, returns the answer, the model, the tokens, the cost and the duration, or a problem document with the `ai.*` error code. The page reaches it through a typed client, as every other page does.
- Error codes: `ai.not_configured`, `ai.no_user`, `ai.quota_exceeded`, `ai.call_failed`.
- Schema `ai`, table `ai_calls`: `id`, `user_id`, `tenant_id` (nullable), `purpose`, `model`, `input_tokens`, `output_tokens`, `input_price_per_million`, `output_price_per_million`, `cost_usd`, `duration_ms`, `succeeded`, `error_code` (nullable), `started_at`. Index on (`user_id`, `started_at`) for the monthly count of BR3.

## Acceptance criteria
- AC1 Given a signed-in user under the limit, when a feature calls `IAiGateway` with a prompt, then it gets the answer text with the input and output tokens and the cost, and one row is written to `ai_calls`.
- AC2 Given a prompt with a JSON schema for the answer, when the gateway is called, then the answer comes back matching that schema.
- AC3 Given no signed-in user, when the gateway is called, then it fails with `ai.no_user`, the Claude API is not called and `ai_calls` gains no row.
- AC4 Given a user whose successful calls this month reached the configured limit, when the gateway is called, then it fails with `ai.quota_exceeded`, the Claude API is not called and `ai_calls` gains no row.
- AC5 Given no API key configured, when the app starts, then it starts, and a call fails with `ai.not_configured` and writes no row.
- AC6 Given the API answers with an error, when the gateway is called, then it fails with `ai.call_failed`, does not throw, and the row in `ai_calls` records the failure with its error code.
- AC7 Given a recorded call, when the row is read, then `cost_usd` equals the tokens multiplied by the prices stored on the row.
- AC8 Given a purpose with its own model in configuration, when the gateway is called for that purpose, then the call and the row use that model; any other purpose uses `claude-opus-5`.
- AC9 Given the app runs outside Development, when `/dev/ai` is opened, then the page renders nothing and the menu entry is absent.
- AC9b Given the app runs outside Development, when `POST /api/v1/ai/diagnostics` is called, then it answers 404: the endpoint is not mapped.
- AC10 Given any code outside the `Ai` building block, when the architecture tests run, then no reference to the `Anthropic` package is found.
- AC11 All new texts appear in pt-BR, pt-PT and en.

## Decisions
- 2026-09-24 — Scope is infrastructure only: gateway, record, dev page. No import, no coach — owner, stop 1 — the dependency F-40 needs is a real call path, not a product feature.
- 2026-09-24 — `IEntitlementService` is created now in a new `Simulab.Plans.Contracts` project, its ADR-0001 #15e home, with an implementation reading limits from `appsettings`; the real `Plans` module replaces the implementation later without touching the gateway — owner, stop 1.
- 2026-09-24 — The contract carries `HasFeatureAsync` and `GetLimitAsync` only. `GetRemainingAsync` (ADR-0001 #15e) waits for the real `Plans` module: remaining needs a meter per feature, and the meter for AI lives with the `ai_calls` data, not in `Plans` — Claude, technical.
- 2026-09-24 — Usage and cost live in their own `ai` schema with the building block's DbContext and migrations, as `Simulab.Jobs` does — owner, stop 1.
- 2026-09-24 — v1 exposes text completion with optional structured output (`output_config.format`). Streaming, documents and tool calls come with the feature that needs them — owner, stop 1.
- 2026-09-24 — Over quota the gateway refuses before calling and returns a `Result` failure, not an exception — owner, stop 1.
- 2026-09-24 — Default model `claude-opus-5` ($5/$25 per million tokens), overridable per purpose in configuration — owner, stop 1.
- 2026-09-24 — Prices live in configuration and the cost is computed and stored per call, with the prices used — owner, stop 1.
- 2026-09-24 — No API key means the app starts and calls fail with `ai.not_configured`, rather than a failed start — owner, stop 1.
- 2026-09-24 — Package `Anthropic` 12.50.0 (MIT, author Anthropic, the official .NET library), added to Central Package Management. No new test package: the gateway is faked in tests (ADR-0001 #42) — owner, stop 1.
- 2026-09-24 — Validation on screen through a development-only page `/dev/ai`, gated exactly like `/dev/ui` and with no new permission — owner, stop 1.
- 2026-09-24 — Adaptive thinking (`thinking: {type: "adaptive"}`), non-streaming, `max_tokens` 16000, SDK default timeout and retries — Claude, technical — `budget_tokens` is rejected by `claude-opus-5`.
- 2026-09-24 — Page texts go in `SharedResources` with `AiDiagnostics.*` keys and the menu key `Nav.Dev.Ai`, as `/dev/ui` does with `Gallery.*` — Claude, technical — a development page does not justify a resource file of its own.
- 2026-09-24 — `ai_calls` carries a nullable `tenant_id`, dormant like every other table (ADR-0001 #7) — Claude, technical.

## Out of scope
- PDF and image input, streaming, tool calls, prompt caching and the Batches API.
- The real `Plans` module: plans, assignments, promo codes, the usage meter and `GetRemainingAsync`.
- A back-office screen for usage and cost per user and period.
- AI-assisted exam import and the AI coach.
- The eval suite and its pass-rate baseline: that is F-40.

## Open questions
- (none)

## Change notes

### v2 — 2026-09-24
- What: the diagnostics page reaches the gateway through a new Development-only `POST /api/v1/ai/diagnostics` in the Api host and a typed client, instead of calling `IAiGateway` in process.
- Why: the premise was false. `Simulab.Web` has no database and references no module (`Simulab.Web.csproj`: only `Catalog.Contracts`, `Identity.Contracts` and `ServiceDefaults`); every page goes through a typed client, and the `ai_calls` table lives in the Api host, which runs the migrations. Option B (giving the Web its own database) contradicts the profile ("Web: no business rules").
- Affected: `## Screens and API` (the "no endpoint" line becomes the endpoint), new AC9b. The gateway, its rules BR1 to BR8 and BR10, the schema and AC1 to AC9, AC10 and AC11 are unchanged.
- Re-approved: 2026-09-24

## Validation script
<!-- Written at the end of build. -->

## Delivery
- Branch: feature/F-41
- Merge: <commit>
- Tests: <count, duration>
- Manual pages: <paths>
