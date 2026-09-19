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
| ✅ | ASP.NET Core middleware resolves every `InvokeAsync` parameter on every request, even inside an untaken branch — resolve an optional heavy dependency from `HttpContext.RequestServices` instead | F-5 | 0.0.26 (`557c7ff`) |
| ✅ | Aspire's `AddRedis()` secures the local container with TLS and a password by default; a plain `ConnectionMultiplexer.Connect` cannot trust its dev certificate and hangs until the socket times out — use the Aspire client integration (`AddRedisClient`) instead | F-5 | 0.0.26 (`557c7ff`) |
| ✅ | OpenIddict issues an encrypted (JWE) access token once an encryption certificate is registered, not a plain signed JWT; a client should never try to decode its own claims out of the token — ask the resource server for them instead | F-5 | 0.0.26 (`557c7ff`) |
| ✅ | Central Package Management's transitive pinning can float a shared package (e.g. `Aspire.Hosting`) to a higher version than a sibling package (`Aspire.Hosting.Testing`) pins, because another package elsewhere in the solution needs the newer one. An obsolete-API investigation must check the resolved version in `obj/project.assets.json`, not the pinned version in `Directory.Packages.props` | B-1 | 0.0.26 (`557c7ff`) |
| ✅ | `[generic]` A coverage row maps a criterion to a test that goes through the path the user reaches; a method written for a criterion with no caller in production code is a gap, not coverage | B-3 | 0.0.27 (`641d463`) |
| ✅ | `[stack: aspire]` The ServiceDefaults template disables retries for unsafe HTTP methods (`Retry.DisableForUnsafeHttpMethods()`): a retried POST replays single-use tokens and sends emails twice | B-3 | 0.0.27 (`641d463`) |
| ✅ | `[stack: blazor-server]` State that changes during a session (rotating tokens, permissions) lives in a server-side store keyed by an id in the cookie, never in the cookie: a circuit cannot rewrite it | B-3 | 0.0.27 (`641d463`) |

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
