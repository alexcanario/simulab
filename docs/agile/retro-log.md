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
