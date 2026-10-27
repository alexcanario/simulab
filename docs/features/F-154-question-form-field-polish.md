---
feature: F-154
epic: Question bank
status: idea
board: 213
version: 1
---
# Question form field polish

Technical terms: [glossary](../glossary.md)

## Summary
Three field details of the Stitch reference screen on the question form. The subject lookup opens its list on click, as the topic lookup already does (`MinChars="0"`, `OpenOnFocus`; the subject field keeps the default of 2 characters today). The difficulty options show a label per level ("Level 3 (Medium)") and a 1-to-5 dot gauge instead of the bare number (`DifficultyOptions` in `QuestionForm.razor`). The rich text fields (statement, base text, explanation) show a character counter against their limits in `QuestionLimits` (10,000 / 20,000 / 10,000). Source: discussion D-6 (2026-10-24).

Added 2026-10-27: the difficulty label and the 1-to-5 dot gauge are delivered by F-184 (`AppLevelField`, epic Data type controls); this item keeps the subject lookup that opens on click and the rich text counters.

## Start
- Depends on: nothing.
- Waits on (to start): nothing.
- Needed to validate: the owner, on screen.
- Suggested path: `/agile:refine` → `/agile:build`. Whether the counter counts the text without tags or the stored HTML, and whether it belongs to the kit's rich text field for every form, is unknown — settled at /agile:refine.
- Parallel with: unknown — settled at /agile:refine.
