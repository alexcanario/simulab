---
feature: F-113
epic: Question bank
status: idea
board: 161
version: 1
---
# Table button in the rich text editor

Technical terms: [glossary](../glossary.md)

## Summary
A curator inserts and edits a table inside a question text (statement, base text, option, explanation) with a button of the visual editor. F-95 ships `AppRichTextField` without a table button: a table pasted or imported as HTML is kept by the sanitizer and shown in the preview, but cannot be created or edited in the editor, because Quill 2's table module was not verified at its refinement (decision of 2026-10-10, screen design).

## Start
- Depends on: F-95 (refining; creates `AppRichTextField` and the sanitizer allow-list, which already keeps `table thead tbody tr th td`).
- Waits on (to start): unknown — settled at `/agile:refine` (whether Quill 2's table module, or another editor extension, edits tables accessibly with the keyboard).
- Needed to validate: the local app host — owner.
- Suggested path: `/agile:refine` → `/agile:build`; `/agile:screen` only if the toolbar needs a table dialog.
- Parallel with: unknown — settled at `/agile:refine`.
