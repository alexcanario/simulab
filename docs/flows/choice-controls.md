---
title: Choice controls
summary: The kit controls for choices (switch, segmented, yes/no, level, level filter, language), their texts and the bool column, what each does to the value and which screens use them
items: [F-184]
---
# Choice controls

Technical terms: [glossary](../glossary.md)

## Trigger
A user picks one or several options: a difficulty, a true/false key, a content language, a period. The page picks the control by how many options there are and whether the effect is immediate (catalog: `docs/design/data-type-controls.md`, 2.4).

## Steps
1. `AppSwitch` (`bool`): a setting that takes effect at once, `role="switch"`; no screen uses it yet, because no setting in the app applies without a Save.
2. `AppSegmentedField<TValue>`: two to four short options in one row (`radiogroup` of `radio` buttons, one tab stop, arrows move the choice). Each option has the id `<field id>-<value>` (`none` for no value).
3. `AppYesNoField` (`bool?`) is the segmented field with "Yes" and "No", or the page's words (`TrueText`, `FalseText`); `NotSetText` adds a first option that means no value (a filter's "All"). `AppBoolText` and `AppBoolColumn` show the same words, an icon in the column, a dash for none.
4. `AppLevelField` (`int?`): one dot per level and the level name beside them (`Names` is required: the page gives the names). The chosen dot and those below it are filled, each dot is a `radio` named "n — name", arrows, Home and End move the level, a clear button sets no level (absent when `Required`).
5. `AppLevelFilterField`: the filter mode, one toggle chip (`aria-pressed`) per level with its mini dots and name, plus an optional "not set" chip; the page keeps the set and answers `OnToggle`.
6. `AppLevelBadge`: the mini dots and the name as text, for a table or a card.
7. `AppLanguageField` (`string?`, a culture code): an `AppSelectField` of `SupportedCultures.All` in their native names; `AllText` adds the empty "All" option of a filter.
8. Used by `QuestionForm` (difficulty, true/false key, content language), `Questions` (difficulty filter chips and the badge in the table), `BaseTextForm`, `BaseTexts` (the filter), `ExamForm` and `Account` (the language). The colour ramp, the gauge and the slider of the level are F-191.

## Rules
- No colour carries meaning: the dots are filled or empty, the name is on screen, and the selected state of a segment or a chip is a heavier border and a bold word.
- The names of the difficulty levels live in the question bank resources (`Questions.Difficulty.Level.1` to `.5`); the kit holds none.
- The Api stays the authority for the difficulty range, the key and the language.

## Wireframe
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

```text
+----------------------------------------------+
| Difficulty                          [changed]|
| [oo Very easy] [ooo Easy] [oooo Hard] [Not set]|
|                                              |
| Statement            Difficulty     [changed]|
| What is...?          (o)(o)(o)( )( ) Medium  |
+----------------------------------------------+
```

```text
+----------------------------------------------+
| Choices                                 [new]|
| Switch | Segmented | Yes/No | Level | Badge  |
| Level filter | Language | Bool text / column |
| empty | filled | error | disabled | read-only|
+----------------------------------------------+
```

Mockup: [F-191-level-visuals.html](../features/mockups/F-191-level-visuals.html) (the level field, badge and filter, drawn for F-191 with colour).

## Errors
- None of its own: the controls show the error the page gives (`Error`), such as `question.difficulty_invalid` or the key's code.
