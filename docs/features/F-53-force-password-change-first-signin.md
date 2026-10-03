---
feature: F-53
epic: Foundation and identity
status: idea
board: 779
version: 1
---
# Force a password change at first sign-in

Technical terms: [glossary](../glossary.md)

## Summary
Let an account be marked "must change password", and block sign-in from doing anything else until the owner of the account picks a new password. Split out of F-52 (seed admin user), which does not need it because its production password comes from a secret. It needs a flag on the user, a block in the sign-in flow until the password is changed, and a migration. No such flag exists today (checked in the code on 2026-09-29).

## Start
- Depends on: F-5 (sign-in); F-7 (password reset, the natural way to set the new password).
- Waits on: nothing.
- Suggested path: `/agile:refine`.
- Parallel with: none known.
