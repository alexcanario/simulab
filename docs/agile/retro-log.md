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
