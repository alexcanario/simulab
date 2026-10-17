---
feature: F-121
epic: Question bank
status: idea
board: 172
version: 1
---
# Email the author of a review decision

Technical terms: [glossary](../glossary.md)

## Summary
When a reviewer approves or rejects a question sent for review, the person who sent it gets an email with the decision and, for a rejection, the reason (`IEmailSender`, templates in the three UI locales). Captured at the F-105 refinement (owner, 2026-10-16): F-105 itself leaves the author to find the decision in the question form's history card and keeps email out of its scope. Source: epic Question bank (E-4).

## Start
- Depends on: F-105.
- Waits on (to start): nothing.
- Needed to validate: the local app host with Mailpit and two users (an author and a reviewer) — owner.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: unknown — settled at /agile:refine.
