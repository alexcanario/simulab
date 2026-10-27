---
feature: F-187
epic: Data type controls
status: idea
board: 257
version: 1
---
# Reserve controls: money, percentage, time, date-time, month

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: idea (2026-10-27)
- Stopped at: Captured by the breakdown of the epic data-type-controls.
- Next action: Refine it after F-181 (owner).
- Waiting on: F-181

## Summary
The kit gains the controls of types the app does not hold yet, shown only in the gallery until a screen uses them: `AppMoneyField` (`decimal` + currency, currency required, never `double`), `AppMoneyText`, `AppMoneyColumn`, `AppPercentField` (stored 0–100), `AppPercentText`, `AppPercentColumn`, `AppTimeField` (`TimeOnly`), `AppDateTimeField` (`DateTimeOffset`, stored UTC, time zone shown), `AppDateTimeText`, `AppDateTimeColumn` and `AppMonthField`. Catalog: `docs/design/data-type-controls.md`, sections 2.2 and 2.3.

## Start
- Depends on: F-181 (`AppNumberFormat`).
- Waits on (to start): nothing.
- Needed to validate: nothing.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: F-188.

## Decisions
- 2026-10-27 — Epic Data type controls — breakdown of the epic — owner
- 2026-10-27 — Built now, before any screen needs them — owner
- 2026-10-27 — Percentage stored 0–100 — owner
