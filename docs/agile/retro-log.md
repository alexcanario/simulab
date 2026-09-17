# Retro log

One entry per retro: date, item, lessons and where each one went.

## 2026-09-17 — Bootstrap session (no item)
Raised by the owner after the bootstrap, approved the same day.

| # | Lesson | Kind | Where it went |
|---|---|---|---|
| 1 | The model used by each agent and activity should be chosen on purpose, not inherited by accident | Project setting | `CLAUDE.md`, section "Models" |
| 2 | Simulae's screens drifted apart (hover on some tables only, two ways to edit an item, two icon families: 258 Filled vs 58 Outlined, 2 shared components for 71 pages). A written rule alone does not hold a UI standard | Project rule + build check | `.claude/rules/agile/ui.md`; feature F-1 "UI kit and gallery" (kit, `AppIcons`, gallery, architecture tests) |
| 3 | `plugin` — agents and skills in agile@canary declare no `model:`; the `reviewer` inherits whatever the session runs | Plugin improvement | This log (see below) |

### Plugin notes (`plugin`)
- **Model per agent and activity.** Add `model:` to `agents/reviewer.md` (strongest available) and consider it for skills. Add a "Models" table to `templates/project-claude.md` and a question to the bootstrap quiz (round 7 or a new round): which model for review, for bulk mechanical work, for searches. The project override should live in `CLAUDE.md` so the owner can change it without touching the plugin.
- **UI standards as a core rule.** Consider a `rules/core/ui.md` with the stack-neutral parts (one behavior everywhere, one icon family behind semantic names, one way to edit an item, destructive-action confirmation, list states, action vocabulary) and a quiz question in round 5 (icon family, edit pattern). Consider a "UI kit and gallery" step in the web profiles before the first screen is built.
- **Bootstrap quiz gaps found in this session.** Entitlements were reduced to "plans with features" and the owner had to raise time-bound grants and promo codes; taxonomy shape came up only after the summary. Suggestion: in round 3, when the brief mentions plans, ask about trials, time-bound grants and usage limits; add a closing question "which domain concept worries you most?" before the summary.

## 2026-09-17 — Plugin notes delivered; project brought to agile@canary 0.0.7
- The three `plugin` notes above went into agile@canary **0.0.7** (commit `97053a3` in `D:\dev\agile-canary`, with the owner's authorization): `model:` on the reviewer, core rule `ui`, quiz questions 13b, 23b–23d, 34 and the closing question.
- This project was refreshed by hand from 0.0.4 to 0.0.7 (there is no `/agile:sync` yet): `workflow.md`, templates, profile (Simulab section kept), rules `build-config` and `ui` (core). The Simulab UI choices moved from `ui.md` to `ui-project.md`.
- Build configuration from `templates/dotnet/` adopted: `.editorconfig`, `BannedSymbols.txt`, `global.json`, `.gitattributes`, analyzers in `Directory.Build.props`. `TreatWarningsAsErrors` is now **off**, as the core rule says: the gate fails on new warnings instead.
- Findings fixed, not suppressed: unused using, `Shared` renamed `SharedResources` (CA1716), a test that constructed `JsonSerializerOptions`. Two local, commented pragmas: `Error` (CA1716, Visual Basic keyword only) and `AppJson` (RS0030, the one allowed place).
- `plugin` — the template `.editorconfig` asks `_camelCase` for every private field, constants and `static readonly` included. This project added a PascalCase rule for those two in its own `.editorconfig`. Proposed for `templates/dotnet/.editorconfig` (the owner's file, not edited).

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
- **App host left running.** `feature-build` should stop any app host Claude started (preview) before ending the turn; `gate.js` could detect MSB3027/MSB3021 "file is locked" and report "close the app host" instead of a plain build failure.
- **Commit on the wrong branch.** Skills should check `git branch --show-current` right before each commit; consider a PreToolUse hook that refuses `git commit` on the main branch outside `/agile:ship` or an explicit authorization.
- **Hanging tests.** `gate.js` (stop and ship) should run `dotnet test` with `--blame-hang-timeout` so a hung test fails in seconds with its name instead of blocking the turn.
