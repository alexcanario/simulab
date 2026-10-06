---
feature: F-92
epic: Foundation and identity
status: idea
board: 137
version: 1
---
# An admin requires a user to change their password

Technical terms: [glossary](../glossary.md)

## Summary
From the back office user list (F-9), an admin marks any user "must change password", so their next sign-in asks for a new password before anything else. Split out of F-53 (owner, 2026-10-06), which only marks the seeded administrator. Needs a permission check on the endpoint and on the screen, an action on the user row, and must revisit Google sign-in for marked accounts: F-53 left the Google path unchanged because its only marked account cannot have Google linked.
Added 2026-10-06 (F-53 review): the refresh grant and the Google grant (`TokenEndpoints.cs`) issue tokens without looking at `MustChangePassword`. Not a bypass while only the seeded administrator is marked (it has no session and no Google link), but once an admin can mark a user who already has a session, both grants need the guard (an answer of `refresh_token_invalid` for the refresh, the password-change challenge for Google) and a test each.

## Start
- Depends on: F-53 (the mark and the sign-in step); F-9 (back office user list, done).
- Waits on (to start): nothing.
- Needed to validate: unknown — settled at /agile:refine.
- Suggested path: `/agile:refine F-92`.
- Parallel with: unknown — settled at /agile:refine.
