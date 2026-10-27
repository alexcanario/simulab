---
feature: F-182
epic: Data type controls
status: idea
board: 252
version: 1
---
# Duration and numeric range controls

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: idea (2026-10-27)
- Stopped at: Captured by the breakdown of the epic data-type-controls.
- Next action: Refine it after F-181 (owner).
- Waiting on: F-181, F-168

## Summary
The kit gains `AppDurationField` (one `TimeSpan?` control, hours and minutes), `AppDurationText` ("4 h 30 min"), `AppDurationColumn` and `AppNumberRangeField` (two inputs under one label, min ≤ max), in the gallery. The screens move to them: the exam duration of F-168, typed today as two `int?` fields (hours, minutes), and the answer size min/max of `RubricKeySection`. Catalog: `docs/design/data-type-controls.md`, sections 2.2 and 2.3.

## Start
- Depends on: F-181, F-168.
- Waits on (to start): nothing.
- Needed to validate: nothing.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: F-185.

## Decisions
- 2026-10-27 — Epic Data type controls — breakdown of the epic — owner
