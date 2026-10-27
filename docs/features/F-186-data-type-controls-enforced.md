---
feature: F-186
epic: Data type controls
status: idea
board: 256
version: 1
---
# Data type controls enforced by the build

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: idea (2026-10-27)
- Stopped at: Captured by the breakdown of the epic data-type-controls.
- Next action: Refine it after F-181 to F-185 (owner).
- Waiting on: F-181, F-182, F-183, F-184, F-185

## Summary
Architecture tests refuse, outside the kit, `InputType.Number`, `InputType.Email`, `InputType.Url` and `InputType.Telephone` on `AppTextField`, a numeric `TValue` on `AppTextField`, and the MudBlazor input components the kit wraps (`MudNumericField`, `MudDatePicker`, `MudDateRangePicker`, `MudTimePicker`, `MudSwitch`, `MudSlider`, `MudRating`, `MudToggleGroup`, `MudColorPicker`). `ui-project.md` gains the line "pick the control by data type from `docs/design/data-type-controls.md`" and the gallery a "Data types" index. Catalog: section 4.

## Start
- Depends on: F-181, F-182, F-183, F-184, F-185.
- Waits on (to start): nothing.
- Needed to validate: nothing.
- Suggested path: `/agile:autopilot F-186` (small and clear).
- Parallel with: none.

## Decisions
- 2026-10-27 — Epic Data type controls — breakdown of the epic — owner
- 2026-10-27 — Comes before the reserve features F-187 and F-188 — the tests need only the migrated screens
