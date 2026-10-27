---
status: draft
owner: Alex Canario
date: 2026-10-27
---

# Data type controls — one control per data type

Proposal for the UI kit (`src/Hosts/Simulab.Web/Components/Ui/`): every data type has exactly one input control, one display formatter and one table column. A page picks the control by the type of the data, never by habit. Types the app does not hold yet ("reserve") are decided now and built now too, as feature 7 of the epic (owner, 2026-10-27).

## 1. What the screens use today (2026-10-27)

Sweep of the 76 `.razor` files outside the kit on `main`, plus the open worktrees F-168 (validating) and F-169 (building).

| Data | Screen | Control today | Problem |
|---|---|---|---|
| Points per question, cut-off, essay max score (decimal) | F-168 `ExamEditionForm`, `NoticeSubjectDialog` | `AppTextField TValue="string"` | Decimal typed as free text; each page parses and validates its own way; no culture separator rule |
| Exam duration | F-168 `ExamEditionForm` | Two `AppTextField TValue="int?"` (hours, minutes) | One value split in two fields; no single duration display |
| Question count | F-168 `NoticeSubjectDialog` | `AppTextField TValue="string"` + `InputType.Number` | Integer as string |
| Notice year | `ExamEditionForm`, `CatalogSearch` filter | `AppTextField TValue="int?"` + `InputType.Number` | No year range, spinner arrows, accepts `e` |
| Answer size min/max lines or characters | `RubricKeySection` | Two text fields | A range without a range control |
| Notice URL | `ExamEditionForm` | `AppTextField` + `InputType.Url` | No "open link" action, validation per page |
| E-mail | `SignIn`, `SignUp`, `ForgotPassword`, `ResetPassword`, `CheckEmail`, `VerifyEmail` | `AppTextField` + `Autocomplete="username"` | Six pages repeat trim, lower-case and `inputmode` |
| TOTP code | `SignIn`, `Security` (3×) | `AppTextField` + `Autocomplete="one-time-code"` | No digits-only input, no paste of the whole code |
| Plain long text | dialogs (`IssuingAuthority`, `Organizer`), `AiDiagnostics` | `AppTextField Lines="3"` | Fine, but no auto-grow and no named control |
| Difficulty (1–5) | `QuestionForm` / `Questions` filter | `AppSelectField int?` / `AppMultiPickField` | A level shown as a list; no visual scale |
| True/false key (`bool?`) | `QuestionForm` | `AppRadioCards bool?` | Works; becomes the standard yes/no/unset control |
| Period filter | `AccountEvents`, `RoleHistory` | `AppSelectField` (7/30/90 days) | Fine as presets; no custom range |
| Date | `ExamEditionForm` | `AppDateField` (`DateOnly`) | Already standard |
| Dates and numbers in tables | 11 tables | `AppDateColumn`, `AppNumberColumn` | Already standard; missing money, percent, duration, boolean, score columns |

Not used anywhere yet: money, percentage, time of day, date-time, month, phone, tax id, color, hierarchy picker, slider, language, image with alt text as a standalone field.

## 2. Catalog: data type → control

Each row is one standard. "Display" is the read-only formatter (a static `*Text` helper like the existing `FileSizeText`), "Column" is the `AppDataTable` column.

### 2.1 Text

| Data type | C# type | Input control | Display | Column | Rules |
|---|---|---|---|---|---|
| Short text (name, title) | `string` | `AppTextField` (exists) | plain / `AppTruncatedText` | `PropertyColumn` + truncate | `MaxLength`, `Counter` when the limit is close to real use |
| Long plain text (description, reason) | `string` | **`AppTextAreaField`** (new) | `AppTruncatedText` with line clamp | truncate | Auto-grow from `MinLines` to `MaxLines`, counter always on |
| Rich text (statement, base text) | `string` (HTML) | `AppRichTextField` (exists) | sanitized HTML | not in tables | — |
| E-mail | `string` | **`AppEmailField`** (new) | `AppEmailText` (mailto optional) | text | `type=email`, `inputmode=email`, trim + lower-case on leave, `Autocomplete` required (`username` / `email`) |
| Password | `string` | `AppPasswordField` + `AppPasswordStrength` (exist) | never shown | never | — |
| URL | `string` / `Uri` | **`AppUrlField`** (new) | `AppLink` external, host only | host only | Validates `http(s)`, adds `https://` when missing, "open in new tab" adornment |
| Phone | `string` (E.164) | **`AppPhoneField`** (reserve) | formatted by country | formatted | Country picker + mask; stores E.164 |
| Code / reference / slug | `string` | **`AppCodeField`** (new) | monospace | monospace | No spaces, optional upper-case, `spellcheck=false`; e.g. notice reference, paper number labels |
| One-time code | `string` | **`AppOtpField`** (new) | never | never | 6 digits, `inputmode=numeric`, paste fills all, auto-submit optional |
| Tax id (CPF / NIF) | `string` | **`AppTaxIdField`** (reserve) | masked, partly hidden | masked | Mask and check digit per country; personal data rules apply |
| Search text | `string` | inside `AppDataTable`; **`AppSearchField`** (new) outside tables | — | — | Magnifier, debounce, clear button, same look as the table search |
| Color | `string` (hex) | **`AppColorField`** (reserve) | swatch + hex | swatch | Palette from identity tokens first, contrast check |

### 2.2 Numbers

Number fields use `type="text"` with `inputmode`, never `type="number"`: `type="number"` accepts `e`, shows spinners, changes the value on mouse wheel and breaks the comma of pt-BR in some browsers. Parsing and formatting go through one helper (`AppNumberFormat`, culture of the user).

| Data type | C# type | Input control | Display | Column | Rules |
|---|---|---|---|---|---|
| Integer / count (questions, attempts) | `int?` | **`AppIntegerField`** (new) | `AppNumberText` (`N0`) | `AppNumberColumn` (exists) | `Min`, `Max`, optional stepper buttons, `inputmode=numeric`, `Unit` suffix (`questões`) |
| Decimal | `decimal?` | **`AppDecimalField`** (new) | `AppNumberText` (`N{Decimals}`) | `AppNumberColumn` | `Decimals` is `[EditorRequired]` (the meaning decides it); accepts only the culture separator, the other is a parse error; rounds on leave, never on typing |
| Score / points | `decimal?` | `AppDecimalField` preset **`AppScoreField`** | `AppScoreText` ("2,5 pts") | **`AppScoreColumn`** | `Decimals` from the exam rule `ScoreDecimalPlaces`; `Unit` = points |
| Percentage | `decimal?` (0–100) | **`AppPercentField`** (reserve) | `AppPercentText` ("72,5 %") | **`AppPercentColumn`** | Stored as 0–100; `%` suffix; `Min 0 Max 100` |
| Money | `decimal` + currency code | **`AppMoneyField`** (reserve, plans) | `AppMoneyText` ("R$ 29,90", "29,90 €") | **`AppMoneyColumn`** | Currency is `[EditorRequired]`; 2 decimals from the currency; never `double` |
| Year | `int?` | **`AppYearField`** (new) | plain, no grouping ("2026", never "2.026") | `PropertyColumn` | 4 digits, default range 1950 … current + 2 |
| Numeric range | `(int? Min, int? Max)` | **`AppNumberRangeField`** (new) | "10–30 linhas" | text | Two inputs, one label, checks min ≤ max; used for answer size and score filters |
| Proportional value (weight) | `int` / `decimal` | **`AppSliderField`** (reserve) | number + unit | number | Only when the exact value matters less than the proportion; always with a number input beside it |
| Order / position | `int` | **`AppOrderControls`** (new, from the move-up/down buttons of `NoticeSubjectsSection`) | position number | `AppNumberColumn` | Keyboard move, announced to screen readers |
| File size | `long` | — | `FileSizeText` (exists) | number | — |

### 2.3 Date and time

All stored as `DateOnly`, `TimeOnly`, `DateTimeOffset` (UTC) or `TimeSpan`. Shown in the user's culture and time zone.

| Data type | C# type | Input control | Display | Column | Rules |
|---|---|---|---|---|---|
| Date | `DateOnly?` | `AppDateField` (exists) | `AppDateText` (`d`) | `AppDateColumn` (exists) | Typing and picker; `Min`/`Max` |
| Date range (filter, period) | `(DateOnly? From, DateOnly? To)` | **`AppDateRangeField`** (new) | "01/10 – 27/10/2026" | — | Presets (7, 30, 90 days, this year) + custom; replaces the per-page period selects |
| Time of day | `TimeOnly?` | **`AppTimeField`** (reserve) | `HH:mm` | text | 24 h in pt-BR / pt-PT, culture in en; e.g. exam start time |
| Date and time | `DateTimeOffset?` | **`AppDateTimeField`** (reserve) | `AppDateTimeText` (`g`, user time zone) | **`AppDateTimeColumn`** | Stored UTC; the time zone shown next to the value |
| Month and year | `DateOnly?` (day 1) | **`AppMonthField`** (reserve) | "out/2026" | text | e.g. expected notice month |
| Duration | `TimeSpan?` | **`AppDurationField`** (new) | `AppDurationText` ("4 h 30 min") | **`AppDurationColumn`** | One control, hours + minutes (seconds optional); replaces the F-168 pair |
| Relative moment | `DateTimeOffset` | — | **`AppRelativeTime`** ("há 3 dias", exact date in the tooltip) | in audit tables | — |
| Countdown | `TimeSpan` | — | exam timer (F-169 session kit) | — | Declared exception: exam session |

### 2.4 Choice

The order of the rule: how many options, are they known up front, does one choice need a description.

| Data type | C# type | Input control | Display | Column | When |
|---|---|---|---|---|---|
| Yes/no that applies at once (a setting) | `bool` | **`AppSwitch`** (new) | `AppBoolText` ("Sim/Não") | **`AppBoolColumn`** (icon + text) | Effect immediate, no "Save" |
| Yes/no in a form, agreement | `bool` | `AppCheckbox` (exists) | `AppBoolText` | `AppBoolColumn` | Saved with the form; legal acceptance |
| Yes / no / not set | `bool?` | **`AppYesNoField`** (new, segmented) | `AppBoolText` with "—" | `AppBoolColumn` | Filters ("anulada: todas/sim/não") and keys like `TrueFalseKey` |
| Enum, 2–4 short options | enum | **`AppSegmentedField`** (new) | `AppStatusChip` or text | text | Options fit on one line, no description |
| Enum, options need a description | enum | `AppRadioCards` (exists) | text | text | e.g. edition status, question type |
| Enum or list, 5–100 items, held whole | enum / id | `AppSelectField` (exists) | text | text | Rule F-114 |
| List searched on the server | id | `AppLookupField` (exists) | text / `AppLink` | text | Rule F-114 |
| Many from a list | ids | `AppMultiPickField` (exists) | chips | chips (max 3 + "+n") | — |
| Free tags | strings | `AppChipsField` (exists) | chips | chips | — |
| Free text with suggestions | `string` | `AppSuggestField` (exists) | text | text | — |
| Hierarchy (area → subject → topic) | id path | **`AppTreePickField`** (reserve) | breadcrumb "Direito › Constitucional › Direitos fundamentais" | breadcrumb truncated | Replaces chained lookups when an item picks a topic |
| Level / scale (difficulty 1–5) | `int?` | **`AppLevelField`** (new) | **`AppLevelBadge`** (dots + name) | `AppLevelBadge` | Named steps from resources ("Muito fácil" … "Muito difícil"); filter mode allows many |
| Status | enum | — | `AppStatusChip` (exists) | `AppStatusChip` | Tone from `AppStatusTone` |
| Language of the content | culture code | **`AppLanguageField`** (new, select preset) | language name in the UI language | text | Each exam and question carries its language |

### 2.5 Files and media

| Data type | Input control | Display | Rules |
|---|---|---|---|
| Document (notice PDF) | `AppFileField` (exists) | name + `FileSizeText` + download | Types and size limit are `[EditorRequired]` |
| Image with alt text | **`AppImageField`** (reserve; today only inside the rich text dialog) | thumbnail | Alt text required, preview, size limit |

### 2.6 Relations and people

| Data type | Input control | Display |
|---|---|---|
| User | `AppLookupField` preset **`AppUserLookup`** | name + e-mail |
| Entity reference (organizer, authority, exam) | `AppLookupField` | `AppLink` to the read-only page |

## 3. Shared contract of every input control

1. `Id` and `Label` `[EditorRequired]`; label above; `Required` mark; `Hint`; `Error` beside the field.
2. `Value` / `ValueChanged` with the real type (`decimal?`, `TimeSpan?`, never `string` for a number).
3. States in the gallery: empty, filled, focus, error, disabled, read-only, long label, long value, three locales, light and dark.
4. Parse on leave and on submit; a value that does not parse keeps the raw text and shows the error (never clears what the user typed).
5. Parameters whose right value depends on meaning are `[EditorRequired]`: `Decimals`, `Currency`, `Autocomplete`, file types.
6. Every new resource key in pt-BR, pt-PT and en; bUnit tests per control (parse, format per culture, bounds, a11y attributes).

## 4. Enforcement

- Architecture test: `InputType.Number`, `InputType.Email`, `InputType.Url`, `InputType.Telephone` on `AppTextField` fail the build outside the kit; a numeric `TValue` on `AppTextField` fails too.
- Architecture test: `MudNumericField`, `MudDatePicker`, `MudDateRangePicker`, `MudTimePicker`, `MudSwitch`, `MudSlider`, `MudRating`, `MudToggleGroup`, `MudColorPicker` appear only inside the kit (same pattern as `MudTable`).
- `ui-project.md` gets one line: "Pick the control by data type from `docs/design/data-type-controls.md`."
- Gallery `/dev/ui` gets a section "Data types" in the order of this catalog.

## 5. Proposed cut (one feature per line, each one session or less)

| # | Feature | Contents | Migrates |
|---|---|---|---|
| 1 | Number foundation | `AppNumberFormat`, `AppIntegerField`, `AppDecimalField`, `AppScoreField`, `AppYearField`, `AppNumberText`, `AppScoreText`, `AppScoreColumn` | question count, points, cut-offs, essay max score (after F-168 merges), notice year ×2 |
| 2 | Duration and ranges | `AppDurationField`, `AppDurationText`, `AppDurationColumn`, `AppNumberRangeField` | exam duration (F-168), answer size |
| 3 | Text kinds | `AppTextAreaField`, `AppEmailField`, `AppUrlField`, `AppCodeField`, `AppOtpField`, `AppSearchField` | 6 e-mail fields, 4 TOTP fields, notice URL, 3 multi-line fields |
| 4 | Choice kinds | `AppSwitch`, `AppSegmentedField`, `AppYesNoField`, `AppLevelField`, `AppLevelBadge`, `AppBoolText`, `AppBoolColumn`, `AppLanguageField` | difficulty (form and filter), `TrueFalseKey`, checkboxes that are settings |
| 5 | Dates | `AppDateRangeField`, `AppRelativeTime`, `AppDateText` | period filters in `AccountEvents`, `RoleHistory` |
| 6 | Enforcement | architecture tests of section 4, rule line, gallery index | — |
| 7 | Reserve types | `AppMoneyField`, `AppMoneyText`, `AppMoneyColumn`, `AppPercentField`, `AppPercentText`, `AppPercentColumn`, `AppTimeField`, `AppDateTimeField`, `AppDateTimeText`, `AppDateTimeColumn`, `AppMonthField`, `AppPhoneField`, `AppTaxIdField`, `AppColorField`, `AppTreePickField`, `AppSliderField`, `AppImageField`, `AppUserLookup` | none — built now, shown only in the gallery until a screen uses them (owner, 2026-10-27) |

Order and ids: epic [data-type-controls](../epics/data-type-controls.md) (F-181 to F-188). The enforcement comes before the reserve types, which are split in two features (money, percent, dates and times; phone, tax id, color, tree pick, slider, image, user). Items 1 and 2 wait for F-168 to merge (it owns `ExamEditionForm` and `NoticeSubjectDialog` today).

## 6. Open questions

1. Reserve types: answered — build all now, as feature 7 (owner, 2026-10-27).
2. Decimal input: answered — only the culture separator (`,` in pt-BR / pt-PT, `.` in en); the other one is a parse error (owner, 2026-10-27).
3. Percentage storage: answered — 0–100 (owner, 2026-10-27).
4. Level control look: answered — dots with the level name (owner, 2026-10-27).
