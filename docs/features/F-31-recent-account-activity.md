---
feature: F-31
epic: Foundation and identity
status: idea
board: 750
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
# Recent account activity for the user

## Summary
Show each person their own recent security events on `/account/security` — signed in, sign-in failed, password changed, two-factor turned on or off — read from the trail F-21 records. F-21 keeps the trail for Admins only (owner, question 2, 2026-09-23); this is what lets the owner of an account notice an access that is not theirs.

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
- Branch: feature/F-31
