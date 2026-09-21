# Retro log

One entry per retro: date, item, lessons and where each one went.

## Plugin notes — status
Every note tagged `plugin` in this log, and what happened to it in agile@canary (`D:\dev\agile-canary`). ✅ = implemented in the plugin · ⏳ = still open. A new `plugin` note gets a row here with ⏳.

| Status | Note | From | Plugin version (commit) |
|---|---|---|---|
| ✅ | Model per agent and activity | Bootstrap session | 0.0.7 (`97053a3`) |
| ✅ | UI standards as a core rule, quiz questions, UI kit before the first screen | Bootstrap session | 0.0.7 (`97053a3`) |
| ✅ | Bootstrap quiz gaps: trials and promo codes, closing question, reading an existing code base | Bootstrap session | 0.0.7 (`97053a3`) |
| ✅ | PascalCase for constants and `static readonly` in the template `.editorconfig` | Sync to 0.0.7 | 0.0.10 (`b7128e5`) |
| ✅ | A command to refresh the project copies (`/agile:sync`) | Sync to 0.0.7 | 0.0.8 (`0361053`) |
| ✅ | Parallel work on request (`/agile:build <id> --worktree`) | Owner question | 0.0.9 (`8ab237e`) |
| ✅ | App host left running: gate names the locked build output; build and ship stop what Claude started | F-1 | 0.0.10 (`b7128e5`) |
| ✅ | Commit on the wrong branch: branch check before each commit and a guard hook on the main branch | F-1 | 0.0.10 (`b7128e5`) |
| ✅ | Hanging tests: hang timeout in the gate, hung tests reported by name | F-1 | 0.0.10 (`b7128e5`) |
| ✅ | Screen check before the coverage table: library state styles in both themes; keyboard steps stay in the validation script | F-2 | 0.0.11 (`a9babf4`) |
| ✅ | The refinement package question must cover the packages the tests need, not only the production ones | F-3 | 0.0.11 (`a9babf4`) |
| ✅ | A building block that reads an app-host connection string is exercised through the app host in the item that creates it, not only against a test container | F-4 | 0.0.15 (`b221360`) |
| ✅ | At ship, check that every decision naming a file ("... in X.md") is reflected in that file | F-4 | 0.0.15 (`b221360`) |
| ✅ | Guard hook: refuse a commit on the main branch that touches the file of an item that is not done | F-12 | 0.0.19 (`c48c34e`) |
| ✅ | Retro commits carry no code or tests; a code lesson becomes an item | F-12 | 0.0.19 (`c48c34e`) |
| ✅ | Technical terms used with the owner go into a glossary section in the same step | F-8 | 0.0.28 (`6f1a488`) |
| ✅ | bUnit: after a click whose handler awaits, assert with `WaitForAssertion` | F-8 | 0.0.29 (`afe7431`) |
| ✅ | A per-test-class `WebApplicationFactory` clears its Npgsql pool on dispose | F-8 | 0.0.29 (`afe7431`) |
| ✅ | Gate output is saved whole to a file; the gate prints the new warnings in its last lines | B-7 | 0.0.31 (`b6af246`) |
| ✅ | Anonymous endpoints check every input limit before the account lookup | B-7 | 0.0.31 (`b6af246`) |
| ✅ | Validation-script terminal steps in Bash and PowerShell, run before handing over | B-4 | 0.0.31 (`b6af246`) |
| ✅ | A premise about how a library stores or protects data is verified in its source or docs during refinement | F-11 | 0.0.36 (`cfedd49`) |
| ✅ | In a multi-step sign-in, only the last step clears the failure count | F-11 | 0.0.36 (`cfedd49`) |
| ✅ | Refinement checks package versions against the registry; build re-checks before adding | F-11 | 0.0.36 (`cfedd49`) |
| ✅ | A "never leave zero X" rule is judged before and after, not only after | F-10 | 0.0.35 (`7303a61`) |
| ✅ | A theme's colour tokens get a contrast test over the palette | F-10 | 0.0.35 (`7303a61`) |
| ✅ | A Blazor Server dialog is driven from the browser pane in one call | F-10 | 0.0.35 (`7303a61`) |
| ✅ | Never change state in an app host Claude did not start | B-4 | 0.0.31 (`b6af246`) |
| ✅ | Blazor Server: request data a circuit needs is read in `App` and passed to the interactive root | B-4 | 0.0.31 (`b6af246`) |
| ✅ | Kit parameters whose value depends on the page's meaning have no default (`[EditorRequired]`) | B-5 | 0.0.32 (`33dfa1e`) |
| ✅ | Source and test files are written with the Write/Edit tools, never through a script's string literals | B-6 | 0.0.32 (`33dfa1e`) |
| ✅ | A colour or contrast bug is checked in both themes and on every surface the component sits on | B-6 | 0.0.32 (`33dfa1e`) |
| ✅ | ASP.NET Core middleware resolves every `InvokeAsync` parameter on every request, even inside an untaken branch — resolve an optional heavy dependency from `HttpContext.RequestServices` instead | F-5 | 0.0.26 (`557c7ff`) |
| ✅ | Aspire's `AddRedis()` secures the local container with TLS and a password by default; a plain `ConnectionMultiplexer.Connect` cannot trust its dev certificate and hangs until the socket times out — use the Aspire client integration (`AddRedisClient`) instead | F-5 | 0.0.26 (`557c7ff`) |
| ✅ | OpenIddict issues an encrypted (JWE) access token once an encryption certificate is registered, not a plain signed JWT; a client should never try to decode its own claims out of the token — ask the resource server for them instead | F-5 | 0.0.26 (`557c7ff`) |
| ✅ | Central Package Management's transitive pinning can float a shared package (e.g. `Aspire.Hosting`) to a higher version than a sibling package (`Aspire.Hosting.Testing`) pins, because another package elsewhere in the solution needs the newer one. An obsolete-API investigation must check the resolved version in `obj/project.assets.json`, not the pinned version in `Directory.Packages.props` | B-1 | 0.0.26 (`557c7ff`) |
| ✅ | `[generic]` A coverage row maps a criterion to a test that goes through the path the user reaches; a method written for a criterion with no caller in production code is a gap, not coverage | B-3 | 0.0.27 (`641d463`) |
| ✅ | `[stack: aspire]` The ServiceDefaults template disables retries for unsafe HTTP methods (`Retry.DisableForUnsafeHttpMethods()`): a retried POST replays single-use tokens and sends emails twice | B-3 | 0.0.27 (`641d463`) |
| ✅ | `[stack: blazor-server]` State that changes during a session (rotating tokens, permissions) lives in a server-side store keyed by an id in the cookie, never in the cookie: a circuit cannot rewrite it | B-3 | 0.0.27 (`641d463`) |
| ✅ | `[stack: blazor]` A Razor attribute mistake compiles: a string parameter without `@` is literal text, and a wrong generic parameter name only fails at runtime. Every new page gets a bUnit test that renders it | F-9 | 0.0.35 (`7303a61`) |
| ✅ | `[generic]` A premise taken from an earlier item's file is verified against the code and against the items that touched it since; a bug can have moved the behaviour | F-9 | 0.0.35 (`7303a61`) |
| ✅ | `[profile: modular-monolith]` A row a request must not lose is staged on the caller's own `DbContext`; the shared table is mapped into it with `ExcludeFromMigrations()` | F-13 | 0.0.36 (`cfedd49`) |
| ✅ | `[generic]` When an effect moves out of the request, re-read every test that asserted it: "nothing was sent" passes for free once the effect is deferred | F-13 | 0.0.36 (`cfedd49`) |
| ✅ | `[stack: ef-core]` A `SaveChanges` that fails after `Remove` leaves the entry `Deleted`; the next save repeats the DELETE instead of writing the error | F-13 | 0.0.36 (`cfedd49`) |
| ⏳ | `[stack: .NET]` A warning count is quoted only from the gate or a `--no-incremental` build; an incremental build hides the warnings of unchanged projects | F-14 | — |

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
