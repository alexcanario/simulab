---
feature: F-38
epic: Foundation and identity
status: idea
board: 757
version: 1
---
<!--
One file per feature. Save as: docs/features/F-<number>-<slug>.md
Status flow: idea -> refining -> approved -> building -> validating -> done
- idea: title and summary only (/agile:idea).
- refining: sections below filled during /agile:refine.
- approved: set only after the product owner says "approve F-<number>" and Open questions is empty or deferred.
- building / validating / done: set by /agile:build and /agile:ship.
Remove these comments when the file leaves `idea`.
-->
# Sign-in rate limit per client

## Summary
Limit sign-in attempts per client on `/connect/token`, as `ClientRateLimiter` already does for registration, resend and password reset (`IdentityRateLimits`). Today the only brake is the per-account lockout (5 attempts / 15 minutes), so someone spraying many accounts, or insisting on addresses that belong to no account, is not slowed down at all — and since F-21 every attempt also writes a row in the account event trail. Found while refining F-21 (2026-09-23).

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
- Branch: feature/F-38
