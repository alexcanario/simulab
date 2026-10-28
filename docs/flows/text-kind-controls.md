---
title: Text kind controls
summary: The six kit controls for kinds of text (text area, e-mail, URL, code, one-time code, search), what each does to the value and which screens use them
items: [F-183]
---
# Text kind controls

Technical terms: [glossary](../glossary.md)

## Trigger
A user types or pastes into a field whose data is a kind of text: an e-mail, a code, a web address, a long description, a search. The page picks the control by the kind of data (catalog: `docs/design/data-type-controls.md`, 2.1); every control is built on `AppTextField` and keeps its label, hint, error and description.

## Steps
1. `AppEmailField`: on leave, the value is trimmed and lower-cased; the input is `type=email`, `inputmode=email`, `spellcheck=false`; `Autocomplete` is required (`username` where the address signs in).
2. `AppOtpField`: on leave, only the first six digits stay, so `123 456` or `123-456` pasted becomes `123456`; `inputmode=numeric`, `pattern=[0-9]*`, `autocomplete=one-time-code`. The page sends the code with its button only.
3. `AppCodeField`: on leave, white space is removed (and letters raised with `UpperCase`); monospace, `spellcheck=false`, `autocapitalize=off`. Used for a recovery code and for a field that takes "a code or a recovery code".
4. `AppTextAreaField`: starts at `MinLines` (3), grows to `MaxLines` (10) and then scrolls; the counter `n/max` is always on, so the value follows the typing (`oninput`) after a 250 ms pause.
5. `AppUrlField`: on leave, the value is trimmed and `https://` is added when it names no scheme; only `http` and `https` are valid (`Common.Url.Invalid` otherwise, the typed text kept); a valid value shows an "open in a new tab" link (`target=_blank`, `rel="noopener noreferrer"`) over the end of the input. The page's own `Error` wins over the field's.
6. `AppSearchField`: magnifier, 300 ms pause, clear button. `AppDataTable` renders its search box with it.

## Rules
- The server rules do not change: the Api still checks every e-mail, code, URL and length. Identity normalizes the e-mail on lookup (`UserDirectory`) and recovery codes ignore spaces and hyphens and case (`RecoveryCodes`).
- A field that takes a recovery code is never an `AppOtpField`: a recovery code is not six digits.
- Where each control is used: `## Approved list` of `docs/features/F-183-text-kind-controls.md`; `TextKindPageFieldsTests` checks it.

## Wireframe
```text
+----------------------------------------------+
| Sign in                                      |
| Authentication code *                [changed]|
| [1][2][3][4][5][6]   one box, 6 digits       |
| Type the 6-digit code from your app          |
| ( Use a recovery code )                      |
|   -> Recovery code *                [changed]|
|      [ XXXX-XXXX ________ ]  monospace       |
| [ Sign in ]                     [ Cancel ]   |
+----------------------------------------------+
```

```text
+----------------------------------------------+
| Security                                     |
| Turn on two-factor sign-in                   |
| 1. Scan the QR code  2. Confirm              |
| Code *                               [changed]|
| [1][2][3][4][5][6]   AppOtpField             |
| [ Turn on ]                                  |
|----------------------------------------------|
| Code or recovery code *              [changed]|
| [ ________________ ]  AppCodeField           |
| [ Regenerate codes ] / [ Turn off ]          |
+----------------------------------------------+
```

```text
+----------------------------------------------+
| Notice                                       |
| Notice reference        Notice URL [changed] |
| [ Edital 01/2026 ]      [ https://... ][open]|
+----------------------------------------------+
```

```text
+----------------------------------------------+
| Text kinds                              [new]|
| Text area: empty | filled | error | disabled |
| E-mail:    empty | filled | error | disabled |
| URL:       empty | valid+open | error        |
| Code:      empty | filled | upper-case       |
| One-time:  empty | filled | error | disabled |
| Search:    empty | typed + clear             |
+----------------------------------------------+
```

## Errors
- `Common.Url.Invalid` — the URL field holds a value that is not an absolute `http` or `https` address, and the page set no error of its own.
