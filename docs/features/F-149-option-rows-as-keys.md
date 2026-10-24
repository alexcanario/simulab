---
feature: F-149
epic: Interface and screen composition
status: idea
board: 207
version: 1
---
# Option rows as keys in the question form

Technical terms: [glossary](../glossary.md)

## Summary
Split from F-126 (2026-10-23). The option rows of the question form (single choice, multiple answer, true/false) and the key sections of matching and fill-in-the-blanks show a keycap letter per option and the correct one as a green key with the word "Correct"; the student preview (`QuestionPreviewContent.razor`, `QuestionPreviewDialog.razor`) follows. F-126 delivers the frame of the form (numbered section cards, the aside with the publication actions, the amber action, the dialogs); this part changes only the option and key rows. The composition comes from the Stitch reference screen approved on 2026-10-17 (D-4); the colours and controls come from F-125 and F-148.

## Start
- Depends on: F-126
- Waits on (to start): nothing
- Needed to validate: the owner, on screen
- Model: unknown — settled at /agile:refine
- Suggested path: `/agile:refine` → `/agile:screen` → `/agile:build`
- Parallel with: F-127, F-128, F-129 (different pages)
