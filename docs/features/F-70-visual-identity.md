---
feature: F-70
epic: Foundation and identity
status: cancelled
board: 112
version: 1
---
Duplicate of F-63 (2026-10-19): F-63 (done) recorded `docs/design/identity.tokens.json` and `DESIGN.md`, absorbed this scope on 2026-10-04, and F-125 (done) updated them.
# Record the visual identity

Technical terms: [glossary](../glossary.md)

## Summary
Simulab has screens but no `docs/design/identity.tokens.json` and `DESIGN.md`: the recorded source of colors, fonts and radius that screens should follow (agile@canary, since 0.0.86). `/agile:identity` writes them from a file, a website, an image or three basic questions, and writes `docs/design/` only. Raised by the sync with agile@canary 0.1.0 on 2026-10-03.

## Start
- Depends on: nothing.
- Waits on (to start): the owner provides the identity source (a file, a website, an image) or answers three basic questions — owner.
- Needed to validate: the owner looks at the tokens and `DESIGN.md` — owner.
- Suggested path: `/agile:identity` directly (it writes `docs/design/` only), or `/agile:refine` first.
- Parallel with: unknown — settled at `/agile:refine`.
