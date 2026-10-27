---
feature: F-184
epic: Data type controls
status: idea
board: 254
version: 1
---
# Choice controls: switch, segmented, yes/no, level, language

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: idea (2026-10-27)
- Stopped at: Captured by the breakdown of the epic data-type-controls.
- Next action: `/agile:refine F-184` with `/agile:screen` for the level control (owner).
- Waiting on: nothing

## Summary
The kit gains `AppSwitch` (a setting that applies at once), `AppSegmentedField` (2–4 short options), `AppYesNoField` (`bool?`: yes, no, not set), `AppLevelField` and `AppLevelBadge` (dots with the level name, filter mode with many), `AppBoolText`, `AppBoolColumn` and `AppLanguageField`, in the gallery. The screens move to them: difficulty in `QuestionForm` and the `Questions` filter, the true/false key, and the checkboxes that are settings applied at once. The difficulty label and dot gauge of F-154 are delivered here. Catalog: `docs/design/data-type-controls.md`, section 2.4.

## Start
- Depends on: nothing.
- Waits on (to start): nothing.
- Needed to validate: nothing.
- Suggested path: `/agile:refine` with `/agile:screen` (level, segmented, yes/no) → `/agile:build`.
- Parallel with: F-183.

## Decisions
- 2026-10-27 — Epic Data type controls — breakdown of the epic — owner
- 2026-10-27 — The level shows dots with the level name — owner
- 2026-10-27 — Takes the difficulty gauge of F-154 — owner, similar-items card of the epic
