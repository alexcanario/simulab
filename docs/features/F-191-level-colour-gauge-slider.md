---
feature: F-191
epic: Data type controls
status: idea
board: 261
version: 1
---
# Level colour ramp, gauge and slider

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: idea (2026-10-29)
- Stopped at: Captured from the mockup drawn with the owner.
- Next action: Refine it after F-184 (owner).
- Waiting on: F-184

## Summary
The level controls of F-184 (`AppLevelField`, `AppLevelBadge`: dots and the level name, 1 to 5) gain the difficulty look the owner asked for:
- a colour per level on the dots, the badge and the filter chips, along one ramp from the success tone through the warning tone to a true red; the number and the name stay on screen, so colour never speaks alone;
- `AppLevelGauge`, a half ring whose colour changes smoothly with the level, with a needle, the number and the name, and a compact variant for tables and cards;
- a ramp slider, a potentiometer-style fader cap over the gradient track with ticks at 1 to 5, in sync with the field and the gauge.

Mockup: [F-191-level-visuals.html](mockups/F-191-level-visuals.html) (light and dark side by side, with the field states, the badge in a table and the filter mode). Today the question difficulty is an `AppSelectField int?` in `QuestionForm` (levels 1 to 5, see `QuestionLimits`).

## Start
- Depends on: F-184 (`AppLevelField` and `AppLevelBadge`, the dots and the name).
- Waits on (to start): nothing.
- Needed to validate: nothing.
- Suggested path: `/agile:refine` with `/agile:screen` → `/agile:build`.
- Parallel with: unknown — settled at `/agile:refine`.
- Open points for the refinement:
  - the red end of the ramp in the dark theme is `#CD4747` (`errorLighten` of the light theme): the dark `error` is salmon (`#FF9C94`), which does not read as red. It reads under 3:1 on the dark surface, so the contrast tests decide whether the mark's darkened edge is enough or the identity gets a new token;
  - the kit's `success` is teal (`#1A9E8C`), not a pure green: a greener start of the ramp also asks for a token;
  - the level names ("Muito fácil" to "Muito difícil") are proposed in the mockup; the resource files decide the final words in the three locales.
