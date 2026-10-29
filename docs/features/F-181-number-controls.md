---
feature: F-181
epic: Data type controls
status: approved
Autopilot: approved
board: 251
version: 1
---
# Number controls: integer, decimal, score and year

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: approved (2026-10-29)
- Stopped at: Refined and approved by autopilot (`--assume`); the build has not started.
- Next action: Build it in the worktree (autopilot).
- Waiting on: nothing

## Summary
The kit gains `AppNumberFormat` (parse and format in the user's culture, only the culture's decimal separator), `AppIntegerField`, `AppDecimalField` (`Decimals` required), `AppScoreField`, `AppYearField`, the formatters `AppNumberText` and `AppScoreText` and the column `AppScoreColumn`, all in the gallery. The screens move to them: question count, points per question, objective and essay cut-offs and essay max score (F-168 types them as `string` today) and the notice year in `ExamEditionForm`. Number inputs use `type="text"` with `inputmode`, never `type="number"`. Catalog: `docs/design/data-type-controls.md`, section 2.2.

## Start
- Depends on: F-168 (done; owns `ExamEditionForm` and `NoticeSubjectDialog`).
- Waits on (to start): nothing.
- Needed to validate: nothing.
- Model: opus (lane jussara; the kit and two catalog forms change).
- Suggested path: `/agile:autopilot F-181 --assume --worktree`.
- Parallel with: F-185.

## Goal
Each kind of number has one named control, one formatter and one column in the kit, so a page stops holding a number as `string` and repeating its parse, its format and its error, and the build of F-186 can refuse `type="number"`.

## What already exists
- `AppTextField<TValue>` (`src/Hosts/Simulab.Web/Components/Ui/AppTextField.razor`): label above, required mark, hint or error, `InputMode`, `Pattern`, `InputAttributes`, `EndContent` (F-183). No numeric field exists in the kit and no `MudNumericField` is used anywhere.
- `ScoreText` (`Components/Pages/Catalog/ScoreText.cs`): the page-level helper that formats a score with two decimals and parses it in the current culture (`NumberStyles.Number`, which also accepts the group separator).
- `NoticeSubjectDialog`: question count and points per question are `AppTextField<string>` with `InputType.Number`, parsed by the page (`TryParseCount`, `ScoreText.TryParse`), errors `QuestionCountError` and `PointsError`.
- `ExamEditionForm`: objective cut-off, essay max score and essay cut-off are `AppTextField<string>` parsed by `ParsedScore`; the notice year is an `AppTextField<int?>` with `InputType.Number`; duration hours and minutes are number fields that F-182 replaces.
- `CatalogSearch`: the notice year filter is an `AppSelectField<int?>` of the years that exist in the data, not a free number (see Decisions).
- `AppNumberColumn<TItem,TProperty>` (right-aligned `PropertyColumn`) and `FileSizeText`; `CatalogLimits` holds the bounds (question count 1..500, points 0.01..100, score 0.01..1000, `ScoreDecimalPlaces` 2, notice year from 1990, 4 digits).

## Users and use cases
- UC1 A curator types the question count of a notice subject in a field that takes whole numbers only and shows the unit.
- UC2 A curator types points per question, the objective and essay cut-offs and the essay maximum score with the decimal separator of their language (`,` in pt-BR and pt-PT, `.` in en) and sees the value formatted with the exam's decimal places when leaving the field.
- UC3 A curator types the notice year of an edition in a 4-digit field with no thousands separator.
- UC4 A developer finds every number control in every state in the gallery `/dev/ui`.

## Business rules
- BR1 `AppNumberFormat` parses and formats in the culture of the user (`CultureInfo.CurrentCulture`). The decimal separator is the only separator accepted: the group separator and the other language's decimal separator are a parse error. A leading sign is accepted; spaces around the text are trimmed; blank means no value (`null`).
- BR2 `AppIntegerField` (`int?`): whole numbers only; optional `Min`, `Max`, `Unit` (suffix) and `Stepper` (minus and plus buttons); `type="text"`, `inputmode="numeric"`.
- BR3 `AppDecimalField` (`decimal?`): `Decimals` is `[EditorRequired]` (the meaning decides it); `inputmode="decimal"`; on leave the value is rounded to `Decimals` and shown with them (`60,00`), never while typing; a text with the wrong separator or letters is a parse error.
- BR4 `AppScoreField` is `AppDecimalField` with the unit "points" (`pts`); `Decimals` stays required and comes from the exam rule (`CatalogLimits.ScoreDecimalPlaces` today).
- BR5 `AppYearField` (`int?`): 4 digits at most, shown plain ("2026", never "2.026"); default range 1950 to the current year + 2 (`TimeProvider`); `Min` and `Max` override it.
- BR6 A value that does not parse keeps the text the user typed, shows a localized error (the page's own `Error` wins) and reports it through `InvalidChanged`; the bound value is not changed, so a page never saves a stale number: it treats `Invalid` as blocking.
- BR7 `AppNumberText` shows an `int?` or `decimal?` with the culture and a fixed number of decimals, "—" for none; `AppScoreText` shows it with the unit ("2,5 pts"); `AppScoreColumn` is `AppNumberColumn` with `Decimals` required.
- BR8 Every control keeps the shared contract of the catalog (section 3): `Id` and `Label` required, label above, required mark, hint, error, description announced, value never cleared on an error.
- BR9 The server rules do not change: the Api stays the authority for every value (range, decimals); the page keeps its range messages and codes, taken from `CatalogLimits`.

## Screens and API
- No route or endpoint changes; no API contract changes.
- Kit: `AppNumberFormat`, `AppIntegerField`, `AppDecimalField`, `AppScoreField`, `AppYearField`, `AppNumberText`, `AppScoreText`, `AppScoreColumn` in `src/Hosts/Simulab.Web/Components/Ui/`.
- Pages: `NoticeSubjectDialog` (question count, points per question), `ExamEditionForm` (objective cut-off, essay max score, essay cut-off, notice year); their layout stays where it is.
- Gallery `/dev/ui`: a section "Numbers" with the controls in every state.

### Wireframe: Notice subject dialog
```text
+----------------------------------------------+
| Notice subject                               |
| Questions *             Points per question *|
| [ 20 ______ ] questions [ 1,50 ___ ] pts     |
|                              [changed]       |
+----------------------------------------------+
```

### Wireframe: Exam edition form, scores
```text
+----------------------------------------------+
| Notice year *   Objective cut-off  Essay max |
| [ 2026 ]        [ 60,00 ] pts      [ 10,00 ] |
| Essay cut-off                                |
| [ 5,00 ] pts                                 |
|                                   [changed]  |
+----------------------------------------------+
```

### Wireframe: Gallery, Numbers
```text
+----------------------------------------------+
| Numbers                                 [new]|
| Integer / Decimal / Score / Year             |
| empty | filled | error | disabled | read-only|
| long label | stepper | unit | table column  |
+----------------------------------------------+
```

## Risks
- A page saves a stale number after a parse error — the control does not call `ValueChanged` on invalid text and reports `InvalidChanged`; both forms block the save on it, with a test — proven — high impact — two-way door.
- A value typed in one culture and re-read in another — the field is parsed and formatted by the current culture only and nothing is stored as text — proven — low impact — two-way door.
- Typed fields change the markup of inputs that tests find by id — ids stay as they are (`notice-subject-question-count`, `notice-subject-points`, ...) — not proven — low impact — two-way door.

## Docs
- flow: new docs/flows/number-controls.md — the controls, their rules, where each is used and the wireframes above.
- flow: update docs/flows/edition-exam-rules.md — the score fields are typed number controls.
- reference: update docs/design/data-type-controls.md — section 2.2 marks the number controls built.

## Acceptance criteria
- AC1 Given the culture pt-BR or en, when `AppNumberFormat` parses and formats, then only that culture's decimal separator is accepted ("1,5" in pt-BR, "1.5" in en), the group separator and the other separator are refused, blank is `null` and a value formats with the asked decimals.
- AC2 Given `AppIntegerField`, when the user types "20", "20,5", "abc" or blank, then the value is 20, a parse error (text kept), a parse error (text kept) and `null`; the input is `type="text"` with `inputmode="numeric"` and shows the `Unit`.
- AC3 Given `AppDecimalField` with `Decimals` 2, when the user types "1,5" and leaves the field, then the value is 1.5 and the text becomes "1,50"; typing "1.5" in pt-BR is a parse error; the input has `inputmode="decimal"` and `type="text"`.
- AC4 Given `AppScoreField`, when it renders, then it behaves as `AppDecimalField` and shows the unit "pts" in the user's language.
- AC5 Given `AppYearField`, when the user types "2026", then the value is 2026 and shows "2026"; "2.026", "26" and a year out of range are errors; the default range is 1950 to the current year + 2.
- AC6 Given a field with a parse error, when it renders, then the typed text is kept, the error is the description of the input (`aria-invalid`, `aria-describedby`), `InvalidChanged` reported true, and `ValueChanged` was not called; the page's own `Error` wins over the field's.
- AC7 Given `AppNumberText` and `AppScoreText`, when they show 1234.5 with 1 decimal, 2.5 as a score and no value, then they read "1.234,5" in pt-BR, "2,5 pts" and "—".
- AC8 Given `AppScoreColumn` in an `AppDataTable`, when a row has 2.5, then the cell is right-aligned and formatted with the column's `Decimals`.
- AC9 Given the gallery `/dev/ui`, when it opens, then a section "Numbers" shows each control in empty, filled, error, disabled, read-only, long label and stepper states.
- AC10 Given `NoticeSubjectDialog`, when the curator types the question count and the points per question, then both are typed controls bound to `int?` and `decimal?`, a parse error blocks the save, and a valid save sends the same request as before.
- AC11 Given `ExamEditionForm`, when the curator types the objective cut-off, the essay max score, the essay cut-off and the notice year, then they are typed controls, the existing checks (cut-off above the objective total, essay cut-off above the maximum, year range) answer the same, and a parse error blocks the save.
- AC12 Given the sources of those two forms, when searched, then none of those fields uses `InputType.Number` or `AppTextField<string>` for a number.
- AC13 All new texts appear in every locale of `UI locales:` (pt-BR, pt-PT, en).

## Criterion → test
| Criterion | Test |
| --- | --- |
| AC1 | pending |
| AC2 | pending |
| AC3 | pending |
| AC4 | pending |
| AC5 | pending |
| AC6 | pending |
| AC7 | pending |
| AC8 | pending |
| AC9 | pending |
| AC10 | pending |
| AC11 | pending |
| AC12 | pending |
| AC13 | pending |

## Decisions
- 2026-10-27 — Epic Data type controls — breakdown of the epic — owner
- 2026-10-27 — Decimal input accepts only the culture separator — owner
- 2026-10-29 — This run is on Sonnet 5.5, not on the lane's opus: a session cannot switch its own model — lane jussara — assumed: model-choice
- 2026-10-29 — `CatalogSearch` keeps its notice year filter as `AppSelectField`: it offers the years that exist in the data (`filters.NoticeYears`), so a free year field would let the user pick a year with no result; the summary's "the filter moves" came from the epic text and is not true of the code — verified in the code — assumed: premise-correction
- 2026-10-29 — Duration hours and minutes of `ExamEditionForm` stay as they are: F-182 replaces them with `AppDurationField` — epic cut — confirmed: docs/design/data-type-controls.md section 5
- 2026-10-29 — A control reports a parse error through `InvalidChanged` and does not call `ValueChanged`; the page treats it as blocking — a number must never be saved stale — assumed: technical
- 2026-10-29 — Range messages stay on the pages (their Api codes and `CatalogLimits`); the controls only take `Min` and `Max` for the stepper and the year range — fewer moving parts, no behaviour change — assumed: technical
- 2026-10-29 — Typed fields are built on `AppTextField<string>` and keep the raw text; they do not use `MudNumericField` — the catalog refuses it (section 4) and it breaks the pt-BR comma — confirmed: docs/design/data-type-controls.md
- 2026-10-29 — `AppNumberText` and `AppScoreText` are components (the unit "pts" is localized) with static `Format` helpers in `AppNumberFormat` for code; `AppScoreColumn` extends `AppNumberColumn` and takes `Decimals` — assumed: technical
- 2026-10-29 — No new package: everything uses MudBlazor and the framework already in the solution — assumed: packages

## Out of scope
- Duration and range controls (F-182); percent and money (F-187); the slider (F-191); the enforcement architecture tests (F-186).
- A free year field in the `CatalogSearch` filter.

## Open questions
- (none)

## Change notes

## Events

- 2026-10-29 assumed — 9 answers
