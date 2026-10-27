---
feature: F-181
epic: Data type controls
status: idea
board: 251
version: 1
---
# Number controls: integer, decimal, score and year

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: idea (2026-10-27)
- Stopped at: Captured by the breakdown of the epic data-type-controls.
- Next action: Refine it after F-168 merges (owner).
- Waiting on: F-168

## Summary
The kit gains `AppNumberFormat` (parse and format in the user's culture, only the culture's decimal separator), `AppIntegerField`, `AppDecimalField` (`Decimals` required), `AppScoreField`, `AppYearField`, the formatters `AppNumberText` and `AppScoreText` and the column `AppScoreColumn`, all in the gallery. The screens move to them: question count, points per question, objective and essay cut-offs and essay max score (F-168 types them as `string` today) and the notice year in `ExamEditionForm` and the `CatalogSearch` filter. Number inputs use `type="text"` with `inputmode`, never `type="number"`. Catalog: `docs/design/data-type-controls.md`, section 2.2.

## Start
- Depends on: F-168 (owns `ExamEditionForm` and `NoticeSubjectDialog`).
- Waits on (to start): nothing.
- Needed to validate: nothing.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: F-185.

## Decisions
- 2026-10-27 — Epic Data type controls — breakdown of the epic — owner
- 2026-10-27 — Decimal input accepts only the culture separator — owner
