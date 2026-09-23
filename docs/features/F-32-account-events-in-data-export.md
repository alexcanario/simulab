---
feature: F-32
epic: Foundation and identity
status: idea
board: 751
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
# Account events in the data export

## Summary
Include the account's own security events (F-21) in the data export of F-16, which today returns the profile, the sessions and the consent records with their IP address. The events are personal data of the account holder, so a data subject request should carry them. F-21 left F-16 untouched (owner, question 8, 2026-09-23) because F-16 is already shipped.

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
- Branch: feature/F-32
