---
title: Number controls
summary: The kit controls for numbers (integer, decimal, score, year), their text formatters and the score column, what each does to the value and which screens use them
items: [F-181]
---
# Number controls

Technical terms: [glossary](../glossary.md)

## Trigger
A user types a number into a field, or a page shows one: a count of questions, points per question, a cut-off, a maximum score, a notice year. The page picks the control by the kind of number (catalog: `docs/design/data-type-controls.md`, 2.2). Every input is a text input with the numeric keyboard, never `type="number"`.

## Steps
1. `AppIntegerField` (`int?`): whole numbers only; optional `Min`, `Max`, `Unit` and `Stepper` (minus and plus buttons); `inputmode="numeric"`.
2. `AppDecimalField` (`decimal?`): `Decimals` is required, because the meaning decides it; `inputmode="decimal"`. When the user leaves the field the value is rounded to `Decimals` (half away from zero) and shown with them ("1,5" becomes "1,50" in pt-BR); nothing is rounded while typing.
3. `AppScoreField` is `AppDecimalField` with the unit "pts"; `Decimals` still comes from the page (`CatalogLimits.ScoreDecimalPlaces` today).
4. `AppYearField` (`int?`): four digits at most, plain ("2026", never "2.026"), from 1950 to the current year + 2 unless `Min` or `Max` say otherwise.
5. All four are built on `AppNumberInput`, which holds the raw text. `AppNumberFormat` parses and formats in `CultureInfo.CurrentCulture`.
6. `AppNumberText` and `AppScoreText` show a number ("1.234,5") and a score ("2,5 pts") as text, "—" for none; `AppScoreColumn` is `AppNumberColumn` with `Decimals`.
7. Used by `NoticeSubjectDialog` (question count, points per question) and `ExamEditionForm` (objective cut-off, essay maximum, essay cut-off, notice year). The `CatalogSearch` year filter stays a select of the years that exist in the data.

## Rules
- Only the decimal separator of the culture is accepted: `,` in pt-BR and pt-PT, `.` in en. The group separator and the other language's separator are a parse error (`AppNumberFormat.TryParseDecimal`).
- A text that does not parse (or is out of range) keeps what the user typed, shows the control's message (the page's own `Error` wins) and is reported through `InvalidChanged`. `ValueChanged` is not called, so the page never holds a stale number; the two forms treat `InvalidChanged` as blocking the save.
- Range messages of the screens stay with the screens (the Api's codes and `CatalogLimits`); the controls only know `Min` and `Max`.
- The Api stays the authority for every value.

## Wireframe
```text
+----------------------------------------------+
| Notice subject                               |
| Questions *             Points per question *|
| [ 20 ______ ]           [ 1,50 ___ ] pts     |
|                              [changed]       |
+----------------------------------------------+
```

```text
+----------------------------------------------+
| Notice year *   Objective cut-off  Essay max |
| [ 2026 ]        [ 60,00 ] pts      [ 10,00 ] |
| Essay cut-off                                |
| [ 5,00 ] pts                                 |
|                                   [changed]  |
+----------------------------------------------+
```

```text
+----------------------------------------------+
| Numbers                                 [new]|
| Integer / Decimal / Score / Year             |
| empty | filled | error | disabled | read-only|
| long label | stepper | unit | table column  |
+----------------------------------------------+
```

## Errors
- `Number.Invalid` — the decimal field holds a text that is not a number in the culture ("1.5" in pt-BR).
- `Number.InvalidInteger` — the whole-number field holds anything but digits and a sign.
- `Number.Range` — the number is outside `Min` and `Max` (for a year, 1950 to the current year + 2).
