---
bug: B-7
feature: F-4
status: idea
board: 724
severity: low
---
# Sign-up accepts a name longer than the column

## What happens
1. Call `POST /api/v1/identity/registrations` directly with a `fullName` of 121 characters or more.
2. `RegisterUserHandler` stores the trimmed name without a length check; the `users.full_name` column is 120 characters, so the insert fails and the answer is a 500 instead of a validation error.

The sign-up page limits the field to 120 characters (`MaxLength="120"`), so only a direct API call reaches it. Found while building F-8, which validates the same limit on the profile (`ProfileLimits.FullNameMaxLength`, `profile.full_name_too_long`).

## Expected
The Api refuses a name over 120 characters with a stable validation code, as the profile does, and nothing is created.

## Cause
<!-- Confirmed in code during refinement. -->

## Fix

## Regression test

## Open questions
- (none)
