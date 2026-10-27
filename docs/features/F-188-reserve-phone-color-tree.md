---
feature: F-188
epic: Data type controls
status: idea
board: 258
version: 1
---
# Reserve controls: phone, tax id, color, tree pick, slider, image, user

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: idea (2026-10-27)
- Stopped at: Captured by the breakdown of the epic data-type-controls.
- Next action: Refine it after F-183, with `/agile:screen` for the tree pick and the image field (owner).
- Waiting on: F-183

## Summary
The kit gains the rest of the catalog, shown only in the gallery until a screen uses them: `AppPhoneField` (country and mask, stored E.164), `AppTaxIdField` (CPF / NIF mask and check digit, partly hidden on display), `AppColorField` (identity palette first, contrast check), `AppTreePickField` (area → subject → topic, breadcrumb display), `AppSliderField` (always with a number input), `AppImageField` (preview, alt text required) and `AppUserLookup`. Catalog: `docs/design/data-type-controls.md`, sections 2.1, 2.2, 2.4 to 2.6.

## Start
- Depends on: F-183 (`AppCodeField` pattern for masked text).
- Waits on (to start): nothing.
- Needed to validate: nothing.
- Suggested path: `/agile:refine` with `/agile:screen` (tree pick, image) → `/agile:build`.
- Parallel with: F-187.

## Decisions
- 2026-10-27 — Epic Data type controls — breakdown of the epic — owner
- 2026-10-27 — Built now, before any screen needs them — owner
