---
feature: F-155
epic: Question bank
status: idea
board: 214
version: 1
---
# Autosave of the question draft

Technical terms: [glossary](../glossary.md)

## Summary
The question form keeps the curator's unsaved edits without a click on Save and shows when they were last kept ("Autosaved 2 min ago"), as in the Stitch reference screen. Source: discussion D-6 (2026-10-24); it reverts the D-4 line that left autosave out, at the owner's request. Whether autosave writes a draft version on the server (and how it meets `QuestionVersion` and the review of a published question, F-123) or only keeps the edits in the browser is unknown — settled at /agile:refine.

## Start
- Depends on: nothing.
- Waits on (to start): nothing.
- Needed to validate: the owner, on screen.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: unknown — settled at /agile:refine.
