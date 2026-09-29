---
feature: F-52
epic: Foundation and identity
status: refining
board: 778
version: 1
---
# Seed the admin user

## Summary
Seed one administrator account, `admin@simulab.local`, with unrestricted access, in every environment (development, test and production). The development password was given by the owner in chat and is deliberately not written in this file: a credential does not go into a committed document (`git.md`); it goes to the seed or example-config file when the item is built. The production password is not defined yet.

## Start
- Depends on: F-6 (roles, permissions and the Admin seed role are done); F-5 (sign-in).
- Waits on: the owner, for the production password and how it is delivered (user secret or environment variable, never the repository) — unknown until `/agile:refine`.
- Suggested path: `/agile:refine`. Points to settle there: what "unrestricted" means against the permission-by-permission model of F-6 (the Admin role plus every permission that exists later, or a separate rule); whether a fixed, known password is acceptable in production at all (recommended: no — production gets a generated or secret-provided password and a forced change at first sign-in); email verification and TOTP state of the seeded account.
- Parallel with: none known.
