---
feature: F-84
epic: Foundation and identity
status: idea
board: 127
version: 1
---
# Rotate the OpenIddict certificates without signing users out

Technical terms: [glossary](../glossary.md)

## Summary
F-73 moves the Api's OpenIddict signing and encryption certificates to Key Vault with 24 months of validity and a manual renewal that signs every user out once (F-73 BR7). Here the Api loads the current and the previous certificate of each role, signs and encrypts with the current one and still reads tokens made with the previous one, so a renewal keeps every session. Raised in the F-73 refinement on 2026-10-04; the owner kept it out of F-73.

## Start
- Depends on: F-73 (certificates in Key Vault).
- Waits on (to start): nothing beyond F-73.
- Needed to validate: unknown — settled at /agile:refine.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: unknown — settled at /agile:refine.
