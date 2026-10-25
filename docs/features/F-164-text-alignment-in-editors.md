---
feature: F-164
epic: Question bank
status: idea
board: 230
version: 1
---
# Text alignment in the rich text editor

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: idea (2026-10-25)
- Stopped at: Captured as an idea from D-8 (2026-10-25); nothing refined yet.
- Next action: Run /agile:refine F-164.
- Waiting on: nothing

## Summary
Let the author align paragraphs (left, center, right, justified) in every rich text editor of the app, so a simulated exam copies the original paper as faithfully as possible. Today no editor aligns: F-95 left alignment out on purpose, the toolbar `AppRichTextToolbar` has no button, and `QuestionHtmlSanitizer` drops every class and CSS property. Decided in D-8 (2026-10-25): alignment only (no indent, font size or spacing); four alignments with left as the default; every editor, full and compact; stored as the classes `ql-align-center`, `ql-align-right` and `ql-align-justify`, kept by the sanitizer only on a paragraph, a list item and a blockquote; the CSS rules live in `app-question-content`, validated on the back-office previews (the student's exam and review screens do not exist yet and must use that class); pasted text tries to keep its alignment and the build proves what works with Word, PDF and web text. This replaces the "no align" decision of F-95, to be recorded in `## Decisions` of this item at refinement.

## Start
- Depends on: nothing.
- Waits on (to start): nothing.
- Needed to validate: the owner, on screen, with a real exam text pasted from Word or PDF.
- Model: unknown — settled at /agile:refine.
- Suggested path: `/agile:refine` → `/agile:build`. Related, not a dependency: F-113 (table button in the same editor, `idea`). Whether the alignment survives a paste from Word is unknown — settled at the build.
- Parallel with: unknown — settled at /agile:refine.
