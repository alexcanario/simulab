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

Added 2026-10-29: a first drawing of `AppTimeField`, the twin of `AppDateField` — the reader types `hh:mm` in the culture's format; a picker opened by the clock icon (hour 00–23, minute in steps of 5, "Now" text button, "Ok" primary keycap) is an aid, never the only way. States: empty, focus, error, disabled; light and dark. Mockup: [F-187-time-field.html](mockups/F-187-time-field.html).

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
