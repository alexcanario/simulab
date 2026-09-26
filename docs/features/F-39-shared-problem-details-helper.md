---
feature: F-39
epic: Foundation and identity
status: approved
board: 760
version: 1
---
# One problem-details helper for every module's API

## Summary
`CatalogEndpoints.Problem`/`StatusFor`, `IdentityEndpoints.Problem`/`StatusFor` and, since F-41, `AiDiagnosticsEndpoints.Problem`/`StatusFor` are the same code: they turn an `Error` into the problem-details response with its status and its stable `code`. Move it to a new building block all three call, so the answer a caller gets does not depend on which module wrote the endpoint, and a fourth module inherits it. An architecture test then refuses the fourth copy, which is how this debt grew twice. Raised by the independent review of F-33 (2026-09-23); the third copy was recorded here during F-41 (2026-09-24).

## Start
- Depends on: nothing. F-33 and F-41 are done and on `main`.
- Waits on: nothing — the owner answered every question on 2026-09-26.
- Suggested path: `/agile:build F-39` — no screen, and the shape is settled.
- Parallel with: F-43 (visual language). F-39 touches no screen, no theme, no kit and no resource file; F-43 touches nothing under `src/Modules/*/Api/`.

## Goal
One answer shape for the whole API. Today three copies of the same mapping drift independently, and the Identity one already has a capability — a status override and extensions — that the other two lack, so the same `ErrorKind` can leave two endpoints differently.

## What already exists
- Three copies, verified today: `CatalogEndpoints.cs:32` and `:45`, `IdentityEndpoints.cs:459` and `:478`, `AiDiagnosticsEndpoints.cs:60` and `:69`.
- The three `StatusFor` are identical: NotFound → 404, Conflict → 409, BusinessRule → 422, Forbidden → 403, anything else → 400.
- The three `Problem` are not. Identity's takes an optional status override and `params (string Name, object Value)[] extensions`; Catalog's and the Ai one take the `Error` alone.
- Callers: 33 in `Simulab.Identity.Api`, 11 in `Simulab.Catalog.Api`, 3 in `Simulab.Api`. Four pass the rich form: `IdentityEndpoints.cs:366`, `:399`, `:435` and `TotpEndpoints.cs:100`, all `423 Locked` with `retryAfterSeconds`.
- `IdentityApiClient` reads `retryAfterSeconds` out of the problem details in four places (`:98`, `:137`, `:180`, `:416`), so that extension is a shipped contract.
- No building block references ASP.NET today, and `SharedKernel` may not: `BuildingBlockBoundaryTests.SharedKernel_DoesNotDependOnEfCoreOrAspNet`.
- `TokenEndpoints.cs:27` says the OpenIddict routes answer with the protocol's own shape (`error` / `error_description`), not problem details.
- Nothing anywhere produces per-field validation problem details.

## Users and use cases
- UC1 A client of any Simulab endpoint gets the same problem-details shape and the same status for the same kind of failure, whichever module answered.
- UC2 An endpoint that must add something to the answer — the `retryAfterSeconds` of a locked sign-in — still can, through the same helper.
- UC3 A developer who writes a fourth module cannot quietly start a fourth copy: the build refuses it and says where the shared one is.

## Business rules
- BR1 One place turns an `Error` into an HTTP answer: a new building block `Simulab.ApiResults`. Every `/api/v1/` endpoint of every module and of the Api host calls it.
- BR2 The mapping is the one all three copies already agree on: NotFound → 404, Conflict → 409, BusinessRule → 422, Forbidden → 403, anything else → 400. No status changes in this item.
- BR3 The answer keeps the shape it has today: RFC 9457 problem details with `Status`, `Title` = the code, `Detail`, and the `code` extension the UI localizes by.
- BR4 One method, with optional parameters: the status override and the extensions of the Identity copy. A caller that passes neither gets exactly what it got before.
- BR5 The `retryAfterSeconds` extension of the `423 Locked` answers survives unchanged: it is a shipped contract the Web reads.
- BR6 No type outside `Simulab.ApiResults` declares a status mapping for `ErrorKind` or builds a `ProblemDetails` from an `Error`. The architecture test fails the build and names the file.
- BR7 The building block references the ASP.NET framework and `SharedKernel`, and nothing else. It is listed in `docs/agile/profile.md` in this item, as the rule requires for a new building block.
- BR8 The OpenIddict token endpoints keep the protocol's own error shape (ADR-0001 #1) and are not touched.

## Screens and API
- No screen. No route changes, no new error code, no status change: the same requests get the same answers.
- New project `src/BuildingBlocks/Simulab.ApiResults`.

## Acceptance criteria
- AC1 Given any `ErrorKind`, when the helper maps it, then the status is the one of BR2, and the test covers every member of the enum plus a value outside it.
- AC2 Given an `Error`, when the helper builds the answer, then it carries `Status`, `Title` = the code, `Detail` and the `code` extension.
- AC3 Given a caller that passes a status override and extensions, when the helper builds the answer, then the override wins and every extension is present.
- AC4 Given the sign-in of a locked account, when it answers through the real HTTP pipeline, then it is still `423` with `retryAfterSeconds`.
- AC5 Given the Catalog, Identity and Ai endpoints after the move, when their existing tests run, then every one passes unchanged: no test of an endpoint's answer is edited in this item.
- AC6 Given a type outside `Simulab.ApiResults` that maps `ErrorKind` to a status or builds a `ProblemDetails` from an `Error`, when the architecture test runs, then it fails and names that type; and the test fails too if it finds nothing to check.
- AC7 Given `docs/agile/profile.md`, when the item is done, then `Simulab.ApiResults` is in its list of building blocks.
- AC8 No new UI text: the codes, their meanings and their resource keys are unchanged.

## Decisions
- 2026-09-26 — The helper lives in a new building block `Simulab.ApiResults` — owner — no building block references ASP.NET today and `SharedKernel` may not; making Catalog call `IdentityEndpoints.Problem` would break the module boundary instead.
- 2026-09-26 — One method with optional parameters, in the shape of the Identity copy — owner — it is a superset of the other two, so the 44 simple calls keep their shape and the 4 rich ones keep compiling.
- 2026-09-26 — An architecture test refuses a fourth copy — owner — this debt grew twice (F-33 made the second, F-41 the third); only a build failure stops the fourth.
- 2026-09-26 — Out: the OpenIddict token endpoints, the Web side, and per-field validation problem details — owner — the first has the protocol's shape by ADR-0001 #1, the second reads `code` and does not change, and the third has no code producing it today.
- 2026-09-26 — No new NuGet package: the building block reaches ASP.NET through `<FrameworkReference Include="Microsoft.AspNetCore.App" />` — Claude, technical — that is how a class library uses `Results` and `ProblemDetails` without a package.
- 2026-09-26 — The move is mechanical and its proof is that no endpoint test changes (AC5) — Claude, technical — the answers are a shipped contract, so a test that had to be edited would mean the behaviour moved.

## Out of scope
- The OpenIddict token endpoints (`TokenEndpoints`), which answer with `error` / `error_description`.
- Anything on the Web side: it reads the `code` extension and is untouched.
- Per-field validation problem details: no code produces them today.
- Changing any status, any error code or any route.

## Open questions
- (none)

## Change notes

## Validation script
<!-- Written at the end of build. -->

## Delivery
- Branch: `feature/F-39`
- Merge: <commit>
- Tests: <count, duration>
- Manual pages: <paths>
