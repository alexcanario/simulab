---
feature: F-184
epic: Data type controls
status: approved
Autopilot: approved
board: 254
version: 1
---
# Choice controls: switch, segmented, yes/no, level, language

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: approved (2026-10-29)
- Stopped at: Refined and approved by autopilot (`--assume`); the build has not started.
- Next action: Build it in the worktree (autopilot).
- Waiting on: nothing

## Summary
The kit gains `AppSwitch` (a setting that applies at once), `AppSegmentedField` (2–4 short options), `AppYesNoField` (`bool?`: yes, no, not set), `AppLevelField` and `AppLevelBadge` (dots with the level name; `AppLevelFilterField` for the filter mode with many levels), `AppBoolText`, `AppBoolColumn` and `AppLanguageField`, in the gallery. The screens move to them: difficulty in `QuestionForm` and in the `Questions` filter and table, the true/false key of `QuestionForm`, and the five content-language selects. The difficulty label and dot gauge of F-154 are delivered here. Catalog: `docs/design/data-type-controls.md`, section 2.4.

## Start
- Depends on: nothing.
- Waits on (to start): nothing.
- Needed to validate: nothing.
- Model: opus (lane jussara; the kit and the question form change).
- Suggested path: `/agile:autopilot F-184 --assume --worktree`.
- Parallel with: F-183.

## Goal
Each kind of choice has one named control, one text and one column in the kit, so a page stops picking a select, a radio group or a checkbox by habit, and the difficulty shows as dots with its name wherever it is read or set.

## What already exists
- `AppCheckbox`, `AppRadioCards`, `AppSelectField`, `AppMultiPickField`, `AppStatusChip` and `AppNumberColumn` in `src/Hosts/Simulab.Web/Components/Ui/`. No `MudSwitch`, `MudToggleGroup` or `MudSlider` is used anywhere.
- Difficulty: `QuestionForm` `question-difficulty` is an `AppSelectField int?` (`DifficultyOptions`: not set, then "1" to "5"); the `Questions` filter `questions-filter-difficulty` is an `AppMultiPickField` (levels 1 to 5 and "Not set", state in `QuestionListState` and the address by `QuestionListQueryString`); the table column is an `AppNumberColumn`. Limits: `QuestionLimits.DifficultyMin` 1, `DifficultyMax` 5. No resource key names a level.
- True/false key: `question-true-false-key` is an `AppRadioCards<bool?>` with two cards ("True", "False"); a frozen question shows plain text.
- Content language: `AppSelectField<string>` fed by `SupportedCultures.All` in `QuestionForm` (`question-content-language`), `BaseTextForm` (`base-text-content-language`), `ExamForm` (`exam-content-language`), `BaseTexts` (the filter) and `Account` (`account-preferred-language`).
- Checkboxes: every `AppCheckbox` is saved with its form or is local state (sign-up consent, role and permission lists, the correct-answer mark, "I saved the codes", "decorative image"). **No checkbox is a setting applied at once**, so `AppSwitch` has no screen to move to today (see Decisions).

## Users and use cases
- UC1 A curator sets the difficulty of a question by clicking one of five dots and reads its name ("Easy", "Medium"…), or clears it.
- UC2 A curator marks a true/false question as true or false in one segmented control.
- UC3 A curator or a student filters the question list by one or more difficulty levels, including "not set", and reads the difficulty of each row as dots with the name.
- UC4 A user picks a content language in the same control on every form.
- UC5 A developer finds every choice control in every state in the gallery `/dev/ui`.

## Business rules
- BR1 `AppSwitch` (`bool`): a switch with a visible label, `role="switch"`, `aria-checked`; its change takes effect at once, so a page never puts it in a form with a Save button.
- BR2 `AppSegmentedField<TValue>`: 2 to 4 short options in one row, one selected; `role="radiogroup"` with `role="radio"` buttons; arrow keys move the choice; `Value` may be unset unless `Required`.
- BR3 `AppYesNoField` (`bool?`) is the segmented field with the options "Yes" and "No"; `TrueText` and `FalseText` replace the words ("True", "False"); an optional third option for "not set" (`NotSetText`, as in a filter "All"); `AppBoolText` shows the same words and "—" for none; `AppBoolColumn` shows an icon with the text.
- BR4 `AppLevelField` (`int?`): one dot per level from `Min` to `Max`, the chosen level and those below it filled; the level name beside the dots (`Names` is `[EditorRequired]`, one per level, from the page's resources); a clear button when not `Required`; `role="radiogroup"`, each dot a `role="radio"` named "n — name"; arrows, Home and End move the choice. No colour carries meaning: the number and the name stay on screen (the colour ramp, the gauge and the slider are F-191).
- BR5 `AppLevelFilterField`: the filter mode of the level control: one toggle chip per level (mini dots and the name, `aria-pressed`) plus an optional "not set" chip; many may be on.
- BR6 `AppLevelBadge`: the mini dots and the level name as text; none reads "Not set". It is the display of the level and its table cell.
- BR7 `AppLanguageField` (`string?`, a culture code): an `AppSelectField` preset with the options from `SupportedCultures.All` in their native names.
- BR8 Every control keeps the shared contract (catalog section 3): `Id` and `Label` required, label above, required mark, hint, error, description announced.
- BR9 The server rules do not change: the Api stays the authority for the difficulty range, the key and the language.

## Screens and API
- No route or endpoint changes; no API contract changes.
- Kit: `AppSwitch`, `AppSegmentedField`, `AppYesNoField`, `AppLevelField`, `AppLevelFilterField`, `AppLevelBadge`, `AppBoolText`, `AppBoolColumn`, `AppLanguageField` in `src/Hosts/Simulab.Web/Components/Ui/`.
- Pages: `QuestionForm` (difficulty, true/false key, content language), `Questions` (difficulty filter and column), `BaseTextForm`, `BaseTexts`, `ExamForm`, `Account` (content or preferred language).
- Gallery `/dev/ui`: a section "Choices" with the controls in every state.
- Mockup: the level field, badge and filter were drawn with the owner for F-191 ([F-191-level-visuals.html](mockups/F-191-level-visuals.html)); this item builds the same shapes without colour, gauge or slider. The other controls are standard kit patterns drawn by the wireframes.

### Wireframe: Question form, difficulty and true/false key
```text
+----------------------------------------------+
| Difficulty                          [changed]|
| (o)(o)(o)( )( )  Medium            [x clear] |
| From 1 (easiest) to 5 (hardest). Optional.   |
|                                              |
| Correct answer *                    [changed]|
| [ True | False ]   segmented                 |
+----------------------------------------------+
```

### Wireframe: Questions list, difficulty
```text
+----------------------------------------------+
| Difficulty                          [changed]|
| [oo Very easy] [ooo Easy] [oooo Hard] [Not set]|
|                                              |
| Statement            Difficulty     [changed]|
| What is...?          (o)(o)(o)( )( ) Medium  |
+----------------------------------------------+
```

### Wireframe: Gallery, Choices
```text
+----------------------------------------------+
| Choices                                 [new]|
| Switch | Segmented | Yes/No | Level | Badge  |
| Level filter | Language | Bool text / column |
| empty | filled | error | disabled | read-only|
+----------------------------------------------+
```

## Risks
- The true/false key changes control and a wrong value reaches the Api — the field keeps `bool?` and the page sends the same value; the existing tests of the key are rewritten for the new control and the save test is kept — proven — high impact — two-way door.
- The difficulty filter loses its address state — the page keeps `QuestionListState` and `QuestionListQueryString`; only the control that toggles a level changes, with the filter tests kept — proven — low impact — two-way door.
- Tests that find the old markup by id or role (`question-difficulty` as a select, radio cards for the key) break — the ids stay and the tests are rewritten with the control — not proven — low impact — two-way door.

## Docs
- flow: new docs/flows/choice-controls.md — the nine controls, where each is used and the wireframes above.
- flow: update docs/flows/question-form-composition.md#Steps — difficulty and the true/false key use the new controls.

## Acceptance criteria
- AC1 Given `AppSwitch`, when the user clicks or presses Space, then `ValueChanged` fires at once with the new `bool`, and the control is a `role="switch"` with `aria-checked`.
- AC2 Given `AppSegmentedField`, when the user clicks an option or presses an arrow key, then that option is selected and `ValueChanged` fires; the group is a `radiogroup` of `radio` buttons.
- AC3 Given `AppYesNoField` with `TrueText` "True" and `FalseText` "False", when the user picks "False", then the value is `false`; with `NotSetText` the third option sets `null`; an unset value shows no option selected.
- AC4 Given `AppLevelField` with five names, when the user clicks the third dot, then the value is 3, three dots are filled and the name of level 3 is shown; the clear button sets `null` (absent when `Required`); arrows and Home and End move the level; each dot is named "n — name".
- AC5 Given `AppLevelFilterField`, when the user toggles chips, then each chip reports `aria-pressed` and the page receives the set of levels and the "not set" flag.
- AC6 Given `AppLevelBadge`, `AppBoolText` and `AppBoolColumn`, when they show 3, true and no value, then they read the level name with its dots, the words "Yes" or the page's `TrueText`, and "—" or "Not set".
- AC7 Given `AppLanguageField`, when it renders, then its options are `SupportedCultures.All` in native names and it behaves as `AppSelectField<string>`.
- AC8 Given the gallery `/dev/ui`, when it opens, then a section "Choices" shows each control in empty, filled, error, disabled and read-only states.
- AC9 Given `QuestionForm`, when the curator sets the difficulty and the true/false key, then they are `AppLevelField` and `AppYesNoField`, the form saves the same values as before, and a frozen question still shows them read-only.
- AC10 Given `Questions`, when the user filters by difficulty, then the filter is `AppLevelFilterField`, the address and the list answer as before, and the table shows each difficulty with `AppLevelBadge`.
- AC11 Given the five language fields, when searched in the sources, then each uses `AppLanguageField`.
- AC12 All new texts appear in every locale of `UI locales:` (pt-BR, pt-PT, en).

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

## Decisions
- 2026-10-27 — Epic Data type controls — breakdown of the epic — owner
- 2026-10-27 — The level shows dots with the level name — owner
- 2026-10-27 — Takes the difficulty gauge of F-154 — owner, similar-items card of the epic
- 2026-10-29 — `AppSwitch` is built and shown in the gallery only: the code has no checkbox that is a setting applied at once (every `AppCheckbox` is saved with its form or is local), so the summary's "the checkboxes that are settings" has nothing to move — verified in the code — assumed: premise-correction
- 2026-10-29 — The level colour ramp, the gauge and the slider stay in F-191 (it depends on this item); the dots here are one neutral tone — epic cut — confirmed: docs/features/F-191-level-colour-gauge-slider.md
- 2026-10-29 — The filter mode is its own control, `AppLevelFilterField`, instead of a mode of `AppLevelField`: one is a radio group and the other a set of toggle chips, which share no keyboard model — assumed: technical
- 2026-10-29 — `AppLevelField` takes the level names from the page (`Names`): the names depend on what is measured (the difficulty of a question), so the kit holds none; the difficulty names live in the question bank resources — assumed: technical
- 2026-10-29 — The true/false key moves from two radio cards to the segmented yes/no field (`TrueText` "True", `FalseText` "False"): it is a `bool?` of two short words, the catalog's rule for keys like `TrueFalseKey` — confirmed: docs/design/data-type-controls.md 2.4
- 2026-10-29 — Screen design reuses the mockup drawn for F-191 (the dots and the name, the badge, the filter chips) and the text wireframes; no new mockup — assumed: screen-design
- 2026-10-29 — No new package: everything uses MudBlazor and the framework already in the solution — assumed: packages
- 2026-10-29 — This run is on Sonnet 5.5, not on the lane's opus: a session cannot switch its own model — lane jussara — assumed: model-choice

## Out of scope
- The colour ramp, the gauge and the slider (F-191); the enforcement tests (F-186); `AppRadioCards` stays for choices that need a description.

## Open questions
- (none)

## Change notes

## Events

- 2026-10-29 assumed — 9 answers
