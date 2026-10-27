---
feature: F-178
epic: Exam Simulator
status: idea
board: 246
version: 1
---
# Written and structured answers in the simulator

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: idea (2026-10-27)
- Stopped at: Captured as part 2 of the F-169 split; not refined.
- Next action: Refine it with `/agile:refine F-178` after F-169 is built (owner).
- Waiting on: F-169

## Summary
Split from F-169 (2026-10-27). On the simulator screen of F-169, the student answers the other five question types: `Matching` (a right option for each left item), `FillInTheBlanks` (one box per blank of the statement), `ShortAnswer` (a text), `OpenAnswer` (a text with its character limit shown) and `Essay` (a text box with a live line counter, the minimum and maximum lines of the question shown, the maximum enforced as F-166 BR5 does). F-169 covers the option types (`SingleChoice`, `TrueFalse`, `MultipleAnswer`), the content read, the answer sheet, the sections and the submit.

## Start
- Depends on: F-169.
- Waits on (to start): nothing.
- Needed to validate: the guard sample edition of F-110, which has every question type (exists); the Manaus 2026 essay arrives with F-163.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: none.
