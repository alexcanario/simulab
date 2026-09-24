---
feature: F-41
epic: Foundation and identity
status: idea
board: 762
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
# AI gateway

## Summary
Every AI call in Simulab goes through `IAiGateway`, which checks `IEntitlementService` and records usage and cost (`CLAUDE.md`, Stack). Neither interface exists in `src/` today, so no feature can call a model yet. This feature introduces the gateway with its first real model call against the Claude API, the entitlement check and the usage and cost record. It unblocks F-40 (eval suite for model calls), which has nothing to evaluate until a real call exists.

## Start
- Depends on: nothing (no item in `docs/features/` provides `IAiGateway`; confirmed by `grep -rn "IAiGateway" src/ tests/` returning no match)
- Waits on: owner decision on which product use case makes the first model call, and whether `IEntitlementService` (also absent from `src/`) is built here or in its own item
- Suggested path: `/agile:discuss` first — the direction is open (which use case, which model, how cost is recorded and where)
- Parallel with: unknown — settled at /agile:refine
