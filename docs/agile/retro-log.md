# Retro log

One entry per retro: date, item, lessons and where each one went.

## Plugin notes — status
Every note tagged `plugin` in this log, and what happened to it in agile@canary (`D:\dev\agile-canary`). ✅ = implemented in the plugin · ⏳ = still open. A new `plugin` note gets a row here with ⏳.

| Status | Plugin | Note | From | Plugin version (commit) |
|---|---|---|---|---|
| ✅ | agile | Model per agent and activity | Bootstrap session | 0.0.7 (`97053a3`) |
| ✅ | agile | UI standards as a core rule, quiz questions, UI kit before the first screen | Bootstrap session | 0.0.7 (`97053a3`) |
| ✅ | agile | Bootstrap quiz gaps: trials and promo codes, closing question, reading an existing code base | Bootstrap session | 0.0.7 (`97053a3`) |
| ✅ | agile | PascalCase for constants and `static readonly` in the template `.editorconfig` | Sync to 0.0.7 | 0.0.10 (`b7128e5`) |
| ✅ | agile | A command to refresh the project copies (`/agile:sync`) | Sync to 0.0.7 | 0.0.8 (`0361053`) |
| ✅ | agile | Parallel work on request (`/agile:build <id> --worktree`) | Owner question | 0.0.9 (`8ab237e`) |
| ✅ | agile | App host left running: gate names the locked build output; build and ship stop what Claude started | F-1 | 0.0.10 (`b7128e5`) |
| ✅ | agile | Commit on the wrong branch: branch check before each commit and a guard hook on the main branch | F-1 | 0.0.10 (`b7128e5`) |
| ✅ | agile | Hanging tests: hang timeout in the gate, hung tests reported by name | F-1 | 0.0.10 (`b7128e5`) |
| ✅ | agile | Screen check before the coverage table: library state styles in both themes; keyboard steps stay in the validation script | F-2 | 0.0.11 (`a9babf4`) |
| ✅ | agile | The refinement package question must cover the packages the tests need, not only the production ones | F-3 | 0.0.11 (`a9babf4`) |
| ✅ | agile | A building block that reads an app-host connection string is exercised through the app host in the item that creates it, not only against a test container | F-4 | 0.0.15 (`b221360`) |
| ✅ | agile | At ship, check that every decision naming a file ("... in X.md") is reflected in that file | F-4 | 0.0.15 (`b221360`) |
| ✅ | agile | Guard hook: refuse a commit on the main branch that touches the file of an item that is not done | F-12 | 0.0.19 (`c48c34e`) |
| ✅ | agile | Retro commits carry no code or tests; a code lesson becomes an item | F-12 | 0.0.19 (`c48c34e`) |
| ✅ | agile | Technical terms used with the owner go into a glossary section in the same step | F-8 | 0.0.28 (`6f1a488`) |
| ✅ | agile | bUnit: after a click whose handler awaits, assert with `WaitForAssertion` | F-8 | 0.0.29 (`afe7431`) |
| ✅ | agile | A per-test-class `WebApplicationFactory` clears its Npgsql pool on dispose | F-8 | 0.0.29 (`afe7431`) |
| ✅ | agile | Gate output is saved whole to a file; the gate prints the new warnings in its last lines | B-7 | 0.0.31 (`b6af246`) |
| ✅ | agile | Anonymous endpoints check every input limit before the account lookup | B-7 | 0.0.31 (`b6af246`) |
| ✅ | agile | Validation-script terminal steps in Bash and PowerShell, run before handing over | B-4 | 0.0.31 (`b6af246`) |
| ✅ | agile | A premise about how a library stores or protects data is verified in its source or docs during refinement | F-11 | 0.0.36 (`cfedd49`) |
| ✅ | agile | In a multi-step sign-in, only the last step clears the failure count | F-11 | 0.0.36 (`cfedd49`) |
| ✅ | agile | Refinement checks package versions against the registry; build re-checks before adding | F-11 | 0.0.36 (`cfedd49`) |
| ✅ | agile | A "never leave zero X" rule is judged before and after, not only after | F-10 | 0.0.35 (`7303a61`) |
| ✅ | agile | A theme's colour tokens get a contrast test over the palette | F-10 | 0.0.35 (`7303a61`) |
| ✅ | agile | A Blazor Server dialog is driven from the browser pane in one call | F-10 | 0.0.35 (`7303a61`) |
| ✅ | agile | Never change state in an app host Claude did not start | B-4 | 0.0.31 (`b6af246`) |
| ✅ | agile | Blazor Server: request data a circuit needs is read in `App` and passed to the interactive root | B-4 | 0.0.31 (`b6af246`) |
| ✅ | agile | Kit parameters whose value depends on the page's meaning have no default (`[EditorRequired]`) | B-5 | 0.0.32 (`33dfa1e`) |
| ✅ | agile | Source and test files are written with the Write/Edit tools, never through a script's string literals | B-6 | 0.0.32 (`33dfa1e`) |
| ✅ | agile | A colour or contrast bug is checked in both themes and on every surface the component sits on | B-6 | 0.0.32 (`33dfa1e`) |
| ✅ | agile | ASP.NET Core middleware resolves every `InvokeAsync` parameter on every request, even inside an untaken branch — resolve an optional heavy dependency from `HttpContext.RequestServices` instead | F-5 | 0.0.26 (`557c7ff`) |
| ✅ | agile | Aspire's `AddRedis()` secures the local container with TLS and a password by default; a plain `ConnectionMultiplexer.Connect` cannot trust its dev certificate and hangs until the socket times out — use the Aspire client integration (`AddRedisClient`) instead | F-5 | 0.0.26 (`557c7ff`) |
| ✅ | agile | OpenIddict issues an encrypted (JWE) access token once an encryption certificate is registered, not a plain signed JWT; a client should never try to decode its own claims out of the token — ask the resource server for them instead | F-5 | 0.0.26 (`557c7ff`) |
| ✅ | agile | Central Package Management's transitive pinning can float a shared package (e.g. `Aspire.Hosting`) to a higher version than a sibling package (`Aspire.Hosting.Testing`) pins, because another package elsewhere in the solution needs the newer one. An obsolete-API investigation must check the resolved version in `obj/project.assets.json`, not the pinned version in `Directory.Packages.props` | B-1 | 0.0.26 (`557c7ff`) |
| ✅ | agile | `[generic]` A coverage row maps a criterion to a test that goes through the path the user reaches; a method written for a criterion with no caller in production code is a gap, not coverage | B-3 | 0.0.27 (`641d463`) |
| ✅ | agile | `[stack: aspire]` The ServiceDefaults template disables retries for unsafe HTTP methods (`Retry.DisableForUnsafeHttpMethods()`): a retried POST replays single-use tokens and sends emails twice | B-3 | 0.0.27 (`641d463`) |
| ✅ | agile | `[stack: blazor-server]` State that changes during a session (rotating tokens, permissions) lives in a server-side store keyed by an id in the cookie, never in the cookie: a circuit cannot rewrite it | B-3 | 0.0.27 (`641d463`) |
| ✅ | agile | `[stack: blazor]` A Razor attribute mistake compiles: a string parameter without `@` is literal text, and a wrong generic parameter name only fails at runtime. Every new page gets a bUnit test that renders it | F-9 | 0.0.35 (`7303a61`) |
| ✅ | agile | `[generic]` A premise taken from an earlier item's file is verified against the code and against the items that touched it since; a bug can have moved the behaviour | F-9 | 0.0.35 (`7303a61`) |
| ✅ | agile | `[profile: modular-monolith]` A row a request must not lose is staged on the caller's own `DbContext`; the shared table is mapped into it with `ExcludeFromMigrations()` | F-13 | 0.0.36 (`cfedd49`) |
| ✅ | agile | `[generic]` When an effect moves out of the request, re-read every test that asserted it: "nothing was sent" passes for free once the effect is deferred | F-13 | 0.0.36 (`cfedd49`) |
| ✅ | agile | `[stack: ef-core]` A `SaveChanges` that fails after `Remove` leaves the entry `Deleted`; the next save repeats the DELETE instead of writing the error | F-13 | 0.0.36 (`cfedd49`) |
| ✅ | agile | `[stack: .NET]` A warning count is quoted only from the gate or a `--no-incremental` build; an incremental build hides the warnings of unchanged projects | F-14 | 0.0.37 (`fd68a6c`) |
| ✅ | agile | `[stack: .NET]` After `git stash` / `stash pop` or a branch switch, rebuild before running tests; `--no-build` runs the other tree's binaries and the counts lie | B-10 | 0.0.37 (`fd68a6c`) |
| ✅ | agile | `[generic]` Every `gate.js` mode ends with a verdict line; `stop` with nothing marked says so instead of exiting silently | B-10 | 0.0.37 (`fd68a6c`) |
| ✅ | agile | `[stack: ef-core]` DocGen template: build each model through its design-time factory (snake_case), strip `Module` from the name, drop the `Relational` reference and the `NoWarn`, skip `bin/`/`obj/`, Auth column only with security, orphans are stale | F-15 | 0.0.38 (`836a0c1`) |
| ✅ | agile | `[profile: modular-monolith]` The route map's OpenAPI document comes from the `/openapi/v1.json` integration test, not from `ApiDescription.Server` at build | F-15 | 0.0.38 (`836a0c1`) |
| ✅ | agile | `[generic]` A "nothing uses X" premise is verified by the effect (built model, snapshot, output), not by one helper's callers | F-15 | 0.0.38 (`836a0c1`) |
| ✅ | agile | `[generic]` A new rule about a test pattern comes with a sweep of the existing tests for that pattern | B-11 | 0.0.39 (`a8b2ded`) |
| ✅ | agile | `[generic]` A flaky test is reproduced with a clean-build loop, and its fix is proven by the same loop (N green in a row) | B-11 | 0.0.39 (`a8b2ded`) |
| ✅ | agile | `[stack: blazor]` A bUnit assertion about anything that follows a click (snackbar, dialog closing, JS interop) uses `WaitForAssertion`, new tests included | F-16 | 0.0.39 (`a8b2ded`) |
| ✅ | agile | `[generic]` A screen behind sign-in that Claude may not sign in to: say so, cover the signed-in flow in the validation script, check through the app host what needs no account | F-16 | 0.0.39 (`a8b2ded`) |
| ✅ | agile | `[generic]` When an owner's answer turns something into an idea, refine and build call `idea-capture` instead of writing the file by hand | B-12 | 0.0.39 (`a8b2ded`) |
| ✅ | agile | `[generic]` Gate `stop` with nothing marked says how to check the affected projects instead of only skipping | B-12 | 0.0.39 (`a8b2ded`) |
| ✅ | agile | `[stack: DocGen]` A test of generated output counts each section's occurrences, not only its presence | B-12 | 0.0.39 (`a8b2ded`) |
| ✅ | agile | `[generic]` Refinement never switches a shared checkout or one on another item's branch: `git branch <item> main` and a worktree, or ask | B-13 | 0.0.41 (`6c854ea`) |
| ✅ | agile | `[generic]` Ship checks the current branch is main and lists `git log <main>..<branch>` for another item's id before merging | B-13 | 0.0.41 (`6c854ea`) |
| ✅ | agile | `[generic]` A feature with a visual output is prototyped and measured at real size in the target viewer before approval | F-26 | 0.0.41 (`6c854ea`) |
| ✅ | agile | `[stack: DocGen]` Entity diagrams as a DBML schema per module in place of the Mermaid entities page | F-26 | 0.0.41 (`6c854ea`) |
| ✅ | agile | `[generic]` Previewing an app host from a worktree: temporary launch configuration with the absolute project path, restored afterwards | F-17 | 0.0.52 (`181120c`) |
| ✅ | agile | `[stack: .NET]` Before `git worktree remove`: `dotnet build-server shutdown` and Visual Studio closed | F-17 | 0.0.52 (`181120c`) |
| ✅ | agile | `[generic]` A premise about query performance is measured with an `EXPLAIN` on the test container before approval | F-18 | 0.0.52 (`181120c`) |
| ✅ | agile | `[stack: DocGen]` The data dictionary shows the filter of a partial index | F-18 | 0.0.52 (`181120c`) |
| ✅ | agile | `[generic]` A library premise that reading its source does not pin is reproduced in a scratch project against a test container before asking; source read raw, not summarized | F-19 | 0.0.52 (`181120c`) |
| ✅ | agile | `[generic]` An authentication or account-linking item gets the independent review on the refined item file before approval, not only on the code | F-20 | 0.0.52 (`181120c`) |
| ✅ | agile | `[stack: Blazor Server]` A page peeks a single-use ticket in `OnInitialized` (prerender runs it twice) and spends it on success; a ticket in a URL is bound to the browser by an HttpOnly cookie | F-20 | 0.0.52 (`181120c`) |
| ✅ | agile | `[generic]` feature-ship: confirming with the owner that the IDE is closed on the worktree is a blocking question **before** `git worktree remove`, because a removal that fails halfway unregisters the worktree and leaves the folder on disk | F-21 | 0.0.52 (`181120c`) |
| ✅ | agile | `[generic]` feature-build: before copying an existing pattern, look for an open item that exists to remove that pattern; if there is one, say so and let the owner choose between following it now and recording the debt | F-22 | 0.0.52 (`181120c`) |
| ✅ | agile | `[generic]` feature-refinement: when a bug's cause is found in code that exists only on an unmerged branch (its feature is still `validating`), say so in `## Cause` and record that the build waits for that merge; then create the item worktree from the merged `main` | B-14 | 0.0.52 (`181120c`) |
| ✅ | agile | `[generic]` feature-ship: after `gate.js ship`, `git status` on the item worktree must show no tracked file changed outside bin/obj; a generated file the test run rewrote (for example `docs/api/Simulab.Api.json`) is committed with the item before the merge is requested, not found when the worktree removal refuses | B-15 | 0.0.57 (`19e1f95`) |
| ⏳ | agile | `[stack: DocGen]` feature-ship step 7b says a new module or external system "updates `docs/architecture/overview.md` by hand", but DocGen reports stale and deletes every file under `docs/architecture/` it does not generate (`GeneratedDocs.StaleFiles`/`Write`); a hand-written overview lives outside that folder (Simulab: `docs/architecture-overview.md`, guarded by `ArchitectureOverviewTests`) | F-23 | |
| ⏳ | agile | `[generic]` feature-refinement: when two business actors would share one table, the refinement asks who owns what before reusing it — a screen will offer the wrong one and the Api will accept it | F-34 | |
| ⏳ | agile | `[stack: MudBlazor]` screen-design: an accessibility claim about a library component (an ARIA role, an announced state) is verified in the gallery's DOM before it enters the item file — `MudAutocomplete` 9.9 renders a plain text input, with no `role="combobox"` and no `aria-expanded` | F-34 | |
| ⏳ | agile | `[generic]` The Stop gate can print GREEN while a new warning sits in a project its incremental build skipped: a warning count is trustworthy only from a non-incremental build. Also, a test that rewrites a committed file (an OpenAPI document, a snapshot) has its output swept in by `git add -A` — the build skill should say to read `git status` before committing, not stage blindly | F-41 | |
| ⏳ | agile | `[profile: modular-monolith]` feature-refinement: before writing `## Screens and API`, confirm which host owns the screen's data and how that host reaches it. In a modular monolith the UI and the API are different hosts, so "the page calls the service in process" is a premise, not a given | F-41 | |
| ⏳ | agile | `[generic]` feature-build step 17: a validation step that asks the owner to change code names the file, where the lines go and the exact failure to expect, and any step that compiles says to stop the app host first — a running host locks `bin/` and the build dies with MSB3027 before a single test runs | F-39 | |
| ⏳ | agile | `[generic]` feature-build step 8b: the brief for the `frontend` agent must separate test infrastructure (helpers, contexts, fixtures — changeable) from assertions (fixed). Told only "do not edit existing tests", it left a required conversion undone and reported it as a stop | F-43 | |
| ⏳ | agile | `[generic]` autopilot step 5: the approval card says "with the criteria below", but a card round is a single tool call, so anything written after it reaches the owner only once they have already answered. The criteria have to be in the message that precedes the cards | F-27 | |
| ⏳ | agile | `[generic]` retro-lessons step 6: an `idea` a retro raises says what the finding item already fixed and what is left, so the next refinement does not read a closed gap as a live one. F-45's summary described `AiDbContext` as never loaded, while F-24 had added it in the same item that raised the idea | F-45 | |
| ⏳ | agile | `[generic]` A plugin note becomes ✅ only when an executable scenario proves the behavior changed: each note gets a minimal fixture repository, a prompt and a check; the scenario fails before the plugin change and passes after it. Start with the simple version (scenarios run by hand in the plugin session before marking ✅); a principles check by the `reviewer`, scheduled harvesting of notes into issues and pruning rules whose scenario passes without them come later, only if the first step pays off | Owner session 2026-09-26 | |
| ⏳ | agile | `[generic]` `gate.js ship`: when the red tests are all outside the item's diff, say so in the last line. The gate already knows both. On F-28 it went red twice on a test in another project before anyone suspected the suite itself, and the flake had been sitting on `main` | B-19 | |
| ⏳ | agile | `[stack: Blazor Server]` A `computer` `screenshot` call in the browser pane can time out and disconnect a Blazor Server circuit mid-dialog, losing the dialog's state; prefer `read_page`/`get_page_text` over `screenshot` when driving a Blazor Server dialog | F-32 | |
| ⏳ | agile | `[stack: Microsoft.Extensions.AI]` The DocGen template's `ModelToolAttribute` comment says an `Async` suffix is dropped from the tool name; `AIFunctionFactory` 10.5.1 keeps it (F-49 test: `two or more tools are called "GetExamAsync"`). Also, with no tool the template writes no `tools.md`; say so in the comment, or write an empty catalogue | F-49 | |
| ⏳ | agile | `[stack: bUnit + MudBlazor]` A test that freezes the `TimeProvider` (`FakeTimeProvider`) also freezes the debounce of `MudAutocomplete`: the search never fires. Advance the fake clock past the debounce after typing (F-35 test `Organizer_Typing_AsksTheOrganizerListWithTheTerm`: 5 s timeout, only the exam GET in `Api.Received`; passed after `_clock.Advance`) | F-35 | |
| ⏳ | agile | `[generic]` When the `frontend` agent has no shell, its report says so; the main session then builds and runs the tests before reading the files. On F-35 the first `dotnet test` of its output showed `Failed: 51`, one cause: `inputmode` passed as text to a MudBlazor enum parameter | F-35 | |
| ⏳ | agile | `[generic]` `feature-ship` step 11 and `worktrees.md`: the lock probe gates the removal (`&&`, never `;`): a failed probe stops before `git worktree remove`, which on a held folder unregisters the worktree and leaves the folder half deleted. A `.vs/` folder in the worktree is a hint the IDE holds it | F-37 | |
| ⏳ | agile | `[generic]` `feature-build` step 12: the preview tool (`preview_start` with `.claude/launch.json`) starts the app host from the session's main checkout, not from the item's worktree; for an item in a worktree, start it from the worktree (Bash, in the background) and say so | F-38 | |
| ⏳ | agile | `[generic]` `feature-ship`: refuse the merge when the full gate output of this item was not produced in the shipping session (a session with no SDK stops at `validating`); cause not verified: whether the F-52 ship ran from a session without the SDK | F-38 | |
| ⏳ | agile | `[generic]` `feature-ship` step 4 and the Stop hook: `gate.js ship` runs in the foreground, or the Stop hook skips while a ship gate holds the worktree — both build the same `obj/` folders and collide (a background ship gate failed with 211 missing-reference errors while the hook reported MSB3061) | F-44 | |
| ⏳ | agile | `[generic]` `feature-build` step 17: the validation script of an item that has a migration names the worktree's own database (`Database__Name`, which `AppHost.cs` reads) in its start step, never "your local database" — otherwise the first start applies the migration to the shared one | F-42 | |
| ⏳ | agile | `[generic]` `feature-ship` step 11 and `worktrees.md`: the lock probe (rename the folder to `<folder>.probe` and back) is one command run alone that stops the ship when it fails, so `git worktree remove` can never run after a failed probe | F-42 | |
| ⏳ | agile | `[generic]` `feature-ship` step 8b: run the architecture tests after writing the bumped `<Version>` and before committing it — a test that pins a version number goes red on the first bump (F-46: two `AppVersionTests` failed, `Failed: 2, Passed: 162`) | F-46 | |
| ⏳ | agile | `[generic]` `version.js` writes `<Version>` to the root `Directory.Build.props`, while the `AppVersionTests` the project got from F-60 pin it to `Simulab.Api.csproj`: after F-47's ship `gate.js ship` was red on `main` and blocked F-48 and F-50 (B-21). The plugin should name where the version lives in one place (bootstrap or `/agile:sync`) so a test and the script cannot disagree | F-50 | |
| ⏳ | agile | `[generic]` The Stop gate hook builds the worktree while a background `dotnet clean`/`build` loop of the same session runs in it, and both fail (the loop's runs come back empty). The gate should detect a `dotnet` build already running in the worktree and defer instead of reporting `RED`. Seen again at B-22 with a background `dotnet test` loop: `MSB3061 (0 -> 12)`, then `hostpolicy.dll was not found` in every run | F-48, B-22, B-24 | |
| ⏳ | agile | `[generic]` The Stop gate SKIPS a diff with no code file, yet test projects read documents (here `VocabularyTests` reads `docs/glossary.md`, `ArchitectureOverviewTests` reads `docs/infra.md`): a docs-only edit can break them unseen until ship. The gate should run the test projects that read the changed docs, or treat those docs as code | F-67 | |
| ⏳ | agile | `[generic]` `feature-build` step 12 requires opening the screen through the app host, while `workflow.md` says "never start, mount or reset a database or volume outside the test containers": the two texts disagree. Say which wins (suggestion: the owner's yes, asked once per item, covers the item's own database, as F-79 did with `Database__Name`) | F-79 | |
| ⏳ | agile | `[generic]` The `frontend` agent has only Read, Grep, Glob, Write and Edit: it cannot run a build or a test, yet `feature-build` step 8b asks it for the real counts. Say in step 8b that the main session runs `dotnet build` and the screen's tests right after the report, before trusting its criterion → test rows | F-74 | |
| ⏳ | agile | `[profile: modular-monolith]` `feature-refinement` writes routes and column names into the item without checking them: F-74 said `/api/v1/exams/...` and a `position` column, while the module's prefix is `/api/v1/catalog` and `Position` is already the glossary word for Cargo. Ask refinement to check the module's route prefix against its endpoints file and each new column name against `docs/glossary.md` before approval | F-74 | |
| ⏳ | agile | `[generic]` `gate.js ship` prints only `[FAIL]` and the test name for a failed test, never its failure message or stack trace. A flaky test that failed once (B-22: `DataExportPageTests.Dialog_AccountWithoutPassword_ShowsHowToCreateOne`, F-50 ship, 2026-10-03) left no message, and 30 measured runs at the refinement could not reproduce it, so the cause stayed an inference. Keep each failed test's message and stack (a `trx` logger, or the `Error Message`/`Stack Trace` block) in a file and name the file in the RED line | B-22 | |
| ⏳ | agile | `[generic]` `gate.js stop` prints only the projects it built and `agile gate GREEN`, never which test projects it ran or their totals (F-54, 2026-10-05: 6 `built` lines, then GREEN), so a green stop gate cannot be told apart from one that ran nothing. Print the test projects run and their counts, as `gate.js ship` does. Cause not verified: the tests may have run in silence or been skipped | F-54 | |
| ⏳ | agile | `[generic]` The ship gate keeps only the names of failing tests (`tail(r.out, 25)` in `gate.js`), so the 24 identical failure messages of F-51 were lost and the cause needed a refinement round to reproduce; keep the first failure message of each failing project in the gate output | B-24 | |
| ⏳ | agile | `[generic]` `literal-guard.js` blocked a `sed -i` on a test file but let a `python - <<'EOF'` script rewrite `SeedAdminTests.cs` (exit 0, 76 insertions in the diff): extend the guard to a script that opens and writes a repository file (`open(...,'w')`, `Path.write_text`, `Set-Content`, `File.WriteAllText`) | F-53 | |
| ⏳ | agile | `[generic]` Refinement: a business rule that says a step "does not do Y" (clear a counter, renew a stamp) is checked against every helper that does Y before the file is approved; BR3 of F-53 said the step did not clear the failure count, and `SecondFactor.VerifyAsync` (shared with the account page) does, which only the build design pass found and cost a change note | F-53 | |

This project receives those versions through `/agile:sync`; the last one recorded is in `.claude/agile/sync.json`.

## 2026-09-17 — Bootstrap session (no item)
Raised by the owner after the bootstrap, approved the same day.

| # | Lesson | Kind | Where it went |
|---|---|---|---|
| 1 | The model used by each agent and activity should be chosen on purpose, not inherited by accident | Project setting | `CLAUDE.md`, section "Models" |
| 2 | Simulae's screens drifted apart (hover on some tables only, two ways to edit an item, two icon families: 258 Filled vs 58 Outlined, 2 shared components for 71 pages). A written rule alone does not hold a UI standard | Project rule + build check | `.claude/rules/agile/ui.md`; feature F-1 "UI kit and gallery" (kit, `AppIcons`, gallery, architecture tests) |
| 3 | `plugin` — agents and skills in agile@canary declare no `model:`; the `reviewer` inherits whatever the session runs | Plugin improvement | This log (see below) |

### Plugin notes (`plugin`)
- ✅ **Model per agent and activity.** Add `model:` to `agents/reviewer.md` (strongest available) and consider it for skills. Add a "Models" table to `templates/project-claude.md` and a question to the bootstrap quiz (round 7 or a new round): which model for review, for bulk mechanical work, for searches. The project override should live in `CLAUDE.md` so the owner can change it without touching the plugin.
- ✅ **UI standards as a core rule.** Consider a `rules/core/ui.md` with the stack-neutral parts (one behavior everywhere, one icon family behind semantic names, one way to edit an item, destructive-action confirmation, list states, action vocabulary) and a quiz question in round 5 (icon family, edit pattern). Consider a "UI kit and gallery" step in the web profiles before the first screen is built.
- ✅ **Bootstrap quiz gaps found in this session.** Entitlements were reduced to "plans with features" and the owner had to raise time-bound grants and promo codes; taxonomy shape came up only after the summary. Suggestion: in round 3, when the brief mentions plans, ask about trials, time-bound grants and usage limits; add a closing question "which domain concept worries you most?" before the summary.

## 2026-09-17 — Plugin notes delivered; project brought to agile@canary 0.0.7
- The three `plugin` notes above went into agile@canary **0.0.7** (commit `97053a3` in `D:\dev\agile-canary`, with the owner's authorization): `model:` on the reviewer, core rule `ui`, quiz questions 13b, 23b–23d, 34 and the closing question.
- This project was refreshed by hand from 0.0.4 to 0.0.7 (there is no `/agile:sync` yet): `workflow.md`, templates, profile (Simulab section kept), rules `build-config` and `ui` (core). The Simulab UI choices moved from `ui.md` to `ui-project.md`.
- Build configuration from `templates/dotnet/` adopted: `.editorconfig`, `BannedSymbols.txt`, `global.json`, `.gitattributes`, analyzers in `Directory.Build.props`. `TreatWarningsAsErrors` is now **off**, as the core rule says: the gate fails on new warnings instead.
- Findings fixed, not suppressed: unused using, `Shared` renamed `SharedResources` (CA1716), a test that constructed `JsonSerializerOptions`. Two local, commented pragmas: `Error` (CA1716, Visual Basic keyword only) and `AppJson` (RS0030, the one allowed place).
- ✅ `plugin` — the template `.editorconfig` asks `_camelCase` for every private field, constants and `static readonly` included. This project added a PascalCase rule for those two in its own `.editorconfig`. Proposed for `templates/dotnet/.editorconfig` (the owner's file, not edited).

## 2026-09-17 — agile@canary 0.0.8: `/agile:sync`
- The manual refresh done twice today became the command `/agile:sync` (plugin commit `0361053`, with the owner's authorization). From now on plugin updates reach this project through it, between features.
- This project: `docs/agile/workflow.md` refreshed to 0.0.8; `.claude/agile/sync.json` records version 0.0.8 and the 30 copied files; `.claude/agile/sync-base/` keeps the plugin text of the edited profile, for a three-way merge next time.

## 2026-09-17 — F-1 UI kit and gallery
| # | Lesson | Kind | Where it went |
|---|---|---|---|
| 1 | The app host Claude opened to check the screen locked the Web DLLs; the Stop gate then failed with "build failed" | Plugin improvement | This log (see below) |
| 2 | The branch was switched outside the session (IDE) and a docs commit landed on `main` unnoticed; it was moved back with the owner's OK | Plugin improvement | This log (see below) |
| 3 | bUnit + MudBlazor tests hung the suite for 5 minutes: `await InvokeAsync` waited for a dialog result, and MudBlazor services need async disposal | Project rule + plugin improvement | `.claude/rules/agile/project.md` ("UI tests"); `KitTestContext` in `tests/Simulab.Web.Tests/Ui`; this log |

### Plugin notes (`plugin`)
- ✅ **App host left running.** `feature-build` should stop any app host Claude started (preview) before ending the turn; `gate.js` could detect MSB3027/MSB3021 "file is locked" and report "close the app host" instead of a plain build failure.
- ✅ **Commit on the wrong branch.** Skills should check `git branch --show-current` right before each commit; consider a PreToolUse hook that refuses `git commit` on the main branch outside `/agile:ship` or an explicit authorization.
- ✅ **Hanging tests.** `gate.js` (stop and ship) should run `dotnet test` with `--blame-hang-timeout` so a hung test fails in seconds with its name instead of blocking the turn.

## 2026-09-17 — Plugin notes of F-1 delivered in agile@canary 0.0.10
- The owner asked to bring every open recommendation of this log to the plugin. Done in 0.0.10 (plugin commit `b7128e5`); 0.0.9 (`8ab237e`) had added `--worktree`. The status table at the top of this file now tracks every `plugin` note.
- What changed for this project once it syncs: the gate says "build blocked" and names the process when an app host locks the DLLs; a test running for more than 120 s fails with its name; a commit on `main` is refused while `feature/F-<n>` is open and checked out nowhere (bypass: the comment `# agile:main-ok`, after the owner's yes).
- How it was implemented differs from the note in one point: the guard does not look for "outside `/agile:ship`" (a hook cannot know which skill is running). It looks at git: current branch is main, no merge in progress, an unmerged item branch that no worktree holds.
- Not verified inside a real session yet: the `PreToolUse` hook firing on the PowerShell tool, and its message reaching Claude. Tested from the command line (nine scenarios).
- This project is still at 0.0.8 in `.claude/agile/sync.json`. Run `/agile:sync` between features, after updating the installed plugin.

## 2026-09-17 — Sync from agile@canary 0.0.8 to 0.0.10
- Copied: `.claude/rules/agile/git.md` (branch check before every commit, guard hook, worktrees), `.claude/rules/agile/workflow.md` (WIP limit per checkout), `docs/agile/templates/project-claude.md` (`Worktrees:` line), `docs/agile/workflow.md` (manual 0.0.10).
- Merged: `docs/agile/profile.md` (two test lines: hang timeout, bUnit with MudBlazor), no conflicts; the Simulab section is kept.
- Left alone: `.editorconfig` (only comments differ; the PascalCase rules are the same). No build file changed, so no rebuild.
- Declined for now: the `Worktrees:` line in `CLAUDE.md` (added when parallel work is first requested).

## 2026-09-17 — F-2 App shell and navigation
Approved by the owner the same day.

| # | Lesson | Kind | Where it went |
|---|---|---|---|
| 1 | `MudNavLink` with `OnClick` renders a `div`, not a link; bUnit tests did not catch it, the screen did | Build check | `tests/Simulab.ArchitectureTests/UiKitBoundaryTests.cs` (`Razor_MudNavLink_HasNoOnClick`) |
| 2 | With `<base href="/">`, an in-page link (`#main-content`) navigates to `/` | Project rule | `.claude/rules/agile/ui-project.md` |
| 3 | `plugin` — two defects appeared only on screen (MudBlazor's active nav link style beat the drawer CSS: blue on dark blue; the link above), and the browser pane sends an empty key for Enter | Plugin improvement | This log (see below) |

- ✅ **Screen check before the coverage table.** In feature-build, open the screen through the app host before the coverage table and check the contrast of library states (active, hover, focus) in both themes; the browser pane cannot send Enter, so keyboard steps always stay in the validation script.

## 2026-09-17 — F-3 Data and messaging foundation
Approved by the owner the same day.

| # | Lesson | Kind | Where it went |
|---|---|---|---|
| 1 | A tenant query filter that captures the tenant value leaks rows between requests (Simulae TK #184); the base context captures `this` instead | Project rule | `.claude/rules/agile/project.md` (Data) |
| 2 | Three shared projects were born in this item (`Simulab.Persistence`, `Simulab.Email`, `tests/Simulab.Testing`) and the profile was updated only at the end | Project rule | `.claude/rules/agile/project.md` (Data) |
| 3 | `plugin` — `Aspire.Hosting.Testing` was missing from the refinement package list and had to be asked mid-build | Plugin improvement | This log (see below) |

- ✅ **Test packages in the refinement question.** In feature-refinement, the package question must cover the packages the tests need (host testing, containers, fakes), not only the ones the production code needs; a missing one becomes a stop mid-build.

## 2026-09-17 — Plugin notes of F-2 and F-3 delivered in agile@canary 0.0.11
- Both open notes went into the plugin (commit `a9babf4`), at the owner's request. No ⏳ row is left in the status table.
- F-2: `feature-build` has a screen check before the coverage table (the component library's own states in light and dark mode, what each element really is, in-page links), and the validation script gains one keyboard-only pass of the main task.
- F-3: the package question of `feature-refinement` lists what the tests need too (host testing, containers, fakes, UI test libraries), with license and version, so the owner's yes is given once.
- This project is at 0.0.10 in `.claude/agile/sync.json`. 0.0.11 changes no file this project copies except `docs/agile/workflow.md`; `/agile:sync` brings it, between features.

## 2026-09-18 — F-4 Sign-up and email verification
Approved by the owner the same day.

| # | Lesson | Kind | Where it went |
|---|---|---|---|
| 1 | The verification email never reached Mailpit under the app host: the MailPit connection string carries the container's own SMTP port (1025). F-3 tested `IEmailSender` only against a test container, so the wiring was first exercised when F-4 sent an email | Test + plugin | `AppHostModelTests.Api_GetsTheSmtpAddressFromTheMappedEndpoint` (seen failing without the fix) and `Api_GetsTheVerificationLinkOfTheWeb`; `plugin` note |
| 2 | An approved decision said the `AuthLayout` exception would be written in `ui-project.md`, and it was not | Project rule + plugin | `.claude/rules/agile/ui-project.md` (the missing line); `plugin` note: check decisions that name a file at ship |
| 3 | BR11 (always the same answer) and AC9 (a code for the throttled resend) contradicted each other and were caught only in build; a code that only an existing account can reach reveals who is registered. F-5 and F-7 face the same risk | Project rule | `.claude/rules/agile/project.md`, "Access and entitlements" |

## 2026-09-18 — Plugin notes of F-4 delivered in agile@canary 0.0.15
- Both notes went into the plugin (commit `b221360`): `feature-build` exercises anything wired through the app host in the item that creates it; `feature-ship` checks that every decision naming a file is true in that file before `## Delivery`. No ⏳ row is left.
- Recorded from a temporary worktree of `main` while F-12 was in progress on its own branch. This project is still at 0.0.10 in `.claude/agile/sync.json`; `/agile:sync` after F-12 brings 0.0.11–0.0.15 (workflow copies in en and pt-BR, CA1716 line, rules).

## 2026-09-18 — F-12 Group projects into folders
Approved by the owner the same day.

| # | Lesson | Kind | Where it went |
|---|---|---|---|
| 1 | Two Claude sessions shared one checkout: the other one shipped F-4, deleted `feature/F-4` and switched to `main` while F-12 was being refined, so the refinement commit landed on `main`; it later rewrote a commit of this session | Project rule | `.claude/rules/agile/project.md`, "Sessions and retro" |
| 2 | The guard hook refuses a commit on `main` only while an item is in `building`; a refinement commit of an item in `refining` went through | Plugin | `plugin` note (⏳) |
| 3 | The F-4 retro commit (`e03cb5c`) added test code with 2 `CS0618` straight to `main`, outside the gate; it surfaced in F-12 and became B-1 | Project rule + plugin | `.claude/rules/agile/project.md`, "Sessions and retro"; `plugin` note (⏳) |

## 2026-09-18 — Plugin notes of F-12 delivered in agile@canary 0.0.19
- Both notes went into the plugin (commit `c48c34e`): the guard also refuses a commit on `main` that carries an item file in refining, approved, building or validating; a retro commit carries no code or tests. The sync `apply` now records the hash of each copied file, so the partial manual refreshes of this project stop looking like edits.
- Workflow manuals refreshed to 0.0.19 in the same commit. `.claude/agile/sync.json` is still at 0.0.10: `/agile:sync` brings the profile, the CA1716 line and the rules.

## 2026-09-18 — F-5 Sign-in and sign-out
Approved by the owner the same day. All three lessons found on screen, while building the OpenIddict/Redis session code.

| # | Lesson | Kind | Where it went |
|---|---|---|---|
| 1 | `RevocationCheckMiddleware` declared `IRefreshSessionStore` as an `InvokeAsync` parameter; ASP.NET Core resolved it (opening a Redis connection) on every request, authenticated or not, even though the code only used it inside an `if` that most anonymous requests never enter | Plugin improvement | This log (see below) |
| 2 | The first sign-in through the app host hung indefinitely: `ConnectionMultiplexer.Connect(connectionString)` could not validate the TLS certificate Aspire's local Redis container uses by default | Project setting + plugin | `CLAUDE.md`, Project-specific rules; this log |
| 3 | The Web tried to decode `sub`/`email` out of the access token itself and threw, because OpenIddict's development encryption certificate makes it a JWE, not a plain JWT; Blazor Server's error boundary then froze the page mid-render with no useful message | Plugin improvement | This log (see below) |

### Plugin notes (`plugin`)
- ✅ **Middleware and optional dependencies.** A core rule or a note in the ASP.NET Core / stack profile: a custom middleware's `InvokeAsync` should resolve an optional or heavy dependency (a cache, a second data store) from `HttpContext.RequestServices` inside the branch that needs it, never as a declared parameter — the latter runs on every request regardless of the branch taken.
- ✅ **Aspire secures local resources by default.** `AddRedis()` (and likely other Aspire hosting resources) enables TLS and a password for the local container unless told otherwise. A stack note or quiz question for any profile using Aspire: consume it through the matching `Aspire.<Client>` client-integration package (`builder.Add<X>Client(...)`), which wires the certificate trust, never a plain driver connection built from the raw connection string.
- ✅ **OpenIddict encrypted tokens.** When an encryption certificate is registered (the common default-cert quickstart), OpenIddict's access token is a JWE, not a JWS: a client cannot decode its own claims out of it. A note for an OpenIddict-based auth profile: the client that needs its own `sub`/`email`/custom claims should ask the resource server (an authenticated "who am I" endpoint), not self-decode the token.

## 2026-09-18 — B-1 Replace the obsolete Aspire environment API in AppHostModelTests
Approved by the owner the same day.

| # | Lesson | Kind | Where it went |
|---|---|---|---|
| 1 | The first fix attempt was based on reflecting the `Aspire.Hosting` assembly at the version pinned for `Aspire.Hosting.Testing` (13.4.6) in `Directory.Packages.props`, and it threw at runtime: Central Package Management's transitive pinning had actually floated `Aspire.Hosting` to 13.5.4 for this test project, because `Aspire.Hosting.Redis` (added by F-5, in the same solution) needs that version. The obsolete-vs-current API differs between the two | Plugin improvement | This log (see below) |

### Plugin notes (`plugin`)
- ✅ **CPM transitive pinning floats shared packages.** A core rule or a note wherever build-config is documented: when investigating an obsolete-API warning or any version-dependent behavior, check the *resolved* version in `obj/project.assets.json` for that specific project, not the version pinned in `Directory.Packages.props` — `CentralPackageTransitivePinningEnabled` can raise a shared package's version because a sibling package elsewhere in the solution needs it, even though nothing pins that shared package directly.

## 2026-09-19 — F-6 Permissions and seed roles
Approved by the owner in the previous session. Both lessons found on screen, while validating the item.

| # | Lesson | Kind | Where it went |
|---|---|---|---|
| 1 | The account menu (`UserMenu.razor`, shipped with F-5) never opened on a real click: it wrapped a custom `MudIconButton` inside `MudMenu`'s `ActivatorContent`, which never receives the `mud-menu-icon-button-activator` class `MudMenu` wires its click handling to — only its own `Icon` parameter does. Confirmed live: `LanguageSwitch.razor` (using `Icon`, no custom activator) opened fine on the same page. Registered as B-2, fixed on the F-6 branch | Project rule | `.claude/rules/agile/ui-project.md` |
| 2 | A test needed to move the clock forward past the 10 s permission cache window; `IMemoryCache`'s expiration clock is not a swappable `TimeProvider` in this stack, so a `FakeTimeProvider` in the test host had no effect on it. Replaced with a small `TimeProvider`-backed cache class (`PermissionCache`) instead | Project rule | `.claude/rules/agile/project.md`, "Packages" |

## 2026-09-19 — Sync with agile@canary 0.0.25
From 0.0.10 to 0.0.25, run with `/agile:sync` on `main` after F-6 shipped.
- Copied (untouched copies): rules `build-config` (a new project is created in its `## Layout` folder, mirrored in the solution folders and registered in the architecture and layout tests), `definition-of-done` (`DocGen --check` when the project has generated docs), `git` (an item file that is not `done` is committed on the item branch; docs-only commits on `main` carry no code), `output-style` (interactive question cards); template `bug.md` (duplicates of a business rule under `## Cause`, one regression test per occurrence fixed); `workflow.md` and `workflow.pt-BR.md` (0.0.21 → 0.0.25).
- Merged by hand: `docs/agile/profile.md`. The plugin's layout change generalizes what this project did in F-12, so the Simulab text was kept in both conflicts; the architecture test line "Layout" was added (`SolutionLayoutTests` exists); the DocGen sentence was left out until the "Technical docs" feature adopts it.
- Proposed and declined as not needed: `.editorconfig` differs from the plugin's only in two comments (this file names Simulab and F-12). The other build files did not change upstream.
- Build: 0 warnings, 0 errors; the full suite was skipped with the owner's OK (no build file changed; `build-config` gained a prose line only).

## 2026-09-19 — B-3 The Web never refreshes the access token nor notices a revoked session
Approved by the owner the same day, after the ship.

| # | Lesson | Kind | Where it went |
|---|---|---|---|
| 1 | F-5 counted UC5 (silent refresh) as covered, yet `AuthClient.RefreshAsync` had no caller: the coverage table pointed at a test of the method, not of the path a user reaches. Found only while refining F-7 | Plugin improvement | This log (see below) |
| 2 | The Aspire ServiceDefaults template's `AddStandardResilienceHandler()` retries POST by default; a retried refresh presents a token the first attempt consumed and ends a live session (found by `/agile:review`). Fixed in this project's `ServiceDefaults` in B-3 | Plugin improvement | This log (see below) |
| 3 | In Blazor Interactive Server the auth cookie cannot be rewritten from a circuit, so rotating refresh tokens kept in the cookie die on the first in-page refresh. B-3 moved them to a Redis store keyed by an id in the cookie | Plugin improvement | This log (see below) |

### Plugin notes (`plugin`)
- ✅ **`[generic]` Coverage goes through the caller.** In the build skill's coverage check: a criterion maps to a test that exercises the path the user reaches (page, endpoint, handler that calls the code); a public method added for a criterion that nothing in production calls is reported as a gap.
- ✅ **`[stack: aspire]` No retries for unsafe methods.** The `ServiceDefaults` template calls `http.AddStandardResilienceHandler(options => options.Retry.DisableForUnsafeHttpMethods())`. The per-client alternative, `RemoveAllResilienceHandlers`, is experimental (`EXTEXP0001`).
- ✅ **`[stack: blazor-server]` Session state stays on the server.** Tokens and anything else that changes during a session live in a server-side store (Redis, data-protected) keyed by an id the cookie carries; the cookie is checked in `OnValidatePrincipal` and open circuits revalidate with `RevalidatingServerAuthenticationStateProvider`.

## 2026-09-19 — F-8 My account and preferred language
Approved by the owner the same day, after the ship.

| # | Lesson | Kind | Where it went |
|---|---|---|---|
| 1 | The owner asked for the technical terms used in reports and reviews ("major", "CSRF") to be explained in the glossary | Project rule + plugin improvement | `.claude/rules/agile/project.md`, "Talking to the owner"; `docs/glossary.md` gained `## Technical terms`; this log (see below) |
| 2 | A `Task.Yield()` added after the review made bUnit's `Click()` return before the page navigated; the test read the URL too early and failed only in the ship's full suite | Project rule + plugin improvement | `.claude/rules/agile/project.md`, "UI tests"; this log (see below) |
| 3 | One more test class exhausted the test PostgreSQL's connections (`53300: too many clients`): each disposed host kept its Npgsql pool. Fixed in `IdentityApiFactory` in F-8 | Plugin improvement | This log (see below) |

### Plugin notes (`plugin`)
- ✅ **`[generic]` Explain technical terms to the owner.** The project glossary template gets a `## Technical terms` section (term, owner-language word, meaning), and the output-style rule says a technical term used with the owner for the first time is added there in the same step.
- ✅ **`[stack: bunit]` Wait for what an async handler does.** After a click whose handler awaits (an Api call, `Task.Yield`), assert with `WaitForAssertion`: `Click()` returns when the handler first yields, not when it finishes.
- ✅ **`[stack: ef-core-npgsql]` Clear the pool of a per-class test host.** A `WebApplicationFactory` with a database per test class clears its Npgsql pool on dispose (`NpgsqlConnection.ClearPool`), or idle pooled connections exhaust the container's `max_connections`.

## 2026-09-19 — B-7 Sign-up accepts a name or email longer than the column
Approved by the owner the same day, after the ship.

| # | Lesson | Kind | Where it went |
|---|---|---|---|
| 1 | The first ship run of the gate stopped on "new warnings", and the list was lost: its output had been piped through `grep`. A second run was clean; the warning stays unknown | Project rule + plugin improvement | `.claude/rules/agile/project.md`, "Sessions and retro"; this log (see below) |
| 2 | B-7 was written by hand during the F-8 build, without the bug template: `## Validation script` and `## Delivery` were missing and found only in the build | Project rule | `.claude/rules/agile/project.md`, "Sessions and retro" |
| 3 | The 500 also told whether an account existed (registered address 202, new one 500), breaking the same-answer rule of anonymous endpoints | Project rule + plugin improvement | `.claude/rules/agile/project.md`, "Access and entitlements"; this log (see below) |

### Plugin notes (`plugin`)
- ✅ **`[generic]` Keep the gate's failure visible.** The build and ship skills say to save the gate's whole output to a file and quote from it; `gate.js` repeats the new warnings (code, file, line) in its last lines, so a truncated view still shows them.
- ✅ **`[generic]` Same answer means same checks first.** In the API contract rules: on an anonymous endpoint, every input check (format, length, column width) runs before the lookup that tells the paths apart; an unhandled error on one path (a 500) is an answer too.

## 2026-09-19 — B-4 Per-client limits are shared by the whole site
Approved by the owner the same day, after the ship. The owner said another session updates the plugin with these notes.

| # | Lesson | Kind | Where it went |
|---|---|---|---|
| 1 | The owner asked for more detail and PowerShell commands: the validation script had one Bash line with a secret to paste by hand | Project rule + plugin improvement | `.claude/rules/agile/project.md`, "Talking to the owner"; this log (see below) |
| 2 | Claude tested the validation commands against the app host the owner had open and used up the limits of four addresses in that instance | Project rule + plugin improvement | `.claude/rules/agile/project.md`, "Sessions and retro"; this log (see below) |
| 3 | A Blazor Server circuit has no HTTP request of its own: the visitor's address had to be read at the first request and carried into the circuit | Plugin improvement | This log (see below) |

### Plugin notes (`plugin`)
- ✅ **`[generic]` Terminal steps for both shells.** The build skill's validation script: a step that needs a terminal gives the command for Git Bash and for PowerShell 7 (on Windows), run by Claude before handing over, with the expected output and how to repeat it.
- ✅ **`[generic]` Hands off the owner's running app.** Claude never changes state (sign-ups, counted requests, data) in an app host it did not start; it asks first, or uses data no one else uses and says which.
- ✅ **`[stack: blazor-server]` Request data reaches the circuit through the root.** Data from the first HTTP request that a circuit needs later (visitor address, a header) is read in `App` (static render, `HttpContext` available), passed to the interactive root component as a parameter (the framework protects it in the page) and kept in a scoped service; a circuit has no `HttpContext`.

## 2026-09-19 — Sync with agile@canary 0.0.29
From 0.0.25 to 0.0.29, run with `/agile:sync` on `main` after B-7 shipped.
- Copied (untouched copies): rules `api-contracts` (a client asks the API for its claims instead of decoding the token; a middleware resolves an optional dependency inside the branch that needs it), `build-config` (read the version resolved in `obj/project.assets.json`), `output-style` (technical terms reach the owner with their pt-BR word from the glossary); templates `glossary.md` ("Technical terms" section) and `project-claude.md` (glossary line); `workflow.md` and `workflow.pt-BR.md` (0.0.25 → 0.0.29).
- Merged by hand: `docs/agile/profile.md`. One conflict, Simulab text kept (F-12 note, build files line); added the Aspire client integration, `ServiceDefaults` without retries for unsafe methods, session state on the server with Interactive Server, the Npgsql pool of a per-class test host and bUnit `WaitForAssertion`. The DocGen sentence stays out until the "Technical docs" feature.
- Proposed and applied: four rows in `docs/glossary.md` "Technical terms" from the plugin template (coverage gap, validation script, gate, warnings baseline); the glossary line of `CLAUDE.md` names the technical terms.
- Build files did not change upstream. Build: 0 warnings, 0 errors; the full suite was skipped with the owner's OK (the rules gained prose lines only).

## 2026-09-19 — Sync with agile@canary 0.0.31
From 0.0.29 to 0.0.31. Approved by the owner.
- Copied: `.claude/rules/agile/api-contracts.md` (anonymous endpoints run every input check before the lookup, from B-7), `docs/agile/workflow.md` and `docs/agile/workflow.pt-BR.md` (gate verdict on the last line and whole output saved, from B-7; terminal steps for Git Bash and PowerShell 7 and hands off the owner's app host, from B-4).
- Merged by hand (`git merge-file`, no conflict): `docs/agile/profile.md` gained the Blazor Interactive Server line on request data reaching the circuit through the root component (from B-4); the Simulab sections are kept.
- Left alone: the six build files (no upstream change).
- Noted, not changed: the anonymous-endpoint rule now lives in `api-contracts.md` and also in `project.md` (B-7); a later retro may drop the project copy.
- No build file or build-checked rule changed, so the suite was not rerun.

## 2026-09-19 — B-5 The sign-in password field asks for a new password
Approved by the owner the same day, after the ship.

| # | Lesson | Kind | Where it went |
|---|---|---|---|
| 1 | The bug came from a silent default in a kit component: the sign-in page said nothing and inherited `new-password` | Project rule + plugin improvement | `.claude/rules/agile/ui-project.md` (commit `cf97ec3`); this log (see below) |

### Plugin notes (`plugin`)
- ✅ **`[generic]` No silent defaults for meaning.** In the core `ui` rule: a kit parameter whose right value depends on what the page means (autocomplete, input purpose, a destructive action's wording) has no default; it is required (in Blazor `[EditorRequired]`, whose `RZ2012` warning the gate refuses), so a page that forgets it fails the build.

## 2026-09-19 — B-6 Links fail the AA contrast (auth footer and three more)
Recorded in this log at the owner's request (2026-09-19); no project rule was added.

| # | Lesson | Kind | Where it went |
|---|---|---|---|
| 1 | A C# test written through a Python script had its `\b` turned into a backspace character: the architecture test matched nothing and passed. Caught only because a regression test must be seen failing before the fix | Plugin improvement | This log (see below) |
| 2 | The bug said "dark theme only"; computing the ratio from the theme found the footer failing in both themes and the same library link failing on three more screens | Plugin improvement | This log (see below) |

### Plugin notes (`plugin`)
- ✅ **`[generic]` Write code with the file tools.** Source and test files are written with the Write and Edit tools, never through a script's string literals (a heredoc'd Python or shell string): escapes such as `\b` or `\t` become control characters, and a test can compile and pass without matching anything.
- ✅ **`[generic]` Contrast is checked everywhere the component sits.** In the `ui` rule: a colour or contrast bug is checked in both themes and against every surface the same component sits on (card, page background, app bar), with the ratio first computed from the theme tokens and then measured on screen; a premise like "dark theme only" is verified, not assumed.

## 2026-09-19 — Sync with agile@canary 0.0.33
From 0.0.31 to 0.0.33. Approved by the owner.
- Copied: `.claude/rules/agile/ui.md` (kit parameters whose value depends on the page's meaning are `[EditorRequired]`, from B-5; contrast checked in both themes and on every surface, from B-6), `.claude/rules/agile/workflow.md` (files written with Write/Edit, never through a script's string literals, from B-6), `docs/agile/workflow.md` and `docs/agile/workflow.pt-BR.md` (the same rules and the `/agile:version` section).
- Merged by hand: nothing. `docs/agile/profile.md` stays edited with no upstream change.
- Left alone: the six build files and `tests/Directory.Build.props` (no upstream change).
- `ui.md` now has a build check (`RZ2012`), so the build and the full suite ran: build 0 warnings, 0 errors, 0 `RZ2012`; 433 tests passed, 0 failed; warnings baseline unchanged (0 entries), gate GREEN.

## 2026-09-19 — F-9 Role management back office
Recorded in this log at the owner's request (2026-09-19); no project rule and no `CLAUDE.md` line were added.

| # | Lesson | Kind | Where it went |
|---|---|---|---|
| 1 | Two bugs reached the tests because Razor compiles them: `Value="_name"` on a string parameter is the literal text `_name` (the `@` was missing), and the kit's generic parameter is `TValue`, not `T` — the wrong name only fails when the component renders. Both pages and both dialogs were covered by bUnit tests, which is why they were caught before validation | Plugin improvement | This log (see below); the one-line project rule was offered and not added |
| 2 | BR9 promised "the menu follows within 15 minutes", taken from F-6's file. B-3 had already changed it: the Web re-reads the permissions on every page load. The premise came from an earlier item's file, not from today's code, and would have told the Admin something untrue on screen | Plugin improvement | This log (see below) |
| 3 | The independent review (`/agile:review`) found the users list keeping its current page when the role filter changed — a bug no test covered | Nothing | The rule that triggers a review on an authorization change already exists; this was it working |

### Plugin notes (`plugin`)
- ✅ **`[stack: blazor]` A Razor attribute mistake compiles.** On a component parameter, a string attribute written without `@` is literal text (`Value="_name"` sets the two words), and a generic type parameter under the wrong name (`T` instead of the component's `TValue`) fails only when the component renders. Neither is a build error, so every new page and dialog gets a bUnit test that renders it with its real parameters.
- ✅ **`[generic]` A premise about another item is checked against today's code.** When refinement states how something already behaves, quoting the file of an earlier item is not enough: a later bug or feature may have moved it (F-9 inherited "within 15 minutes" from F-6 after B-3 had made it "at the next page load"). Verify it in the code, and look at the items that touched that code since.

## 2026-09-20 — F-10 Account erasure

| # | Lesson | Kind | Where it went |
|---|---|---|---|
| 1 | BR11 said the erasure is refused when no active manager is left. As written it also refused an ordinary account in a system that had no manager at all, so every erasure failed on a fresh database. The rule had to count managers before the change as well | Plugin improvement | This log (see below) |
| 2 | The dark palette had kept the light error red. No screen had used it as text on a dark card until the danger zone, so 2.93:1 went through F-4 to F-9 unseen; the light `ErrorContrastText` was worse (1.92:1) on a filled destructive button | Test or build check + project rule | `ThemeContrastTests` in the repository, one line in `project.md`, and this log |
| 3 | The browser pane recreates the Blazor circuit between tool calls, so a dialog opened in one call is gone in the next. Several rounds were spent clicking a dialog that no longer existed; driving the whole flow inside one call worked | Project rule | One line in `project.md`, and this log |

### Plugin notes (`plugin`)
- ✅ **`[generic]` A "never leave zero X" rule is judged before and after.** Written as "none after", it also blocks the case where the hole already existed (F-10: erasing an ordinary account in a system with no manager at all). Write it, and build it, as "there was at least one before and none after".
- ✅ **`[generic]` A theme's colour tokens get a contrast test over the palette.** A screen check finds a bad ratio once, on the surfaces that screen happens to use; a test over the theme keeps every token honest and catches the pair that no screen has rendered yet (F-10 found the dark error red at 2.93:1 after six features).
- ✅ **`[stack: Blazor Interactive Server]` A dialog is driven from the browser pane in one call.** The pane recreates the circuit between tool calls, so refs go stale and an open dialog disappears; open, fill and confirm in a single call, and keep keyboard steps in the validation script.

## 2026-09-20 — F-13 Identity emails through the job queue

| # | Lesson | Kind | Where it went |
|---|---|---|---|
| 1 | BR2 asked for the job and the token in one transaction. Two `DbContext`s over one PostgreSQL connection is not available with Npgsql; mapping the shared table into the caller's own context (`ExcludeFromMigrations()`, the owning context keeps the migration) made one `SaveChanges` do both | Project setting | One line in `docs/agile/profile.md`, and this log |
| 2 | Moving the emails out of the request emptied three tests without failing them: "the mail server fails, the answer is the same" no longer touched the mail server at all, so it proved nothing. Found by `/agile:review`, not by the suite | Project rule | One line in `project.md` (Integration), and this log |
| 3 | The `Remove` of a finished job sat inside the handler's `try`. A delete that fails there leaves the entry `Deleted`, so the error-handling save repeats the DELETE instead of writing `Status` and `LastError`, and the exception escapes the whole poll. Reachable through the at-least-once path (BR10) | Project rule | One line in `project.md` (Integration), and this log; moved to `profile.md` by the 0.0.36 sync (2026-09-21) |

### Plugin notes (`plugin`)
- ✅ **`[profile: modular-monolith]` An outbox row is staged on the caller's unit of work.** A row a request must not lose (a job, an outbox message) is written by the same `SaveChanges` as the data that justifies it: the shared table is mapped into the caller's own `DbContext` with `ExcludeFromMigrations()`, while the context that owns the table keeps the migration. Sharing one connection between two contexts is neither needed nor available with Npgsql.
- ✅ **`[generic]` An effect that leaves the request empties the tests that asserted it.** When an email, an event or long work moves to a queue, every test that asserted the effect is re-read, not only made to compile: an assertion that "nothing was sent" passes for free once nothing is sent synchronously. F-13 shipped three such tests past a green suite; the independent review caught them.
- ✅ **`[stack: ef-core]` A failed `SaveChanges` keeps the entry state you set.** After `Remove`, the entry stays `Deleted`: a later save on the same entity repeats the DELETE instead of writing the fields the error handler just set, and throws again. The delete that closes a unit of work goes outside the `try` that handles that unit of work's own failure.

## 2026-09-20 — Sync agile@canary 0.0.33 -> 0.0.35
- Copied: `.claude/rules/agile/ui.md` (a test over the theme tokens checks the contrast of every foreground/background pair, in both themes), `docs/agile/workflow.md` and `docs/agile/workflow.pt-BR.md` (version 0.0.35: refinement checks every premise against today's code; a rule that protects a minimum is written as a before-and-after; the stack lessons and the `ui` section carry the three notes this project sent back from F-9 and F-10).
- Merged by hand: `docs/agile/profile.md` — one upstream line added to the test strategy (a Razor attribute mistake compiles; every new page and dialog gets a bUnit test that renders it). The Simulab sections were kept untouched.
- Left alone: the six build files and `tests/Directory.Build.props` (no upstream change). Templates and the other rules were already identical.
- `ui.md` has a build check, so the build and the full suite ran: build 0 warnings, 0 errors; 514 tests passed, 0 failed, 0 skipped (slowest project 24 s); warnings baseline unchanged (0 entries), gate GREEN.
- Open: the new `ui.md` line asks for a contrast test over the theme tokens covering every pair. `ThemeContrastTests` (F-10) covers the pairs the screens use, not the whole palette. Registered as F-17 (board 730).

## 2026-09-21 — F-11 Two-factor sign-in with TOTP

| # | Lesson | Kind | Where it went |
|---|---|---|---|
| 1 | The approved file said ASP.NET Identity stores recovery codes hashed; it stores them in plain text (`UserStoreBase.ReplaceCodesAsync`). Found only at build start, and it became change note v3 | Plugin improvement | This log |
| 2 | The password step cleared the failure count, so with a second step whoever had the password could try unlimited codes and the lockout (AC9) could never trigger. Found during the build, not by a test written from the file | Project rule + plugin improvement | One line in `project.md` (Access and entitlements), and this log |
| 3 | The approved package versions (Otp.NET 1.4.0, QRCoder 1.6.0) were behind the latest stable (1.4.1, 1.8.0) | Plugin improvement | This log |

### Plugin notes (`plugin`)
- ✅ **`[generic]` Verify a library's storage premise in refinement.** A premise about how a library stores or protects data (hashing, encryption, what it writes where) is verified in the library's source or docs during refinement, never taken from memory. F-11 approved "hashed, as Identity does" and the build found plain text.
- ✅ **`[stack: aspnet-identity]` Multi-step sign-in and the lockout.** In a sign-in with more than one step, only the last step clears the failure count (`ResetAccessFailedCountAsync`); a step that clears it gives unlimited tries to the next one.
- ✅ **`[generic]` Package versions are checked at refinement time.** The refinement package list is checked against the registry at that moment; the build re-checks it before adding the package.

## 2026-09-21 — Sync agile@canary 0.0.35 -> 0.0.36
- Copied: `docs/agile/workflow.md` and `docs/agile/workflow.pt-BR.md` (version 0.0.36: refinement verifies a premise about how a library stores or protects data in its source or docs, and checks package versions against the registry; the coverage table re-reads every test of an effect that leaves the request; the stack lessons carry the EF Core, ASP.NET Identity and outbox notes this project sent back from F-11 and F-13).
- Merged by hand: `docs/agile/profile.md` — two upstream lines added to the test strategy (a failed `SaveChanges` keeps the entry `Deleted`, so the delete that closes a unit of work goes outside the `try`; only the last step of a multi-step sign-in clears the failure count). One conflict: the outbox line written by the F-13 retro and the same lesson from upstream; the upstream wording was kept, so the next merge has nothing to reconcile. The Simulab sections were kept untouched.
- Left alone: the six build files and `tests/Directory.Build.props` (no upstream change).
- No build file and no rule with a build check changed: no build or suite run was needed.
- Decided with the owner: the rule "the delete that closes a unit of work goes outside the `try`" now lives in `profile.md`, so its copy in `project.md` (F-13 retro) is removed in a separate retro commit.

## 2026-09-21 — B-8 Outlined primary buttons in the kit may fail AA contrast

| # | Lesson | Kind | Where it went |
|---|---|---|---|
| 1 | The same primary blue had been patched screen by screen four times (F-7, B-6, F-9, F-11) before anyone measured it in the palette; the palette fix plus two theory tests closed all six occurrences at once | Project rule | One line in `project.md` (UI tests) |
| 2 | Right after the theme switch the filled buttons measured 2.88:1; the value was read during MudBlazor's 0.25 s background-color transition. Parked, they read 5.74:1 | Project rule | One line in `project.md` (UI tests) |
| 3 | The browser pane failed two screenshots while the app window was behind another window; the computed-style measurements were the real evidence anyway | Nothing | One-off |

### Plugin notes (`plugin`)
- None. Lesson 1 is a Simulab design choice and lesson 2 is specific to MudBlazor.

## 2026-09-21 — B-9 Info colour and email buttons still use the old primary blue

| # | Lesson | Kind | Where it went |
|---|---|---|---|
| 1 | Three items in a row (F-10 error, B-8 primary, B-9 info/success/warning) fixed the same contrast defect one token at a time; success read at 2.24:1 on every account and sign-in page while two palette fixes shipped | Project rule | One line in `project.md` (UI tests) |
| 2 | An alert with `rgba(..., 0.06)` behind its text measures 1.26:1 read straight from the CSS; composited over the first opaque ancestor it is 4.12:1 — a real failure, but not the one the raw number showed | Project rule | One line in `project.md` (UI tests) |
| 3 | A bare `MudAlert` outside the kit had slipped into three pages; `<MudAlert` joined the tokens `UiKitBoundaryTests` forbids, so the build holds the rule instead of a written line | Test or build check | `tests/Simulab.ArchitectureTests/UiKitBoundaryTests.cs` (shipped with B-9) |

### Plugin notes (`plugin`)
- None. All three are MudBlazor or Simulab specific.

## 2026-09-21 — F-14 Role change audit trail

| # | Lesson | Kind | Where it went |
|---|---|---|---|
| 1 | Incremental builds reported 0 warnings while a CS1573 sat in the new domain entity; only the app host's fresh build showed it | Plugin improvement | Note below, row ⏳ in "Plugin notes — status" |
| 2 | The missing-key test listed error-code prefixes by hand, so `role_change.period_invalid` had no text in any language until the review caught it | Test or build check | New item F-22 (every error code has a text, by reflection) |
| 3 | `.app-nav-tooltip { display: block }` lost to MudBlazor's later `.mud-tooltip-inline`, so short menu items sat side by side since F-2 (found in validation) | Project rule | One line in `project.md` (UI tests) |

### Plugin notes (`plugin`)
- `[stack: .NET]` feature-build step 14: a warning count is quoted only from the gate or a `--no-incremental` build; an incremental build skips unchanged projects and hides their warnings (F-14).

## 2026-09-21 — B-10 Icon buttons below 3:1 contrast

| # | Lesson | Kind | Where it went |
|---|---|---|---|
| 1 | Board 737 was set to `Active` at approval and `Resolved` at validation; `references/board.md` maps them to `New` and `Active`, and it was not read before the change | Nothing | One-off; the reference already holds the mapping |
| 2 | After `git stash` / `stash pop`, `dotnet test --no-build` ran the binaries built without the change: 324 tests instead of 328 | Plugin improvement | Note below, row ⏳ in "Plugin notes — status" |
| 3 | `gate.js stop` run by hand printed nothing when no file was marked, though the skill says its last line is always GREEN or RED; a silent gate proves nothing | Plugin improvement | Note below, row ⏳ in "Plugin notes — status" |

### Plugin notes (`plugin`)
- `[stack: .NET]` feature-build step 10: after `git stash` / `stash pop` (or any branch switch), rebuild before running tests; `--no-build` runs the binaries of the other tree and the counts lie (B-10: 324 of 328).
- `[generic]` gate.js: every mode ends with a verdict line; `stop` with nothing marked prints `agile gate SKIPPED: nothing marked` instead of exiting silently (B-10).

## 2026-09-21 — Sync with agile@canary 0.0.37
From 0.0.36 to 0.0.37, run with `/agile:sync` on `main` after F-15 shipped.
- Copied (untouched copies): `workflow.md` and `workflow.pt-BR.md` (0.0.36 → 0.0.37: the gate's `SKIPPED` verdict and "Honest counts" in section 9).
- Left alone: `docs/agile/profile.md` (edited here, unchanged upstream since the last sync). Build files did not change upstream.
- No build: no build file and no rule changed. The rest of 0.0.37 (`gate.js`, the `feature-build` and `feature-ship` steps) comes from the plugin itself.

## 2026-09-21 — F-15 Generated technical docs

| # | Lesson | Kind | Where it went |
|---|---|---|---|
| 1 | The DocGen template built the EF model with plain `UseNpgsql` options, so the data dictionary came out in PascalCase while the database is snake_case; it also kept `Module` in the module name, referenced `Relational` without a central version, carried a `NoWarn`, searched `bin/`/`obj/` for the OpenAPI document and showed an empty Auth column | Plugin improvement | Note below, row ⏳ in "Plugin notes — status" |
| 2 | `Microsoft.Extensions.ApiDescription.Server` starts `Program` at build and failed on the missing `simulab` connection string; change note v2 moved the document to the `/openapi/v1.json` test | Plugin improvement | Note below, row ⏳ in "Plugin notes — status" |
| 3 | Refinement concluded "no entity uses `NULLS NOT DISTINCT`" from the callers of `TenantIndexBuilderExtensions`; `users` calls `AreNullsDistinct` directly (AC3 corrected in v2) | Plugin improvement | Note below, row ⏳ in "Plugin notes — status" |

### Plugin notes (`plugin`)
- `[stack: ef-core]` DocGen template: build each model through the context's `IDesignTimeDbContextFactory` when there is one (conventions such as snake_case live in the options); strip a trailing `Module` from the module name; drop the explicit `Microsoft.EntityFrameworkCore.Relational` reference (no central version, it comes through Npgsql) and the `NoWarn` (it conflicts with rule `build-config`; use `CultureInfo.InvariantCulture`); never read the OpenAPI document from `bin/`/`obj/`; show the Auth column only when the document declares security; a file the generator no longer produces counts as stale (F-15).
- `[profile: modular-monolith]` The route map's OpenAPI document is written by the existing `/openapi/v1.json` integration test (`WebApplicationFactory`) into `docs/api/`, not by `Microsoft.Extensions.ApiDescription.Server`: build-time generation starts `Program` and fails when it requires connection strings (F-15).
- `[generic]` feature-refinement step 4: a premise that nothing uses a feature is verified by its effect (the built model, the migration snapshot, the generated output), not by the callers of one helper: code can reach the same effect without it (F-15).

## 2026-09-21 — Sync with agile@canary 0.0.38
From 0.0.37 to 0.0.38, run with `/agile:sync` on `main`. It brings the three F-15 plugin notes.
- Copied (untouched copies): rule `api-contracts` (the `/openapi/v1.json` test writes the document to `docs/api/` when DocGen is used; never generated at build), `workflow.md` and `workflow.pt-BR.md` (DocGen through the design-time factory and the OpenAPI document from the test; a "nothing uses X" premise is checked by its effect).
- Left alone: `docs/agile/profile.md` (edited here, unchanged upstream). Build files unchanged upstream.
- Kept on purpose: the rule names `docs/api/openapi.json`; this project writes `docs/api/Simulab.Api.json`, which `tools/Simulab.DocGen` accepts as well.
- No build: no build file and no rule with a build check changed.

## 2026-09-21 — B-11 Flaky overflow menu test

| # | Lesson | Kind | Where it went |
|---|---|---|---|
| 1 | The F-8 rule (assert with `WaitForAssertion` after a click whose handler awaits) left two older tests with the same pattern; one was flaky for weeks | Plugin improvement | Note below, row ⏳ in "Plugin notes — status" |
| 2 | The fault showed in about 35% of runs, so one green gate proved nothing; a clean-build loop reproduced it (2 in 3, 3 in 8) and proved the fix (10 in 10) | Plugin improvement | Note below, row ⏳ in "Plugin notes — status" |

### Plugin notes (`plugin`)
- `[generic]` retro-lessons: a new rule about a test pattern comes with a sweep of the existing tests for that pattern in the same retro; each hit is fixed in the item or captured as a bug (B-11: the F-8 rule left two older tests flaky).
- `[generic]` feature-build, bugs: a flaky test is reproduced with a loop (clean build before each run), and its fix is proven by the same loop, N green runs in a row, with the real counts in the bug file (B-11: 2 in 3 and 3 in 8 before, 10 in 10 after).

## 2026-09-22 — F-16 Download my data

| # | Lesson | Kind | Where it went |
|---|---|---|---|
| 1 | The new bUnit test for the snackbar asserted on the line after the action and failed, the same defect B-11 had just fixed in older tests | Plugin improvement | Note below, row ⏳ in "Plugin notes — status" |
| 2 | The signed-in screen could not be checked through the app host: Claude's rules forbid creating accounts and typing passwords. The route, the 401, the sign-in redirect and the served script were checked; the rest went to the validation script | Plugin improvement | Note below, row ⏳ in "Plugin notes — status" |
| 3 | Refinement wrote 400 for a wrong password; the rule `api-contracts` and the erasure say 422 (change note v2) | Nothing | One-off; the rule already holds the answer |

### Plugin notes (`plugin`)
- `[stack: blazor]` feature-build: a bUnit assertion about anything that follows a click (snackbar, dialog closing, JS interop) uses `WaitForAssertion`, including in tests written in the same session where that rule was applied to older tests (F-16, right after B-11).
- `[generic]` feature-build step 12: a screen behind sign-in cannot be checked by Claude when its rules forbid entering credentials. Say so in the report, cover the signed-in flow in the validation script, and check through the app host what needs no account (the route in the OpenAPI document, the 401, the redirect to sign-in, the served static asset) (F-16).

## 2026-09-22 — B-12 DocGen lists JSON-owned types as extra tables
Approved by the owner the same day, after the ship.

| # | Lesson | Kind | Where it went |
|---|---|---|---|
| 1 | F-25 (from a refinement answer) and F-26 (from a request during validation) were written by hand instead of through `/agile:idea`, against the B-7 line in `project.md`: no template comment block, and a board title with the id prefix that had to be fixed | Plugin improvement | Note below, row ⏳ in "Plugin notes — status"; the project rule already covers it |
| 2 | `gate.js stop` run by hand after the fix said `agile gate SKIPPED: nothing marked` although the files had been edited with Edit; why nothing was marked was not verified. A `--no-incremental` build of the affected project and its tests stood in for it | Plugin improvement | Note below, row ⏳ in "Plugin notes — status" |
| 3 | The F-15 test checked that each table appears in the dictionary, not that it appears once, so three `role_changes` sections passed | Plugin improvement | Note below, row ⏳ in "Plugin notes — status" |

### Plugin notes (`plugin`)
- `[generic]` feature-refinement and feature-build: when an owner's answer or a request during build or validation becomes an idea, the skill calls `idea-capture` (template, board mirror, next number) instead of writing the file by hand (B-12: F-25 and F-26).
- `[generic]` gate.js `stop`: with nothing marked, the `SKIPPED` line also names what to run instead (the changed projects since the main branch, built `--no-incremental`, and their test projects), so a manual check is not left to guesswork (B-12; follows the B-10 note).
- `[stack: DocGen]` DocGen template tests: a test of generated output counts the occurrences of each section or box (exactly one per table), not only its presence (B-12: three `role_changes` sections passed the F-15 test).

## 2026-09-22 — Sync with agile@canary 0.0.39
From 0.0.38 to 0.0.39, run with `/agile:sync` on `main`.
- Copied (untouched copies): `workflow.md` and `workflow.pt-BR.md` (0.0.38 → 0.0.39: `gate.js stop` run by hand now takes the changed files from git, a screen behind sign-in, flaky tests proven by a loop, generated output counted, ideas through `idea-capture`, and the test sweep in the retro).
- Merged: `docs/agile/profile.md`, no conflict — the bUnit line now covers anything that follows a click and the tests written today.
- Build files did not change upstream. No build: no build file and no rule changed; the rest of 0.0.39 (`gate.js` and the skill steps) comes from the plugin itself.

## 2026-09-22 — Sync with agile@canary 0.0.40
From 0.0.39 to 0.0.40, run with `/agile:sync` on `main`.
- Copied (untouched copies): `workflow.md` and `workflow.pt-BR.md` (0.0.39 → 0.0.40: the sync names what only `/agile:bootstrap` installs and the project lacks, and offers to capture a feature for it).
- Left alone: `docs/agile/profile.md` (edited here, unchanged upstream). Build files did not change upstream. `missingCapabilities` empty. No build: no build file and no rule changed.

## 2026-09-22 — B-13 Identity tables missing user foreign keys
Two lessons approved by the owner at the B-13 retro, recorded now that this file is free of another session's changes.
- `plugin` `[generic]` feature-refinement step 11: never switch a checkout that is on another item's branch or shared with another session; create the branch with `git branch <item> main` and commit the item file from a worktree, or ask (B-13: an F-25 merge landed on `bug/B-13`).
- `plugin` `[generic]` feature-ship steps 9-10: before merging, check `git branch --show-current` is the main branch, and list `git log <main>..<branch>`; stop when a commit carries another item's id (B-13).

## 2026-09-22 — F-26 Readable entity diagrams
Shipped in `814f79e`. Built in a worktree; the format changed during validation (change note v2: Mermaid with ELK → DBML).
- `plugin` `[generic]` feature-refinement: a feature whose output is visual (a diagram, a generated page) is prototyped and rendered at real size in the target viewer before approval, with its size measured against the viewer's width (F-26: the approved Mermaid ELK diagram was 6016 px wide in a fixed-width Markdown preview, and the format changed to DBML during validation).
- `plugin` `[stack: DocGen]` Entity diagrams as a DBML schema per module (`<Module>/schema.dbml`: short types, not null, keys, standard columns counted in the table note, one `Ref` per foreign key, a `TableGroup` per set of linked tables, indexes over shown columns), linked in the index with the dbdiagram VS Code extension, in place of the Mermaid `entities.md` (F-26).

## 2026-09-22 — Sync with agile@canary 0.0.41
From 0.0.40 to 0.0.41, run with `/agile:sync` on `main`.
- Copied (untouched copies): `workflow.md` and `workflow.pt-BR.md` (0.0.40 → 0.0.41: entity diagrams as DBML by default with Mermaid as an option, a visual output prototyped at real size before approval, a checkout on another item's branch never switched, and the ship checking the current branch and `main..branch` before merging).
- Left alone: `docs/agile/profile.md` (edited here, unchanged upstream since the last sync). Build files did not change upstream.
- The plugin reported no missing capability: this project already has DocGen (F-15). Its `tools/Simulab.DocGen` is the project's own; the plugin template is never copied over it.
- No build: no build file and no rule changed.

## 2026-09-23 — F-17 Theme palette contrast test
Shipped in `92ac5fe`. Built in a worktree; 16 palette pairs fixed, every theme colour has a declared role.
- Project rule: `.claude/rules/agile/project.md`, the line on reading a rendered colour after the theme transition now adds that with the browser pane hidden transitions never finish, so `* { transition: none !important }` is injected before measuring (F-17: the field border read the previous theme's colour for over 2 s).
- ✅ `plugin` `[generic]` worktrees.md, Work: `preview_start` reads only the main checkout's `.claude/launch.json`; to preview an app host from a worktree, add a temporary configuration with the absolute `--project` path there and restore the file right after, saying so in the report (F-17).
- ✅ `plugin` `[stack: .NET]` worktrees.md, Ship steps 3-4: before `git worktree remove`, run `dotnet build-server shutdown` and ask the owner to close Visual Studio; on Windows the removal failed halfway (`Invalid argument`) and left a folder without `.git`, held by `.vs` (F-17).

## 2026-09-23 — F-18 Job claim index for stale rows
Shipped in `2ce7ec8`. Built in a worktree; change note v2 after the premise was measured false during build.
- ✅ `plugin` `[generic]` feature-refinement: a premise about query performance is measured before approval, with a throwaway `EXPLAIN` on the test container over a realistic row count; "not measured" does not go to approval (F-18: the review's "PostgreSQL cannot use the index for the OR" reached the build, where the old index turned out to use a BitmapOr with no sequential scan).
- Project rule: `.claude/rules/agile/project.md` (Data): a change to the job table's model adds the Jobs migration and an empty migration in every module context that maps it (`AddJobQueue`), or EF reports pending model changes at startup (F-18: `JobsActiveIndexSnapshot` in Identity).
- Item F-28 (board 747): the DocGen data dictionary shows a partial index without its filter; `plugin` `[stack: DocGen]` note for the plugin's DocGen template too (F-18: `ix_jobs_active_created_at on created_at`, no `WHERE status IN (0, 1)`).

## 2026-09-23 — F-19 Quiet the first-migration log
Shipped in `b7c7863`. Built in a worktree; no change note.
- Project rule: `.claude/rules/agile/project.md` (Sessions and retro): before `git worktree remove`, `dotnet build-server shutdown` and the owner confirms Visual Studio is closed on that folder (F-19: the removal failed halfway with `Invalid argument` and left the folder held by `.vs`, the second time after F-17).
- ✅ `plugin` `[generic]` feature-refinement: when reading a library's source does not pin a premise about its behavior, reproduce it in a scratch project outside the repository against a test container, with logging on, before asking the owner; read the source as the raw file, not a summary (F-19: the EF and Npgsql source did not show which command failed on an empty database; a scratch console showed the exact `SELECT` in minutes).

## 2026-09-23 — F-20 Google sign-in
Shipped in `644f24b`. Built in a worktree; change notes v2 (build start) and v3 (after the independent review).
- ✅ `plugin` `[generic]` feature-refinement: an item that touches authentication or account linking gets the independent review (`change-review`) on the refined item file before approval, not only on the code (F-20: the lockout/two-factor interplay and the rule for when Google's `email_verified` may link by email reached the build and became change notes v2 and v3).
- ✅ `plugin` `[stack: Blazor Server]` profile: a page that reads a single-use ticket from its query peeks it in `OnInitialized` (prerender runs it twice) and spends it only when the action succeeds; a ticket carried in a URL is also bound to the browser with an HttpOnly cookie (F-20: the code-step and confirmation tickets; review finding 1).
- Nothing: Visual Studio recreated the merged `feature/F-19` and put it in the F-20 worktree; `git.md` already warns that an IDE can switch the branch behind the session, and the branch check before each commit held.

## 2026-09-23 — F-21 Account event audit trail
Shipped in `3ee01a5`. Built in a worktree; no change notes (the file held from approval to ship).
- Item: `/connect/token` has no per-client rate limit — only the per-account lockout — so spraying accounts or insisting on unknown addresses is not slowed down, and since this item each attempt also writes a trail row. Captured as F-38 (AB#757), not as a rule: it needs code.
- ✅ `plugin` `[generic]` feature-ship: the confirmation that the IDE is closed on the worktree is a blocking question **before** `git worktree remove`, not a recommendation. Here the removal ran with Visual Studio holding `.vs/Simulab.slnx/solutionOpened`, failed halfway, and left the worktree unregistered in git with its folder still on disk — a state neither "removed" nor "usable". The project rule already said to ask (F-17, F-19); the skill lets the step pass without it.
- Nothing: a two-factor action right after a code sign-in needs the next step's code in tests (`Clock.Advance(Step)`). It cost one red test, and F-11 BR4 already documents that a step is spent once.

## 2026-09-23 — F-22 Every error code has a text
Shipped in `e1df7eb`. Built in a worktree; no change notes. The check passed over the real solution on its first run: 55 codes, 53 with texts, 2 exempt with a reason.
- Project rule (`project.md`, UI tests): an item whose product is a guard is seen failing on the mistake it catches before it ships, and that step goes into the validation script. Here the guard was broken three ways — a text removed, a code with no text, a text left behind by a rename — and only the third of those proved the failure message names the file to edit and the exempt list.
- ✅ `plugin` `[generic]` feature-build: before copying an existing pattern, look for an open item that exists to remove it. F-21 added `AccountEventResourcesTests` with a code prefix typed by hand while F-22 — the item that exists to delete exactly that — sat one position ahead in the backlog; two hours later this item removed it again.
- Nothing: the two codes with no text (`identity.forbidden`, `mfa_required`) are not gaps, so the item shipped with an exempt list of two rather than two texts nobody would read.

## 2026-09-24 — B-14 A concurrent duplicate organizer answers 409 instead of 500
Shipped in `151fcda`. Built in a worktree; no change notes. The regression test was seen failing first (5 red, `DbUpdateException` 23505), then green; full suite 1011 tests, 52 s.
- Project rule (`project.md`, Sessions and retro): every item lives in its own worktree and the main checkout stays on `main`. The checkout was found on `bug/B-14` behind the session (the branch then could not be used by a worktree until it went back to `main`), the second time after B-13.
- ✅ `plugin` `[generic]` feature-refinement: a bug whose cause is found in code that exists only on an unmerged branch says so in `## Cause`, and the build waits for that merge (here F-33 merged while the item was being refined).
- Nothing: forcing a race with a store wrapper that always answers "free" made the database the only arbiter and the test deterministic; a one-off pattern.

## 2026-09-24 — B-15 Sorting organizers by kind uses the English name, not the label on screen
Shipped in `fe1ac6c`. Built in a worktree; no change notes. The gate passed on its third run: the first was blocked by a leftover app host, the second by one red `AppRowActionsTests` test (green in isolation five times, then in the suite; the B-11 flaky, not the item). Full suite 1024 tests, 46 s.
- ✅ `plugin` `[generic]` feature-ship: the Api host tests regenerated `docs/api/Simulab.Api.json` during `gate.js ship`, and the change stayed uncommitted in the worktree until `git worktree remove` refused it; the OpenAPI document reached `main` in a separate commit (`5951aad`) after the merge. Step 4 of the skill should check the worktree for tracked files changed by the run and commit them before the merge is requested.
- Project rule (`project.md`, Sessions and retro): after stopping an app host started by hand, confirm with `netstat` that its ports are free; a `Simulab.AppHost.exe` outlived the process tree that was killed and locked the build output of the ship gate.
- Nothing: `preview_start` runs the app host from the main checkout, not from the worktree; here it was noticed from the log line "Application host directory" and the check was redone from the worktree. One-off, and the log line gives it away.

## 2026-09-24 — F-23 C4 architecture overview
Shipped in `1cacd3d` under `/agile:autopilot --worktree`, two stops, nothing assumed. One false premise found in refinement: the page could not sit inside `docs/architecture/`. Full suite 1033 tests, 46 s; build 16 s, 0 warnings.
- ⏳ `plugin` `[stack: DocGen]` feature-ship step 7b points a hand-written overview at `docs/architecture/overview.md`, a folder where DocGen deletes every file it does not generate; the overview belongs outside it (here `docs/architecture-overview.md`, with a drift guard against the app host).
- Nothing: an AwesomeAssertions `BeEmpty()` on a collection printed only the first problem ("found at least one item"); joining the problems into one string showed them all. One-off.
- Nothing: an empty leftover folder `wt/simulab/f-33` from an earlier ship; delete it by hand.

## 2026-09-24 — Sync with agile@canary 0.0.41 → 0.0.57
- Copied: rules `definition-of-done`, `workflow`; templates `bug`, `epic`, `feature`, `project-claude`; `docs/agile/workflow.md` and `workflow.pt-BR.md` (autopilot, `## Start` section, execution plan in epics).
- Merged: `docs/agile/profile.md` (Blazor prerender and single-use tickets; xUnit 2.x vs v3 note), no conflicts, project sections kept.
- Left alone: build files (no upstream change).
- Declined: the Evals line in `CLAUDE.md` until an eval suite exists; captured as an idea instead.

## 2026-09-25 — F-34 Exams back office
Shipped in `3386b40` from the worktree `f-34`, version 2. Validated on screen on behaviour only: the owner rejected the look of the screens, which became F-43 (D-2). Full suite 1191 tests, 56 s; build 23 s, 0 new warnings.
- Project rule (`project.md`, UI tests): an accessibility claim about a library component is verified in the gallery's DOM before it enters the item file; also a `plugin` note, `[stack: MudBlazor]`.
- Project rule (`project.md`, new section Enums): code that orders or maps an enum is written over `Enum.GetValues`, and its test reads the values instead of counting — `OrganizerKindOrder` was written for exactly three kinds and the fourth broke the caller order in silence.
- ⏳ `plugin` `[generic]` feature-refinement: two business actors sharing one table reached validation as a real defect — the picker offered a board where the contracting body was required, and the Api accepted it. The refinement should ask who owns what before reusing a table.
- Nothing: the palette the owner rejected is already Simulae's (ADR-0001 #30); the gap was composition, not hues. That is F-43, not a rule.

## 2026-09-25 — Sync with agile@canary 0.0.57 -> 0.0.61
- Copied: rules `git`, `workflow` (every item gets its worktree at `/agile:refine`; the subagent line now names `system-design`, `architect`, `ux-designer` and `frontend`, with the main session still owning the conversation); template `project-claude`; `docs/agile/workflow.md` and `workflow.pt-BR.md`.
- Edited by hand: `CLAUDE.md` — a `Worktrees:` line in Working agreement, and three Models lines for the new agents (60 lines, at the limit).
- Left alone: `docs/agile/profile.md` (`upstreamChanged: false`); the six build files (no upstream change).
- `missingCapabilities`: none.
- Noted: the session loaded its skills from the 0.0.59 cache while the installed plugin was 0.0.61; the sync ran with the 0.0.61 scripts. Worth watching if it repeats.

## 2026-09-25 — F-41 AI gateway
Shipped in `28356db` from the worktree `feature-41`, version 2. Full suite 1214 tests, 61 s; build 20 s, 0 warnings. Validated on screen by the owner; Claude had opened the app host against its own database `simulab_f41` first.
- Project rule (`project.md`, new section API): a route mapped only in Development is excluded from the OpenAPI document. The diagnostics route had reached `docs/api/Simulab.Api.json` through the host test that rewrites it, and the DocGen route map then documented an endpoint production never serves.
- ⏳ `plugin` `[generic]`: the Stop gate reported GREEN with a new `CS1574` in `Simulab.Ai`, which its incremental build had skipped; only `--no-incremental` showed it. The same commit also carried a file a test had rewritten, staged by `git add -A`.
- ⏳ `plugin` `[profile: modular-monolith]`: `## Screens and API` claimed the page would call `IAiGateway` in process. The Web host has no database and references no module, so the build stopped for a change note (v2) that added the Development-only endpoint and the typed client.
- Nothing: the two contrast defects found during the screen check belong to components this item only passed through (B-16 field hints, B-17 selected menu item); they are bugs, not rules.

## 2026-09-25 — F-24 Table and column descriptions
Shipped in `e785233` from the worktree `feature-24`, version 2. Full suite 1229 tests, 68 s; build 22 s, 0 warnings. Validated by the owner reading the four generated dictionaries; Claude had measured the result in a database the app host built from scratch (14 tables described, 196 of 198 columns).
- Nothing, and worth saying: the two read-only design passes earned their cost. Before a line of code they found that `role_changes.added` and `role_changes.removed` are `ToJson` containers with no `IProperty`, which made BR1 impossible as written, and that `users` carries twelve columns BR2 did not exempt. Both became change note v2 and were settled with the owner instead of being discovered halfway through.
- Project rule (`project.md`, UI tests): an assertion over a generated document pins the part it is about, not the whole line. Four dbml expectations broke when every column gained a note. The sweep is complete: of the 35 whole-line assertions in `tests/`, six run over the real models — four were fixed here, and the other two are about an index and about the JSON containers, which never gain a comment.
- Idea (needs code): F-45 (AB#768). `EntityModelsTests.RealModels()` had never loaded `AiDbContext` although DocGen has generated `docs/architecture/Ai/` since F-41, so the generator and the test list disagreed in silence and the new rule would have passed without ever looking at `ai_calls`. A test should make the two agree by construction.

## 2026-09-26 — F-39 One problem-details helper for every module
Shipped in `d1ee633` from the worktree `feature-39`, version 3. Full suite 1247 tests, 58 s; build 24 s, 0 warnings. Built in parallel with F-43, which was in `building` in its own worktree: no file overlapped.
- Project rule (`project.md`, UI tests): a guard looks for the shape of what it forbids, not for two names near each other. The first detector flagged `GoogleSignInEndpoints` and `IdentityEndpoints`, both legitimate callers passing a status override — which naturally mention `ErrorKind` and `StatusCodes.Status` together.
- Project rule (`project.md`, UI tests): an exemption exists only if the detector would flag that file without it. The sharpened detector stopped matching all three files the allowlist was about to exempt, so the list became a negative control (change note v3). The sweep found no other guard with either pattern; `ErrorCodeTextTests.NeverShown` is a real exemption, its entries do fire.
- ⏳ `plugin` `[generic]`: step 6 of the validation script asked the owner to pick the file, the place and whether a `using` was missing, and never said the app host has to be stopped. The owner asked what it meant, and when I tried to rerun it myself a `Simulab.Api` left running from the worktree locked `bin/` and the build died before any test.
- Worth recording: the two design passes paid for themselves a second time. They found that `TotpEndpoints` already maps `NotFound` to 401 so the TOTP routes do not reveal whether an account exists — which made BR6 impossible as approved (change note v2) — and that `.editorconfig` sets `CA1062` to `none`, so the reason the first pass gave for a null guard was false. Two of its own claims were wrong and the files said so: five rich callers, not four, and eleven Catalog call sites, not ten.

## 2026-09-26 — F-43 Visual language and kit composition
Shipped in `7c771d4` from the worktree `feature-43`. Nine composition patterns into the kit with their gallery sections, the three scales every `.app-*` rule now reads, the exam form recomposed, and B-16 and B-17 absorbed and closed. Full suite 1317 tests, 54 s; build 19 s, 0 warnings. The `ux-designer` and `frontend` agents ran for the first time.
- Project rule (`project.md`, UI tests): a contrast pair is read from the stylesheet and resolved against the palette, never repeated by hand. B-16 and B-17 hid in exactly that gap — the tests measured the pairs the palette declares while the CSS painted with other tokens, one of them a MudBlazor default nobody had chosen. The sweep the rule asks for found one pair still unpinned (`text-secondary` on `primary-lighten`, the selected radio card's description, measured 6.33 light / 4.60 dark); captured as an item, not fixed on `main`.
- Project rule (`project.md`, UI tests): a kit field component renders its id on the element the label and the error summary point at. `AppRadioCards` rendered none, so the summary's link had nowhere to jump — found only when the scope became cards.
- ⏳ `plugin` `[generic]` feature-build step 8b: the brief for a producing agent has to separate test infrastructure from assertions. Told "every existing test must still pass, unchanged", the `frontend` agent left the scope as a select rather than touch the `Set<T>` helper, and reported it as a stop. The stop was correct and well argued; the instruction was what was wrong.
- Worth recording: two defects came from the screen and from nothing else — the status chip's outline at 1.42:1 on the dark card, and the missing id above. Both are pinned by tests now, but no test would have found either.

## 2026-09-26 — Session: how the plugin can improve itself (no item)
Raised by the owner in conversation. No project rule or code change.
- ⏳ `plugin` `[generic]`: the retro → plugin note → sync loop already works (121 ✅ lines in this log), but a ✅ proves the text changed, not the behavior. The `WaitForAssertion` rule reached the plugin in 0.0.29 after F-8, and the same defect came back in B-11 and again in the next item's new bUnit test. Proposal: every plugin note gets an executable scenario (minimal fixture repository, prompt, check) that fails before the plugin change and passes after it — the plugin's version of "a bug has a regression test seen failing". Simple version first: scenarios run by hand in the plugin session before a note is marked ✅, with no scheduler. Later, only if that pays off: (a) the `reviewer` checks each plugin change against the plugin's `CLAUDE.md` principles, with the checkable ones turned into scripts; (b) a scheduled routine reads every project's `retro-log.md` and opens deduplicated issues on Project 5, never editing the plugin; (c) a rule whose scenario passes without it is a candidate for removal, which keeps the always-loaded budget. Owner approval, merge and sync stay human.

## 2026-09-26 — F-45 The DocGen tests see every context DocGen documents
Shipped in `a59ec29` from the worktree `feature-45`. `RealModels()` is now derived from the closure of DocGen's own project references, and `DocGenModelDriftTests` (14 tests) compares the modules loaded with the module folders under `docs/architecture/`, in both directions. Full suite 1331 tests, 68 s; build 20 s, 0 warnings. No screen, no UI text, no manual page.
- Project rule (`project.md`, UI tests): the injected defect that proves a guard is verified in the file before the run. The first attempt to drop the `Simulab.Ai` reference used a grep pattern that matched nothing, the suite stayed green, and a working guard and an experiment that never happened look identical. The second attempt, with `sed`, made the guard report `Ai: DocGen documents docs/architecture/Ai/ and no test loads its model` while every other rule over `RealModels()` stayed green — which is exactly how F-24's four weeks of blindness passed unnoticed.
- ⏳ `plugin` `[generic]`: an idea a retro raises says what the finding item already fixed. The F-24 retro described `AiDbContext` as never loaded without saying F-24 had just added it, so this item's summary read as a live gap; only re-reading the code at refinement showed the remaining problem was structural.
- Nothing, and worth saying: a Windows path written inside a script's string literal (`\feature-45`) landed in the item file as a formfeed and took three attempts to repair, because the shell had already halved the backslashes. The core rule `workflow` already forbids writing files through script string literals; this is its exact failure mode.

## 2026-09-26 — F-27 Retention of failed jobs
Shipped in `9a4e8e6` under `/agile:autopilot`, two stops, three recommended answers at stop 1. A Failed job row is kept 90 days from `created_at`, then deleted by `JobCleanup` inside the worker's own poll. Full suite 1327 tests, 92 s; build 24 s, 0 warnings. No screen, so the app manual is untouched and the retention went to `docs/infra.md`.
- Project rule (`project.md`, Sessions and retro): check `grep "^## "` after filling an item file from the template. The refinement replaced the body from `## Goal` to the first `## Decisions` and left a second, empty `Decisions` and `Out of scope` behind; it reached `main` and was removed in `c37b8eb`.
- ⏳ `plugin` `[generic]` autopilot step 5: the approval card promised "the criteria below" and the criteria were written after the cards, so the owner approved criteria they had not seen. A card round is one tool call; whatever the card refers to must precede it.
- Nothing: `CA1873` on the log call (two boxed ints) became a source-generated `[LoggerMessage]`, which the repository already uses. The gate caught it at the first build — the system working, not a lesson.

## 2026-09-26 — B-18 The selected radio card's description is not pinned by a contrast test
Shipped in `5fd91c3` from the worktree `bug-18`. One `Theory` in `ThemeContrastTests` holds `text-secondary` on `primary-lighten` at the shared 4.5:1, both colours read from `app.css` through `AppCssColours`. Nothing on screen changed: no CSS, no palette token, no component. Full suite 1343 tests, 93 s; build 32 s, 0 warnings. `Simulab.Web.Tests` went from 614 to 616.
- Project rule (`project.md`, UI tests): the injected defect is chosen so that only the new guard goes red. `#6E819F` was the obvious tone and it also took the field hint to 4.10:1, so the run would have proven the suite reacts, not that this test is about this pair; `#758AAB` failed exactly one case of 99 at 4.356:1 while the hint kept 4.62:1 on the card and 5.07:1 on the page.
- Project rule (`project.md`, UI tests): a contrast ratio written into an item is re-measured from the committed tokens at refinement. The file carried 6.33:1 for light from the F-43 sweep; the committed tokens give 6.19:1. The conclusion did not change, but the criterion had been written over a number that does not reproduce.
- Worth recording: the guard was the whole product of the item, so the only evidence that it works is the failing run. Both directions were exercised in the same turn — red with the diff shown first, then green with `git diff src/` empty — which is now what the two rules above ask for together.
- Observation, no lesson: the worktree root `D:\dev\_icontrol\wt\simulab` still holds `f-33` and `feature-39`, folders git no longer registers, plus two loose `identity.dbml`/`identity.dbdiagram` files. Left untouched by the owner's decision (2026-09-26).

## 2026-09-26 — sync agile@canary 0.0.61 → 0.0.63
One subject: the item worktree folder is now named `f-<n>-<desc>` / `b-<n>-<desc>` (`<desc>`: up to 20 characters of the slug, cut at a hyphen) instead of `<type>-<n>`. Copied: `.claude/rules/agile/git.md`, `.claude/rules/agile/naming.md`, `docs/agile/templates/project-claude.md`, `docs/agile/workflow.md`, `docs/agile/workflow.pt-BR.md`. Merged: nothing. Left alone: `docs/agile/profile.md` (`edited`, upstream unchanged) and the six `manual` build files (all upstream unchanged). No build file changed, so no build, no suite and no baseline refresh. Proposed and approved: the `Worktrees:` line of `CLAUDE.md` now names the new folder pattern. The three open worktrees (`feature-28`, `feature-29`, `feature-30`) keep their names until their merges, as the upstream text allows. `missingCapabilities` empty.
- Owner decision: the sync ran with three items open (F-28 validating, F-29 building, F-30 approved) because all four checkouts were clean and nothing in the diff touches code or build files.
- ⏳ `plugin` `[generic]`: `sync.js record` died with `Error: UNKNOWN: unknown error, open '.claude/agile/sync.json'` (errno -4094) and left the file at the previous version; the identical retry succeeded. A transient Windows lock on a file the script opens for write ends a sync half-applied — the five files were already copied — with no hint that a retry is all it needs. Proposal: `record` retries the write a few times before failing, and its error names the file and says the copies already happened.
- Also seen, outside the sync: `0.0.63` ships a ship-time `gate.js docs` driven by a `docs` block in `.claude/agile/build.json`. Simulab has no `.claude/agile/build.json`, so the ship is unchanged; recorded only so the next reader knows the door exists.
- Project rule (`project.md`, Sessions and retro): two sessions ran `/agile:sync` on the main checkout at the same time. One of them copied the five files, wrote `CLAUDE.md`, `sync.json` and the retro entry and committed `f4c414e`; the other's `apply` reported `same` for all five and then read a working tree that went from eight modified files to clean between two commands. Nothing was lost — the plan was identical and the script is idempotent — but the two sessions shared one git index, and the second one could have committed the first one's half-written state. "One Claude session per checkout" already existed; what was missing is that a command writing to the main checkout has to check for the other writer before it starts, not trust the rule. Proposal: the rule below.

## 2026-09-26 — B-19 The full suite is not reliably green under parallel load
Shipped in `18170d8` from the worktree `b-19-full-suite-flaky`. Two test-side fixes, 15 lines across two test
files, nothing under `src/`. Baseline before the fix, measured in the item's own worktree: 3 full runs, 2 red.
After: 5 consecutive clean-build full runs green, 1343 tests each. Ship gate GREEN, build 43 s 0 warnings, suite
126 s (the suite grew from 93 s at B-18, still well inside the < 5 min budget).
- Project rule (`project.md`, UI tests): a test that starts a `BackgroundService` waits for its effect before
  `StopAsync`. `StartAsync` is `Task.Run(() => ExecuteAsync(stoppingCts.Token), stoppingCts.Token)`, and
  `Task.Run` never runs a work item whose token was cancelled before the pool dequeued it, so an immediate
  `StopAsync` can drop the body entirely. Sweep done: `grep "\.StartAsync("` over `tests/` found one other call,
  already correct.
- Project rule (`project.md`, UI tests): a test whose subject is "nothing happened" carries one assertion that
  proves the code ran at all. This one asserted no job was handled and no row was removed, and both held most
  firmly when the worker had never started — it passed for the exact reason it should have failed.
- ⏳ `plugin` `[generic]`: `gate.js ship` should say when every red test is outside the item's diff. See the row
  in "Plugin notes — status".
- Worth recording, no lesson: the bug's own report named two tests and the wrong `RecordingLoggerProvider` (two
  classes share that name). The refinement's three baseline runs found a third test, the most frequent one, which
  the report had never seen. A bug written from two observations is a starting point, not an inventory.
- Not a lesson, a live gap: C3 (`MainLayoutTests.MenuButton_Desktop_TogglesCollapsedAndWritesCookie`) was deferred
  by the owner, never reproduced, and did not fail in any of the 8 full runs of this item. If it returns it needs
  its own repro.

## 2026-09-26 — F-28 Partial index filter in the data dictionary
Shipped in `8f6be36` from the worktree `feature-28`. `EntityModels.cs` (+37) and its tests (+63, with a
shared-table context for the F-25 test model); the visible result is two generated lines, now
``- `ix_jobs_active_created_at` on created_at (where status IN (0, 1))``. Ship gate GREEN on the first attempt:
build 46 s 0 warnings, suite 1349 passed 0 failed in 121 s, architecture tests 135 (six more than before).
- **No new rule.** This item's lessons were already harvested by B-19, which it paid for: F-28's ship gate went
  red twice on `JobWorkerTests`, a test in another project it does not touch, and that is what turned the flaky
  suite into an item instead of a shrug. Adding a rule here would duplicate the two B-19 lines above.
- Worth recording, no lesson: the item sat at `validating` from 2026-09-26 until B-19 shipped, on the owner's
  decision to hold the merge rather than re-run the gate until it happened to be green. The first gate run after
  B-19 was green, so the hold cost one afternoon and bought a suite whose red means something.
- Process note, no action: three items were open in parallel (F-28, F-29, F-30) against a recommended limit of
  two, and the one that blocked all three was a test defect in none of them.

## 2026-09-27 — Sync with agile@canary 0.0.71
From 0.0.63 to 0.0.71. Approved by the owner. Ran after finding F-29 already merged to `main` (`9f532fc`) by
another session while this one still saw it as `validating`; the stale worktree `feature-29` and the local
`feature/F-29` branch were removed first (no remote branch, `main` was `0 0` against `origin/main`).
- Copied: `.claude/rules/agile/git.md` (delete a branch on the remote only when `git ls-remote` lists it; an item
  branch usually was never pushed), `.claude/rules/agile/workflow.md` (an issue body is edited only whole, via
  `--body-file`), `docs/agile/templates/project-claude.md` (`Plugins:` line), `docs/agile/workflow.md` and
  `docs/agile/workflow.pt-BR.md` (manual 0.0.63 → 0.0.71: solution SDK from `global.json`, `engine: msbuild`,
  the DocGen tool catalogue, plugin notes scoped by `Plugins:`, conditional remote branch delete).
- Merged by hand: `docs/agile/profile.md`. The only upstream change was one sentence on the DocGen tool
  catalogue (`ModelToolAttribute`, since 0.0.70); added to the "Technical docs (F-15)" line, marked not yet
  adopted. The Simulab-specific sections and the customized Layout block are unchanged.
- Approved: `CLAUDE.md` gained `Plugins: agile`.
- Captured as an idea, not built: **F-49 DocGen tool catalogue** (board 774) — the capability the plugin ships
  since 0.0.70 that this project does not use yet.
- Left alone: the six build files (`.editorconfig`, `Directory.Build.props`, `Directory.Packages.props`,
  `.gitignore`, `global.json`, `tests/Directory.Build.props`) — only placeholder-vs-`Simulab` wording differs,
  no functional change.
- No build file or build-checked rule changed, so the suite was not rerun. Warnings baseline: present, untouched.
  No ⏳ plugin notes delivered by this version.

## 2026-09-27 — F-30 (sign-up writes in one transaction)
Merged `feature/F-30` at `7218d2d` (AB#749). Full suite: 799 passed, 0 failed (build 29s, tests 72s).
- Project rule added to `.claude/rules/agile/project.md` ("Access and entitlements"): testing a race on an
  Identity unique index needs `UserManager`'s own pre-check turned off (`RequireUniqueEmail` for the email
  one) or the translation tested directly against `DbUpdateException` — a lying `IUserDirectory` alone never
  reaches the database, and `AddLoginAsync` has no switch for its own check. Found while writing AC8/AC8b:
  the first attempt left only one `INSERT INTO identity.users` in the test log, and the Google case threw
  `InvalidOperationException: The Google login of a new account could not be linked.` instead of hitting
  `pk_user_logins`. Will recur in F-47, which gives the same guarantee to the module's other multi-write
  handlers.

## 2026-09-27 — F-32 Account events in the data export
Merged `feature/F-32` at `795244c` (AB#751). Ship gate GREEN: build 22 s, 0 warnings; full suite 1404 tests, 0
failed, ~80 s.
- Project rule added to `.claude/rules/agile/git.md`: stage files by name in the main checkout, never `git add
  -A`/`git add .`. Closing the item, a broad stage swept up an unrelated deletion another session had left
  uncommitted in the shared checkout (`docs/features/F-49-docgen-tool-catalogue.md`, mid-setup for its own
  worktree) straight into the F-32 close commit (`f4a3187`). Fixed in the next commit (`b690449`); evidence:
  `git show f4a3187 --stat` listed `delete mode 100644 docs/features/F-49-docgen-tool-catalogue.md` alongside
  the F-32 change. The underlying risk (two sessions, one checkout) was already known; this is it recurring in
  a new shape, at the ship step rather than mid-build.
- ⏳ `plugin` `[stack: Blazor Server]`: verifying the "Your data" card and its download dialog through the app
  host, the browser pane's own `computer` `screenshot` call timed out and the Blazor Server circuit
  reconnected mid-dialog, losing its state — the console log showed repeated `WebSocket connected to
  wss://.../_blazor` inside two minutes. `read_page`/`get_page_text` never triggered it. The existing rule
  (drive a dialog in one call) assumes the calls themselves are cheap; `screenshot` in this pane was not.

## 2026-09-28 — Sync with agile@canary 0.0.78
From 0.0.71 to 0.0.78, run while F-31 was `validating` (owner re-invoked the sync to continue).
- Copied: `docs/agile/workflow.md`, `docs/agile/workflow.pt-BR.md`.
- Merged: nothing. Left alone: `.claude/rules/agile/git.md` and `docs/agile/profile.md` (edited, upstream unchanged); every build file (`manual`, no upstream change).
- Baseline: present, nothing to take. Plugin notes delivered: none to mark.
- Docs command: declared now in `.claude/agile/build.json` (`dotnet run --project tools/Simulab.DocGen`).
- DocGen tool catalogue (since 0.0.70): not installed by a sync; already covered by F-49 (approved, AB#774), so no new idea was captured.
- Declined: nothing.

## 2026-09-28 — F-31 Recent account activity for the user
Closed after the fact: `feature/F-31` was merged at `f1d3f28` (2026-09-27) but the item file stayed `validating`, the board item `Active` and the local branch alive until the owner validated on 2026-09-28. Closed now: file `done` with the criterion → test table, AB#750 `Closed`, branch deleted with `git branch -d` (was fully merged; never pushed).
- Lesson (proposed as nothing for now): a merge done before the ship step finished left git and the item file disagreeing, found only because a sync stopped on the `validating` status. If it recurs, propose a start-of-session check that flags an item file not `done` whose `feature/F-<n>` merge commit is already in `main`.

## 2026-09-28 — F-49 DocGen tool catalogue
- 1 (plugin: agile `[stack: Microsoft.Extensions.AI]`): the template's `ModelToolAttribute` comment says the `Async` suffix is dropped from the tool name; `AIFunctionFactory` 10.5.1 keeps it. Evidence: the test `Render_TwoToolsWithTheSameDerivedName_Fails` failed with `two or more tools are called "GetExamAsync"`. The template also writes no `tools.md` while there is no tool. Note above, row ⏳ in "Plugin notes — status".
- 2 (nothing): `docs/architecture/Identity/routes.md` was stale on `main` since F-32 (missing `GET /api/v1/identity/account-events/mine`); found when F-49 regenerated the docs. Cause not verified, so no rule.

## 2026-09-29 — F-35 Exam editions back office
- 1 (plugin: agile `[stack: bUnit + MudBlazor]`): a frozen `TimeProvider` also freezes the `MudAutocomplete` debounce; advance the fake clock. Note above, row pending in "Plugin notes — status".
- 2 (project rule): after a save that turns the add page into the edit form, `StateHasChanged(); await Task.Yield();` comes before `NavigateTo(..., replace: true)`, or the `NavigationLock` reads a stale `HasChanges`. Evidence: the bUnit URL stayed `http://localhost/` until the `Yield` was added. Added to `.claude/rules/agile/project.md`. `ExamForm.razor` navigates without it and was not checked for the same defect.
- 3 (plugin: agile `[generic]`): the `frontend` agent had no shell and delivered 51 failing screen tests; the main session builds and tests before trusting the files. Note above, row pending.
- Declined: nothing.

## 2026-09-29 — F-36 Catalog browsing for students
Shipped in `ba70e94` (merge of `feature/F-36`), merge recorded in `f0a931e`. Board AB#755 read back as `Closed`. Full suite 1682 passed, 0 failed, 74 s test + 23 s build.
- 1 (plugin: agile `[generic]`, second occurrence of the F-35 note, no new row): the `frontend` agent again had no shell (its tools are Read, Grep, Glob, Write, Edit) and reported it had run nothing. The first `dotnet test tests/Hosts/Simulab.Web.Tests` of its output showed `Failed: 16`, all `System.FormatException : Format specifier was invalid` from `Format="D"` on an `AppNumberColumn<…, int>` (MudBlazor formats through a floating-point value); the fix was `Format="0"`. Evidence: the saved run output `web1.txt` and the agent's own report. Still pending in "Plugin notes — status" (line of the `frontend` agent without a shell); a fix in the plugin removes the cost of every screen item.
- 2 (nothing): the `git worktree remove` at ship ended with `failed to delete ... Invalid argument`, leaving the folder (with `.vs/`) on disk while git had already unregistered it. The blocking question "is the IDE closed?" was not asked before the removal (`references/worktrees.md`, ship step 3); the rule already exists, so no new one. The folder was still busy after the owner closed the IDE and the stale shells of this session were stopped; cause not verified.
- Declined: nothing.

## 2026-09-29 — F-37 Catalog seed from Simulae
Shipped in `a8ab607` (merge of `feature/F-37`), closed in `8894abf`. Board AB#756 read back as `Closed`. Full suite 1692 passed, 0 failed, 75 s test + 26 s build.
- 1 (plugin: agile `[generic]`, row ⏳ in "Plugin notes — status"): the ship chained the lock probe and `git worktree remove` with `;`. Evidence: the probe printed `mv: cannot move ... Device or resource busy` and the removal still ran, ending with `error: failed to delete ... Invalid argument`; git had already unregistered the worktree and the folder (with `.vs/`) stayed on disk. Second time (F-36 had the same ending); the F-36 cause was not verified either, and this folder was still busy after the owner's IDE was asked to close.
- Not kept (nothing): a script located `## Sources` by first text match, hit a mention in BR12 and deleted the acceptance criteria and decisions; the diff stat (42 deletions) caught it and git restored the file. The Write/Edit rule already covers it. The BR12 source check changed the Curitiba notice number and date: a one-off.
- Declined: nothing.

## 2026-10-01 — F-52 Seed the admin user
Merged in `d513274` (merge of `feature/F-52`). The owner validated and authorized the merge on 2026-10-01. Not verified: this session had no .NET SDK, so the build, the tests and the full suite were never run by it, and no test time is recorded. Board AB#778 was not closed from here (no `az boards`); the two item branches could not be deleted on the remote (`git push --delete` ended with `unexpected disconnect while reading sideband packet`, twice), the local one was deleted.
- 1 (plugin: agile `[generic]`, proposed): `/agile:build` finds out that the toolchain is missing only after the code is written. Evidence: `dotnet: command not found` on the first build attempt, after the item was approved; the item then sat in `building` with no gate output, and the ship needed the owner's run elsewhere. Proposal: the build starts with a toolchain check (`dotnet --version`, the version in `global.json`) and stops with a question before writing code when it fails.
- 2 (project rule, proposed): a password, even a development-only one, lives in the user secrets and never in a committed settings file. Evidence: committing the generated development password in `appsettings.Development.json` was blocked as a credential leak, and the owner then moved it to the user secrets (change note 1 of F-52; BR7 and AC10 rewritten). A non-secret development value (the OpenIddict client secret, the TOTP key) stays where `docs/infra.md` says. Proposed text for `.claude/rules/agile/project.md` under "Data" or "Access": "A password, even a development-only one, is read from the user secrets or the environment, never from a committed settings file (F-52)."
- 3 (nothing): the owner's commit that moved the password out also deleted the OpenIddict client secret and the TOTP settings from `appsettings.Development.json` (12 deletions); it was found by reading the diff before the merge and restored. One occurrence, caught by the existing diff reading; no rule.
- Declined: nothing.

## 2026-10-01 — F-38 Sign-in rate limit per client
Merged in `43c9c28` (merge of `feature/F-38`), pushed in `8ffba61`. The owner validated and authorized the merge on 2026-10-01. Full suite 1746 passed, 0 failed, 78 s test + 17 s build. Board AB#757 read back as `Closed` (Azure Boards; the owner then said the current board is GitHub, see lesson 3).
- 1 (project rule, applied in `project.md`; code check captured as an idea): a test host must pin every configuration key a developer's user secrets can feed. Evidence: `dotnet test tests/Hosts/Simulab.Api.Tests` on `main` gave `Failed: 10, Passed: 2` with `Failed to connect to 127.0.0.1:1`; with `Identity:SeedAdmin:Password` set empty in its `ApiFactory`, 12 of 12 passed. `SimulabApiFactory` already did it.
- 2 (plugin: agile `[generic]`, row ⏳ in "Plugin notes — status"): the preview tool starts the app host from the session's main checkout. Evidence: `preview_logs` showed `D:\dev\_icontrol\simulab\src\...` while the item sat in `D:\dev\_icontrol\wt\simulab\f-38-sign-in-rate-limit`; the app host was started with `dotnet run` from the worktree through Bash.
- 3 (plugin: agile `[generic]`, row ⏳; cause not verified): F-52 reached `main` with a test file that did not compile (CS1929 in `SeedAdminTests.cs`), two analyzer warnings and ten red `Simulab.Api.Tests`, found only when F-38 built; the F-52 entry above says that session had no .NET SDK. Also found: a merged PR on `origin/main` moved `CLAUDE.md` to the GitHub board while the items lived in Azure Boards; the owner confirmed GitHub is current.
- Declined: nothing.

## 2026-10-02 — F-44 The issuing authority's acronym off the screen
Merged in `bf3d4f4` (merge of `feature/F-44`), pushed with `5becd98`; `main` had already been fast-forwarded to `3665997` and pushed by something outside this session before that merge. The owner validated on screen and typed ship on 2026-10-02. Full suite 1749 passed, 0 failed, 112 s test + 18 s build. Board #19 closed, Status Done (read back).
- 1 (plugin: agile `[generic]`, row ⏳ in "Plugin notes — status"): the Stop hook and a background `gate.js ship` collide in one worktree. Evidence: the hook reported `MSB3061 Unable to delete file` (0 -> 1) while the ship gate output ended `211 Error(s)` with CS0012 missing references in `Simulab.Identity.Infrastructure`; the same gate in the foreground ended `agile gate GREEN`.
- 2 (test, applied in the item): `app-cell-secondary` had no rule in `app.css`, so the scope's state or city ran into the scope name on screen (found by the owner). `AppCellClassesTests` fails when a page writes an `app-cell-*` class the stylesheet does not define; seen failing with the rule renamed.
- Declined: the F-52 guard was loosened for `appsettings.Development.json` at the owner's explicit choice (twice), recorded in the item, `docs/infra.md` and `project.md`; no new rule. The preview-starts-the-main-checkout lesson was already recorded by F-38.

## 2026-10-02 — F-42 State picker for the exam scope
Merged in `74616f9` (merge of `feature/F-42`), pushed with `2313446`. The owner validated on screen and said to ship on 2026-10-02. Full suite 1808 passed, 0 failed, 69 s test + 21 s build, `agile gate GREEN`, 0 new warnings. Board #81 closed, Status Done (read back). The independent review found no blocker or major and four minors; three were fixed in the item and the manual went at ship.
- 1 (plugin: agile `[generic]`, row ⏳ in "Plugin notes — status"): the validation script I wrote said "your local database" for an item with a migration, although `AppHost.cs` reads `Database:Name` and `worktrees.md` says an item with a migration uses its own database. Evidence: `select migration_id from catalog.__ef_migrations_history order by 1` in the shared `simulab` database, through the container, ended `20261002114230_NormalizeExamStates` while `feature/F-42` was unmerged. No data changed (the seven exams there were all Municipal); the script was corrected to `Database__Name=simulab_f42` and the useful commands went to `docs/infra.md`.
- 2 (plugin: agile `[generic]`, row ⏳): at ship the lock probe failed and the removal ran anyway. Evidence: `mv: cannot move '.../f-42-state-picker' to '.../f-42-state-picker.probe': Device or resource busy`, then `git worktree remove` printed `error: failed to delete '...': Invalid argument`; `git worktree list` no longer showed the worktree and the folder stayed on disk with `.vs`, `bin` and `obj`. The chain I wrote joined only the two `mv`s with `&&`; the cause of the lock, verified afterwards with `handle.exe -accepteula -nobanner f-42-state-picker`: `docker.exe pid: 76336 type: File` and `docker-compose.exe pid: 68664 type: File` on the folder (not Visual Studio, which was closed; the earlier guess about `.vs` and `bash.exe` was wrong). Nothing was stopped.
- 3 (nothing new): Git Bash rewrites `/d` in `docker run -v vol:/d:ro` to `D:/`. Evidence: `ls: D:/: No such file or directory`. Already written in `docs/infra.md` ("Useful commands"), one case, so no rule.
- Declined: nothing. Left for the owner: the orphan folder `D:\dev\_icontrol\wt\simulab\f-42-state-picker` (unregistered, holds only merged content and ignored build output) is removed once `docker.exe` and `docker-compose.exe` let go of it (all its contents were already deleted; only the empty folder is held).

## 2026-10-02 — Sync with agile@canary 0.0.97
From 0.0.78 to 0.0.97, run after F-42 shipped (main clean, no item open).
- Copied (never edited, plugin changed): rules `api-contracts`, `definition-of-done`, `i18n`, `ui`, `workflow`; templates `bug`, `feature`, `infra`, `project-claude`; `docs/agile/workflow.md` and `workflow.pt-BR.md`. New: `docs/agile/templates/store-checklist.md`.
- Merged by hand with the recorded base: `.claude/rules/agile/git.md` (clean merge: typing ship authorizes the push, and the lock probe replaces asking to close the IDE) and `docs/agile/profile.md` (one conflict: the project's own Technical docs, Simulab F-12 and solution-root paragraphs were kept, and only the new `<Version>` paragraph was added).
- Left alone: `.editorconfig`, `Directory.Build.props`, `Directory.Packages.props`, `global.json`, `tests/Directory.Build.props` (manual, no upstream change); `.gitignore` (upstream changed, the project's copy differs a lot, nothing proposed); `project.md`, ADRs, items, glossary, `CLAUDE.md`.
- Proposed, not applied (the owner approved the files, not `CLAUDE.md`): two wording lines of `CLAUDE.md` follow the new templates ("merge authorized (typing `/agile:ship <id>` is that gate)" and "Ask before: pushing or merging to `main` outside a ship").
- Missing capabilities: app version `<Version>` on `Simulab.Api.csproj` captured as an idea; DocGen tool catalogue and visual identity (`/agile:identity`) declined for now.
- Baseline: present. Docs command: declared (`dotnet run --project tools/Simulab.DocGen`). Plugin notes delivered: none in the plan. Build and suite not run: no build file and no rule with a build check changed.

## 2026-10-02 — Sync with agile@canary 0.0.102
From 0.0.97 to 0.0.102, run on a clean main with no item `building` or `validating`.
- Copied (never edited, plugin changed): templates `infra` and `project-claude`; `docs/agile/workflow.md` and `workflow.pt-BR.md`.
- Merged with the recorded base: `docs/agile/profile.md` (clean merge, no conflict: the new "Deploy recipe (containers + Aspire)" section was added).
- Manual, applied: `.gitignore` got three lines only (`.env.*`, `!.env.example`, `aspire-output/`): Aspire writes a deploy's secrets in plain text there.
- Left alone: `.claude/rules/agile/git.md` (edited, upstream unchanged); `.editorconfig`, `Directory.Build.props`, `Directory.Packages.props`, `global.json`, `tests/Directory.Build.props` (manual, no upstream change).
- Missing capabilities captured as ideas: F-61 DocGen tool catalogue (#100), F-62 deploy command per environment (#101), F-63 visual identity (#102). The app version is F-60 (#99), captured at the 0.0.97 sync.
- Baseline: present. Docs command: declared (`dotnet run --project tools/Simulab.DocGen`). Plugin notes delivered: none. Build and suite not run: only Markdown and `.gitignore` changed.

## 2026-10-02 - Retro F-60 (app version on the Api project)
- Plugin note: for `modular-monolith`, `scripts/publish.js` matches every `*.Api.csproj` under `src` (`Simulab.Catalog.Api`, `Simulab.Identity.Api` are module class libraries, not deployables) and does not list `Simulab.Web`, the second deployable host, so `/agile:publish` stays blocked for this solution.
- Plugin note: `feature-ship` step 8b bumps `<Version>` on every ship, but the item that introduces the version has no rule: its ship either bumps at once (0.1.0 becomes 0.2.0 and a test pinned to the seed goes red) or does not bump. Say which, and say that a test must compare the csproj version with the assembly version instead of pinning a number.
- Project follow-up: `AppVersionTests` (AC1, AC3) pin `0.1.0`; the next ship's bump turns them red. Loosen them in the next item that ships, before its bump.

## 2026-10-02 — Sync with agile@canary 0.0.104
From 0.0.102 to 0.0.104, run on a clean main with no item `building` or `validating`.
- Copied (never edited, plugin changed): template `project-claude`; `docs/agile/workflow.md` and `workflow.pt-BR.md`.
- Left alone: `docs/agile/profile.md` (edited by project, upstream changed the version location from Api.csproj to Directory.Build.props, Simulab's own policy kept); `.claude/rules/agile/git.md` (edited, upstream unchanged); all manual files — `.editorconfig`, `Directory.Build.props`, `Directory.Packages.props`, `global.json`, `tests/Directory.Build.props`, `.gitignore` (none had upstream changes worth applying).
- Missing capabilities: 4 — DocGen tool catalogue (item F-61 #100), app version movement to Directory.Build.props (F-60 / planned for next ship), deploy command per environment (F-62 #101), visual identity (F-63 #102).
- Baseline: present. Docs command: declared (`dotnet run --project tools/Simulab.DocGen`). Plugin notes delivered: none. Build and full suite: green — build 12 s (0 warnings), full suite 2010 tests passed (0 failed, ~130 s total).

## 2026-10-02 - Retro F-46 (derive the production assembly list)
- Plugin note: `[generic]` `feature-ship` step 8b — run the architecture tests after writing the bumped `<Version>` and before committing it. Evidence: the first test run after the bump printed `Failed!  - Failed:     2, Passed:   162`, and the commit had already gone out because the command chained `git commit` after a `grep` that always exits 0 (fixed on the branch before the merge). Tracked in the table above.
- Nothing: the board Status field stayed at `Ready` during the build because only the label was changed (own omission, once).

## 2026-10-02 - Sync with agile@canary 0.0.105
From 0.0.104 to 0.0.105, run on a clean main with no item `building` or `validating`.
- Copied (never edited, plugin changed): template `project-claude`; `docs/agile/workflow.md` and `workflow.pt-BR.md` (question 2f, offline-first).
- Left alone: `.claude/rules/agile/git.md` and `docs/agile/profile.md` (edited, upstream unchanged); all manual build files (no upstream change).
- Missing capabilities: DocGen tool catalogue (F-61 #100), deploy command per environment (F-62 #101), visual identity (F-63 #102); all already captured, nothing new.
- Found: `/agile:publish develop` stopped because the environments table of `docs/infra.md` lacks the `Deploy command`, `Check URL` and `Version (deployed on)` columns (F-62) and has no environment named `develop`.
- Baseline: present. Docs command: declared. Plugin notes delivered: none. Build and suite not run: only Markdown changed.
- Manual, applied: `.gitignore` got `!docs/releases/` after `[Rr]eleases/`: the Visual Studio template pattern ignored the folder where `publish.js` writes the notes, and the first `release` failed at `git add`.
- Plugin note: `scripts/publish.js` `package` listed `Simulab.Catalog.Api` and `Simulab.Identity.Api` (module class libraries under `src/Modules/*`) as deployables and left out `Simulab.Web`; the zips of v0.3.0 do not run the app (adds to the F-60 note). `plugin: agile`.
- Plugin note: `templates/dotnet/gitignore` ignores `[Rr]eleases/`, which swallows `docs/releases/`; add `!docs/releases/` to the template. `plugin: agile`.

## 2026-10-03 - F-50 Table search announces results
Shipped as 0.4.0 (merge 7037e30). The full gate needed three runs: a hung test of this item, then two version tests red on main (B-21).
- Project rule, applied: a test that holds a load never awaits the call that started it (`.claude/rules/agile/project.md`). Evidence: `agile gate RED: a test hung (Simulab.slnx)` with `not finished: ...Search_FirstLoadCancelledByASecond_SaysOnlyTheSecondTotal`; it passed alone. Swept the other hold-style tests (`HoldFind`, `HoldFilters`, `RefreshGate`): all release before awaiting, nothing to fix.
- Plugin note: `version.js` puts `<Version>` in `Directory.Build.props` while `AppVersionTests` pinned it to `Simulab.Api.csproj` (B-21). Evidence: `Failed: 2, Passed: 2` on `AppVersionTests`, and `Directory.Build.props:4` holds `0.3.0`. The tests were fixed in this item; B-21 stays with the owner. `plugin: agile [generic]`.
- Idea, captured: `DataExportPageTests.Dialog_AccountWithoutPassword_ShowsHowToCreateOne` failed once in the full suite (1 of 833) and passed 7 of 7 alone. Cause not verified.

## 2026-10-03 - F-48 Drive the search debounce from TimeProvider
Shipped as 0.5.0 (merge c8d525c), tests only: the refinement found the premise false (MudBlazor 9.9.0 already reads an injected `TimeProvider`), so no production file changed.
- Plugin note: the Stop gate hook collides with a background build loop of the same worktree. Evidence: `agile gate RED: build failed` with `MSB3030 ... because it was not found` while `dotnet clean` ran in the loop, and `loop.txt` showed runs 1 and 2 empty. The loop was redone in the foreground, in blocks of 5. `plugin: agile [generic]`.
- Nothing: the premise check in the library source at the pinned tag (already in the refinement step 6); the `board: 773` header that matched no GitHub issue (the real one was 24), cause not verified, no audit captured (owner: no); capturing B-21 for a failure F-50 had already fixed on main, a one-off.

## 2026-10-03 - Sync with agile@canary 0.0.110
From 0.0.105 to 0.0.110, run on a clean main; F-62 was `validating` in its worktree and the owner said to go on (only docs touched).
- Copied (never edited, plugin changed): templates `infra` (desktop updates section) and `project-claude` (offline-first also for `desktop` with an API); `docs/agile/workflow.md` and `workflow.pt-BR.md` (0.0.110, question 26b).
- Left alone: `.claude/rules/agile/git.md` and `docs/agile/profile.md` (edited, upstream unchanged); all manual build files (no upstream change).
- Missing capabilities, declined (owner: no): DocGen tool catalogue, visual identity.
- Baseline: present. Docs command: declared. Plugin notes delivered: none. Build and suite not run: only Markdown changed.

## 2026-10-03 - Sync with agile@canary 0.0.111
From 0.0.110 to 0.0.111, run on a clean main; F-62 was `validating` in its worktree and only docs were touched.
- Copied (never edited, plugin changed): rule `output-style`; templates `adr`, `bug`, `discussion`, `epic`, `feature`, `infra` (glossary link line) and `glossary` (`Meaning (pt-BR)` column); `docs/agile/workflow.md` and `workflow.pt-BR.md` (0.0.111, section 17).
- Left alone: `.claude/rules/agile/git.md` and `docs/agile/profile.md` (edited, upstream unchanged); all manual build files (no upstream change).
- Glossary (owner: all): `Meaning (pt-BR)` column written for every existing row of both tables; 54 technical-term rows added, drawn from a sampled scan of the docs, not an exhaustive read; the `Technical terms: [glossary](...)` line added under the title of 97 documents (infra, features, bugs, ADR, discussions, epics except the README, release notes). The worktree copies of F-62 and F-68 get theirs at their merge.
- Missing capabilities, declined (owner: no): DocGen tool catalogue, visual identity.
- Baseline: present. Docs command: declared. Plugin notes delivered: none. Build and suite not run: only Markdown changed.

## 2026-10-03 - Sync with agile@canary 0.1.0
From 0.0.111 to 0.1.0 (the plugin now numbers versions by what changed), run on a clean main; F-62 was `validating` and F-68 `approved` in their worktrees, only docs touched.
- Copied (never edited, plugin changed): `docs/agile/workflow.md` and `workflow.pt-BR.md` (0.1.0, version numbering paragraph).
- Left alone: `.claude/rules/agile/git.md` and `docs/agile/profile.md` (edited, upstream unchanged); all manual build files (no upstream change).
- Glossary: nothing to add (column and link lines already there).
- Missing capabilities, captured as ideas (owner: yes): DocGen tool catalogue as F-69 (board 111), visual identity as F-70 (board 112).
- Baseline: present. Docs command: declared. Plugin notes delivered: none. Build and suite not run: only Markdown changed.

## 2026-10-04 - F-67 Data region decision
- ⏳ plugin: agile [generic] — The Stop gate reported `agile gate SKIPPED: no code file changed since the main branch` on a docs-only diff, while `tests/Simulab.ArchitectureTests/VocabularyTests.cs:20` reads `docs/glossary.md` and `ArchitectureOverviewTests.cs:175` reads `docs/infra.md`; the architecture tests were run by hand (164 passed). Plugin note, row in the table above.
- The refinement's premise check (Brazil adequate under Art. 45 GDPR since 2026-01-26) turned a second EU environment into one ADR. The workflow already asks for it: nothing to change.

## 2026-10-04 - Sync with agile@canary 0.4.0
From 0.1.0 to 0.4.0, run on a clean main; no item `building` or `validating` (F-51 to F-74 `approved`, F-79 `refining`, each in its worktree), only docs touched.
- Copied (never edited, plugin changed): `.claude/rules/agile/workflow.md` (status `cancelled`, duplicate only), templates `feature.md`, `bug.md`, `backlog.md` (same exit), `infra.md` (`## Cloud accounts`), `docs/agile/workflow.md` and `workflow.pt-BR.md` (sections 14.46 to 14.49).
- Left alone: `.claude/rules/agile/git.md` and `docs/agile/profile.md` (edited, upstream unchanged); all manual build files (no upstream change).
- Glossary (owner: 2a): 15 technical-term rows added from an agent sweep of the docs, each checked absent from the table and present in the docs; not an exhaustive read. `Technical terms:` line added to `docs/decisions/ADR-0002-host.md` and `docs/epics/README.md`.
- Missing capabilities: `## Cloud accounts` in `docs/infra.md` (since 0.2.0) found similar to F-68 (approved, `## Access` table); owner chose to improve F-68 through `/agile:change F-68`, no new item. DocGen tool catalogue and visual identity already captured (F-69, F-70).
- Baseline: present. Docs command: declared. Plugin notes delivered: none. `CLAUDE.md`: 563 words, ~3.6k tokens always loaded.

## 2026-10-04 - F-79 Subjects and topics back office
- Lesson 1 (plugin: agile [generic]): the app-host check of `feature-build` step 12 and the database rule of `workflow.md` disagree; resolved in the item by asking the owner once. Cause read from the two texts, not run. Went to the plugin notes table above.
- Nothing else kept: the pinned page and menu lists failing when a page is added worked as guards, and a heredoc write was refused by the existing rule. Build not run (only Markdown changed); architecture tests run by hand because they read `docs/glossary.md`.

## 2026-10-04 - Sync with agile@canary 0.7.0
From 0.4.0 to 0.7.0, run on a clean main; no item `building` or `validating` (F-51 to F-89 `approved` or `refining`, each in its worktree), only docs touched.
- Copied (never edited, plugin changed): template `infra.md` (`Update source (linux)`, `Beta channel`, `## Code signing`, desktop only), `docs/agile/workflow.md` and `workflow.pt-BR.md` (0.7.0: beta channel, Linux updates, code signing, `/agile:publish --beta`); the pt-BR/en links between the two stayed correct.
- Left alone: `.claude/rules/agile/git.md` and `docs/agile/profile.md` (edited, upstream unchanged); all manual build files (no upstream change).
- Glossary (owner: 2A): column and `Technical terms:` link lines already present in every done document (103 checked); the technical-term sweep of the docs was not run and became F-91 (board 134, idea).
- Missing capabilities (owner: 3A): no new item. `## Cloud accounts` is in F-68 (change note v2); visual identity is F-70; the DocGen tool catalogue was delivered by F-49 (F-69 closed as already delivered).
- ⏳ plugin: agile [generic] — `sync.js plan` still lists the DocGen tool catalogue under `missingCapabilities` while `tools/Simulab.DocGen/ToolCatalogueDoc.cs` and `docs/architecture/tools.md` exist (F-49); the check should look for the delivered files, not only the template. It raised the duplicates F-61 and F-69 in earlier syncs.
- Baseline: present. Docs command: declared. Plugin notes delivered: none. `CLAUDE.md`: 563 words, ~3.6k tokens always loaded. Build and suite not run: only Markdown changed.

## 2026-10-04 - F-74 Notice subjects per edition
- Lesson 1 (plugin: agile [generic]): the `frontend` agent cannot run a build or a test. Evidence: its report said "this session has no command tool", and the main session's build then found two failures it could not have seen (`ExamEditionFormTests` expecting three section cards, and `FieldLengthTests` refusing a typed `MaxLength="100"` in `UiGallery.razor`). Recorded as a plugin note, status ⏳.
- Lesson 2 (plugin: agile [profile: modular-monolith]): the item named routes and a column that disagreed with the code. Evidence: `system-design` found `CatalogEndpoints.cs:16` (`/api/v1/catalog`) against the item's `/api/v1/exams/...`, and `docs/glossary.md:26` (`Position` is Cargo) against the item's `position` column; both fixed in `## Decisions`. Recorded as a plugin note, status ⏳.
- Not kept: the `DocSetTests.Generate_TwiceWithNoCodeChange_GivesTheSameText` failure in the first `gate ship` run (passed alone, 4/4, and in the second full run; cause not verified, one occurrence); a subagent launched before the file it was told to read existed; test labels shorter than the 2-character minimum.
- Build and suite not run: only Markdown changed.

## 2026-10-05 - B-22 Data export dialog test fails now and then
- Lesson 1 (project rule, `project.md`): a stress loop of parallel test processes is valid only on a copy of the build output under the worktree's `bin/`. Evidence: on `bin/Debug` the Stop hook printed `MSB3061 (0 -> 12)` and every run ended `The library 'hostpolicy.dll' required to execute the application was not found`; on a copy outside the repository each run had `Failed: 13` (`Expected directory not to be <null> because the tests must run inside the repository.`); on `bin/b22-stress` 40 of 40 runs gave `Passed: 1044`.
- Lesson 2 (project rule, `project.md`, line widened): the refinement grep covered only `Click()` then `NavigationManager.Uri`. Evidence: the first valid loop failed 2 of 40 on `AccountErasureTests.cs:75` (`Expected dialogs.Find("button.app-erase-confirm").HasAttribute("disabled") to be False, but found True.`), a `Type()` then attribute read that 30 earlier runs missed; the final grep found two more `Click()` then `Uri` reads with a blank line between (B-23). Sweep of the pattern: fixed in B-22 (six tests) and captured as B-23 (board #135).
- Lesson 3 (plugin: agile [generic]): not a new note; it is the open note on the Stop gate rebuilding a worktree under a background loop (F-48), now with a second occurrence. Row in the table updated: From `F-48, B-22`.
- Build and suite not run: only Markdown changed.

## 2026-10-05 - F-51 Import the municipal guard taxonomy
- Lesson 1 (bug, B-24, board #136): the first `gate.js ship` run was `agile gate RED` with 24 of 408 `Simulab.Identity.Tests` failing; `dotnet test tests/Modules/Identity/Simulab.Identity.Tests` alone gave `Passed: 408`, and the second full run was `agile gate GREEN` (2198 tests, 141 s). Cause not verified: the failure messages were not read. The `docs/infra.md` premise from B-19 (a red full run is a real failure) is in doubt until B-24 measures it.
- Not kept: the worktree folder held by something outside the session at ship (the rename probe failed twice until the owner closed what was open) and an untracked `.contextkeeper/` that made `git worktree remove` refuse; cause not verified, one occurrence.
- Build and suite not run: only Markdown changed.

## 2026-10-05 - F-54 Shared client rate limits across Api instances
- Lesson 1 (project rule, `project.md`): restart or stop one app host resource with `aspire resource <name> restart|stop --apphost <csproj>`. Evidence: the command printed `Resource 'api' restarted successfully` and the `Simulab.Api` process id went from 64604 to 98788; two clicks in the dashboard through the browser pane opened the wrong menu.
- Lesson 2 (plugin: agile [generic]): `gate.js stop` prints no test projects or totals. Row added to the plugin notes table, cause not verified.
- Not kept: starting the preview in the same step as the `launch.json` edit that a hook blocked (a one-off slip; `worktrees.md` already says to point the launch configuration at the absolute path), and the open note that `feature-build` step 12 contradicts `workflow.md` on starting a database (second occurrence; the owner said yes in this item, the existing row covers it).
- Build and suite not run: only Markdown changed.

## 2026-10-06 - B-24 Identity tests fail in bulk in one full gate run
- Lesson 1 (plugin: agile [generic]): the ship gate keeps only the names of failing tests, so the F-51 evidence (24 identical Npgsql timeouts) was gone. Row added to the plugin notes table.
- Lesson 2 (project rule, `project.md`): a test project that starts a `WebApplicationFactory` synchronously relies on the thread-pool minimum set in `tests/Directory.Build.props`; keep the guard `ThreadPoolMinimumTests` in every such project. Evidence: with the option removed from the `runtimeconfig.json` files, 4 of 12 projects fail their guard; 40 process runs under load were green with it.
- Lesson 3 (plugin: agile [generic]): not a new note; the open note on the Stop gate rebuilding a worktree under a background loop (F-48, B-22) had a third occurrence: it deleted `Simulab.Api.deps.json` from `bin/Debug` under the first full-suite round (`Failed: 115` Identity, `Failed: 104` Catalog) and the round had to be repeated.
- Build and suite not run: only Markdown changed.

## 2026-10-06 - F-53 Force a password change at first sign-in
- Lesson 1 (project rule, `project.md`): a test named for an event or a state change asserts that event or state itself. Evidence: the independent review found `Change_UnknownChallenge_IsInvalidAndRecordsTheFailure` ending in `user.Id.Should().NotBeEmpty()`, which cannot fail, and four more cases that checked only the error code; each now asserts exactly one `SignInFailed` with reason `ChallengeInvalid` (59 tests of the two new classes green after the fix). Sweep of the existing tests for the same pattern: not run, a hand search cannot tell a dead assertion from a live one; the review of each new item reads for it.
- Lesson 2 (plugin: agile [generic]): `literal-guard.js` let a `python` heredoc write a test file. Evidence: the command exited 0 and `git diff --stat` showed 76 insertions in `SeedAdminTests.cs`; the next command, a `sed -i`, was blocked. Row added to the plugin notes table. The file was checked afterwards (build and diff), the rule was still broken.
- Lesson 3 (plugin: agile [generic]): refinement did not check BR3 ("does not clear the failure count") against the shared `SecondFactor.VerifyAsync`, which clears it on a right code (`SecondFactor.cs:42`). Found by the build design pass and settled by change note 1. Row added to the plugin notes table.
- Build and suite not run: only Markdown changed.

## 2026-10-06 - Sync with agile@canary 0.16.0
- From 0.7.0 to 0.16.0. Copied: `docs/agile/templates/infra.md`, `docs/agile/templates/project-claude.md`, `docs/agile/workflow.md`, `docs/agile/workflow.pt-BR.md`. Merged without conflicts: `docs/agile/profile.md` (new "Deploy recipe (Azure Container Apps + Aspire)" section added; "Simulab specifics" kept). Left alone: `.claude/rules/agile/git.md` (edited, upstream unchanged), build files (no upstream change).
- Baseline: present. Docs command: already declared (`dotnet run --project tools/Simulab.DocGen`). Plugin notes marked: none delivered.
- Proposed: an item adding `## Cloud accounts` to `docs/infra.md` (to be captured as an idea); `/agile:identity` and the DocGen tools catalogue (F-61) declined for now.
- Build and suite not run: only Markdown changed.
