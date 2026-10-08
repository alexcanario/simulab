---
feature: F-88
epic: Cloud hosting and operations
status: idea
board: 131
version: 1
---
# Handle bounced and spam-flagged emails

Technical terms: [glossary](../glossary.md)

## Summary
Once F-66 sends real emails from the cloud, some will bounce (address does not exist) or be flagged as spam. Today nothing reads those signals. This item reads the delivery reports of Azure Communication Services and decides what the app does with an address that bounces or complains. Raised as out of scope by the F-66 refinement, 2026-10-04.

## Start
- Depends on: F-66.
- Waits on (to start): unknown — settled at /agile:refine.
- Needed to validate: staging with the email service — owner.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: unknown — settled at /agile:refine.
