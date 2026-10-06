---
feature: F-85
epic: Foundation and identity
status: idea
board: 128
version: 1
---
# Existing users accept a new legal document version

Technical terms: [glossary](../glossary.md)

## Summary
Today only sign-up checks the terms and privacy versions (`RegistrationTerms.CheckAsync`); a signed-in user is never told when the privacy policy or the terms move to a new version. Show a notice, or ask for a new acceptance, at sign-in when the current version differs from the one in the user's `ConsentRecord`. Raised as out of scope by the F-71 refinement, 2026-10-04. Must exist before the first environment with real users.

## Start
- Depends on: nothing (F-71 is the first version change, 2026-v2 of the privacy policy).
- Waits on (to start): nothing.
- Needed to validate: unknown — settled at /agile:refine.
- Suggested path: `/agile:refine` → `/agile:build` (with `/agile:screen` if a new acceptance screen is chosen).
- Parallel with: unknown — settled at /agile:refine.
