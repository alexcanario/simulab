---
bug: B-5
feature: F-5
status: approved
board: 721
severity: low
---
# The sign-in password field asks for a new password

## What happens
1. Open `/sign-in` and inspect the password field.
2. It carries `autocomplete="new-password"` (the `AppPasswordField` default), so password managers offer to generate a password instead of filling the saved one.

## Expected
The sign-in password field says `autocomplete="current-password"`, as F-5's screen section asks (`AppPasswordField.Autocomplete` exists since F-7).

## Cause
Confirmed in code on 2026-09-19 (by reading; not reproduced in a browser yet):
- `src/Hosts/Simulab.Web/Components/Pages/Identity/SignIn.razor:63` — the sign-in password field does not set `Autocomplete`, so it gets the kit's default, `new-password` (`src/Hosts/Simulab.Web/Components/Ui/AppPasswordField.razor:41`), which the field renders as the input's `autocomplete` attribute. A password manager reads it as "choose a new password".
- Root of the risk: the kit field has a silent default. Any new field that asks for the password the user already has gets the wrong value unless its page remembers to override it.

Every other use of `AppPasswordField` (no reimplementation of the rule, only callers of the kit field):
- `ChangePassword.razor:36` (current password) sets `current-password` explicitly — correct.
- `ChangePassword.razor:47` and `:60`, `ResetPassword.razor:35` and `:48`, `SignUp.razor:45` and `:58` (new password and its confirmation) rely on the default `new-password` — correct today.
- `UiGallery.razor:104` (dev-only gallery) relies on the default.

No duplicate found.

## Fix
- BR1 The sign-in password field renders `autocomplete="current-password"`, so a password manager fills the saved password.
- BR2 The kit's `AppPasswordField` has no default any more: every page says which password the field asks for, the one the user has (`current-password`) or a new one (`new-password`). A page that does not say fails the build (the Razor compiler's missing required parameter warning, which the gate refuses).
- BR3 The eight uses state their value: sign-in and change password's current field are `current-password`; sign-up, reset password and change password's new and confirmation fields are `new-password`; the gallery shows `new-password`. Only sign-in changes what the browser sees.

## Acceptance criteria
- AC1 Given `/sign-in`, then the password input has `autocomplete="current-password"`. (BR1)
- AC2 Given `/account/password`, then the current password input has `current-password` and the new and confirmation inputs have `new-password`; given `/sign-up` and `/reset-password`, then both password inputs have `new-password`. (BR3)
- AC3 Given the kit's `AppPasswordField`, then its autocomplete parameter is required and has no default. (BR2)
- AC4 No new user-facing text; the missing-key test stays green.

## Decisions
- 2026-09-19 — Fix sign-in and remove the kit field's silent default — owner; the default is what caused the bug, and a required parameter makes the next password field say what it asks for.
- 2026-09-19 — Out of scope: other fields' autocomplete (email and name already state theirs) and testing specific password managers; only the HTML attribute is checked — owner.
- 2026-09-19 — The parameter becomes an enum (`PasswordAutocomplete.CurrentPassword` / `NewPassword`) marked `[EditorRequired]`, rendered as `current-password` / `new-password` — two valid values only, no typo possible, and a missing value is the compiler's warning `RZ2012`, which the gate fails on.
- 2026-09-19 — No new packages, for code or tests.

## Out of scope
- The autocomplete of other fields (email, name).
- Behaviour of specific password managers.

## Regression test

## Open questions
- (none)

## Change notes

## Validation script

## Delivery
