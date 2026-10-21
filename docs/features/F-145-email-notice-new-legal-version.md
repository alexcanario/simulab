---
feature: F-145
epic: Foundation and identity
status: idea
board: 203
version: 1
---
# E-mail notice of a new legal document version

Technical terms: [glossary](../glossary.md)

## Summary
Send an e-mail to every user when the terms or the privacy policy move to a new version, so people hear about it before the acceptance page (F-85) stops them in the app. It needs the e-mail provider (F-66) and a rule of when to notify (every new version, only some, how long before). Raised as out of scope by the F-85 refinement, 2026-10-21; F-85 itself shows the acceptance page and sends no e-mail.

## Start
- Depends on: F-85 (the acceptance page) and F-66 (the e-mail provider).
- Waits on (to start): nothing beyond the dependencies.
- Needed to validate: unknown — settled at /agile:refine.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: unknown — settled at /agile:refine.
