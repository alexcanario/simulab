---
bug: B-5
feature: F-5
status: idea
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
<!-- Confirmed in code during refinement. -->

## Fix

## Regression test

## Open questions
- (none)

## Validation script

## Delivery
