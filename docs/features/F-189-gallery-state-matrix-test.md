---
feature: F-189
epic: Data type controls
status: idea
board: 259
version: 1
---
# Gallery state matrix test for the kit controls

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: idea (2026-10-29)
- Stopped at: Captured from the independent review of F-183 (finding 6).
- Next action: Refine it (owner).
- Waiting on: nothing

## Summary
`UiGalleryTextKindsTests` (F-183) only checks that each state caption (empty, filled, error, disabled, read-only, long value) appears somewhere in the section "Text kinds", so a control that lacks a state still passes; the review found the OTP and search controls missing states while the test was green. Add a test that reads each control's own column in the gallery and checks every state that applies to it, so a state dropped from one control fails. The F-183 test file is locked and shipped: this is a new test file, or an unlock recorded in the item. Seen in F-183 review #1 and #6; the missing states themselves were fixed there.

## Start
- Depends on: nothing (F-183 is done).
- Waits on (to start): nothing.
- Needed to validate: nothing.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: F-190.
