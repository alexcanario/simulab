---
feature: F-39
epic: Foundation and identity
status: validating
board: 760
version: 3
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
- Callers: 33 in `Simulab.Identity.Api`, 11 in `Simulab.Catalog.Api`, 3 in `Simulab.Api`. Five pass the rich form: `IdentityEndpoints.cs:366`, `:399`, `:435` and `TotpEndpoints.cs:100` are `423 Locked` with `retryAfterSeconds`; `GoogleSignInEndpoints.cs:40` passes a `429 Too Many Requests` override with no extension.
- `TotpEndpoints.Failure` (`:89-91`) turns `ErrorKind.NotFound` into `401` instead of the usual `404`, so the TOTP endpoints do not reveal whether an account exists. It is a deliberate per-endpoint decision, not a copy of the mapping.
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
- BR6 No file outside `Simulab.ApiResults` carries the general mapping this feature exists to remove: a table from `ErrorKind` to a status, or a `ProblemDetails` built from an `Error`. A single endpoint's own decision about one kind is allowed — the TOTP `401` of `TotpEndpoints.Failure` is one — but it is named in the test with its reason, so a new one is a deliberate act and not a quiet fourth copy. The architecture test fails the build and names the file and the line.
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
- AC6 Given a file outside `Simulab.ApiResults` that carries the general mapping, when the architecture test runs, then it fails and names the file and the matched line; and the test fails too if it scanned nothing or if its own positive control stops matching.
- AC6b Given the three files that decide a status for one kind on purpose (`TotpEndpoints`, `RevocationCheckMiddleware`, `PermissionForbiddenResultHandler`), when the architecture test runs, then it asserts the detector does **not** flag them, naming each with its reason; and it fails if that list is empty or names a file that no longer exists.
- AC7 Given `docs/agile/profile.md`, when the item is done, then `Simulab.ApiResults` is in its list of building blocks.
- AC8 No new UI text: the codes, their meanings and their resource keys are unchanged.

## Decisions
- 2026-09-26 — The helper lives in a new building block `Simulab.ApiResults` — owner — no building block references ASP.NET today and `SharedKernel` may not; making Catalog call `IdentityEndpoints.Problem` would break the module boundary instead.
- 2026-09-26 — One method with optional parameters, in the shape of the Identity copy — owner — it is a superset of the other two, so the 44 simple calls keep their shape and the 4 rich ones keep compiling.
- 2026-09-26 — An architecture test refuses a fourth copy — owner — this debt grew twice (F-33 made the second, F-41 the third); only a build failure stops the fourth.
- 2026-09-26 — Out: the OpenIddict token endpoints, the Web side, and per-field validation problem details — owner — the first has the protocol's shape by ADR-0001 #1, the second reads `code` and does not change, and the third has no code producing it today.
- 2026-09-26 — No new NuGet package: the building block reaches ASP.NET through `<FrameworkReference Include="Microsoft.AspNetCore.App" />` — Claude, technical — that is how a class library uses `Results` and `ProblemDetails` without a package.
- 2026-09-26 — The guard is one file scan over `src/**/*.cs`, not reflection over the assemblies — Claude, technical, from the architect pass — a reflection guard reads `SolutionAssemblies.All`, a hand-kept list that F-24 caught drifting; a file scan sees a fourth module's file the day it is written. It flags a file carrying the whole word `ErrorKind` together with `new ProblemDetails` or `StatusCodes.Status`, keeps a named allowlist with a reason per entry, and carries its own positive control so a scan that matches nothing fails.
- 2026-09-26 — `ArgumentNullException.ThrowIfNull(error)` is added to the moved method for consistency with `CatalogEndpoints.cs:34`, not because an analyzer asks: `.editorconfig:147` sets `CA1062` to `none` — Claude, technical — the design pass gave the analyser as the reason and it was wrong.
- 2026-09-26 — Each calling file gets `using static Simulab.ApiResults.ApiProblem;` so all 47 call sites keep reading `Problem(error)` — Claude, technical — that is what makes AC5 ("no endpoint test edited") a real check of behaviour rather than of churn.
- 2026-09-26 — The type is `ApiProblem`, not `ApiResults` — Claude, technical — a type with its namespace's name trips CA1724, and a type named `Results` would collide with `Microsoft.AspNetCore.Http.Results` at every call site.
- 2026-09-26 — `Simulab.ApiResults` is added to `ModuleBoundaryTests.IsEfCoreOrAspNetCore` (`:44-51`) — Claude, technical — this item creates the first building block that carries ASP.NET, and without that line a `Domain` or `Application` project could reference it and smuggle ASP.NET past the rule that exists to stop exactly that.
- 2026-09-26 — The move is mechanical and its proof is that no endpoint test changes (AC5) — Claude, technical — the answers are a shipped contract, so a test that had to be edited would mean the behaviour moved.

## Out of scope
- The OpenIddict token endpoints (`TokenEndpoints`), which answer with `error` / `error_description`.
- Anything on the Web side: it reads the `code` extension and is untouched.
- Per-field validation problem details: no code produces them today.
- Changing any status, any error code or any route.

## Open questions
- (none)

## Change notes

### v2 — 2026-09-26
- What: BR6 forbids the general mapping, not every per-endpoint decision about one `ErrorKind`; the exceptions are named in the test with their reason. New AC6b. `## What already exists` corrected: five rich callers, not four, and one of them is a `429`, not a `423`.
- Why: found by the design passes and verified in the files. `TotpEndpoints.Failure` (`:89-91`) already turns `ErrorKind.NotFound` into `401` so the TOTP endpoints do not reveal whether an account exists — a deliberate decision documented in its own summary. BR6 as written forbade it while BR2 forbade changing it, so the pair was impossible. `GoogleSignInEndpoints.cs:40` is a fifth rich caller the file had missed.
- Affected: BR6, AC6; added AC6b; two lines of `## What already exists`. Every other rule and criterion unchanged.
- Re-approved: 2026-09-26

### v3 — 2026-09-26
- What: AC6b becomes a negative control instead of an exemption list. The test names the three per-endpoint decisions and asserts the detector leaves them alone, rather than exempting them from it.
- Why: with the detector keyed on the table itself (`ErrorKind.X => StatusCodes.StatusY`, `case ErrorKind.`, or a `ProblemDetails` built from an `error`), none of the three is flagged — verified by running the detector's own regexes against `TotpEndpoints.cs`. An exemption list that never fires is dead weight that tells the next reader those files carry a copy of the mapping, which they do not. The protection is the same: a laxer detector turns the control red.
- Affected: AC6b only. BR6 and every other rule and criterion unchanged.
- Re-approved: 2026-09-26

## Validation script
Nothing changes on screen: the same requests get the same answers. What is worth checking is that the answers really did not move, and that the guard works. Run from the worktree `D:\dev\_icontrol\wt\simulab\feature-39`.

1. Confirm the three copies are gone and one remains.
   - Git Bash: `grep -rn "int StatusFor(" src/ --include=*.cs | grep -v obj`
   - PowerShell 7: `Get-ChildItem src -Recurse -Filter *.cs | Where-Object { $_.FullName -notlike '*\obj\*' } | Select-String 'int StatusFor\('`
   - Expected: one line, in `src/BuildingBlocks/Simulab.ApiResults/ApiProblem.cs`.
2. Confirm no endpoint test was edited to make it pass — that is the proof the behaviour did not move (AC5).
   - Both shells: `git diff --stat main...HEAD -- tests/Modules tests/Hosts/Simulab.Api.Tests`
   - Expected: no output.
3. Start the app host and sign in as an Admin, then open **Administração → Papéis**, open the same role in two tabs, delete it in one and then delete it in the other. Expected: the second tab shows the "not found" message, not a raw code — the 404 still carries its `code`.
4. On the sign-in page, get an account locked (five wrong passwords) and try once more. Expected: the message says how many seconds to wait. That is the `423` with `retryAfterSeconds`, the answer shape this item had to preserve exactly.
5. Switch the language to **English** and repeat step 4. Expected: the same message in English, because the page still localizes by `code`.
6. See the guard refuse a fourth copy. **Stop the app host first** (step 8), and close the solution in the IDE if it has it open: a running `Simulab.Api` locks `bin/` and the build fails with `MSB3027 ... file is locked by: "Simulab.Api"` before any test runs.
   1. Open `src/Modules/Catalog/Simulab.Catalog.Api/OrganizerEndpoints.cs`.
   2. Add this line to the block of `using` at the top (the file does not have it, because after this item it no longer needs `ErrorKind`):
      ```
      using Simulab.SharedKernel.Results;
      ```
   3. Paste these five lines immediately **before the last `}` of the file** — that closing brace ends the class, and the method has to be inside it:
      ```
      private static int StatusFor(ErrorKind kind) => kind switch
      {
          ErrorKind.NotFound => StatusCodes.Status404NotFound,
          _ => StatusCodes.Status400BadRequest
      };
      ```
   4. Run, in either shell: `dotnet test tests/Simulab.ArchitectureTests --filter "FullyQualifiedName~ProblemDetailsBoundaryTests"`
   5. Expected: `Failed: 2, Passed: 2`. `OnlyTheBuildingBlock_CarriesTheMapping` fails with `Found in: Modules/Catalog/Simulab.Catalog.Api/OrganizerEndpoints.cs`, and `TheDeletedCopies_AreGone` fails too. Two failures is the right answer: one test sees the table, the other sees the method name come back.
   6. Undo it with git, not by hand: `git checkout -- src/Modules/Catalog/Simulab.Catalog.Api/OrganizerEndpoints.cs`, then `git status --short` and expect no line for that file.
7. Reach step 4's sign-in with the keyboard only: Tab into the address, type, Tab to the password, type, Tab to the button, Enter. Expected: the same locked message.
8. Stop the app host.

## Delivery
- Branch: `feature/F-39`
- Merge: <commit>
- Tests: <count, duration>
- Manual pages: <paths>
