---
feature: F-54
epic: Foundation and identity
status: refining
board: 95
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
# Shared client rate limits across Api instances

Technical terms: [glossary](../glossary.md)

## Summary
Move `ClientRateLimiter` (registration, resend, password reset and the F-38 sign-in failure limit) from the in-memory, per-process counter to a shared counter in Redis, so the limits still hold when the Api runs more than one instance. Found while refining F-38 (2026-10-01).

## Start
- Depends on: F-38 (adds the sign-in failure limit to the same limiter).
- Waits on: unknown — settled at /agile:refine (whether a second Api instance is planned).
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: unknown — settled at /agile:refine.

## Goal
<!-- Why this feature exists, in one or two sentences. -->

## Users and use cases
- UC1 <Actor> <does something> <and gets a result>.

## Business rules
- BR1 <Rule>.

## Screens and API
- <Route> — <purpose>

## Acceptance criteria
- AC1 Given <context>, when <action>, then <result>.

## Decisions
- <YYYY-MM-DD> — <decision> — <reason>

## Out of scope
- <Item>

## Open questions
- (none)

## Change notes

## Validation script
1. <Step> → <expected result>

## Delivery
- Branch: feature/F-54
