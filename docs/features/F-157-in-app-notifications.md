---
feature: F-157
epic: Foundation and identity
status: idea
board: 216
version: 1
---
# In-app notifications

Technical terms: [glossary](../glossary.md)

## Summary
A bell in the app bar shows a dot when there is something new for the signed-in user and lists the notices (for example a review decided, a question sent for review), as in the Stitch reference screen. F-121 stays the email to the author of a review decision; this item is the notice inside the app. Source: discussion D-6 (2026-10-24); it reverts the D-4 line that left notifications out, at the owner's request. Which events notify whom, read state and how long notices are kept are unknown — settled at /agile:refine.

## Start
- Depends on: nothing.
- Waits on (to start): nothing.
- Needed to validate: the owner, on screen.
- Suggested path: `/agile:refine` (with `/agile:screen`) → `/agile:build`.
- Parallel with: F-121.
