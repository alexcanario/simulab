---
feature: F-190
epic: Data type controls
status: idea
board: 260
version: 1
---
# One http(s) address check for the URL fields

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: idea (2026-10-29)
- Stopped at: Captured from the independent review of F-183 (finding 7).
- Next action: Refine it (owner).
- Waiting on: nothing

## Summary
The rule "only `http` and `https`, with a host" now lives in four places: `AppUrlField.IsValid` (the kit) and the private checks of `ExamEditionForm.razor` (around line 708), `IssuingAuthorityDialog.razor` (around line 108) and `OrganizerDialog.razor` (around line 147). Have the three pages call `AppUrlField.IsValid` and keep their own module error codes, so the rule has one copy. The review estimated about 9 lines less. Seen in F-183 review #7; the pages and the control behave the same today, so this is a cleanup with no visible change.

## Start
- Depends on: nothing (F-183 is done).
- Waits on (to start): nothing.
- Needed to validate: nothing.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: F-189.
