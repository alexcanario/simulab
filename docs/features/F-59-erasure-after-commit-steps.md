---
feature: F-59
epic: Foundation and identity
status: refining
board: 98
version: 1
---
<!--
One file per feature. Save as: docs/features/F-<number>-<slug>.md
Status flow: idea -> refining -> approved -> building -> validating -> done
- idea: title, summary and start only (/agile:idea, /agile:epic).
- refining: sections below filled during /agile:refine.
- approved: set only after the product owner says "approve F-<number>" and Open questions is empty or deferred.
- building / validating / done: set by /agile:build and /agile:ship.
Remove these comments when the file leaves `idea`.
-->
# The account erasure's after-commit steps survive a crash

Technical terms: [glossary](../glossary.md)

## Summary
`EraseAccountHandler` (`src/Modules/Identity/Simulab.Identity.Application/Account/EraseAccountHandler.cs`) commits
the erasure inside `RunExclusiveAsync`, then revokes every refresh session in Redis and publishes the `UserErased`
integration event, both after the commit and with no outbox. A crash between the commit and those two steps leaves
an erased account whose sessions are still alive, and other modules that never hear about the erasure. Raised while
refining F-47 (2026-10-01); the owner chose to keep it out of F-47.

## Start
- Depends on: nothing.
- Waits on: nothing.
- Suggested path: `/agile:discuss` first (an outbox, or revoking the sessions before the commit).
- Parallel with: anything outside `Simulab.Identity`.
- Cause not verified: measure it at /agile:refine. Symptom seen in the code: both steps run after
  `RunExclusiveAsync` returns, with nothing that retries them.
